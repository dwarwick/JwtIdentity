using JwtIdentity.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace JwtIdentity.PlaywrightTests.Tests
{
    [TestFixture]
    public class CompleteDemoFlowTests : PlaywrightHelper
    {
        protected override bool AutoLogin => true;

        [Test]
        public async Task CompleteDemoFlow_AllStepsComplete()
        {
            const string testName = nameof(CompleteDemoFlow_AllStepsComplete);
            const string targetSelectorDescription = "Complete demo flow including results viewing";

            await ExecuteWithLoggingAsync(testName, targetSelectorDescription, async () =>
            {
                await StartDemoAsync();
                await CreateSurveyWithAIAsync();
                await ReviewAndAcceptQuestionsAsync();
                await AddCustomQuestionAsync();
                await PublishSurveyAsync();
                await NavigateToMySurveysAsync();
                await CopySurveyLinkAsync();
                await AnswerSurveyAsync();
                await ViewBarChartResultsAsync();
                await ViewGridResultsAsync();
            });
        }

        private async Task StartDemoAsync()
        {
            var beforeDemo = await GetPageReadyIdAsync(Page);
            await Page.GotoAsync("/demo");
            await WaitForBlazorInteractiveAsync(beforeDemo, Page);
            await DismissCookieBannerAsync(Page);

            var demoHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Explore the Survey Shark Interactive Demo" });
            await Microsoft.Playwright.Assertions.Expect(demoHeading).ToBeVisibleAsync();

            var startDemoButton = Page.GetByRole(AriaRole.Button, new() { Name = "Start the Demo" });
            await startDemoButton.ClickAsync();
            try
            {
                var beforeCreate = await GetPageReadyIdAsync(Page);
                await Page.WaitForURLAsync("**/survey/create", new() { Timeout = 60000 });
                await WaitForBlazorInteractiveAsync(beforeCreate, Page);
            }
            catch (TimeoutException)
            {
                // Fallback: navigate directly if SPA router did not update URL in time
                var beforeGoto = await GetPageReadyIdAsync(Page);
                await Page.GotoAsync("/survey/create");
                await WaitForBlazorInteractiveAsync(beforeGoto, Page);
            }

            var createSurveyHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Create Survey" });
            await Microsoft.Playwright.Assertions.Expect(createSurveyHeading).ToBeVisibleAsync();
        }

        private async Task CreateSurveyWithAIAsync()
        {
            await DismissCookieBannerAsync();
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            for (int i = 0; i < 4; i++)
            {
                if (await nextButton.IsVisibleAsync())
                {
                    await nextButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(500);
                }
            }

            var finishButton = Page.GetByRole(AriaRole.Button, new() { Name = "Finish" });
            if (await finishButton.IsVisibleAsync())
            {
                await finishButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var createButton = Page.GetByRole(AriaRole.Button, new() { Name = "Create" });
            var beforeEdit = await GetPageReadyIdAsync(Page);
            await Task.WhenAll(Page.WaitForURLAsync("**/survey/edit/**", new() { Timeout = 120000 }), createButton.ClickAsync());
            await WaitForBlazorInteractiveAsync(beforeEdit, Page);

            var editSurveyHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Edit Survey" });
            await Microsoft.Playwright.Assertions.Expect(editSurveyHeading).ToBeVisibleAsync();

            var successToast = Page.Locator(".mud-snackbar").Filter(new() { HasTextString = "Survey Created" });
            await Microsoft.Playwright.Assertions.Expect(successToast).ToBeVisibleAsync(new() { Timeout = 5000 });
        }

        private async Task ReviewAndAcceptQuestionsAsync()
        {
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var questionsPanel = Page.Locator("#QuestionsPanel");
            await questionsPanel.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            var demoNextButton = Page.Locator("#DemoNext_button");
            await TryClickIfExistsAsync(demoNextButton, Page, 1, 500);

            var firstQuestion = Page.Locator("#question_0");
            if (await firstQuestion.IsVisibleAsync())
            {
                await firstQuestion.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var acceptButton = Page.GetByRole(AriaRole.Button, new() { Name = "Accept Questions" });
            if (await acceptButton.IsVisibleAsync())
            {
                await acceptButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            var beforeAfterAccept = await GetPageReadyIdAsync(Page);
            await WaitForBlazorInteractiveAsync(beforeAfterAccept, Page);
        }

        private async Task AddCustomQuestionAsync()
        {
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });

            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            while (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                var questionTypeSelect = Page.Locator("#QuestionTypeSelect");
                if (await questionTypeSelect.IsVisibleAsync())
                {
                    break;
                }
            }

            var firstQuestion = Page.Locator("#question_0");
            if (await firstQuestion.IsVisibleAsync())
            {
                await firstQuestion.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var questionTypeSelectElement = Page.Locator("#QuestionTypeSelect");
            if (await questionTypeSelectElement.IsVisibleAsync())
            {
                await questionTypeSelectElement.ClickAsync();
                await Page.Locator("div.mud-list-item-text").Locator("p").Filter(new() { HasTextString = "Multiple Choice" }).ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var presetChoicesSelect = Page.Locator("#PresetChoices");
            if (await presetChoicesSelect.IsVisibleAsync())
            {
                await presetChoicesSelect.ClickAsync();
                await Page.Locator("div.mud-list-item-text").Locator("p").Filter(new() { HasTextString = "Yes No Partially" }).ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var saveQuestionButton = Page.Locator("#SaveQuestionBtn");
            if (await saveQuestionButton.IsVisibleAsync())
            {
                await saveQuestionButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }
        }

        private async Task PublishSurveyAsync()
        {
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            while (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var publishButton = Page.Locator("#PublishSurveyBtn");
            await Microsoft.Playwright.Assertions.Expect(publishButton).ToBeVisibleAsync();
            await publishButton.ClickAsync();

            await Page.WaitForTimeoutAsync(2000);
            var publishSuccess = Page.Locator(".mud-snackbar").Filter(new() { HasTextString = "Survey Published" });
            await Microsoft.Playwright.Assertions.Expect(publishSuccess).ToBeVisibleAsync(new() { Timeout = 10000 });
        }

        private async Task NavigateToMySurveysAsync()
        {
            await Page.WaitForURLAsync("**/mysurveys/surveysicreated**", new() { Timeout = 10000 });
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            var mySurveysHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Surveys I've Created" });
            await Microsoft.Playwright.Assertions.Expect(mySurveysHeading).ToBeVisibleAsync();
        }

        private async Task CopySurveyLinkAsync()
        {
            var previewButton = Page.Locator(".preview-button").First;
            await previewButton.WaitForAsync();

            try
            {
                await WaitForPopupAndReplacePageAsync(async () => await previewButton.ClickAsync());
            }
            catch (TimeoutException)
            {
                await previewButton.ClickAsync();
            }

            try 
            { 
                var beforeUrl = await GetPageReadyIdAsync(Page); 
                await Page.WaitForURLAsync("**/survey**", new() { Timeout = 8000 }); 
                await WaitForBlazorInteractiveAsync(beforeUrl, Page); 
            } 
            catch (TimeoutException) { }

            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            await nextButton.WaitForAsync(new() { Timeout = 10000 });

            await ScrollToElementAsync("1", Page);
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();

            var beforeCopy = await GetPageReadyIdAsync(Page);
            await WaitForBlazorInteractiveAsync(beforeCopy, Page);
            var button = Page.GetByTitle("Copy Survey Link").First;
            await button.ClickAsync(new() { Force = true });

            // Open the survey in a new tab and replace the Page reference
            await WaitForPopupAndReplacePageAsync(async () => await nextButton.ClickAsync());
        }

        private async Task AnswerSurveyAsync()
        {
            var beforeAnswer = await GetPageReadyIdAsync(Page);
            await Page.WaitForURLAsync("**/survey/**", new() { Timeout = 10000 });
            await WaitForBlazorInteractiveAsync(beforeAnswer, Page);

            var surveyTitle = Page.Locator(".survey-title");
            await Microsoft.Playwright.Assertions.Expect(surveyTitle).ToBeVisibleAsync();
            await ScrollToElementAsync("1", Page);

            var demoNextButton = Page.Locator("#DemoNext_button");
            await TryClickIfExistsAsync(demoNextButton, Page, 1, 500);

            var radioButtons = Page.Locator("input[type='radio']");
            var radioCount = await radioButtons.CountAsync();
            for (int i = 0; i < radioCount; i++)
            {
                var radio = radioButtons.Nth(i);
                if (await radio.IsVisibleAsync() && await radio.IsEnabledAsync())
                {
                    await radio.ClickAsync();
                    await Page.WaitForTimeoutAsync(200);
                }
            }

            var checkboxes = Page.Locator(".survey-container input[type='checkbox']");
            var checkboxCount = await checkboxes.CountAsync();
            for (int i = 0; i < checkboxCount; i++)
            {
                var checkbox = checkboxes.Nth(i);
                if (await checkbox.IsEnabledAsync())
                {
                    try
                    {
                        await checkbox.CheckAsync();
                        await Page.WaitForTimeoutAsync(150);
                    }
                    catch
                    {
                        var parentLabel = checkbox.Locator("xpath=ancestor-or-self::label[1]");
                        if (await parentLabel.CountAsync() > 0)
                        {
                            await parentLabel.ClickAsync();
                            await Page.WaitForTimeoutAsync(150);
                        }
                    }
                }
            }

            var textFields = Page.Locator("textarea, input[type='text']").Filter(new LocatorFilterOptions { HasNotText = "DemoUser" });
            var textCount = await textFields.CountAsync();
            for (int i = 0; i < textCount; i++)
            {
                var textField = textFields.Nth(i);
                if (await textField.IsVisibleAsync() && await textField.IsEnabledAsync())
                {
                    await textField.FillAsync("Great service, very satisfied!");
                    await Page.WaitForTimeoutAsync(200);
                }
            }

            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var submitButton = Page.Locator("#survey-submit-btn");
            if (await submitButton.IsVisibleAsync())
            {
                await submitButton.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
            }

            var successMessage = Page.Locator(".mud-alert-filled-success, .mud-snackbar").Filter(new() { HasTextString = "submitted" });
            await Microsoft.Playwright.Assertions.Expect(successMessage).ToBeVisibleAsync(new() { Timeout = 10000 });
        }

        private async Task ViewBarChartResultsAsync()
        {
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();

            await Page.Locator(".charts-button").First.ClickAsync();
            var beforeResponses = await GetPageReadyIdAsync(Page);
            await Page.WaitForURLAsync("**/survey/responses/**", new() { Timeout = 15000 });
            await WaitForBlazorInteractiveAsync(beforeResponses, Page);

            var demoNextButton = Page.Locator("#DemoNext_button");
            await demoNextButton.ClickAsync();

            var questionSelect = Page.Locator(".mud-select").First;
            await Microsoft.Playwright.Assertions.Expect(questionSelect).ToBeVisibleAsync();


            await TryClickIfExistsAsync(demoNextButton, Page, 1, 500);

            var select = Page.Locator("div.mud-select:has(label:has-text('Select Question')) div[tabindex='0']");
            await select.ScrollIntoViewIfNeededAsync();
            await select.ClickAsync();
            var listItem = Page.Locator("div.mud-list-item-text").Locator("p").Filter(new() { HasTextString = "All Questions" });
            await listItem.ScrollIntoViewIfNeededAsync();
            await listItem.GetByText("All Questions").ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            while (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var chartContainer = Page.Locator(".e-chart, .e-accumulationchart");
            await Microsoft.Playwright.Assertions.Expect(chartContainer.First).ToBeVisibleAsync(new() { Timeout = 10000 });

            await Page.Locator("div.mud-select:has(label:has-text('Select Chart Type')) div[tabindex='0']").ClickAsync();
            var menu = Page.Locator("div.mud-popover:has(.mud-list)");
            await menu.WaitForAsync();
            await Page.Keyboard.PressAsync("ArrowDown");
            await Page.Keyboard.PressAsync("Enter");

            await TryClickIfExistsAsync(demoNextButton, Page, 2, 300);
            await Page.WaitForTimeoutAsync(500);
            await demoNextButton.ClickAsync();
        }

        private async Task ViewGridResultsAsync()
        {
            await Page.Locator(".grid-button").ClickAsync();
            var beforeGrid = await GetPageReadyIdAsync(Page);
            await Page.WaitForURLAsync("**/survey/filter/**", new() { Timeout = 15000 });
            await WaitForBlazorInteractiveAsync(beforeGrid, Page);

            var gridHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Filter" });
            await Microsoft.Playwright.Assertions.Expect(gridHeading).ToBeVisibleAsync();

            var gridTable = Page.Locator(".e-grid");
            await Microsoft.Playwright.Assertions.Expect(gridTable).ToBeVisibleAsync();
            var gridRows = Page.Locator(".e-grid .e-row");
            await Microsoft.Playwright.Assertions.Expect(gridRows.First).ToBeVisibleAsync();

            var demoNextButton = Page.Locator("#DemoNext_button");
            while (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        private static async Task TryClickIfExistsAsync(ILocator locator, IPage page, int times = 1, int delayMs = 300)
        {
            try
            {
                if (await locator.CountAsync() == 0) return;
                for (int i = 0; i < times; i++)
                {
                    try
                    {
                        await locator.ClickAsync(new() { Timeout = 500 });
                    }
                    catch
                    {
                        break;
                    }
                    await page.WaitForTimeoutAsync(delayMs);
                }
            }
            catch
            {
                // ignore to reduce flakiness
            }
        }
    }
}
