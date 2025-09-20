using System.Collections.Generic;

namespace JwtIdentity.Client.Pages.Docs.Concepts
{
    public class AuthorizationModel : DocsPageModel
    {
        protected IReadOnlyList<PermissionRow> PermissionRows { get; } = new List<PermissionRow>
        {
            new("Surveys.Read", "View surveys and analytics without editing content.", "Contributors"),
            new("Surveys.Manage", "Create, edit, and delete surveys within assigned workspaces.", "Survey Owners"),
            new("Responses.Export", "Export responses to CSV or Excel for offline analysis.", "Survey Owners"),
            new("Users.Manage", "Invite, suspend, and assign roles to users across the tenant.", "Administrators"),
            new("Settings.Configure", "Update global configuration, integrations, and billing settings.", "Administrators")
        };

        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("auth-overview", "Authentication flow overview"),
                Toc("role-types", "Role hierarchy"),
                Toc("permission-model", "Permission model"),
                Toc("best-practices", "Best practices")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Concepts", string.Empty),
                Crumb("Authorization", "/docs/concepts/authorization", true)
            };

            return PageConfig(
                "concepts",
                toc,
                breadcrumbs,
                Link("/docs/getting-started", "Getting Started"),
                Link("/docs/components/bar-chart", "Components: Bar Chart"));
        }

        protected record PermissionRow(string Permission, string Description, string Role);
    }
}
