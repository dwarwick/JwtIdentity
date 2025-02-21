namespace JwtIdentity.Client.Pages.Navigation
{
    public class MyNavMenuModel : BlazorBase, IDisposable
    {
        private bool _disposed = false;

        [Parameter]
        public bool DarkTheme { get; set; }

        [Parameter]
        public EventCallback<bool> DarkThemeChanged { get; set; }
        protected bool _drawerOpen { get; set; } = false;

        protected override void OnInitialized()
        {
            ((CustomAuthStateProvider)AuthStateProvider!).OnLoggedOut += UpdateLoggedIn;
            CustomAuthorizationMessageHandler.OnUnauthorized += UpdateLoggedIn;
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
                    CustomAuthorizationMessageHandler.OnUnauthorized -= UpdateLoggedIn;
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
