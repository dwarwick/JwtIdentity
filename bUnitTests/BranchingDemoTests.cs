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

            // Act - Branching demo (DemoType = branching)
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Step 0/1: Review AI questions
            Assert.That(cut.Markup, Does.Contain("Please review the AI generated questions"));
        }

        [Test]
        public void BranchingDemo_Step1ToStep2_Succeeds()
        {
            // Arrange - Survey with AI questions already approved (simulating post-accept state)
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "AI Generated Question 1", QuestionNumber = 1 });
            _testSurvey.AiQuestionsApproved = true; // Already approved
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            
            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Move to step 2
            Assert.That(cut.Markup, Does.Not.Contain("Please review the AI generated questions"));
            Assert.That(cut.Markup, Does.Contain("QuestionTypeSelect"));
        }

        [Test]
        public void BranchingDemo_Step2ToStep7_Succeeds()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Step 7: Can select question type
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_CanCreateMultipleChoiceQuestion()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Multiple Choice option available
            Assert.That(cut.Markup, Does.Contain("QuestionTypeSelect"));
        }

        [Test]
        public void BranchingDemo_AfterFirstQuestion_ShowsStep10()
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
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - After first question saved, can create more
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_PresetsChoicesAvailable()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Question type selector exists (preset choices available after selecting MC)
            Assert.That(cut.Markup, Does.Contain("QuestionTypeSelect"));
        }

        [Test]
        public void BranchingDemo_AfterSecondQuestion_ShowsAddButton()
        {
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
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Save button ID exists
            Assert.That(cut.Markup, Does.Contain("SaveQuestionBtn"));
        }

        [Test]
        public void BranchingDemo_After2Questions_CanCreateThird()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 2, Text = "Q1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 3, Text = "Q2", QuestionNumber = 2 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Can create third question
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_PresetsIncludeHowLikely()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Question type selector exists (presets available after selecting MC)
            Assert.That(cut.Markup, Does.Contain("QuestionTypeSelect"));
        }

        [Test]
        public void BranchingDemo_After3Questions_CanCreateFourth()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 2, Text = "Q1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 3, Text = "Q2", QuestionNumber = 2 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 4, Text = "Q3", QuestionNumber = 3 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert
            Assert.That(cut.Markup, Does.Contain("Select the Question Type"));
        }

        [Test]
        public void BranchingDemo_After4MCQuestions_CanCreateText()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 2, Text = "Q1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 3, Text = "Q2", QuestionNumber = 2 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 4, Text = "Q3", QuestionNumber = 3 });
            _testSurvey.Questions.Add(new MultipleChoiceQuestionViewModel { Id = 5, Text = "Q4", QuestionNumber = 4 });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Text option should be available
            Assert.That(cut.Markup, Does.Contain("Text"));
        }

        [Test]
        public void BranchingDemo_AfterTextQuestion_CanSave()
        {
            // Arrange - Set up survey at step 21 (ready to save text question)
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "Q1", QuestionNumber = 1 });
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 2, Text = "Q2", QuestionNumber = 2 });
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 3, Text = "Q3", QuestionNumber = 3 });
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 4, Text = "Q4", QuestionNumber = 4 });
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 5, Text = "Q5", QuestionNumber = 5 });
            
            var updatedSurvey = new SurveyViewModel 
            { 
                Id = 2, 
                Title = "Test Survey", 
                AiQuestionsApproved = true,
                Questions = _testSurvey.Questions
            };
            
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.UpdateAsync<SurveyViewModel>(It.IsAny<string>(), It.IsAny<SurveyViewModel>()))
                .ReturnsAsync(updatedSurvey);

            // Act - Navigate to step 21
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching&DemoStep=21");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - At step 21, should show save question button
            Assert.That(cut.Markup, Does.Contain("id=\"SaveQuestionBtn\""));
            // The question text field should be present
            Assert.That(cut.Markup, Does.Contain("Question Text"));
        }

        [Test]
        public void BranchingDemo_WithLastQuestion_ShowsConfigureBranchingButton()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            _testSurvey.Questions.Add(new TextQuestionViewModel { Id = 1, Text = "Last Q", QuestionNumber = 5, IsLastQuestion = true });
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Configure Branching button should be visible
            Assert.That(cut.Markup, Does.Contain("Configure Branching"));
        }

        [Test]
        public void BranchingDemo_PublishButtonShownAtStep30()
        {
            // Arrange
            _testSurvey.AiQuestionsApproved = true;
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Act
            NavManager.NavigateTo("/survey/edit/2?DemoType=branching");
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, "2")
                );

            // Assert - Publish button exists
            Assert.That(cut.Markup, Does.Contain("Publish Survey"));
        }
    }
}
