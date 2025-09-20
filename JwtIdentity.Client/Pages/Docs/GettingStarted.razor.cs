namespace JwtIdentity.Client.Pages.Docs
{
    public class GettingStartedModel : DocsPageModel
    {
        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("create-account", "Create your SurveyShark account"),
                Toc("log-in", "Sign in securely"),
                Toc("first-survey", "Launch your first survey"),
                Toc("next-steps", "Next steps")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Getting Started", "/docs/getting-started", true)
            };

            return PageConfig(
                "getting-started",
                toc,
                breadcrumbs,
                Link("/documentation", "Documentation"),
                Link("/docs/concepts/authorization", "Concepts: Authorization"));
        }
    }
}
