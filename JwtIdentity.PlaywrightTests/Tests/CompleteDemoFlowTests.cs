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
                await GenerateAnalysisAsync();
                await ViewAnalysisAsync();
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

            await TryClickDemoNextButtonAsync();

            var firstQuestion = Page.Locator("#question_0");
            if (await firstQuestion.IsVisibleAsync())
            {
                await firstQuestion.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
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

            var questionTypeSelect = Page.Locator("#QuestionTypeSelect");
            while (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
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

            if (await questionTypeSelect.IsVisibleAsync())
            {
                await questionTypeSelect.ClickAsync();
                var multipleChoiceOption = Page.Locator("div.mud-list-item-text").Locator("p").Filter(new() { HasTextString = "Multiple Choice" });
                await multipleChoiceOption.ClickAsync();
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
                var yesNoPartiallyOption = Page.Locator("div.mud-list-item-text").Locator("p").Filter(new() { HasTextString = "Yes No Partially" });
                await yesNoPartiallyOption.ClickAsync();
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
            var copySurveyLinkButton = Page.GetByTitle("Copy Survey Link").First;
            await copySurveyLinkButton.ClickAsync(new() { Force = true });

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

            await TryClickDemoNextButtonAsync();

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

            var chartsButton = Page.Locator(".charts-button").First;
            await chartsButton.ClickAsync();
            var beforeResponses = await GetPageReadyIdAsync(Page);
            await Page.WaitForURLAsync("**/survey/responses/**", new() { Timeout = 15000 });
            await WaitForBlazorInteractiveAsync(beforeResponses, Page);

            await TryClickDemoNextButtonAsync();

            var questionSelect = Page.Locator(".mud-select").First;
            await Microsoft.Playwright.Assertions.Expect(questionSelect).ToBeVisibleAsync();

            await TryClickDemoNextButtonAsync();

            var questionSelectDropdown = Page.Locator("div.mud-select:has(label:has-text('Select Question')) div[tabindex='0']");
            await questionSelectDropdown.ScrollIntoViewIfNeededAsync();
            await questionSelectDropdown.ClickAsync();
            var allQuestionsOption = Page.Locator("div.mud-list-item-text").Locator("p").Filter(new() { HasTextString = "All Questions" });
            await allQuestionsOption.WaitForAsync();
            await allQuestionsOption.ScrollIntoViewIfNeededAsync();
            await allQuestionsOption.GetByText("All Questions").ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            await ClickAllVisibleDemoNextButtonsAsync();

            var chartContainer = Page.Locator(".e-chart, .e-accumulationchart");
            await Microsoft.Playwright.Assertions.Expect(chartContainer.First).ToBeVisibleAsync(new() { Timeout = 10000 });

            var chartTypeSelectDropdown = Page.Locator("div.mud-select:has(label:has-text('Select Chart Type')) div[tabindex='0']");
            await chartTypeSelectDropdown.ClickAsync();
            var menu = Page.Locator("div.mud-popover:has(.mud-list)");
            await menu.WaitForAsync();
            await Page.Keyboard.PressAsync("ArrowDown");
            await Page.Keyboard.PressAsync("Enter");

            await TryClickDemoNextButtonAsync(2, 300);
            await Page.WaitForTimeoutAsync(500);
            await TryClickDemoNextButtonAsync();
        }

        private async Task ViewGridResultsAsync()
        {
            var gridButton = Page.Locator(".grid-button");
            await gridButton.ClickAsync();
            var beforeGrid = await GetPageReadyIdAsync(Page);
            await Page.WaitForURLAsync("**/survey/filter/**", new() { Timeout = 15000 });
            await WaitForBlazorInteractiveAsync(beforeGrid, Page);

            var gridHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Filter" });
            await Microsoft.Playwright.Assertions.Expect(gridHeading).ToBeVisibleAsync();

            var gridTable = Page.Locator(".e-grid");
            await Microsoft.Playwright.Assertions.Expect(gridTable).ToBeVisibleAsync();
            var gridRows = Page.Locator(".e-grid .e-row");
            await Microsoft.Playwright.Assertions.Expect(gridRows.First).ToBeVisibleAsync();

            await ClickAllVisibleDemoNextButtonsAsync();
        }

        private async Task GenerateAnalysisAsync()
        {
            // Should be back on Surveys I Created page at step 8
            await Page.WaitForURLAsync("**/mysurveys/surveysicreated**", new() { Timeout = 10000 });
            var beforeAnalysis = await GetPageReadyIdAsync(Page);
            await WaitForBlazorInteractiveAsync(beforeAnalysis, Page);

            var mySurveysHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Surveys I've Created" });
            await Microsoft.Playwright.Assertions.Expect(mySurveysHeading).ToBeVisibleAsync();

            // Click Generate Analysis button
            var generateAnalysisButton = Page.GetByRole(AriaRole.Button, new() { Name = "Generate Analysis" });
            await generateAnalysisButton.ClickAsync();

            // Wait for the "Generating analysis" snackbar
            var generatingToast = Page.Locator(".mud-snackbar").Filter(new() { HasTextString = "Generating analysis" });
            await Microsoft.Playwright.Assertions.Expect(generatingToast).ToBeVisibleAsync(new() { Timeout = 5000 });

            // Wait for the "Analysis generated successfully!" snackbar
            var successToast = Page.Locator(".mud-snackbar").Filter(new() { HasTextString = "Analysis generated successfully!" });
            await Microsoft.Playwright.Assertions.Expect(successToast).ToBeVisibleAsync(new() { Timeout = 180000 });

            // The demo should auto-advance to step 9 when analysis completes
            await Page.WaitForTimeoutAsync(1000);
        }

        private async Task ViewAnalysisAsync()
        {
            // Click View Analysis button
            var viewAnalysisButton = Page.GetByRole(AriaRole.Button, new() { Name = "View Analysis" });
            await viewAnalysisButton.ClickAsync();

            var beforeAnalysisPage = await GetPageReadyIdAsync(Page);
            await Page.WaitForURLAsync("**/survey/analysis/**", new() { Timeout = 15000 });
            await WaitForBlazorInteractiveAsync(beforeAnalysisPage, Page);

            var analysisHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Survey Analysis" });
            await Microsoft.Playwright.Assertions.Expect(analysisHeading).ToBeVisibleAsync();

            // Wait for demo popup to appear
            await Page.WaitForTimeoutAsync(1000);

            // Click Next to go to register page
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            await nextButton.ClickAsync();

            var beforeRegister = await GetPageReadyIdAsync(Page);
            await Page.WaitForURLAsync("**/register", new() { Timeout = 10000 });
            await WaitForBlazorInteractiveAsync(beforeRegister, Page);

            var registerHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Register" });
            await Microsoft.Playwright.Assertions.Expect(registerHeading).ToBeVisibleAsync();
        }

        /// <summary>
        /// Attempts to click the demo next button if it exists and is visible.
        /// </summary>
        /// <param name="times">Number of times to attempt clicking (default: 1)</param>
        /// <param name="delayMs">Delay in milliseconds between clicks (default: 500)</param>
        private async Task TryClickDemoNextButtonAsync(int times = 1, int delayMs = 500)
        {
            var demoNextButton = Page.Locator("#DemoNext_button");
            await TryClickIfExistsAsync(demoNextButton, times, delayMs);
        }

        /// <summary>
        /// Clicks all visible demo next buttons in a loop until none are visible.
        /// </summary>
        private async Task ClickAllVisibleDemoNextButtonsAsync()
        {
            var demoNextButton = Page.Locator("#DemoNext_button");
            while (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        /// <summary>
        /// Generic helper to click a locator multiple times if it exists, with error handling to reduce flakiness.
        /// </summary>
        private static async Task TryClickIfExistsAsync(ILocator locator, int times = 1, int delayMs = 300)
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
                    await locator.Page.WaitForTimeoutAsync(delayMs);
                }
            }
            catch
            {
                // ignore to reduce flakiness
            }
        }
    }
}
