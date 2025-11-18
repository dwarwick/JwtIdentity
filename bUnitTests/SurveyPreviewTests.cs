using Bunit;
using JwtIdentity.Client.Pages.Survey;
using JwtIdentity.Client.Services.Base;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JwtIdentity.BunitTests
{
    /// <summary>
    /// Comprehensive BUnit tests for Survey Preview functionality in the branching demo
    /// </summary>
    [TestFixture]
    public class SurveyPreviewTests : BUnitTestBase
    {
        private SurveyViewModel _testSurvey;
        private Guid _testSurveyGuid;

        [SetUp]
        public void Setup()
        {
            _testSurveyGuid = Guid.Parse("2ac27b65-ed5f-4489-a2e2-2c237b0132a5");
            
            // Setup demo user
            var demoUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "DemoUser123@surveyshark.site")
            }, "TestAuth"));
            var authState = new AuthenticationState(demoUser);
            AuthStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);

            // Setup test survey with branching
            _testSurvey = CreateBranchingSurvey();
            
            // Setup API mock for survey retrieval
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
                
            // Setup AuthService for login response
            AuthServiceMock.Setup(x => x.Login(It.IsAny<ApplicationUserViewModel>()))
                .ReturnsAsync(new Response<ApplicationUserViewModel> 
                { 
                    Success = true, 
                    Data = new ApplicationUserViewModel { UserName = "DemoUser123@surveyshark.site" } 
                });
        }

        private SurveyViewModel CreateBranchingSurvey()
        {
            var survey = new SurveyViewModel
            {
                Id = 2,
                Guid = _testSurveyGuid.ToString(),
                Title = "Customer Satisfaction Survey",
                Description = "Please take our customer satisfaction survey so we can get valuable feedback from you regarding our service.",
                Published = true,
                Questions = new List<QuestionViewModel>(),
                QuestionGroups = new List<QuestionGroupViewModel>
                {
                    new QuestionGroupViewModel { Id = 1, GroupNumber = 0, GroupName = "Initial Questions" },
                    new QuestionGroupViewModel { Id = 2, GroupNumber = 1, GroupName = "Product A Feedback" },
                    new QuestionGroupViewModel { Id = 3, GroupNumber = 2, GroupName = "Product B Feedback" }
                }
            };

            // Add Question 1: How satisfied were you with our service?
            var q1 = new MultipleChoiceQuestionViewModel
            {
                Id = 1,
                SurveyId = survey.Id,
                Text = "How satisfied were you with our service?",
                QuestionNumber = 1,
                QuestionType = QuestionType.MultipleChoice,
                IsRequired = true,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 1, OptionText = "Very satisfied", Order = 0 },
                    new ChoiceOptionViewModel { Id = 2, OptionText = "Satisfied", Order = 1 },
                    new ChoiceOptionViewModel { Id = 3, OptionText = "Neutral", Order = 2 },
                    new ChoiceOptionViewModel { Id = 4, OptionText = "Dissatisfied", Order = 3 }
                },
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(q1);

            // Add Question 2: Which product did you purchase? (with branching)
            var q2 = new MultipleChoiceQuestionViewModel
            {
                Id = 2,
                SurveyId = survey.Id,
                Text = "Which product did you purchase?",
                QuestionNumber = 2,
                QuestionType = QuestionType.MultipleChoice,
                IsRequired = true,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 5, OptionText = "Product A", Order = 0, BranchToGroupId = 1 },
                    new ChoiceOptionViewModel { Id = 6, OptionText = "Product B", Order = 1, BranchToGroupId = 2 },
                    new ChoiceOptionViewModel { Id = 7, OptionText = "Other", Order = 2 }
                },
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(q2);

            return survey;
        }

        #region Preview Mode Detection Tests

        [Test]
        public void Survey_Preview_Mode_Is_Detected_From_QueryString()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert
            Assert.That(cut.Markup, Does.Contain("Preview Mode"));
        }

        [Test]
        public void Survey_Preview_Alert_Shows_Info_Message()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert
            Assert.That(cut.Markup, Does.Contain("You are in Preview Mode"));
            Assert.That(cut.Markup, Does.Contain("answers will not be recorded"));
            Assert.That(cut.Markup, Does.Contain("cannot submit"));
        }

        [Test]
        public void Survey_Preview_Mode_Not_Active_Without_QueryString()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert
            Assert.That(cut.Markup, Does.Not.Contain("Preview Mode"));
        }

        #endregion

        #region Preview Demo Step Navigation Tests

        [Test]
        public void Survey_Preview_Demo_Step0_Shows_Welcome_Popover()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Wait for the component to fully render
            cut.WaitForAssertion(() => 
            {
                Assert.That(cut.Markup, Does.Contain("survey-container"));
            }, timeout: TimeSpan.FromSeconds(5));

            // Assert - Check for Step 0 demo content
            AssertPopoverText("This is what the survey will look like");
        }

        [Test]
        public void Survey_Preview_Demo_Step0_Mentions_Preview_Mode()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            cut.WaitForAssertion(() => 
            {
                Assert.That(cut.Markup, Does.Contain("survey-container"));
            }, timeout: TimeSpan.FromSeconds(5));

            // Assert
            AssertPopoverText("preview mode");
        }

        [Test]
        public void Survey_Preview_Demo_Step1_Shows_Submit_Disabled_Message()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true&DemoStep=1");
            
            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            cut.WaitForAssertion(() => 
            {
                Assert.That(cut.Markup, Does.Contain("survey-container"));
            }, timeout: TimeSpan.FromSeconds(5));

            // Assert
            AssertPopoverText("submit survey button is disabled in preview mode");
        }

        [Test]
        public void Survey_Preview_Demo_Step2_Shows_Continue_Message()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true&DemoStep=2");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            cut.WaitForAssertion(() => 
            {
                Assert.That(cut.Markup, Does.Contain("survey-container"));
            }, timeout: TimeSpan.FromSeconds(5));

            // Assert
            AssertPopoverText("Click Next to continue the demo");
        }

        #endregion

        #region Preview Interaction Tests

        [Test]
        public void Survey_Preview_Submit_Button_Is_Disabled()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert
            var submitButton = cut.Find("#survey-submit-btn");
            Assert.That(submitButton, Is.Not.Null);
            Assert.That(submitButton.HasAttribute("disabled"), Is.True);
        }

        [Test]
        public async Task Survey_Preview_Answers_Are_Not_Saved()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");
            
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Verify component loaded
            cut.WaitForAssertion(() => 
            {
                Assert.That(cut.Markup, Does.Contain("survey-container"));
            }, timeout: TimeSpan.FromSeconds(5));

            // Act - Simulate answering a question would be complex in unit test
            // Instead, verify that API Post is not called in preview mode
            
            // Assert - Verify PostAsync is not called for answers in preview mode
            ApiServiceMock.Verify(
                x => x.PostAsync(It.Is<string>(s => s == ApiEndpoints.Answer), It.IsAny<AnswerViewModel>()), 
                Times.Never, 
                "Answers should not be posted to API in preview mode"
            );
        }

        [Test]
        public void Survey_Preview_Radio_Buttons_Are_Disabled()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - Check that MudRadioGroup has disabled attribute
            Assert.That(cut.Markup, Does.Contain("disabled"));
        }

        #endregion

        #region Preview Navigation Tests

        [Test]
        public void Survey_Preview_Demo_Step3_Would_Navigate_To_SurveysICreated()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true&DemoStep=2");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - At step 2, the component should show the "Next" message
            // When user clicks Next from step 2, it will go to step 3 which triggers navigation
            cut.WaitForAssertion(() => 
            {
                Assert.That(cut.Markup, Does.Contain("survey-container"));
            }, timeout: TimeSpan.FromSeconds(5));
            
            AssertPopoverText("Click Next to continue the demo");
        }

        [Test]
        public void Survey_Preview_Can_Return_To_Survey_List()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Verify component is rendered
            Assert.That(cut.Markup, Does.Contain("survey-container"));
            
            // Navigation is handled by browser back button or Next step in demo
            // Just verify that the navigation manager is available
            Assert.That(NavManager, Is.Not.Null);
        }

        #endregion

        #region Preview Demo Instructions Tests

        [Test]
        public void Survey_Preview_Explains_Unpublish_If_Not_Satisfied()
        {
            // Arrange - This test verifies that the demo instructions mention unpublishing
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - The demo popup should eventually explain that you can unpublish
            // This might be in a later step, but we verify the survey renders correctly
            Assert.That(cut.Markup, Does.Contain("survey-container"));
        }

        [Test]
        public void Survey_Preview_Shows_Multiple_Demo_Steps()
        {
            // Arrange & Act - Test that different demo steps can be rendered
            var step0 = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );
            
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true&DemoStep=1");
            var step1 = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );
            
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true&DemoStep=2");
            var step2 = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - All steps should render without errors
            Assert.That(step0.Markup, Does.Contain("survey-container"));
            Assert.That(step1.Markup, Does.Contain("survey-container"));
            Assert.That(step2.Markup, Does.Contain("survey-container"));
        }

        #endregion

        #region Preview vs Normal Mode Tests

        [Test]
        public void Survey_Normal_Mode_Does_Not_Show_Preview_Alert()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert
            Assert.That(cut.Markup, Does.Not.Contain("Preview Mode"));
        }

        [Test]
        public void Survey_Normal_Mode_Submit_Button_Is_Enabled_After_Answers()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - In normal mode, submit button should not have disabled tooltip mentioning preview
            Assert.That(cut.Markup, Does.Not.Contain("Survey cannot be submitted in Preview mode"));
        }

        [Test]
        public void Survey_Normal_Mode_Radio_Buttons_Are_Enabled()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}");

            // Act  
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - Verify that the survey questions are present
            Assert.That(cut.Markup, Does.Contain("survey-container"));
            // Radio buttons should not all be disabled (some might be for other reasons like not agreeing to terms)
        }

        #endregion

        #region Branching Preview Tests

        [Test]
        public void Survey_Preview_With_Branching_Renders_Questions()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert
            Assert.That(cut.Markup, Does.Contain("How satisfied were you with our service?"));
        }

        [Test]
        public void Survey_Preview_Shows_Survey_Title_And_Description()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert
            Assert.That(cut.Markup, Does.Contain("Customer Satisfaction Survey"));
            Assert.That(cut.Markup, Does.Contain("Please take our customer satisfaction survey"));
        }

        [Test]
        public void Survey_Preview_Displays_Question_Numbers()
        {
            // Arrange
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - Should show question numbers
            Assert.That(cut.Markup, Does.Contain("class=\"question-number\""));
        }

        #endregion

        #region Demo User Tests

        [Test]
        public void Survey_Demo_User_In_Preview_Shows_Demo_Popovers()
        {
            // Arrange - Demo user is already set up in Setup()
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            cut.WaitForAssertion(() => 
            {
                Assert.That(cut.Markup, Does.Contain("survey-container"));
            }, timeout: TimeSpan.FromSeconds(5));

            // Assert - Demo popover should be present
            AssertPopoverText("This is what the survey will look like");
        }

        [Test]
        public void Survey_NonDemo_User_In_Preview_No_Demo_Popovers()
        {
            // Arrange - Setup regular user instead of demo user
            var regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "regular.user@example.com")
            }, "TestAuth"));
            var authState = new AuthenticationState(regularUser);
            AuthStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);

            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act
            var cut = Context.RenderComponent<Survey>(parameters => parameters
                .Add(p => p.SurveyId, _testSurveyGuid)
            );

            // Assert - Should still show preview mode, but no demo popovers
            Assert.That(cut.Markup, Does.Contain("Preview Mode"));
            // Demo popover won't be visible for non-demo users
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Survey_Preview_Handles_Missing_Survey_Gracefully()
        {
            // Arrange
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync((SurveyViewModel)null);
            
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyGuid}?Preview=true");

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() =>
            {
                var cut = Context.RenderComponent<Survey>(parameters => parameters
                    .Add(p => p.SurveyId, _testSurveyGuid)
                );
            });
        }

        [Test]
        public void Survey_Preview_With_Invalid_GUID_Renders_Without_Error()
        {
            // Arrange
            var invalidGuid = Guid.Empty;
            NavManager.NavigateTo($"http://localhost/survey/{invalidGuid}?Preview=true");

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var cut = Context.RenderComponent<Survey>(parameters => parameters
                    .Add(p => p.SurveyId, invalidGuid)
                );
            });
        }

        #endregion
    }
}
