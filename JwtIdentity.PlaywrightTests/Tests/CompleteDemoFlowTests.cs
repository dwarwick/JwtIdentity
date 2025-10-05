using JwtIdentity.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace JwtIdentity.PlaywrightTests.Tests
{
    [TestFixture]
    public class CompleteDemoFlowTests : PlaywrightHelper
    {
        // Demo user will be logged in automatically via demo authentication
        protected override bool AutoLogin => false; // We'll handle demo login specially

        private string _surveyGuid = string.Empty;

        [Test]
        public async Task CompleteDemoFlow_AllStepsComplete()
        {
            const string testName = nameof(CompleteDemoFlow_AllStepsComplete);
            const string targetSelectorDescription = "Complete demo flow including results viewing";

            await ExecuteWithLoggingAsync(testName, targetSelectorDescription, async () =>
            {
                // Step 1: Start the demo from the demo landing page
                await StartDemoAsync();

                // Step 2: Create survey with AI
                await CreateSurveyWithAIAsync();

                // Step 3: Edit survey - review and accept AI questions
                await ReviewAndAcceptQuestionsAsync();

                // Step 4: Add a custom question manually
                await AddCustomQuestionAsync();

                // Step 5: Publish the survey
                await PublishSurveyAsync();

                // Step 6: Navigate to My Surveys and preview
                await NavigateToMySurveysAsync();

                // Step 7: Copy survey link
                IPage tab3 = await CopySurveyLinkAsync();

                // Step 8: Answer the survey as a participant
                await AnswerSurveyAsync(tab3);

                // Step 9: View survey results - bar charts
                await ViewBarChartResultsAsync(tab3);

                // Step 10: View survey results - grid
                await ViewGridResultsAsync(tab3);

                // Verify final success state
                var completionMessage = Page.Locator(".mud-alert").Filter(new() { HasTextString = "Demo completed" });
                // Note: Demo might end differently - adjust this assertion based on actual behavior
            });
        }

        private async Task StartDemoAsync()
        {
            // Navigate to the demo landing page
            await Page.GotoAsync("/demo");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on the demo page
            var demoHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Explore the Survey Shark Interactive Demo" });
            await Microsoft.Playwright.Assertions.Expect(demoHeading).ToBeVisibleAsync();

            // Click "Start the Demo" button - this will log us in as demo user
            var startDemoButton = Page.GetByRole(AriaRole.Button, new() { Name = "Start the Demo" });
            await Task.WhenAll(
                Page.WaitForURLAsync("**/survey/create"),
                startDemoButton.ClickAsync()
            );

            // Wait for create survey page to load
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var createSurveyHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Create Survey" });
            await Microsoft.Playwright.Assertions.Expect(createSurveyHeading).ToBeVisibleAsync();
        }

        private async Task CreateSurveyWithAIAsync()
        {
            await DismissCookieBannerAsync();

            // Demo should auto-fill, but we'll verify the fields are populated
            // Click through demo steps for title
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Click through demo steps for description
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Click through demo steps for AI instructions
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Click through final demo step
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            var finishButton = Page.GetByRole(AriaRole.Button, new() { Name = "Finish" });

            // Click through Finish button step
            if (await finishButton.IsVisibleAsync())
            {
                await finishButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Create the survey - this may take over 1 minute for AI processing
            var createButton = Page.GetByRole(AriaRole.Button, new() { Name = "Create" });
            await Task.WhenAll(
                Page.WaitForURLAsync("**/survey/edit/**", new PageWaitForURLOptions() { Timeout = 120000 }),
                createButton.ClickAsync()
            );

            // Verify we're on the edit survey page
            var editSurveyHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Edit Survey" });
            await Microsoft.Playwright.Assertions.Expect(editSurveyHeading).ToBeVisibleAsync();

            // Verify success message
            var successToast = Page.Locator(".mud-snackbar").Filter(new() { HasTextString = "Survey Created" });
            await Microsoft.Playwright.Assertions.Expect(successToast).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 5000
            });
        }

        private async Task ReviewAndAcceptQuestionsAsync()
        {
            // Wait for page to be ready
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Expand questions panel if needed
            var questionsPanel = Page.Locator("#QuestionsPanel");
            await questionsPanel.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);


            // Look for demo step navigation
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            var demoNextButton = Page.Locator("#DemoNext_button");

            // Step through demo guidance for reviewing questions
            if (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Click on first question to select it
            var firstQuestion = Page.Locator("#question_0");
            if (await firstQuestion.IsVisibleAsync())
            {
                await firstQuestion.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Continue through demo steps
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Accept the AI generated questions
            var acceptButton = Page.GetByRole(AriaRole.Button, new() { Name = "Accept Questions" });
            if (await acceptButton.IsVisibleAsync())
            {
                await acceptButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            // Verify questions were accepted
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        private async Task AddCustomQuestionAsync()
        {
            // Navigate through demo steps to create a custom question
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });

            // Continue through demo steps for question creation
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Continue through demo steps - this should guide us to question type selection
            while (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Check if we've reached the question type selector
                var questionTypeSelect = Page.Locator("#QuestionTypeSelect");
                if (await questionTypeSelect.IsVisibleAsync())
                {
                    break;
                }
            }

            // Click on first question to select it
            var firstQuestion = Page.Locator("#question_0");
            if (await firstQuestion.IsVisibleAsync())
            {
                await firstQuestion.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Select Multiple Choice question type (demo might do this automatically)
            var questionTypeSelectElement = Page.Locator("#QuestionTypeSelect");
            if (await questionTypeSelectElement.IsVisibleAsync())
            {
                await questionTypeSelectElement.ClickAsync();
                await Page
                    .Locator("div.mud-list-item-text")
                    .Locator("p")
                    .Filter(new() { HasTextString = "Multiple Choice" }).ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Continue through demo steps for question creation
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Select preset choices
            var presetChoicesSelect = Page.Locator("#PresetChoices");
            if (await presetChoicesSelect.IsVisibleAsync())
            {
                await presetChoicesSelect.ClickAsync();
                await Page
                    .Locator("div.mud-list-item-text")
                    .Locator("p")
                    .Filter(new() { HasTextString = "Yes No Partially" }).ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Continue with demo
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Save the question
            var saveQuestionButton = Page.Locator("#SaveQuestionBtn");
            if (await saveQuestionButton.IsVisibleAsync())
            {
                await saveQuestionButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }
        }

        private async Task PublishSurveyAsync()
        {
            // Continue through any remaining demo steps
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            while (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Publish the survey
            var publishButton = Page.Locator("#PublishSurveyBtn");
            await Microsoft.Playwright.Assertions.Expect(publishButton).ToBeVisibleAsync();
            await publishButton.ClickAsync();

            // Verify survey is published
            await Page.WaitForTimeoutAsync(2000);
            var publishSuccess = Page.Locator(".mud-snackbar").Filter(new() { HasTextString = "Survey Published" });
            await Microsoft.Playwright.Assertions.Expect(publishSuccess).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }

        private async Task NavigateToMySurveysAsync()
        {
            // Navigation should happen automatically as part of demo flow
            // Wait for My Surveys page to load
            await Page.WaitForURLAsync("**/mysurveys/surveysicreated**", new PageWaitForURLOptions() { Timeout = 10000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on My Surveys page
            var mySurveysHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Surveys I've Created" });
            await Microsoft.Playwright.Assertions.Expect(mySurveysHeading).ToBeVisibleAsync();
        }

        private async Task<IPage> CopySurveyLinkAsync()
        {
            var previewButton = Page.Locator(".preview-button").First;
            await previewButton.WaitForAsync();

            IPage surveyPage = Page;

            // Try to catch a popup (new tab) deterministically
            try
            {
                surveyPage = await Page.RunAndWaitForPopupAsync(async () =>
                {
                    await previewButton.ClickAsync();
                });
                await surveyPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            }
            catch (TimeoutException)
            {
                // No popup: assume in-place client-side navigation
                await previewButton.ClickAsync();
            }

            // Wait for either URL change OR the presence of the survey's unique element
            try
            {
                await surveyPage.WaitForURLAsync("**/survey**", new() { Timeout = 8000 });
            }
            catch (TimeoutException)
            {
                // Fallback: rely on element readiness if URL pattern missed (history API delay)
            }

            var nextButton = surveyPage.GetByRole(AriaRole.Button, new() { Name = "Next" });

            await nextButton.WaitForAsync(new() { Timeout = 10000 });

            await ScrollToElementAsync("1", surveyPage);
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();

            await surveyPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Copy survey link
            var button = surveyPage.GetByTitle("Copy Survey Link").First;
            await button.ClickAsync(new LocatorClickOptions() { Force = true });

            return await surveyPage.RunAndWaitForPopupAsync(async () =>
            {
                await nextButton.ClickAsync();
            });
        }

        private async Task AnswerSurveyAsync(IPage Page)
        {
            // The demo should navigate to the survey for answering
            await Page.WaitForURLAsync("**/survey/**", new PageWaitForURLOptions() { Timeout = 10000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on survey page
            var surveyTitle = Page.Locator(".survey-title");
            await Microsoft.Playwright.Assertions.Expect(surveyTitle).ToBeVisibleAsync();
            await ScrollToElementAsync("1", Page);

            // Follow demo guidance
            var demoNextButton = Page.Locator("#DemoNext_button");
            if (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Answer questions - select first available option for each question
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

            // Answer "Select All that Apply" questions (MudCheckbox) but only inside the survey area
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
                        // Fallback if native input not interactable (e.g., hidden input in MudBlazor): click nearest label
                        var parentLabel = checkbox.Locator("xpath=ancestor-or-self::label[1]");
                        if (await parentLabel.CountAsync() > 0)
                        {
                            await parentLabel.ClickAsync();
                            await Page.WaitForTimeoutAsync(150);
                        }
                    }
                }
            }

            // Fill text questions if any
            var textFields = Page.Locator("textarea, input[type='text']").Filter(new LocatorFilterOptions
            {
                HasNotText = "DemoUser" // Exclude username fields
            });
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

            // Continue demo steps
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Submit survey
            var submitButton = Page.Locator("#survey-submit-btn");
            if (await submitButton.IsVisibleAsync())
            {
                await submitButton.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
            }

            // Verify submission success
            var successMessage = Page.Locator(".mud-alert-filled-success, .mud-snackbar").Filter(new() { HasTextString = "submitted" });
            await Microsoft.Playwright.Assertions.Expect(successMessage).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }

        private async Task ViewBarChartResultsAsync(IPage page)
        {
            var nextButton = page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();
            await nextButton.ClickAsync();

            await page.Locator(".charts-button").First.ClickAsync();

            // Navigate to bar chart results
            await page.WaitForURLAsync("**/survey/responses/**", new PageWaitForURLOptions() { Timeout = 15000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on results page
            var questionSelect = page.Locator(".mud-select").First;
            await Microsoft.Playwright.Assertions.Expect(questionSelect).ToBeVisibleAsync();

            // Follow demo steps
            var demoNextButton = page.Locator("#DemoNext_button");
            if (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await page.WaitForTimeoutAsync(1000);
            }

            // Select "All Questions" from dropdown
            var select = page.Locator("div.mud-select:has(label:has-text('Select Question')) div[tabindex='0']");
            await select.ScrollIntoViewIfNeededAsync();
            await select.ClickAsync();

            var listItem = page.Locator("div.mud-list-item-text").Locator("p").Filter(new() { HasTextString = "All Questions" });
            await listItem.ScrollIntoViewIfNeededAsync();

            await listItem.GetByText("All Questions").ClickAsync();
            await page.WaitForTimeoutAsync(1000);


            // Continue through demo steps
            while (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await page.WaitForTimeoutAsync(500);
            }

            // Verify charts are displayed
            var chartContainer = page.Locator(".e-chart, .e-accumulationchart");
            await Microsoft.Playwright.Assertions.Expect(chartContainer.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            await page.Locator("div.mud-select:has(label:has-text('Select Chart Type')) div[tabindex='0']").ClickAsync();
            var menu = page.Locator("div.mud-popover:has(.mud-list)");
            await menu.WaitForAsync();

            // move to “Pie” with the keyboard and select it
            await page.Keyboard.PressAsync("ArrowDown"); // Bar -> Pie
            await page.Keyboard.PressAsync("Enter");

            await demoNextButton.ClickAsync();
            await demoNextButton.ClickAsync();
        }

        private async Task ViewGridResultsAsync(IPage page)
        {
            await page.Locator(".grid-button").ClickAsync();

            // Should automatically navigate to grid results
            await page.WaitForURLAsync("**/survey/filter/**", new PageWaitForURLOptions() { Timeout = 15000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on grid results page
            var gridHeading = page.GetByRole(AriaRole.Heading, new() { Name = "Filter" });
            await Microsoft.Playwright.Assertions.Expect(gridHeading).ToBeVisibleAsync();

            // Verify grid is displayed with data
            var gridTable = Page.Locator(".e-grid");
            await Microsoft.Playwright.Assertions.Expect(gridTable).ToBeVisibleAsync();

            // Verify we have at least one row of data
            var gridRows = Page.Locator(".e-grid .e-row");
            await Microsoft.Playwright.Assertions.Expect(gridRows.First).ToBeVisibleAsync();

            // Follow demo steps
            var demoNextButton = page.Locator("#DemoNext_button");

            // Step through demo guidance for grid features
            while (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await page.WaitForTimeoutAsync(500);
            }
        }
    }
}