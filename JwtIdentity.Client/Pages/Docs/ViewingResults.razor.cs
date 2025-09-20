namespace JwtIdentity.Client.Pages.Docs
{
    public class ViewingResultsModel : DocsPageModel
    {
        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("grid", "Review results in a grid"),
                Toc("charts", "Visualize answers with charts")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Viewing Results", "/docs/viewing-results", true)
            };

            return PageConfig(
                "viewing-results",
                toc,
                breadcrumbs,
                Link("/docs/answering-surveys", "Answering Surveys"),
                Link("/docs/feedback", "Feedback"));
        }
    }
}
