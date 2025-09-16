using JwtIdentity.PlaywrightTests.Helpers;
using NUnit.Framework;

namespace JwtIdentity.PlaywrightTests.Tests
{
    [TestFixture]
    public class AuthTests : PlaywrightHelper
    {
        [Test]
        public async Task LoginTest_Succeeds()
        {
            const string logoutSelector = "a[href='logout']";

            await ExecuteWithLoggingAsync(nameof(LoginTest_Succeeds), logoutSelector, async () =>
            {
                await LoginAsync("playwrightuser@example.com");

                var logoutLink = Page.Locator(logoutSelector);
                await Microsoft.Playwright.Assertions.Expect(logoutLink).ToBeVisibleAsync();

                await logoutLink.ClickAsync();

                var loginLink = Page.Locator("a[href='login']");
                await Microsoft.Playwright.Assertions.Expect(loginLink).ToBeVisibleAsync();
            });
        }
    }
}
