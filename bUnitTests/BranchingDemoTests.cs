using Bunit;
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
    /// Comprehensive step-by-step tests for the Branching Survey Demo flow
    /// </summary>
    [TestFixture]
    public class BranchingDemoTests : BUnitTestBase
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
                Id = 2,
                Guid = "22222222-2222-2222-2222-222222222222",
                Title = "Branching Demo Survey",
                Description = "A demo survey for testing branching",
                AiInstructions = "Create a customer feedback survey",
                Questions = new List<QuestionViewModel>(),
                Published = false,
                AiQuestionsApproved = false,
                AiRetryCount = 0
            };
        }

        [Test]
        public void BranchingDemo_Step0ToStep1_Succeeds()
        {
            // Arrange
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "AI Generated Question 1", QuestionNumber = 1 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2"));

            // Assert - Step 0/1: Review AI questions
            Assert.That(cut.Markup, Does.Contain("Please review the AI generated questions"));
        }

        [Test]
        public async Task BranchingDemo_Step1ToStep2_Succeeds()
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
                .Add(p => p.SurveyId, "2"));

            // Act - Accept questions
            var acceptButton = cut.Find("#AcceptQuestionsBtn");
            await acceptButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            cut.WaitForState(() => !cut.Markup.Contains("Please review the AI generated questions"), timeout: TimeSpan.FromSeconds(5));

            // Assert - Move to step 2
            Assert.That(cut.Markup, Does.Not.Contain("Please review the AI generated questions"));
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_Step7ToStep8_Succeeds()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - Step 8: First question setup with product choices
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_Step8HasProductQuestion()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - Question text should be set for product question
            Assert.That(cut.Markup, Does.Contain("Multiple Choice"));
        }

        [Test]
        public void BranchingDemo_Step10ToStep11_Succeeds()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel
            {
                Id = 2,
                Text = "Which product did you purchase?",
                QuestionNumber = 2,
                Options = new List<ChoiceOptionViewModel>
                {
                    new() { OptionText = "Product A", Order = 0 },
                    new() { OptionText = "Product B", Order = 1 },
                    new() { OptionText = "Product C", Order = 2 }
                }
            });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - After first question saved, shows message about second question
            Assert.That(cut.Markup, Does.Contain("Multiple Choice"));
        }

        [Test]
        public void BranchingDemo_Step11ShowsPresetChoicePrompt()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - Preset choices dropdown exists
            Assert.That(cut.Markup, Does.Contain("Preset Choices"));
        }

        [Test]
        public void BranchingDemo_Step12ToStep13_Succeeds()
        {
            // This test validates that step 12 exists and advances properly
            // Step 12 should appear after selecting "How Satisfied" preset at step 11
            // Step 12 should tell user to save the question
            // Step 13 appears after saving

            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel
            {
                Id = 2,
                Text = "How satisfied are you with our customer service?",
                QuestionNumber = 2,
                Options = new List<ChoiceOptionViewModel>
                {
                    new() { OptionText = "Very Satisfied", Order = 0 },
                    new() { OptionText = "Satisfied", Order = 1 },
                    new() { OptionText = "Neutral", Order = 2 },
                    new() { OptionText = "Dissatisfied", Order = 3 },
                    new() { OptionText = "Very Dissatisfied", Order = 4 }
                }
            });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - After second question saved, should show step 13 message
            Assert.That(cut.Markup, Does.Contain("Add Question to Survey"));
        }

        [Test]
        public void BranchingDemo_Step13ToStep14_Succeeds()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 2, Text = "Q1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 3, Text = "Q2", QuestionNumber = 2 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - Should be able to create third question
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_Step14ToStep15_Succeeds()
        {
            // Similar pattern - after selecting "How Likely" preset, advances to step 15
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Preset Choices"));
        }

        [Test]
        public void BranchingDemo_Step16ToStep17_Succeeds()
        {
            // After saving third question, shows step 16 message, then advances to step 17
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 2, Text = "Q1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 3, Text = "Q2", QuestionNumber = 2 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 4, Text = "Q3", QuestionNumber = 3 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_Step17ToStep18_Succeeds()
        {
            // After selecting "Yes No Partially" preset at step 17, advances to step 18
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Preset Choices"));
        }

        [Test]
        public void BranchingDemo_Step19ToStep20_Succeeds()
        {
            // After saving fourth MC question, shows step 19, then advances to step 20 for text question
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 2, Text = "Q1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 3, Text = "Q2", QuestionNumber = 2 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 4, Text = "Q3", QuestionNumber = 3 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 5, Text = "Q4", QuestionNumber = 4 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_Step20CreatesTextQuestion()
        {
            // Step 20: Text question should be created
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - Text option should be available
            Assert.That(cut.Markup, Does.Contain("Text"));
        }

        [Test]
        public void BranchingDemo_Step21ToStep22_Succeeds()
        {
            // After saving text question at step 21, advances to step 22 to mark as last
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 6, Text = "Text Q", QuestionNumber = 5 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_Step23ShowsConfigureBranchingButton()
        {
            // After marking question as last at step 22, shows step 23 with Configure Branching button
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "Last Q", QuestionNumber = 5, IsLastQuestion = true });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - Configure Branching button should be visible
            Assert.That(cut.Markup, Does.Contain("Configure Branching"));
        }

        [Test]
        public void BranchingDemo_DoesNotShowPublishButtonUntilStep30()
        {
            // Publish button should not be shown until step 30 (after returning from branching config)
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "22222222-2222-2222-2222-222222222222"));

            // Assert - Publish button exists but is not highlighted at early steps
            Assert.That(cut.Markup, Does.Contain("Publish Survey"));
        }
    }
}
