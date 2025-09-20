using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtIdentity.Client.Helpers;
using JwtIdentity.Client.Services;
using JwtIdentity.Common.Auth;
using JwtIdentity.Common.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;

namespace JwtIdentity.Client.Pages.Docs
{
    public class _DocsLayoutModel : BlazorBase
    {
        private readonly List<TocItem> _tocItems = new();
        private readonly List<BreadcrumbItem> _breadcrumbs = new();
        private MudTheme mudTheme = new();
        private readonly MudTheme _lightTheme = new()
        {
            PaletteLight = new PaletteLight
            {
                AppbarBackground = Colors.Gray.Lighten3,
            },
        };
        private IJSObjectReference? _module;

        protected bool _isDarkMode;
        protected string _theme = "light";
        protected bool _cookiesAccepted;
        protected bool _showCookieBanner;

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
        protected AppSettings AppSettings { get; set; } = new();

        protected List<DocsSearchApiService.Hit> SearchResults { get; } = new();

        protected bool ShowSearchResults => SearchQuery.Trim().Length >= 2;

        protected void OpenSidebar()
        {
            SidebarOpen = true;
        }
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
            SelectedTocId = string.Empty;

            _ = InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || !OperatingSystem.IsBrowser())
            {
                return;
            }

            try
            {
                AppSettings = await ApiService.GetPublicAsync<AppSettings>("/api/appsettings");

                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/app.js");

                AuthenticationState authState = await AuthStateProvider.GetAuthenticationStateAsync();

                if (authState.User?.Identity?.IsAuthenticated ?? false)
                {
                    var userId = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        var applicationUserViewModel = await ApiService.GetAsync<ApplicationUserViewModel>($"{ApiEndpoints.ApplicationUser}/{userId}");

                        if (applicationUserViewModel != null)
                        {
                            applicationUserViewModel.Token = await LocalStorage.GetItemAsStringAsync("authToken") ?? string.Empty;
                            ((CustomAuthStateProvider)AuthStateProvider!).CurrentUser = applicationUserViewModel;
                        }
                    }
                }

                _theme = await LocalStorage.GetItemAsStringAsync("theme") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(_theme))
                {
                    if (((CustomAuthStateProvider)AuthStateProvider!).CurrentUser != null)
                    {
                        _theme = ((CustomAuthStateProvider)AuthStateProvider!).CurrentUser?.Theme ?? "light";
                    }
                    else
                    {
                        _theme = "light";
                    }
                }

                await SetTheme();

                var cookiesAccepted = await LocalStorage.GetItemAsync<bool>("cookiesAccepted");
                _cookiesAccepted = cookiesAccepted;

                if (!_cookiesAccepted)
                {
                    await Task.Delay(500);
                    _showCookieBanner = true;
                    StateHasChanged();
                }

                bool consentGiven = await JSRuntime.InvokeAsync<bool>("userHasThirdPartyConsent");
                if (consentGiven)
                {
                    await JSRuntime.InvokeVoidAsync("loadGoogleAds");
                }

                StateHasChanged();
            }
            catch
            {
                // Intentionally ignored to keep the docs layout resilient to API or JS errors.
            }
        }

        private async Task SetTheme()
        {
            await LocalStorage.SetItemAsStringAsync("theme", _theme);
            await LocalStorage.SetItemAsync("LastCheckTime", DateTime.UtcNow);

            if (_theme == "light")
            {
                mudTheme = _lightTheme;
            }

            _isDarkMode = _theme == "dark";

            if (_module != null)
            {
                await _module.InvokeAsync<string>("removeThemes");
                await _module.InvokeAsync<string>("addCss", $"css/app-{_theme}.css");

                if (_theme == "light")
                {
                    await _module.InvokeAsync<string>("addCss", "css/bootstrap5.3.css");
                }
                else
                {
                    await _module.InvokeAsync<string>("addCss", "css/bootstrap5.3-dark.css");
                }
            }

            if (((CustomAuthStateProvider)AuthStateProvider!).CurrentUser != null && ((CustomAuthStateProvider)AuthStateProvider!).CurrentUser?.Theme != _theme)
            {
                ((CustomAuthStateProvider)AuthStateProvider!).CurrentUser!.Theme = _theme;
                await ApiService.UpdateAsync($"{ApiEndpoints.ApplicationUser}/{((CustomAuthStateProvider)AuthStateProvider!).CurrentUser?.Id}", ((CustomAuthStateProvider)AuthStateProvider!).CurrentUser);
            }
        }

        protected async Task HandleThemeChanged()
        {
            _theme = _isDarkMode ? "dark" : "light";
            await SetTheme();
        }

        protected async Task AcceptAllCookies()
        {
            await LocalStorage.SetItemAsync("cookiesAccepted", true);
            _cookiesAccepted = true;
            _showCookieBanner = false;

            await JSRuntime.InvokeVoidAsync("acceptAllCookies");
            await JSRuntime.InvokeVoidAsync("loadGoogleAds");

            StateHasChanged();
        }

        protected async Task RejectThirdPartyCookies()
        {
            await JSRuntime.InvokeVoidAsync("clearThirdPartyServicesCompletely");
            await LocalStorage.SetItemAsync("cookiesAccepted", true);
            _cookiesAccepted = true;
            _showCookieBanner = false;

            await JSRuntime.InvokeVoidAsync("rejectThirdPartyCookies");
            await JSRuntime.InvokeVoidAsync("eval", "location.reload()");
        }

        protected async Task OpenCookieSettings()
        {
            await LocalStorage.RemoveItemAsync("cookiesAccepted");
            _cookiesAccepted = false;
            await JSRuntime.InvokeVoidAsync("clearCookieConsent");
            _showCookieBanner = true;
            StateHasChanged();
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
