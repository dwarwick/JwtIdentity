using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Data.Sqlite;

namespace JwtIdentity.Search
{
    public sealed class DocsSearchIndexer
    {
        private readonly string _dbPath;
        private readonly string _docsDir;

        public DocsSearchIndexer(IWebHostEnvironment environment)
        {
            var appData = Path.Combine(environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(appData);
            _dbPath = Path.Combine(appData, "docs.db");

            // Point to the Client project's Docs folder instead of DocsContent
            var contentRoot = environment.ContentRootPath;
            _docsDir = Path.Combine(contentRoot, "..", "JwtIdentity.Client", "Pages", "Docs");
            
            if (!Directory.Exists(_docsDir))
            {
                // Fallback to old location if Client folder doesn't exist
                _docsDir = Path.Combine(contentRoot, "DocsContent");
                Directory.CreateDirectory(_docsDir);
            }
        }

        private SqliteConnection OpenConnection()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            return connection;
        }

        public void EnsureSchema()
        {
            using var connection = OpenConnection();
            const string create = @"
CREATE VIRTUAL TABLE IF NOT EXISTS docs USING fts5
(
    id UNINDEXED,
    title,
    section,
    headings,
    content,
    url UNINDEXED,
    tokenize = 'porter'
);
";
            using var command = connection.CreateCommand();
            command.CommandText = create;
            command.ExecuteNonQuery();
        }

        public async Task RebuildAsync()
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            using (var clear = connection.CreateCommand())
            {
                clear.Transaction = transaction;
                clear.CommandText = "DELETE FROM docs;";
                clear.ExecuteNonQuery();
            }

            // Check if we're using .razor files or .md files
            var razorFiles = Directory.GetFiles(_docsDir, "*.razor", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith("_")) // Exclude layout files like _DocsLayout.razor
                .ToList();

            if (razorFiles.Any())
            {
                // Process .razor files from the Client project
                foreach (var file in razorFiles)
                {
                    await ProcessRazorFileAsync(connection, transaction, file);
                }
            }
            else
            {
                // Fallback to .md files for backward compatibility
                foreach (var file in Directory.EnumerateFiles(_docsDir, "*.md", SearchOption.AllDirectories))
                {
                    await ProcessMarkdownFileAsync(connection, transaction, file);
                }
            }

            transaction.Commit();
        }

        private async Task ProcessRazorFileAsync(SqliteConnection connection, SqliteTransaction transaction, string file)
        {
            var razorContent = await File.ReadAllTextAsync(file);
            
            // Extract @page directive to get the URL
            var pageMatch = Regex.Match(razorContent, @"@page\s+""([^""]+)""");
            if (!pageMatch.Success)
            {
                return; // Skip files without @page directive (like components)
            }
            
            var url = pageMatch.Groups[1].Value;
            var slug = url.Replace("/docs/", "").ToLowerInvariant();
            var section = slug.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

            // Extract title from <h1> tag
            var h1Match = Regex.Match(razorContent, @"<h1[^>]*>(.*?)</h1>", RegexOptions.Singleline);
            var title = h1Match.Success ? StripHtmlAndRazorSyntax(h1Match.Groups[1].Value).Trim() : slug;

            // Extract headings from <h2>, <h3>, or MudText with Typo.h2/h3
            var headingMatches = new List<string>();
            
            // Match <h2> and <h3> tags
            var htmlHeadings = Regex.Matches(razorContent, @"<h[23][^>]*>(.*?)</h[23]>", RegexOptions.Singleline);
            foreach (Match match in htmlHeadings)
            {
                headingMatches.Add(StripHtmlAndRazorSyntax(match.Groups[1].Value).Trim());
            }
            
            // Match MudText with Typo="Typo.h2" or Typo="Typo.h3"
            var mudHeadings = Regex.Matches(razorContent, @"<MudText[^>]*Typo=""Typo\.h[23]""[^>]*>(.*?)</MudText>", RegexOptions.Singleline);
            foreach (Match match in mudHeadings)
            {
                headingMatches.Add(StripHtmlAndRazorSyntax(match.Groups[1].Value).Trim());
            }
            
            var headings = string.Join(" | ", headingMatches.Where(h => !string.IsNullOrWhiteSpace(h)));

            // Extract all text content by removing Razor directives, HTML tags, and components
            var content = ExtractTextContent(razorContent);

            var id = slug.Replace('/', '-');

            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = @"INSERT INTO docs (id, title, section, headings, content, url)
                                   VALUES ($id, $title, $section, $headings, $content, $url)";
            insert.Parameters.AddWithValue("$id", id);
            insert.Parameters.AddWithValue("$title", string.IsNullOrWhiteSpace(title) ? slug : title);
            insert.Parameters.AddWithValue("$section", ToTitle(section));
            insert.Parameters.AddWithValue("$headings", headings);
            insert.Parameters.AddWithValue("$content", content);
            insert.Parameters.AddWithValue("$url", url);
            await insert.ExecuteNonQueryAsync();
        }

        private async Task ProcessMarkdownFileAsync(SqliteConnection connection, SqliteTransaction transaction, string file)
        {
            var relative = Path.GetRelativePath(_docsDir, file).Replace('\\', '/');
            var slug = Path.ChangeExtension(relative, null)?.ToLowerInvariant() ?? string.Empty;
            var url = "/docs/" + slug;
            var section = slug.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

            var markdown = await File.ReadAllTextAsync(file);
            var titleMatch = Regex.Match(markdown, @"(?m)^\s*#\s+(.+)$");
            var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : slug;

            var headingMatches = Regex.Matches(markdown, @"(?m)^\s*#{2,3}\s+(.+)$");
            var headings = string.Join(" | ", headingMatches.Select(m => m.Groups[1].Value.Trim()));

            var html = Markdown.ToHtml(markdown);
            var content = Regex.Replace(html, "<.*?>", " ");
            content = Regex.Replace(content, @"\s+", " ").Trim();

            var id = slug.Replace('/', '-');

            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = @"INSERT INTO docs (id, title, section, headings, content, url)
                                   VALUES ($id, $title, $section, $headings, $content, $url)";
            insert.Parameters.AddWithValue("$id", id);
            insert.Parameters.AddWithValue("$title", string.IsNullOrWhiteSpace(title) ? slug : title);
            insert.Parameters.AddWithValue("$section", ToTitle(section));
            insert.Parameters.AddWithValue("$headings", headings);
            insert.Parameters.AddWithValue("$content", content);
            insert.Parameters.AddWithValue("$url", url);
            await insert.ExecuteNonQueryAsync();
        }

        private string ExtractTextContent(string razorContent)
        {
            // Remove Razor directives (@page, @layout, @inherits, @using, @rendermode, etc.)
            var content = Regex.Replace(razorContent, @"@\w+[^\r\n]*", " ");
            
            // Remove <PageTitle>, <HeadContent>, and other metadata
            content = Regex.Replace(content, @"<PageTitle>.*?</PageTitle>", " ", RegexOptions.Singleline);
            content = Regex.Replace(content, @"<HeadContent>.*?</HeadContent>", " ", RegexOptions.Singleline);
            
            // Remove comments
            content = Regex.Replace(content, @"@\*.*?\*@", " ", RegexOptions.Singleline);
            content = Regex.Replace(content, @"<!--.*?-->", " ", RegexOptions.Singleline);
            
            // Remove all HTML/component tags but keep their content
            content = Regex.Replace(content, @"<[^>]+>", " ");
            
            // Clean up whitespace
            content = Regex.Replace(content, @"\s+", " ").Trim();
            
            return content;
        }

        private string StripHtmlAndRazorSyntax(string text)
        {
            // Remove Razor expressions like @Icons.Material.Filled.Something
            var cleaned = Regex.Replace(text, @"@[\w.]+", " ");
            
            // Remove HTML tags
            cleaned = Regex.Replace(cleaned, @"<[^>]+>", " ");
            
            // Clean up whitespace
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            
            return cleaned;
        }

        public static string ToTitle(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var replaced = Regex.Replace(value, "[-_]", " ");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(replaced);
        }
    }
}
