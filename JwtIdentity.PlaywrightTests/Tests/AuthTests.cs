using JwtIdentity.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace JwtIdentity.PlaywrightTests.Tests
{
    [TestFixture]
    public class AuthTests : PlaywrightHelper
    {
        [Test]
        public async Task LoginTest_Succeeds()
        {
            const string logoutSelectorDescription = "Toolbar logout link";

            await ExecuteWithLoggingAsync(nameof(LoginTest_Succeeds), logoutSelectorDescription, async () =>
            {
                await LoginAsync("playwrightuser@example.com");

                var logoutLink = Page
                    .GetByRole(AriaRole.Toolbar)
                    .GetByRole(AriaRole.Link, new() { Name = "Logout" });
                await Microsoft.Playwright.Assertions.Expect(logoutLink).ToBeVisibleAsync();

                await logoutLink.ClickAsync();

                var loginLink = Page
                    .GetByRole(AriaRole.Toolbar)
                    .GetByRole(AriaRole.Link, new() { Name = "Login" });
                await Microsoft.Playwright.Assertions.Expect(loginLink).ToBeVisibleAsync();
            });
        }
    }
}
