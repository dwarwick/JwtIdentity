using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;

namespace JwtIdentity.Search
{
    public static class SearchApi
    {
        public static IEndpointRouteBuilder MapSearchApi(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/search", HandleSearch)
               .RequireRateLimiting("search")
               .WithName("DocsSearch");

            return app;
        }

        private static IResult HandleSearch([FromQuery] string q, [FromQuery] int take, [FromServices] IWebHostEnvironment env)
        {
            var query = (q ?? string.Empty).Trim();
            if (query.Length < 2)
            {
                return Results.Json(Array.Empty<object>());
            }

            if (query.Length > 120)
            {
                query = query[..120];
            }

            var dbPath = Path.Combine(env.ContentRootPath, "App_Data", "docs.db");
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT url, title, section, highlight(docs, 4, '<mark>', '</mark>') AS snippet, bm25(docs) AS score
FROM docs
WHERE docs MATCH $query
ORDER BY score
LIMIT $take;
";

            var normalized = query.Contains('"') ? query : string.Join(" AND ", Tokenize(query).Select(t => t + "*"));

            command.Parameters.AddWithValue("$query", normalized);
            command.Parameters.AddWithValue("$take", Math.Clamp(take <= 0 ? 20 : take, 1, 50));

            var results = new List<object>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new
                {
                    url = reader.GetString(0),
                    title = reader.GetString(1),
                    section = reader.GetString(2),
                    snippet = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    score = reader.GetDouble(4)
                });
            }

            return Results.Json(results);
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            foreach (Match match in Regex.Matches(value.ToLowerInvariant(), "[a-z0-9]{2,}"))
            {
                yield return match.Value;
            }
        }
    }
}
