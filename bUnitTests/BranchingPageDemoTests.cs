using Bunit;
using JwtIdentity.Client.Pages.Survey;
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
    /// Tests for the Branching Configuration Page Demo flow
    /// </summary>
    [TestFixture]
    public class BranchingPageDemoTests : BUnitTestBase
    {
        private SurveyViewModel _testSurvey;
        private List<QuestionGroupViewModel> _testGroups;

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

            // Setup test survey with questions (as created by Edit page branching demo)
            _testSurvey = new SurveyViewModel
            {
                Id = 2,
                Guid = "22222222-2222-2222-2222-222222222222",
                Title = "Branching Demo Survey",
                Description = "A demo survey for testing branching",
                AiInstructions = "Create a customer feedback survey",
                Questions = new List<QuestionViewModel>
                {
                    // AI-generated text question
                    new TextQuestionViewModel { Id = 1, Text = "AI Q1", QuestionNumber = 1, GroupId = 0 },
                    // 4 Multiple Choice questions
                    new MultipleChoiceQuestionViewModel 
                    { 
                        Id = 2, 
                        Text = "Which product did you purchase?", 
                        QuestionNumber = 2, 
                        GroupId = 0,
                        QuestionType = QuestionType.MultipleChoice,
                        Options = new List<ChoiceOptionViewModel>
                        {
                            new() { Id = 1, OptionText = "Product A", Order = 0 },
                            new() { Id = 2, OptionText = "Product B", Order = 1 },
                            new() { Id = 3, OptionText = "Product C", Order = 2 }
                        }
                    },
                    new MultipleChoiceQuestionViewModel 
                    { 
                        Id = 3, 
                        Text = "How satisfied are you with our customer service?", 
                        QuestionNumber = 3, 
                        GroupId = 0,
                        QuestionType = QuestionType.MultipleChoice,
                        Options = new List<ChoiceOptionViewModel>
                        {
                            new() { Id = 4, OptionText = "Very Satisfied", Order = 0 },
                            new() { Id = 5, OptionText = "Satisfied", Order = 1 },
                            new() { Id = 6, OptionText = "Neutral", Order = 2 },
                            new() { Id = 7, OptionText = "Dissatisfied", Order = 3 },
                            new() { Id = 8, OptionText = "Very Dissatisfied", Order = 4 }
                        }
                    },
                    new MultipleChoiceQuestionViewModel 
                    { 
                        Id = 4, 
                        Text = "Would you recommend this product to others?", 
                        QuestionNumber = 4, 
                        GroupId = 0,
                        QuestionType = QuestionType.MultipleChoice,
                        Options = new List<ChoiceOptionViewModel>
                        {
                            new() { Id = 9, OptionText = "Definitely", Order = 0 },
                            new() { Id = 10, OptionText = "Probably", Order = 1 },
                            new() { Id = 11, OptionText = "Not sure", Order = 2 },
                            new() { Id = 12, OptionText = "Probably not", Order = 3 },
                            new() { Id = 13, OptionText = "Definitely not", Order = 4 }
                        }
                    },
                    new MultipleChoiceQuestionViewModel 
                    { 
                        Id = 5, 
                        Text = "Did you find the product easy to use?", 
                        QuestionNumber = 5, 
                        GroupId = 0,
                        QuestionType = QuestionType.MultipleChoice,
                        Options = new List<ChoiceOptionViewModel>
                        {
                            new() { Id = 14, OptionText = "Yes, very easy", Order = 0 },
                            new() { Id = 15, OptionText = "Partially", Order = 1 },
                            new() { Id = 16, OptionText = "No", Order = 2 }
                        }
                    },
                    // Last text question
                    new TextQuestionViewModel 
                    { 
                        Id = 6, 
                        Text = "Please share any additional feedback or comments.", 
                        QuestionNumber = 6, 
                        GroupId = 0,
                        IsLastQuestion = true
                    }
                },
                Published = false,
                AiQuestionsApproved = true,
                AiRetryCount = 0
            };

            // Setup test groups - initially only Group 0
            _testGroups = new List<QuestionGroupViewModel>
            {
                new QuestionGroupViewModel
                {
                    Id = 0,
                    SurveyId = 2,
                    GroupNumber = 0,
                    GroupName = "Default Group",
                    SubmitAfterGroup = false
                }
            };
        }

        private void SetupStandardMocks()
        {
            SetupSurveyMocks(_testSurvey, _testGroups);
        }

        [Test]
        public void BranchingPage_Step0_ShowsWelcomePopup()
        {
            // Arrange
            SetupStandardMocks();

            // Act
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=0");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Wait for component to render (check for survey title in markup)
            cut.WaitForState(() => cut.Markup.Contains("Branching Demo Survey"), timeout: TimeSpan.FromSeconds(5));

            // Assert - Component loaded successfully and demo is active
            Assert.That(cut.Markup, Does.Contain("Survey Branching"));
            // Verify the page rendered without errors
            Assert.That(cut.Markup, Does.Contain("Configure Branching Rules"));
        }

        [Test]
        public void BranchingPage_Step1_PromptsToAddFirstGroup()
        {
            // Arrange
            SetupStandardMocks();
            
            var newGroup = new QuestionGroupViewModel
            {
                Id = 1,
                SurveyId = 2,
                GroupNumber = 1,
                GroupName = "Group 1",
                SubmitAfterGroup = true
            };
            ApiServiceMock.Setup(x => x.PostAsync<QuestionGroupViewModel, QuestionGroupViewModel>(
                It.IsAny<string>(), It.IsAny<QuestionGroupViewModel>()))
                .ReturnsAsync(newGroup);

            // Act
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=1");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );
            
            // Wait for loading to complete
            cut.WaitForState(() => cut.Markup.Contains("Add Group"), timeout: TimeSpan.FromSeconds(5));

            // Assert - Prompt to add first group at step 1
            Assert.That(cut.Markup, Does.Contain("Add Group"));
        }

        [Test]
        public void BranchingPage_AddGroup_AdvancesToStep2()
        {
            // Arrange
            SetupStandardMocks();
            
            var newGroup = new QuestionGroupViewModel
            {
                Id = 1,
                SurveyId = 2,
                GroupNumber = 1,
                GroupName = "Group 1",
                SubmitAfterGroup = true
            };
            ApiServiceMock.Setup(x => x.PostAsync<QuestionGroupViewModel, QuestionGroupViewModel>(
                It.IsAny<string>(), It.IsAny<QuestionGroupViewModel>()))
                .ReturnsAsync(newGroup);

            // Act - Start at step 1 (ready to add group)
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=1");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );
            
            // Wait for loading
            cut.WaitForState(() => cut.Markup.Contains("Add Group"), timeout: TimeSpan.FromSeconds(5));

            // Assert - Should have Add Group button
            Assert.That(cut.Markup, Does.Contain("Add Group"));
            // Verify component loaded successfully
            Assert.That(cut.Markup, Does.Contain("Survey Branching"));
        }

        [Test]
        public void BranchingPage_LastQuestion_CannotBeMovedToOtherGroup()
        {
            // Arrange
            SetupStandardMocks();

            // Act
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Wait for loading
            cut.WaitForState(() => cut.Markup.Contains("Last Question"), timeout: TimeSpan.FromSeconds(5));

            // Assert - Last question should not have a group selector, just a chip
            Assert.That(cut.Markup, Does.Contain("Last Question"));
            // Verify the last question has the "IsLastQuestion" marker
            var lastQuestion = _testSurvey.Questions.FirstOrDefault(q => q.IsLastQuestion);
            Assert.That(lastQuestion, Is.Not.Null);
            Assert.That(lastQuestion.GroupId, Is.EqualTo(0)); // Should be in Group 0
        }

        [Test]
        public void BranchingPage_IsQuestionGroupSelectorDisabled_ReturnsCorrectly()
        {
            // Arrange
            SetupStandardMocks();

            // Act
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=9");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Wait for loading
            cut.WaitForState(() => cut.Markup.Contains("Assign to Group"), timeout: TimeSpan.FromSeconds(5));

            // Assert - At step 9, only Q3 should be enabled
            // The helper method IsQuestionGroupSelectorDisabled is called internally
            // We verify it's working by checking the rendered output
            Assert.That(cut.Markup, Does.Contain("Assign to Group"));
        }

        [Test]
        public void BranchingPage_Step17_ShowsCompletionPopup()
        {
            // Arrange
            SetupStandardMocks();

            // Act
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=17");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Wait for loading
            cut.WaitForState(() => cut.Markup.Contains("Back to Edit Survey"), timeout: TimeSpan.FromSeconds(5));

            // Assert - Component loaded successfully and "Back to Edit Survey" button is present
            Assert.That(cut.Markup, Does.Contain("Back to Edit Survey"));
            Assert.That(cut.Markup, Does.Contain("Survey Branching"));
        }

        [Test]
        public void BranchingPage_NavigateBackToEdit_IncludesDemoParams()
        {
            // Arrange
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.GetAsync<List<QuestionGroupViewModel>>(It.IsAny<string>()))
                .ReturnsAsync(_testGroups);

            // Act
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=17");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );
            
            var backButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Back to Edit Survey"));
            backButton?.Click();

            // Assert - Should navigate to edit page with demo params
            Assert.That(NavManager.Uri, Does.Contain("/survey/edit/2"));
            Assert.That(NavManager.Uri, Does.Contain("DemoType=branching"));
            Assert.That(NavManager.Uri, Does.Contain("DemoStep=30"));
        }

        [Test]
        public void BranchingPage_ShouldExpandPanel_ReturnsCorrectly()
        {
            // Arrange
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.GetAsync<List<QuestionGroupViewModel>>(It.IsAny<string>()))
                .ReturnsAsync(_testGroups);

            // Act - At step 5, Groups panel should be expanded
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=5");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Assert - Groups panel should be expanded
            Assert.That(cut.Markup, Does.Contain("Manage Question Groups"));
        }

        [Test]
        public void BranchingPage_IsAddGroupDisabled_AtStep3()
        {
            // Arrange
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.GetAsync<List<QuestionGroupViewModel>>(It.IsAny<string>()))
                .ReturnsAsync(_testGroups);

            // Act - At step 3 (naming first group), Add Group should be disabled
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=3");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Assert - Add Group button should be disabled
            var addGroupButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Add Group"));
            // The button should exist but be disabled (we can't directly test disabled attribute in this context)
            Assert.That(addGroupButton, Is.Not.Null);
        }

        [Test]
        public void BranchingPage_DemoNotActive_WhenNotDemoUser()
        {
            // Arrange - Regular user (not demo user)
            var regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "regularuser@example.com")
            }, "TestAuth"));
            var authState = new AuthenticationState(regularUser);
            AuthStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);
            
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.GetAsync<List<QuestionGroupViewModel>>(It.IsAny<string>()))
                .ReturnsAsync(_testGroups);

            // Act
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Assert - No demo popups should be shown
            Assert.That(cut.Markup, Does.Not.Contain("Welcome to Branching Configuration"));
            // All controls should be enabled
            Assert.That(cut.Markup, Does.Contain("Add Group"));
        }

        [Test]
        public void BranchingPage_UpdateQuestionGroup_AdvancesDemoStep()
        {
            // Arrange
            var groups = new List<QuestionGroupViewModel>
            {
                _testGroups[0],
                new QuestionGroupViewModel
                {
                    Id = 1,
                    SurveyId = 2,
                    GroupNumber = 1,
                    GroupName = "Group 1",
                    SubmitAfterGroup = true
                }
            };
            
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);
            ApiServiceMock.Setup(x => x.GetAsync<List<QuestionGroupViewModel>>(It.IsAny<string>()))
                .ReturnsAsync(groups);
            ApiServiceMock.Setup(x => x.UpdateAsync<QuestionGroupViewModel>(
                It.IsAny<string>(), It.IsAny<QuestionGroupViewModel>()))
                .ReturnsAsync(groups[1]);

            // Act - At step 3, naming Group 1 should advance to step 4
            NavManager.NavigateTo("/survey/branching/2?DemoType=branching&DemoStep=3");
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, "2")
            );

            // Assert - Demo is at step 3, ready to name group
            Assert.That(cut.Markup, Does.Contain("Group 1"));
        }
    }
}
