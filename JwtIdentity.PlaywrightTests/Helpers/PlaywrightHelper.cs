using JwtIdentity.Common.ViewModels;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Net.Http.Json;
using System.Diagnostics;

namespace JwtIdentity.PlaywrightTests.Helpers
{
    public abstract class PlaywrightHelper
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly object BrowserLock = new();
        private static readonly object ServerLock = new();
        private static IPlaywright _sharedPlaywright;
        private static IBrowser _sharedBrowser;
        private static int _instanceCount = 0;
        private static Process _serverProcess;
        private static bool _serverReady;
        
        private string _originalHeadedValue;
        private string _currentBrowserName = "chromium";

        protected IBrowserContext Context { get; private set; }
        protected IPage Page { get; private set; }

        protected virtual bool AutoLogin => false;
        protected virtual string AutoLoginUsername => "playwrightuser@example.com";

        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            lock (BrowserLock)
            {
                if (_instanceCount == 0)
                {
                    ConfigurePlaywrightExecutionMode();
                }
                _instanceCount++;
            }

            // Ensure server is running before browser initialization (once per session)
            EnsureServerRunning();

            // Initialize shared browser if not already done
            if (_sharedPlaywright == null)
            {
                lock (BrowserLock)
                {
                    if (_sharedPlaywright == null)
                    {
                        _sharedPlaywright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
                        
                        var settings = PlaywrightTestConfiguration.Settings;
                        var browserType = _sharedPlaywright.Chromium;
                        _currentBrowserName = browserType.Name;

                        var launchOptions = new BrowserTypeLaunchOptions
                        {
                            Headless = settings.Headless,
                            // Do not hardcode ExecutablePath; let Playwright manage browser binaries cross-platform
                            Args = settings.Headless ? new[] { "--headless=new" } : null
                        };

                        _sharedBrowser = browserType.LaunchAsync(launchOptions).GetAwaiter().GetResult();
                    }
                }
            }
        }

        [SetUp]
        public async Task SetUpAsync()
        {
            // Create a new context and page for each test
            Context = await _sharedBrowser.NewContextAsync(ContextOptions());
            Page = await Context.NewPageAsync();

            if (AutoLogin)
            {
                await LoginAsync(AutoLoginUsername);
                await EnsureAuthenticatedAsync();
            }

            await OnAfterSetupAsync();
        }

        protected virtual Task OnAfterSetupAsync() => Task.CompletedTask;

        [TearDown]
        public async Task TearDownAsync()
        {
            // Close page and context after each test
            if (Page is not null)
            {
                await Page.CloseAsync();
            }

            if (Context is not null)
            {
                await Context.CloseAsync();
            }
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            lock (BrowserLock)
            {
                _instanceCount--;
                if (_instanceCount == 0)
                {
                    try
                    {
                        if (_sharedBrowser is not null)
                        {
                            _sharedBrowser.CloseAsync().GetAwaiter().GetResult();
                            _sharedBrowser = null;
                        }

                        if (_sharedPlaywright is not null)
                        {
                            _sharedPlaywright.Dispose();
                            _sharedPlaywright = null;
                        }

                        // Stop the server if we started it
                        StopServerIfStarted();
                    }
                    finally
                    {
                        RestorePlaywrightExecutionMode();
                    }
                }
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

            // Navigate to login and wait for DOM to be ready (NetworkIdle can be unstable with SignalR/long polling)
            await targetPage.GotoAsync("/login");
            await targetPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Dismiss cookie banner before interacting with inputs to avoid overlay/focus traps
            await DismissCookieBannerAsync(targetPage);

            // Ensure the inputs are present and visible before filling (stabilizes parallel runs)
            var usernameInput = targetPage.Locator("#username");
            var passwordInput = targetPage.Locator("#password");
            await usernameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
            await passwordInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            await FillReliableAsync(usernameInput, username);
            await FillReliableAsync(passwordInput, password);
            
            // Wait for navigation to complete after login
            await Task.WhenAll(
                targetPage.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 30000 }),
                targetPage.ClickAsync("button[type='submit']")
            );
            
            // Avoid strict NetworkIdle since app may keep connections open; give a short settle time instead
            await targetPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await targetPage.WaitForTimeoutAsync(250);
        }

        private static async Task FillReliableAsync(ILocator locator, string value, int attempts = 3)
        {
            for (var i = 0; i < attempts; i++)
            {
                await locator.FillAsync(value);
                try
                {
                    await Microsoft.Playwright.Assertions.Expect(locator).ToHaveValueAsync(value, new() { Timeout = 2000 });
                    return;
                }
                catch
                {
                    // Element may have been re-rendered (Blazor hydration). Retry.
                    await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 2000 });
                }
            }

            // Final assert to surface readable error when it still fails
            await Microsoft.Playwright.Assertions.Expect(locator).ToHaveValueAsync(value, new() { Timeout = 2000 });
        }

        protected async Task DismissCookieBannerAsync(IPage page = null)
        {
            var targetPage = page ?? Page ?? throw new InvalidOperationException("Playwright page has not been initialized.");

            await targetPage.WaitForTimeoutAsync(1200);

            var banner = targetPage.Locator(".cookie-banner.visible");

            if (await banner.CountAsync() == 0)
            {
                return;
            }

            var acceptAllButton = targetPage.GetByRole(AriaRole.Button, new() { Name = "Accept All Cookies" });
            var essentialOnlyButton = targetPage.GetByRole(AriaRole.Button, new() { Name = "Essential Cookies Only" });

            var clickAcceptAll = Random.Shared.Next(0, 2) == 0;
            var acceptAllAvailable = await acceptAllButton.CountAsync() > 0;
            var essentialOnlyAvailable = await essentialOnlyButton.CountAsync() > 0;

            ILocator buttonToClick;

            if (clickAcceptAll && acceptAllAvailable)
            {
                buttonToClick = acceptAllButton;
            }
            else if (!clickAcceptAll && essentialOnlyAvailable)
            {
                buttonToClick = essentialOnlyButton;
            }
            else if (acceptAllAvailable)
            {
                buttonToClick = acceptAllButton;
            }
            else if (essentialOnlyAvailable)
            {
                buttonToClick = essentialOnlyButton;
            }
            else
            {
                return;
            }

            var clickedEssentialOnly = buttonToClick == essentialOnlyButton;

            // Ensure antiforgery cookie exists before any action that could rely on it later
            await EnsureAntiforgeryCookieAsync(targetPage);

            if (clickedEssentialOnly)
            {
                // Use response wait to bind to the consent API request and avoid races
                await targetPage.RunAndWaitForResponseAsync(
                    async () => await buttonToClick.ClickAsync(),
                    r => r.Url.Contains("/api/cookie/consent", StringComparison.OrdinalIgnoreCase)
                );
                // No full navigation expected; give a small settle time only
                await targetPage.WaitForTimeoutAsync(150);
            }
            else
            {
                await buttonToClick.ClickAsync();
                // Rely on banner detachment wait below; add small delay for animations
                await targetPage.WaitForTimeoutAsync(150);
            }

            try
            {
                await banner.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Detached,
                    Timeout = 5000
                });
            }
            catch (TimeoutException)
            {
                // Ignore timeout if the banner disappears during a navigation.
            }
            catch (PlaywrightException)
            {
                // Ignore Playwright errors caused by the banner being removed during navigation.
            }
        }

        private static async Task EnsureAntiforgeryCookieAsync(IPage targetPage)
        {
            try
            {
                // If cookie already exists, do nothing
                var cookies = await targetPage.Context.CookiesAsync();
                if (cookies.Any(c => c.Name.Equals("RequestVerificationToken", StringComparison.OrdinalIgnoreCase) ||
                                     c.Name.StartsWith(".AspNetCore.Antiforgery", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                // Touch a GET page to allow app.UseAntiforgery() to issue the token cookie
                await targetPage.GotoAsync("/");
                await targetPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch
            {
                // Best-effort only
            }
        }

        protected async Task EnsureAuthenticatedAsync(IPage page = null)
        {
            var targetPage = page ?? Page ?? throw new InvalidOperationException("Playwright page has not been initialized.");

            var logoutLink = targetPage
                .GetByRole(AriaRole.Toolbar)
                .GetByRole(AriaRole.Link, new() { Name = "Logout" });

            // Increase timeout for parallel execution scenarios
            await Microsoft.Playwright.Assertions.Expect(logoutLink).ToBeVisibleAsync(new() { Timeout = 30000 });
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

        private void EnsureServerRunning()
        {
            if (_serverReady)
                return;

            lock (ServerLock)
            {
                if (_serverReady)
                    return;

                try
                {
                    // Quick probe
                    if (IsServerRespondingAsync(BaseUrl).GetAwaiter().GetResult())
                    {
                        _serverReady = true;
                        return;
                    }

                    var projectPath = ResolveServerProjectPath();
                    if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
                    {
                        TestContext.Error.WriteLine($"Could not resolve server project path. Skipping auto-start.");
                        return;
                    }

                    var projectDir = Path.GetDirectoryName(projectPath)!;
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project \"{projectPath}\" --urls {BaseUrl}",
                        WorkingDirectory = projectDir,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

                    _serverProcess = Process.Start(psi)!;

                    // Asynchronously read output to avoid buffer blocking
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            while (!_serverProcess.HasExited)
                            {
                                var line = _serverProcess.StandardOutput.ReadLine();
                                if (line == null) break;
                                if (line.Contains("Now listening") || line.Contains("Application started"))
                                {
                                    // hint it's starting up
                                }
                            }
                        }
                        catch { }
                    });

                    // Poll for readiness
                    var started = WaitForServerAsync(BaseUrl, TimeSpan.FromSeconds(45)).GetAwaiter().GetResult();
                    _serverReady = started;
                    if (!started)
                    {
                        TestContext.Error.WriteLine("Server did not become ready in time for Playwright tests.");
                    }
                }
                catch (Exception ex)
                {
                    TestContext.Error.WriteLine($"Failed to ensure server running: {ex}");
                }
            }
        }

        private static async Task<bool> IsServerRespondingAsync(string baseUrl)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var resp = await HttpClient.GetAsync(baseUrl, cts.Token);
                return resp.IsSuccessStatusCode || (int)resp.StatusCode < 500; // consider 3xx/4xx as alive
            }
            catch { return false; }
        }

        private static async Task<bool> WaitForServerAsync(string baseUrl, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                if (await IsServerRespondingAsync(baseUrl)) return true;
                await Task.Delay(500);
            }
            return false;
        }

        private static string ResolveServerProjectPath()
        {
            // Try common relative locations from the test assembly directory upwards
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "..", "..", "..", "..", "JwtIdentity", "JwtIdentity.csproj");
                candidate = Path.GetFullPath(candidate);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            // Fallback to solution-root relative path (if tests run from root)
            var fallback = Path.GetFullPath(Path.Combine("JwtIdentity", "JwtIdentity.csproj"));
            return fallback;
        }

        private static void StopServerIfStarted()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(true);
                    _serverProcess.Dispose();
                }
            }
            catch { }
            finally
            {
                _serverProcess = null;
                _serverReady = false;
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

        protected async Task ScrollToElementAsync(string elementId, IPage pageElement)
        {
            await pageElement.WaitForFunctionAsync("() => !!window.scrollToElement || typeof scrollToElement === 'function'");
            await pageElement.EvaluateAsync(@"id => {
                const fn = window.scrollToElement || (typeof scrollToElement === 'function' ? scrollToElement : null);
                if (fn) {
                    const r = fn(id, { behavior: 'auto', block: 'start', headerOffset: 0 });
                    if (r && typeof r.then === 'function') return r;
                } else {
                    const el = document.getElementById(id);
                    if (el) el.scrollIntoView({behavior:'auto', block:'start'});
                }
            }", elementId);
        }
    }
}
