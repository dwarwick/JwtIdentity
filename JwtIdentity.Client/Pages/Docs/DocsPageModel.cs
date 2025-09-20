using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace JwtIdentity.Client.Pages.Docs
{
    public abstract class DocsPageModel : BlazorBase
    {
        [CascadingParameter]
        protected _DocsLayoutModel LayoutContext { get; set; } = default!;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            LayoutContext.ApplyPageConfiguration(GetPageConfiguration());
        }

        protected abstract _DocsLayoutModel.PageConfiguration GetPageConfiguration();

        protected static _DocsLayoutModel.TocItem Toc(string id, string text, int level = 2)
            => new(id, text, level);

        protected static _DocsLayoutModel.BreadcrumbItem Crumb(string text, string href, bool isCurrent = false)
            => new(text, href, isCurrent);

        protected static _DocsLayoutModel.PagerLink Link(string href, string title)
            => new(href, title);

        protected static _DocsLayoutModel.PageConfiguration PageConfig(
            string section,
            IEnumerable<_DocsLayoutModel.TocItem> toc,
            IEnumerable<_DocsLayoutModel.BreadcrumbItem> breadcrumbs)
            => new(section, toc, breadcrumbs, _DocsLayoutModel.PagerLink.Empty, _DocsLayoutModel.PagerLink.Empty);

        protected static _DocsLayoutModel.PageConfiguration PageConfig(
            string section,
            IEnumerable<_DocsLayoutModel.TocItem> toc,
            IEnumerable<_DocsLayoutModel.BreadcrumbItem> breadcrumbs,
            _DocsLayoutModel.PagerLink previous,
            _DocsLayoutModel.PagerLink next)
            => new(section, toc, breadcrumbs, previous, next);
    }
}
