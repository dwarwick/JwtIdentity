namespace JwtIdentity.Client.Pages.Docs
{
    public class FaqModel : DocsPageModel
    {
        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("account-management", "Account management"),
                Toc("survey-building", "Survey building"),
                Toc("analytics", "Analytics"),
                Toc("support", "Support")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("FAQ", "/docs/faq", true)
            };

            return PageConfig(
                "faq",
                toc,
                breadcrumbs,
                Link("/docs/how-to/export-results", "Guide: Export Results"),
                _DocsLayoutModel.PagerLink.Empty);
        }
    }
}
