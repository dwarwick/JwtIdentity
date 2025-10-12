namespace JwtIdentity.Client.Pages.Docs
{
    public class FeedbackModel : DocsPageModel
    {
        protected override DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("submit-feedback", "Submit product feedback")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Feedback", "/docs/feedback", true)
            };

            return PageConfig(
                "feedback",
                toc,
                breadcrumbs,
                Link("/docs/viewing-results", "Viewing Results"),
                Link("/docs/faq", "FAQ"));
        }
    }
}
