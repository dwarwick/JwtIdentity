using System.Collections.Generic;

namespace JwtIdentity.Client.Pages.Docs.Guides
{
    public class ExportResultsModel : DocsPageModel
    {
        protected IReadOnlyList<FormatRow> Formats { get; } = new List<FormatRow>
        {
            new("CSV", "Use for lightweight exports compatible with spreadsheets and ETL pipelines."),
            new("Excel", "Includes styling, multiple worksheets, and pivot-ready tables."),
            new("JSON", "Ideal for programmatic analysis or importing into downstream systems."),
        };

        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("prepare-data", "Prepare your data"),
                Toc("start-export", "Start the export"),
                Toc("download-formats", "Download formats"),
                Toc("automation-tips", "Automation tips")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Guides", string.Empty),
                Crumb("Export results", "/docs/how-to/export-results", true)
            };

            return PageConfig(
                "guides",
                toc,
                breadcrumbs,
                Link("/docs/api/surveys", "API: Surveys"),
                Link("/docs/faq", "Documentation FAQ"));
        }

        protected record FormatRow(string Name, string Description);
    }
}
