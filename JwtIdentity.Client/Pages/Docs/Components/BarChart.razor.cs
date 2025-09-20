using System.Text;

namespace JwtIdentity.Client.Pages.Docs.Components
{
    public class BarChartModel : DocsPageModel
    {
        protected string BarChartSample { get; } = new StringBuilder()
            .AppendLine("<MudPaper Class=\"pa-4\">")
            .AppendLine("    <MudChart ChartType=\"MudBlazor.ChartType.Bar\" Options=\"@ChartOptions\" Data=\"@ChartData\" />")
            .AppendLine("</MudPaper>")
            .ToString();

        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("component-overview", "Component overview"),
                Toc("configuration", "Configuration options"),
                Toc("accessibility", "Accessibility guidance"),
                Toc("troubleshooting", "Troubleshooting tips")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Components", string.Empty),
                Crumb("Bar Chart", "/docs/components/bar-chart", true)
            };

            return PageConfig(
                "components",
                toc,
                breadcrumbs,
                Link("/docs/concepts/authorization", "Concepts: Authorization"),
                Link("/docs/api/surveys", "API: Surveys"));
        }
    }
}
