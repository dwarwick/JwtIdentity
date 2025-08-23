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
        protected bool _drawerOpen { get; set; } = false;

        protected AppSettings AppSettings { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            AppSettings = await ApiService.GetAsync<AppSettings>("/api/appsettings");
        }

        protected override void OnInitialized()
        {
            ((CustomAuthStateProvider)AuthStateProvider!).OnLoggedOut += UpdateLoggedIn;
            _authorizationHandler = CustomAuthorizationMessageHandler;
            _authorizationHandler.OnUnauthorized += UpdateLoggedIn;
            _drawerOpen = false;
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

        protected void UpdateLoggedIn()
        {
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
