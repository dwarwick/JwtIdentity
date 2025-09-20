using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JwtIdentity.Client.Pages.Docs.Api
{
    public class SurveysModel : DocsPageModel
    {
        protected IReadOnlyList<EndpointRow> Endpoints { get; } = new List<EndpointRow>
        {
            new("GET", "/api/surveys", "List surveys accessible to the authenticated user."),
            new("POST", "/api/surveys", "Create a new survey with optional AI-generated questions."),
            new("GET", "/api/surveys/{id}", "Retrieve a specific survey including published status and analytics."),
            new("PUT", "/api/surveys/{id}", "Update survey metadata or question content."),
            new("POST", "/api/surveys/{id}/close", "Close a survey to stop collecting additional responses.")
        };

        protected string RequestExample { get; } = JsonSerializer.Serialize(new
        {
            title = "Customer Satisfaction", 
            description = "Quarterly NPS survey", 
            useAiGenerator = true,
            aiInstructions = "Create 5 concise questions about support quality"
        }, new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        protected string ResponseExample { get; } = JsonSerializer.Serialize(new
        {
            id = "b2c6f8b1-0c2a-4ae7-9d52-30d2cbe7d001",
            status = "Published",
            createdUtc = "2024-09-01T12:34:56Z",
            ownerId = "a1b2c3",
            aiSummary = "Responses indicate strong satisfaction with onboarding support.",
            links = new { preview = "https://app.surveyshark.com/surveys/b2c6f8" }
        }, new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true
        });

        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("endpoint-summary", "Endpoint summary"),
                Toc("request-example", "Create survey request"),
                Toc("response-fields", "Important response fields"),
                Toc("error-handling", "Error handling")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("API", string.Empty),
                Crumb("Surveys", "/docs/api/surveys", true)
            };

            return PageConfig(
                "api",
                toc,
                breadcrumbs,
                Link("/docs/components/bar-chart", "Components: Bar Chart"),
                Link("/docs/how-to/export-results", "Guide: Export Results"));
        }

        protected record EndpointRow(string Method, string Route, string Description);
    }
}
