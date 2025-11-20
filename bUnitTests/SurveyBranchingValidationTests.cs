using Bunit;
using JwtIdentity.Client.Pages.Survey;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using JwtIdentity.Client.Services.Base;

namespace JwtIdentity.BunitTests
{
    /// <summary>
    /// Tests for the AllQuestionsAnswered method with branching surveys
    /// </summary>
    [TestFixture]
    public class SurveyBranchingValidationTests : BUnitTestBase
    {
        private Guid _testSurveyId;

        [SetUp]
        public void Setup()
        {
            _testSurveyId = Guid.NewGuid();
            
            // Setup authenticated user
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "TestAuth"));
            var authState = new AuthenticationState(authenticatedUser);
            AuthStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);

            // Setup AuthService for anonymous login
            AuthServiceMock.Setup(x => x.Login(It.IsAny<ApplicationUserViewModel>()))
                .ReturnsAsync(new Response<ApplicationUserViewModel>
                {
                    Success = true,
                    Data = new ApplicationUserViewModel { UserName = "anonymous" }
                });

            // Setup default answer post responses
            ApiServiceMock.Setup(x => x.PostAsync(It.Is<string>(s => s == ApiEndpoints.Answer), It.IsAny<AnswerViewModel>()))
                .ReturnsAsync((string endpoint, AnswerViewModel answer) => answer);
        }

        /// <summary>
        /// Creates a linear survey with all questions in Group 0
        /// </summary>
        private SurveyViewModel CreateLinearSurvey()
        {
            var survey = new SurveyViewModel
            {
                Id = 1,
                Guid = _testSurveyId.ToString(),
                Title = "Linear Survey",
                Description = "All questions in group 0",
                Published = true,
                Questions = new List<QuestionViewModel>(),
                QuestionGroups = new List<QuestionGroupViewModel>()
            };

            // Add two required questions in group 0
            survey.Questions.Add(new TextQuestionViewModel
            {
                Id = 1,
                SurveyId = survey.Id,
                Text = "Question 1",
                QuestionNumber = 1,
                QuestionType = QuestionType.Text,
                IsRequired = true,
                GroupId = 0,
                Answers = new List<AnswerViewModel>()
            });

            survey.Questions.Add(new MultipleChoiceQuestionViewModel
            {
                Id = 2,
                SurveyId = survey.Id,
                Text = "Question 2",
                QuestionNumber = 2,
                QuestionType = QuestionType.MultipleChoice,
                IsRequired = true,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 1, OptionText = "Yes", Order = 1 },
                    new ChoiceOptionViewModel { Id = 2, OptionText = "No", Order = 2 }
                },
                Answers = new List<AnswerViewModel>()
            });

            return survey;
        }

        /// <summary>
        /// Creates a branching survey with questions in multiple groups
        /// </summary>
        private SurveyViewModel CreateBranchingSurvey()
        {
            var survey = new SurveyViewModel
            {
                Id = 2,
                Guid = _testSurveyId.ToString(),
                Title = "Branching Survey",
                Description = "Questions in different groups",
                Published = true,
                Questions = new List<QuestionViewModel>(),
                QuestionGroups = new List<QuestionGroupViewModel>
                {
                    new QuestionGroupViewModel { Id = 1, GroupNumber = 0, GroupName = "Initial" },
                    new QuestionGroupViewModel { Id = 2, GroupNumber = 1, GroupName = "Group 1" },
                    new QuestionGroupViewModel { Id = 3, GroupNumber = 2, GroupName = "Group 2" }
                }
            };

            // Question 1 in Group 0 - branches to either Group 1 or Group 2
            survey.Questions.Add(new MultipleChoiceQuestionViewModel
            {
                Id = 10,
                SurveyId = survey.Id,
                Text = "Do you like pizza?",
                QuestionNumber = 1,
                QuestionType = QuestionType.MultipleChoice,
                IsRequired = true,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 10, OptionText = "Yes", Order = 1, BranchToGroupId = 1 },
                    new ChoiceOptionViewModel { Id = 11, OptionText = "No", Order = 2, BranchToGroupId = 2 }
                },
                Answers = new List<AnswerViewModel>()
            });

            // Question 2 in Group 1 (only shown if user answers "Yes" to Q1)
            survey.Questions.Add(new TextQuestionViewModel
            {
                Id = 11,
                SurveyId = survey.Id,
                Text = "What's your favorite pizza topping?",
                QuestionNumber = 2,
                QuestionType = QuestionType.Text,
                IsRequired = true,
                GroupId = 1,
                Answers = new List<AnswerViewModel>()
            });

            // Question 3 in Group 2 (only shown if user answers "No" to Q1)
            survey.Questions.Add(new TextQuestionViewModel
            {
                Id = 12,
                SurveyId = survey.Id,
                Text = "Why don't you like pizza?",
                QuestionNumber = 3,
                QuestionType = QuestionType.Text,
                IsRequired = true,
                GroupId = 2,
                Answers = new List<AnswerViewModel>()
            });

            return survey;
        }

        [Test]
        public async Task LinearSurvey_AllQuestionsAnswered_ReturnsFalse_WhenNotAllAnswered()
        {
            // Arrange
            var survey = CreateLinearSurvey();
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            var cut = Context.RenderComponent<Survey>(parameters);
            await cut.Instance.LoadData();

            // Act - Don't answer any questions
            var result = cut.InvokeAsync(() =>
            {
                var method = typeof(SurveyModel).GetMethod("AllQuestionsAnswered", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (bool)method.Invoke(cut.Instance, null);
            }).Result;

            // Assert
            Assert.That(result, Is.False, "Should return false when required questions are not answered");
        }

        [Test]
        public async Task LinearSurvey_AllQuestionsAnswered_ReturnsTrue_WhenAllAnswered()
        {
            // Arrange
            var survey = CreateLinearSurvey();
            
            // Answer the questions
            var textAnswer = new TextAnswerViewModel
            {
                QuestionId = 1,
                AnswerType = AnswerType.Text,
                Text = "My answer"
            };
            survey.Questions[0].Answers.Add(textAnswer);

            var mcAnswer = new MultipleChoiceAnswerViewModel
            {
                QuestionId = 2,
                AnswerType = AnswerType.MultipleChoice,
                SelectedOptionId = 1
            };
            survey.Questions[1].Answers.Add(mcAnswer);

            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            var cut = Context.RenderComponent<Survey>(parameters);
            await cut.Instance.LoadData();

            // Act
            var result = cut.InvokeAsync(() =>
            {
                var method = typeof(SurveyModel).GetMethod("AllQuestionsAnswered", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (bool)method.Invoke(cut.Instance, null);
            }).Result;

            // Assert
            Assert.That(result, Is.True, "Should return true when all required questions are answered");
        }

        [Test]
        public async Task BranchingSurvey_AllQuestionsAnswered_OnlyValidatesLoadedGroups()
        {
            // Arrange
            var survey = CreateBranchingSurvey();
            
            // Answer Question 1 with "Yes" (branches to Group 1, NOT Group 2)
            var mcAnswer = new MultipleChoiceAnswerViewModel
            {
                QuestionId = 10,
                AnswerType = AnswerType.MultipleChoice,
                SelectedOptionId = 10 // "Yes" - branches to Group 1
            };
            survey.Questions[0].Answers.Add(mcAnswer);

            // Answer Question 2 in Group 1
            var textAnswer = new TextAnswerViewModel
            {
                QuestionId = 11,
                AnswerType = AnswerType.Text,
                Text = "Pepperoni"
            };
            survey.Questions[1].Answers.Add(textAnswer);

            // Question 3 in Group 2 is NOT answered (and should NOT be validated)

            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);

            // Navigate with Preview query parameter
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyId}?Preview=true");

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            var cut = Context.RenderComponent<Survey>(parameters);
            await cut.InvokeAsync(async () => await cut.Instance.LoadData());

            // Access private field _groupsToVisit to simulate branching without triggering rendering
            var groupsToVisitField = typeof(SurveyModel).GetField("_groupsToVisit", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var groupsToVisit = (HashSet<int>)groupsToVisitField.GetValue(cut.Instance);
            groupsToVisit.Add(0); // Group 0
            groupsToVisit.Add(1); // Group 1 (user selected "Yes")

            // Access private field QuestionsToShow and populate it with questions from Group 0 and Group 1
            var questionsToShowProp = typeof(SurveyModel).GetProperty("QuestionsToShow", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var questionsToShow = (List<QuestionViewModel>)questionsToShowProp.GetValue(cut.Instance);
            questionsToShow.Clear();
            questionsToShow.Add(survey.Questions[0]); // Q1 in Group 0
            questionsToShow.Add(survey.Questions[1]); // Q2 in Group 1
            // Q3 in Group 2 is NOT added to QuestionsToShow

            // Act
            var result = await cut.InvokeAsync(() =>
            {
                var method = typeof(SurveyModel).GetMethod("AllQuestionsAnswered", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (bool)method.Invoke(cut.Instance, null);
            });

            // Assert
            // This test will FAIL with current implementation because AllQuestionsAnswered checks ALL survey questions
            // After the fix, it should PASS because it only checks QuestionsToShow
            Assert.That(result, Is.True, 
                "Should return true when all loaded group questions are answered, even if other group questions are not");
        }

        [Test]
        public async Task BranchingSurvey_AllQuestionsAnswered_ReturnsFalse_WhenLoadedGroupNotAnswered()
        {
            // Arrange
            var survey = CreateBranchingSurvey();
            
            // Answer Question 1 with "Yes" (branches to Group 1)
            var mcAnswer = new MultipleChoiceAnswerViewModel
            {
                QuestionId = 10,
                AnswerType = AnswerType.MultipleChoice,
                SelectedOptionId = 10 // "Yes" - branches to Group 1
            };
            survey.Questions[0].Answers.Add(mcAnswer);

            // DON'T answer Question 2 in Group 1 (it should be validated)

            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);

            // Navigate with Preview query parameter
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyId}?Preview=true");

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            var cut = Context.RenderComponent<Survey>(parameters);
            await cut.InvokeAsync(async () => await cut.Instance.LoadData());

            // Access private field _groupsToVisit to simulate branching
            var groupsToVisitField = typeof(SurveyModel).GetField("_groupsToVisit", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var groupsToVisit = (HashSet<int>)groupsToVisitField.GetValue(cut.Instance);
            groupsToVisit.Add(0); // Group 0
            groupsToVisit.Add(1); // Group 1 (user selected "Yes")

            // Access QuestionsToShow and populate it with questions from Group 0 and Group 1
            var questionsToShowProp = typeof(SurveyModel).GetProperty("QuestionsToShow", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var questionsToShow = (List<QuestionViewModel>)questionsToShowProp.GetValue(cut.Instance);
            questionsToShow.Clear();
            questionsToShow.Add(survey.Questions[0]); // Q1 in Group 0 (answered)
            questionsToShow.Add(survey.Questions[1]); // Q2 in Group 1 (NOT answered)

            // Act
            var result = await cut.InvokeAsync(() =>
            {
                var method = typeof(SurveyModel).GetMethod("AllQuestionsAnswered", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (bool)method.Invoke(cut.Instance, null);
            });

            // Assert
            Assert.That(result, Is.False, 
                "Should return false when a required question in a loaded group is not answered");
        }

        [Test]
        public async Task BranchingSurvey_AllQuestionsAnswered_IgnoresUnloadedGroups()
        {
            // Arrange
            var survey = CreateBranchingSurvey();
            
            // Answer Question 1 with "Yes" (branches to Group 1, NOT Group 2)
            var mcAnswer = new MultipleChoiceAnswerViewModel
            {
                QuestionId = 10,
                AnswerType = AnswerType.MultipleChoice,
                SelectedOptionId = 10 // "Yes" - branches to Group 1
            };
            survey.Questions[0].Answers.Add(mcAnswer);

            // Answer Question 2 in Group 1
            var textAnswer = new TextAnswerViewModel
            {
                QuestionId = 11,
                AnswerType = AnswerType.Text,
                Text = "Pepperoni"
            };
            survey.Questions[1].Answers.Add(textAnswer);

            // Question 3 in Group 2 is required but NOT loaded (user didn't branch there)
            // This should NOT cause validation to fail

            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);

            // Navigate with Preview query parameter
            NavManager.NavigateTo($"http://localhost/survey/{_testSurveyId}?Preview=true");

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            var cut = Context.RenderComponent<Survey>(parameters);
            await cut.InvokeAsync(async () => await cut.Instance.LoadData());

            // Access private field _groupsToVisit to simulate branching
            var groupsToVisitField = typeof(SurveyModel).GetField("_groupsToVisit", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var groupsToVisit = (HashSet<int>)groupsToVisitField.GetValue(cut.Instance);
            groupsToVisit.Add(0); // Group 0
            groupsToVisit.Add(1); // Group 1 (user selected "Yes")
            // Group 2 is NOT added (user didn't branch there)

            // Access QuestionsToShow and populate it with questions from Group 0 and Group 1 only
            var questionsToShowProp = typeof(SurveyModel).GetProperty("QuestionsToShow", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var questionsToShow = (List<QuestionViewModel>)questionsToShowProp.GetValue(cut.Instance);
            questionsToShow.Clear();
            questionsToShow.Add(survey.Questions[0]); // Q1 in Group 0 (answered)
            questionsToShow.Add(survey.Questions[1]); // Q2 in Group 1 (answered)
            // Q3 in Group 2 is NOT added (not in branching path)

            // Act
            var result = await cut.InvokeAsync(() =>
            {
                var method = typeof(SurveyModel).GetMethod("AllQuestionsAnswered", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (bool)method.Invoke(cut.Instance, null);
            });

            // Assert
            // This test will FAIL with current implementation because AllQuestionsAnswered checks ALL survey questions including Q3
            // After the fix, it should PASS because it only checks QuestionsToShow (Q1 and Q2)
            Assert.That(result, Is.True, 
                "Should return true even when unloaded groups have required questions that are not answered");
        }
    }
}
