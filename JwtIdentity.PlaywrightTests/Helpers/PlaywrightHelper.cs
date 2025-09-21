using JwtIdentity.Common.ViewModels;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Net.Http.Json;

namespace JwtIdentity.PlaywrightTests.Helpers
{
    public abstract class PlaywrightHelper
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private string _originalHeadedValue;
        private IPlaywright _playwright;
        private IBrowser _browser;
        private string _currentBrowserName = "chromium";

        protected IBrowserContext Context { get; private set; }
        protected IPage Page { get; private set; }

        protected virtual bool AutoLogin => false;
        protected virtual string AutoLoginUsername => "playwrightuser@example.com";

        [OneTimeSetUp]
        public async Task GlobalSetUpAsync()
        {
            ConfigurePlaywrightExecutionMode();

            var settings = PlaywrightTestConfiguration.Settings;

            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            var browserType = _playwright.Chromium;
            _currentBrowserName = browserType.Name;

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = settings.Headless
            };

            _browser = await browserType.LaunchAsync(launchOptions);
            Context = await _browser.NewContextAsync(ContextOptions());
            Page = await Context.NewPageAsync();

            if (AutoLogin)
            {
                await LoginAsync(AutoLoginUsername);
                await EnsureAuthenticatedAsync();
            }

            await OnAfterSetupAsync();
        }

        protected virtual Task OnAfterSetupAsync() => Task.CompletedTask;

        [OneTimeTearDown]
        public async Task GlobalTearDownAsync()
        {
            try
            {
                if (Page is not null)
                {
                    await Page.CloseAsync();
                }

                if (Context is not null)
                {
                    await Context.CloseAsync();
                }

                if (_browser is not null)
                {
                    await _browser.CloseAsync();
                }

                _playwright?.Dispose();
            }
            finally
            {
                RestorePlaywrightExecutionMode();
            }
        }

        protected virtual BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions
            {
                BaseURL = BaseUrl,
                IgnoreHTTPSErrors = true
            };
        }

        protected virtual string BaseUrl => Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL") ?? "https://localhost:5001";
        protected virtual string ApiEndpoint => $"{BaseUrl.TrimEnd('/')}/api/playwrightlog";
        protected virtual string PlaywrightPassword => Environment.GetEnvironmentVariable("PLAYWRIGHT_PASSWORD") ?? "UserPassword123";

        protected string CurrentBrowserName => _currentBrowserName;

        protected async Task LoginAsync(string username, string password = null, IPage page = null)
        {
            var targetPage = page ?? Page ?? throw new InvalidOperationException("Playwright page has not been initialized.");
            password ??= PlaywrightPassword;

            await targetPage.GotoAsync("/login");
            await targetPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await targetPage.FillAsync("#username", username);
            await targetPage.FillAsync("#password", password);
            await targetPage.ClickAsync("button[type='submit']");
            await targetPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        protected async Task EnsureAuthenticatedAsync(IPage page = null)
        {
            var targetPage = page ?? Page ?? throw new InvalidOperationException("Playwright page has not been initialized.");

            var logoutLink = targetPage
                .GetByRole(AriaRole.Toolbar)
                .GetByRole(AriaRole.Link, new() { Name = "Logout" });

            await Microsoft.Playwright.Assertions.Expect(logoutLink).ToBeVisibleAsync();
        }

        protected async Task LogoutAsync(IPage page = null)
        {
            var targetPage = page ?? Page ?? throw new InvalidOperationException("Playwright page has not been initialized.");

            await targetPage.GotoAsync("/logout");
            await targetPage.WaitForURLAsync("**/login");
        }

        protected async Task ExecuteWithLoggingAsync(string testName, string targetSelector, Func<Task> testBody)
        {
            try
            {
                await testBody();
                await LogAsync(new PlaywrightLogViewModel
                {
                    TestName = testName,
                    Status = "Passed",
                    ExecutedAt = DateTime.UtcNow,
                    Browser = CurrentBrowserName
                });
            }
            catch (Exception ex)
            {
                await LogAsync(new PlaywrightLogViewModel
                {
                    TestName = testName,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    FailedElement = targetSelector,
                    ExecutedAt = DateTime.UtcNow,
                    Browser = CurrentBrowserName
                });

                throw;
            }
        }

        private void ConfigurePlaywrightExecutionMode()
        {
            _originalHeadedValue = Environment.GetEnvironmentVariable("HEADED");

            var settings = PlaywrightTestConfiguration.Settings;
            var headedValue = settings.Headless ? "0" : "1";
            Environment.SetEnvironmentVariable("HEADED", headedValue);

            TestContext.Progress.WriteLine($"Playwright headless mode: {settings.Headless}");
        }

        private void RestorePlaywrightExecutionMode()
        {
            Environment.SetEnvironmentVariable("HEADED", _originalHeadedValue);
        }

        private async Task LogAsync(PlaywrightLogViewModel log)
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync(ApiEndpoint, log);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                TestContext.Error.WriteLine($"Failed to log Playwright test result: {ex}");
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }
}
