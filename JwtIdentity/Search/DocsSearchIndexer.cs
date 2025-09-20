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

            _docsDir = Path.Combine(environment.ContentRootPath, "DocsContent");
            Directory.CreateDirectory(_docsDir);
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

            foreach (var file in Directory.EnumerateFiles(_docsDir, "*.md", SearchOption.AllDirectories))
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

            transaction.Commit();
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
