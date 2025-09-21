using JwtIdentity.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace JwtIdentity.PlaywrightTests.Tests
{
    [TestFixture]
    public class SurveyTests : PlaywrightHelper
    {
        protected override bool AutoLogin => true;

        [Test]
        public async Task CreateSurveyUsingAI_SurveyCreated()
        {
            const string testName = nameof(CreateSurveyUsingAI_SurveyCreated);
            const string targetSelectorDescription = "Survey creation success snackbar";

            await ExecuteWithLoggingAsync(testName, targetSelectorDescription, async () =>
            {
                await Page.GotoAsync("/");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await OpenCreateSurveyAsync();

                var createSurveyHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Create Survey" });
                await Microsoft.Playwright.Assertions.Expect(createSurveyHeading).ToBeVisibleAsync();

                await Page.FillAsync("#Title", "Customer Satisfaction Survey");
                await Page.FillAsync("#Description", "Please take our customer satisfaction survey so that we can learn how we did while repairing your refrigerator.");
                await Page.FillAsync("#AiInstructions", "Create 10 questions. The last question should be a free text question so the user can provide any additional feedback.");

                var createButton = Page.GetByRole(AriaRole.Button, new() { Name = "Create" });
                await Task.WhenAll(
                    Page.WaitForURLAsync("**/survey/edit/**"),
                    createButton.ClickAsync());

                var editSurveyHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Edit Survey" });
                await Microsoft.Playwright.Assertions.Expect(editSurveyHeading).ToBeVisibleAsync();

                var successToast = Page.Locator(".mud-snackbar").Filter(new() { HasTextString = "Survey Created" });
                await Microsoft.Playwright.Assertions.Expect(successToast).ToBeVisibleAsync();
            });
        }

        private async Task OpenCreateSurveyAsync()
        {
            var surveysMenu = Page.Locator("#nav-surveys-menu button");
            await surveysMenu.ClickAsync();

            var createSurveyMenuItem = Page.GetByRole(AriaRole.Link, new() { Name = "Create Survey" });
            await Task.WhenAll(
                Page.WaitForURLAsync("**/survey/create"),
                createSurveyMenuItem.ClickAsync());
        }
    }
}
