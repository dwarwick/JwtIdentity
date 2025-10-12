namespace JwtIdentity.Client.Pages.Docs
{
    public class PublishingAndSharingModel : DocsPageModel
    {
        protected override DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("publish", "Publish your survey"),
                Toc("share", "Share with your audience")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Publishing & Sharing", "/docs/publishing-and-sharing", true)
            };

            return PageConfig(
                "publishing-and-sharing",
                toc,
                breadcrumbs,
                Link("/docs/managing-content", "Managing Content"),
                Link("/docs/answering-surveys", "Answering Surveys"));
        }
    }
}
