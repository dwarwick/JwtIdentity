namespace JwtIdentity.Client.Pages.Docs
{
    public class CreatingSurveysModel : DocsPageModel
    {
        protected override DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("ai-assist", "Use AI to jump-start your survey"),
                Toc("create-survey", "Create a new survey shell"),
                Toc("build-questions", "Build out your questions"),
                Toc("edit-questions", "Edit existing questions"),
                Toc("reuse-questions", "Reuse questions from your library")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Creating Surveys", "/docs/creating-surveys", true)
            };

            return PageConfig(
                "creating-surveys",
                toc,
                breadcrumbs,
                Link("/docs/getting-started", "Getting Started"),
                Link("/docs/managing-content", "Managing Content"));
        }
    }
}
