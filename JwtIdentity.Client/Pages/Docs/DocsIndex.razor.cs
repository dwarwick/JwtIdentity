namespace JwtIdentity.Client.Pages.Docs
{
    public class DocsIndexModel : DocsPageModel
    {
        protected override DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("start-here", "Start here"),
                Toc("explore-sections", "Explore key sections"),
                Toc("latest-updates", "Latest updates")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation", true)
            };

            return PageConfig(
                "overview",
                toc,
                breadcrumbs,
                DocsLayoutModel.PagerLink.Empty,
                Link("/docs/getting-started", "Getting Started"));
        }
    }
}
