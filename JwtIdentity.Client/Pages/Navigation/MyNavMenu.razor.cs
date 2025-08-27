using System.Security.Claims;
using JwtIdentity.Common.Auth;

namespace JwtIdentity.Client.Pages.Navigation
{
    public class MyNavMenuModel : BlazorBase, IDisposable
    {
        private bool _disposed = false;
        // Cache handler to ensure subscriptions don't resolve services after disposal
        private CustomAuthorizationMessageHandler? _authorizationHandler;

        [Parameter]
        public bool DarkTheme { get; set; }

        [Parameter]
        public EventCallback<bool> DarkThemeChanged { get; set; }

        [Parameter]
        public AppSettings AppSettings { get; set; } = new();

        protected bool _drawerOpen { get; set; } = false;

        protected bool IsAuthenticated { get; private set; }
        protected bool IsAdmin { get; private set; }
        protected bool CanCreateSurvey { get; private set; }
        protected bool CanLeaveFeedback { get; private set; }
        protected bool CanUseHangfire { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            await SetAuthFlagsAsync();
        }

        protected override void OnInitialized()
        {
            ((CustomAuthStateProvider)AuthStateProvider!).OnLoggedOut += UpdateLoggedIn;
            _authorizationHandler = CustomAuthorizationMessageHandler;
            _authorizationHandler.OnUnauthorized += UpdateLoggedIn;
            _drawerOpen = false;
        }

        private async Task SetAuthFlagsAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false;
            if (IsAuthenticated)
            {
                IsAdmin = user.IsInRole("Admin");
                var permissions = user.Claims
                    .Where(c => c.Type == CustomClaimTypes.Permission)
                    .Select(c => c.Value)
                    .ToHashSet();
                CanCreateSurvey = permissions.Contains(Permissions.CreateSurvey);
                CanLeaveFeedback = permissions.Contains(Permissions.LeaveFeedback);
                CanUseHangfire = permissions.Contains(Permissions.UseHangfire);
            }
            else
            {
                IsAdmin = CanCreateSurvey = CanLeaveFeedback = CanUseHangfire = false;
            }
        }

        protected void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }

        protected async Task DarkThemeChangedHandler(bool value)
        {
            DarkTheme = value;
            await DarkThemeChanged.InvokeAsync(value);
        }

        protected async void UpdateLoggedIn()
        {
            await SetAuthFlagsAsync();
            StateHasChanged();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ((CustomAuthStateProvider)AuthStateProvider!).OnLoggedOut -= UpdateLoggedIn;
                    if (_authorizationHandler != null)
                    {
                        _authorizationHandler.OnUnauthorized -= UpdateLoggedIn;
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

