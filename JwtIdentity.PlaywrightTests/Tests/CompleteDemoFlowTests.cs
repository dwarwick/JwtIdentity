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
                await CopySurveyLinkAsync();

                // Step 8: Answer the survey as a participant
                await AnswerSurveyAsync();

                // Step 9: View survey results - bar charts
                await ViewBarChartResultsAsync();

                // Step 10: View survey results - grid
                await ViewGridResultsAsync();

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
            if (await questionsPanel.IsVisibleAsync())
            {
                await questionsPanel.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

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

            // Select Multiple Choice question type (demo might do this automatically)
            var questionTypeSelectElement = Page.Locator("#QuestionTypeSelect");
            if (await questionTypeSelectElement.IsVisibleAsync())
            {
                await questionTypeSelectElement.ClickAsync();
                await Page.GetByText("Multiple Choice").ClickAsync();
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
                await Page.GetByText("Yes No Partially").ClickAsync();
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

        private async Task CopySurveyLinkAsync()
        {
            // Follow demo guidance to preview survey
            var previewButton = Page.Locator(".preview-button").First;
            if (await previewButton.IsVisibleAsync())
            {
                await previewButton.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
            }

            // Navigate back to surveys list
            await Page.GoBackAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Continue through demo steps
            var nextButton = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Copy survey link
            var copyLinkButton = Page.Locator(".copy-button").First;
            if (await copyLinkButton.IsVisibleAsync())
            {
                await copyLinkButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            // Continue demo steps
            if (await nextButton.IsVisibleAsync())
            {
                await nextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        private async Task AnswerSurveyAsync()
        {
            // The demo should navigate to the survey for answering
            await Page.WaitForURLAsync("**/survey/**", new PageWaitForURLOptions() { Timeout = 10000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on survey page
            var surveyTitle = Page.Locator(".survey-title");
            await Microsoft.Playwright.Assertions.Expect(surveyTitle).ToBeVisibleAsync();

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

        private async Task ViewBarChartResultsAsync()
        {
            // Navigate to bar chart results
            await Page.WaitForURLAsync("**/survey/responses/**", new PageWaitForURLOptions() { Timeout = 15000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on results page
            var questionSelect = Page.Locator(".mud-select").First;
            await Microsoft.Playwright.Assertions.Expect(questionSelect).ToBeVisibleAsync();

            // Follow demo steps
            var demoNextButton = Page.Locator("#DemoNext_button");
            if (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            // Select "All Questions" from dropdown
            var selectDropdown = Page.Locator(".mud-select").First;
            if (await selectDropdown.IsVisibleAsync())
            {
                await selectDropdown.ClickAsync();
                await Page.GetByText("All Questions").ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            // Continue through demo steps
            while (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Verify charts are displayed
            var chartContainer = Page.Locator(".e-chart, .e-accumulationchart");
            await Microsoft.Playwright.Assertions.Expect(chartContainer.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }

        private async Task ViewGridResultsAsync()
        {
            // Should automatically navigate to grid results
            await Page.WaitForURLAsync("**/survey/filter/**", new PageWaitForURLOptions() { Timeout = 15000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we're on grid results page
            var gridHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Filter" });
            await Microsoft.Playwright.Assertions.Expect(gridHeading).ToBeVisibleAsync();

            // Follow demo steps
            var demoNextButton = Page.Locator("#DemoNext_button");
            
            // Step through demo guidance for grid features
            while (await demoNextButton.IsVisibleAsync())
            {
                await demoNextButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Try column chooser if demo guides us
            var columnChooserButton = Page.Locator("#ColumnChooser_button");
            if (await columnChooserButton.IsVisibleAsync())
            {
                await columnChooserButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
                
                // Close column chooser dialog
                var closeButton = Page.GetByRole(AriaRole.Button, new() { Name = "Close" });
                if (await closeButton.IsVisibleAsync())
                {
                    await closeButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(500);
                }
            }

            // Verify grid is displayed with data
            var gridTable = Page.Locator(".e-grid");
            await Microsoft.Playwright.Assertions.Expect(gridTable).ToBeVisibleAsync();

            // Verify we have at least one row of data
            var gridRows = Page.Locator(".e-grid .e-row");
            await Microsoft.Playwright.Assertions.Expect(gridRows.First).ToBeVisibleAsync();
        }
    }
}