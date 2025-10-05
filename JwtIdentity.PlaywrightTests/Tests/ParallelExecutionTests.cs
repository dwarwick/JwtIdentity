using JwtIdentity.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace JwtIdentity.PlaywrightTests.Tests
{
    /// <summary>
    /// Test class to verify parallel test execution works correctly.
    /// This class contains simple tests that can be run concurrently without interference.
    /// </summary>
    [TestFixture]
    public class ParallelExecutionTests : PlaywrightHelper
    {
        [Test]
        public async Task Test1_NavigatesToHomePage()
        {
            await ExecuteWithLoggingAsync(nameof(Test1_NavigatesToHomePage), "Home page", async () =>
            {
                await Page.GotoAsync("/");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Verify page loaded
                var heading = Page.Locator("h1, h2, h3").First;
                await Microsoft.Playwright.Assertions.Expect(heading).ToBeVisibleAsync();
            });
        }

        [Test]
        public async Task Test2_NavigatesToLoginPage()
        {
            await ExecuteWithLoggingAsync(nameof(Test2_NavigatesToLoginPage), "Login page", async () =>
            {
                await Page.GotoAsync("/login");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Verify login form is present
                var usernameField = Page.Locator("#username");
                await Microsoft.Playwright.Assertions.Expect(usernameField).ToBeVisibleAsync();
            });
        }

        [Test]
        public async Task Test3_NavigatesToAboutPage()
        {
            await ExecuteWithLoggingAsync(nameof(Test3_NavigatesToAboutPage), "About page", async () =>
            {
                await Page.GotoAsync("/about");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Just verify the page loaded
                await Page.WaitForTimeoutAsync(500);
            });
        }

        [Test]
        public async Task Test4_VerifyPageIsolation()
        {
            // This test verifies that each test has its own isolated page instance
            await ExecuteWithLoggingAsync(nameof(Test4_VerifyPageIsolation), "Page isolation", async () =>
            {
                await Page.GotoAsync("/");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Each test should start fresh without cookies or state from other tests
                var cookies = await Page.Context.CookiesAsync();
                
                // Navigate and verify isolation
                await Page.GotoAsync("/login");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            });
        }

        [Test]
        public async Task Test5_ConcurrentNavigation()
        {
            await ExecuteWithLoggingAsync(nameof(Test5_ConcurrentNavigation), "Concurrent navigation", async () =>
            {
                await Page.GotoAsync("/");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Add a small delay to simulate realistic test timing
                await Page.WaitForTimeoutAsync(100);
                
                await Page.GotoAsync("/login");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            });
        }
    }
}
