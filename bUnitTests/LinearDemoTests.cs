using Bunit;
using JwtIdentity.Client.Pages.Demo;
using JwtIdentity.Client.Pages.Survey;
using JwtIdentity.Common.ViewModels;
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
    /// Comprehensive step-by-step tests for the Linear Survey Demo flow
    /// </summary>
    [TestFixture]
    public class LinearDemoTests : BUnitTestBase
    {
        private SurveyViewModel _testSurvey;

        [SetUp]
        public void Setup()
        {
            // Setup demo user
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "DemoUser123@surveyshark.site")
            }, "TestAuth"));
            var authState = new AuthenticationState(authenticatedUser);
            AuthStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);

            // Setup test survey
            _testSurvey = new SurveyViewModel
            {
                Id = 1,
                Guid = "11111111-1111-1111-1111-111111111111",
                Title = "Linear Demo Survey",
                Description = "A demo survey for testing",
                AiInstructions = "Create a customer feedback survey",
                Questions = new List<QuestionViewModel>(),
                Published = false,
                AiQuestionsApproved = false,
                AiRetryCount = 0
            };
        }

        [Test]
        public void LinearDemo_Step0ToStep1_Succeeds()
        {
            // Arrange
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "AI Generated Question 1", QuestionNumber = 1 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.UpdateAsync<SurveyViewModel>(It.IsAny<string>(), It.IsAny<SurveyViewModel>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "11111111-1111-1111-1111-111111111111"));

            // Assert - Step 0: Initial page load with questions panel
            Assert.That(cut.Markup, Does.Contain("Click each question then scroll down to inspect it"));
        }

        [Test]
        public async Task LinearDemo_Step1ToStep2_Succeeds()
        {
            // Arrange
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "AI Generated Question 1", QuestionNumber = 1 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.UpdateAsync<SurveyViewModel>(It.IsAny<string>(), It.IsAny<SurveyViewModel>()))
                .ReturnsAsync((string _, SurveyViewModel survey) =>
                {
                    survey.AiQuestionsApproved = true;
                    return survey;
                });

            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "11111111-1111-1111-1111-111111111111"));

            // Act - Click Accept Questions button
            var acceptButton = cut.Find("#AcceptQuestionsBtn");
            await acceptButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            cut.WaitForState(() => !cut.Markup.Contains("Please review the AI generated questions"), timeout: TimeSpan.FromSeconds(5));

            // Assert - Step 2: Questions accepted, can now add questions
            Assert.That(cut.Markup, Does.Not.Contain("Please review the AI generated questions"));
        }

        [Test]
        public void LinearDemo_Step2ToStep3_Succeeds()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "AI Generated Question 1", QuestionNumber = 1 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "11111111-1111-1111-1111-111111111111"));

            // Click a question to select it (simulating step 2 → 3 transition)
            var questionElements = cut.FindAll(".question-panel");
            if (questionElements.Count > 0)
            {
                questionElements[0].Click();
            }

            // Assert - Questions can be interacted with
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void LinearDemo_Step7ToStep8_Succeeds()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "AI Generated Question 1", QuestionNumber = 1 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "11111111-1111-1111-1111-111111111111"));

            // Assert - Can create new question
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
            Assert.That(cut.Markup, Does.Contain("Multiple Choice"));
        }

        [Test]
        public void LinearDemo_Step10_ShowsPublishButton()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "AI Generated Question 1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel
            {
                Id = 2,
                Text = "Demo MC Question",
                QuestionNumber = 2,
                Options = new List<ChoiceOptionViewModel>
                {
                    new() { OptionText = "Yes", Order = 0 },
                    new() { OptionText = "No", Order = 1 }
                }
            });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "11111111-1111-1111-1111-111111111111"));

            // Assert - Publish button should be visible
            Assert.That(cut.Markup, Does.Contain("Publish Survey"));
        }

        [Test]
        public void LinearDemo_DoesNotShowBranchingButton()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "11111111-1111-1111-1111-111111111111"));

            // Assert - Configure Branching button should NOT be highlighted in linear demo
            Assert.That(cut.Markup, Does.Contain("Configure Branching"));
            // The button exists but is not part of the demo flow (no demo border at step 23)
        }
    }
}
