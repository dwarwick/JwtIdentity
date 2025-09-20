namespace JwtIdentity.Client.Pages.Docs
{
    public class ManagingContentModel : DocsPageModel
    {
        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("reorder-questions", "Reorder questions"),
                Toc("reorder-options", "Reorder answer choices"),
                Toc("delete-content", "Delete questions and options")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Managing Content", "/docs/managing-content", true)
            };

            return PageConfig(
                "managing-content",
                toc,
                breadcrumbs,
                Link("/docs/creating-surveys", "Creating Surveys"),
                Link("/docs/publishing-and-sharing", "Publishing & Sharing"));
        }
    }
}
