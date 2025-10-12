using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace JwtIdentity.Client.Pages.Docs
{
    public abstract class DocsPageModel : BlazorBase
    {
        [CascadingParameter]
        protected DocsLayoutModel LayoutContext { get; set; } = default!;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            LayoutContext.ApplyPageConfiguration(GetPageConfiguration());
        }

        protected abstract DocsLayoutModel.PageConfiguration GetPageConfiguration();

        protected static DocsLayoutModel.TocItem Toc(string id, string text, int level = 2)
            => new(id, text, level);

        protected static DocsLayoutModel.BreadcrumbItem Crumb(string text, string href, bool isCurrent = false)
            => new(text, href, isCurrent);

        protected static DocsLayoutModel.PagerLink Link(string href, string title)
            => new(href, title);

        protected static DocsLayoutModel.PageConfiguration PageConfig(
            string section,
            IEnumerable<DocsLayoutModel.TocItem> toc,
            IEnumerable<DocsLayoutModel.BreadcrumbItem> breadcrumbs)
            => new(section, toc, breadcrumbs, DocsLayoutModel.PagerLink.Empty, DocsLayoutModel.PagerLink.Empty);

        protected static DocsLayoutModel.PageConfiguration PageConfig(
            string section,
            IEnumerable<DocsLayoutModel.TocItem> toc,
            IEnumerable<DocsLayoutModel.BreadcrumbItem> breadcrumbs,
            DocsLayoutModel.PagerLink previous,
            DocsLayoutModel.PagerLink next)
            => new(section, toc, breadcrumbs, previous, next);
    }
}
