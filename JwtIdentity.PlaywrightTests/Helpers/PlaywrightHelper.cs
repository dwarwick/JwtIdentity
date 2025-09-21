using JwtIdentity.Common.ViewModels;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Net.Http.Json;

namespace JwtIdentity.PlaywrightTests.Helpers
{
    public abstract class PlaywrightHelper : PageTest
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private string _originalHeadedValue;

        [OneTimeSetUp]
        public void ConfigurePlaywrightExecutionMode()
        {
            _originalHeadedValue = Environment.GetEnvironmentVariable("HEADED");

            var settings = PlaywrightTestConfiguration.Settings;
            var headedValue = settings.Headless ? "0" : "1";
            Environment.SetEnvironmentVariable("HEADED", headedValue);

            TestContext.Progress.WriteLine($"Playwright headless mode: {settings.Headless}");
        }

        [OneTimeTearDown]
        public void RestorePlaywrightExecutionMode()
        {
            Environment.SetEnvironmentVariable("HEADED", _originalHeadedValue);
        }

        protected virtual string BaseUrl => Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL") ?? "https://localhost:5001";
        protected virtual string ApiEndpoint => $"{BaseUrl.TrimEnd('/')}/api/playwrightlog";
        protected virtual string PlaywrightPassword => Environment.GetEnvironmentVariable("PLAYWRIGHT_PASSWORD") ?? "UserPassword123";

        protected string CurrentBrowserName => BrowserType?.Name ?? "chromium";

        public override BrowserNewContextOptions ContextOptions()
        {
            var options = base.ContextOptions();
            options.BaseURL = BaseUrl;
            options.IgnoreHTTPSErrors = true;
            return options;
        }

        protected async Task LoginAsync(string username, string password = null)
        {
            password ??= PlaywrightPassword;
            await Page.GotoAsync("/login");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.FillAsync("#username", username);
            await Page.FillAsync("#password", password);
            await Page.ClickAsync("button[type='submit']");
        }

        protected async Task LogoutAsync()
        {
            await Page.GotoAsync("/logout");
            await Page.WaitForURLAsync("**/login");
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
