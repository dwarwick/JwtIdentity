using System.Collections.Generic;
using JwtIdentity.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JwtIdentity.Client.Pages.Docs
{
    public class _DocsLayoutModel : BlazorBase
    {
        private readonly List<TocItem> _tocItems = new();
        private readonly List<BreadcrumbItem> _breadcrumbs = new();

        [Parameter]
        public RenderFragment Body { get; set; } = default!;

        protected bool SidebarOpen { get; set; } = true;
        protected string SearchQuery { get; set; } = string.Empty;
        protected bool IsSearching { get; set; }
        protected string SelectedTocId { get; set; } = string.Empty;

        protected IReadOnlyList<TocItem> TocItems => _tocItems;
        protected IReadOnlyList<BreadcrumbItem> Breadcrumbs => _breadcrumbs;
        protected PagerLink PreviousLink { get; private set; } = PagerLink.Empty;
        protected PagerLink NextLink { get; private set; } = PagerLink.Empty;
        protected string CurrentSection { get; private set; } = string.Empty;

        protected List<DocsSearchApiService.Hit> SearchResults { get; } = new();

        protected bool ShowSearchResults => SearchQuery.Trim().Length >= 2;

        protected void OpenSidebar()
        {
            SidebarOpen = true;
        }

        protected bool IsSectionActive(string section)
        {
            return string.Equals(CurrentSection, section, StringComparison.OrdinalIgnoreCase);
        }

        protected bool GettingStartedExpanded => IsSectionActive("getting-started");
        protected bool ConceptsExpanded => IsSectionActive("concepts");
        protected bool ComponentsExpanded => IsSectionActive("components");
        protected bool ApiExpanded => IsSectionActive("api");

        protected async Task OnSearchChanged(string value)
        {
            SearchQuery = value ?? string.Empty;
            var trimmed = SearchQuery.Trim();

            if (trimmed.Length < 2)
            {
                SearchResults.Clear();
                IsSearching = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            try
            {
                IsSearching = true;
                await InvokeAsync(StateHasChanged);

                var hits = await ServiceProvider.GetRequiredService<DocsSearchApiService>().SearchAsync(trimmed, 10);

                SearchResults.Clear();
                SearchResults.AddRange(hits);
            }
            catch
            {
                SearchResults.Clear();
            }
            finally
            {
                IsSearching = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void CloseSearch()
        {
            SearchQuery = string.Empty;
            SearchResults.Clear();
            IsSearching = false;
            _ = InvokeAsync(StateHasChanged);
        }

        protected void NavigateToResult(string url)
        {
            CloseSearch();
            Navigation.NavigateTo(url);
        }

        protected void OnTocSelectionChanged(string value)
        {
            SelectedTocId = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(SelectedTocId))
            {
                return;
            }

            var absolute = Navigation.ToAbsoluteUri(Navigation.Uri);
            var target = absolute.GetLeftPart(UriPartial.Path) + "#" + SelectedTocId;
            Navigation.NavigateTo(target, forceLoad: false);
        }

        public void ApplyPageConfiguration(PageConfiguration configuration)
        {
            _tocItems.Clear();
            _tocItems.AddRange(configuration.TocItems);

            _breadcrumbs.Clear();
            _breadcrumbs.AddRange(configuration.Breadcrumbs);

            PreviousLink = configuration.Previous;
            NextLink = configuration.Next;
            CurrentSection = configuration.Section;
            SelectedTocId = string.Empty;

            _ = InvokeAsync(StateHasChanged);
        }

        public record TocItem(string Id, string Text, int Level);

        public record BreadcrumbItem(string Text, string Href, bool IsCurrent = false);

        public record PagerLink(string Href, string Title)
        {
            public static PagerLink Empty { get; } = new(string.Empty, string.Empty);

            public bool IsEmpty => string.IsNullOrWhiteSpace(Href) || string.IsNullOrWhiteSpace(Title);
        }

        public record PageConfiguration(
            string Section,
            IEnumerable<TocItem> TocItems,
            IEnumerable<BreadcrumbItem> Breadcrumbs,
            PagerLink Previous,
            PagerLink Next);
    }
}
