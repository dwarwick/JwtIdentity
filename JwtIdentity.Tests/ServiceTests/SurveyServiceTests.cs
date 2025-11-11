using System;
using System.Linq;
using System.Threading.Tasks;
using JwtIdentity.Common.Helpers;
using JwtIdentity.Interfaces;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Moq;
using NUnit.Framework;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class SurveyServiceTests : TestBase<SurveyService>
    {
        private SurveyService _service;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            var mockQuestionHandlerFactory = new Mock<IQuestionHandlerFactory>();
            _service = new SurveyService(MockDbContext, MockLogger.Object, mockQuestionHandlerFactory.Object);
        }

        [Test]
        public void GetSurvey_ReturnsSurvey_WhenGuidExists()
        {
            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Desc",
                Guid = "abc-123",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>()
            };
            MockDbContext.Surveys.Add(survey);
            MockDbContext.SaveChanges();

            var result = _service.GetSurvey("abc-123");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Guid, Is.EqualTo("abc-123"));
        }

        [Test]
        public void GetSurvey_ReturnsNull_WhenGuidDoesNotExist()
        {
            var result = _service.GetSurvey("notfound");
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ValidateSurveyForPublishing_ReturnsValid_WhenNoQuestionGroups()
        {
            // Arrange
            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Test Description",
                Guid = "test-guid",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>
                {
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 0 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>()
            };
            MockDbContext.Surveys.Add(survey);
            MockDbContext.SaveChanges();

            // Act
            var (isValid, errorMessage) = await _service.ValidateSurveyForPublishingAsync(survey.Id);

            // Assert
            Assert.That(isValid, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public async Task ValidateSurveyForPublishing_ReturnsInvalid_WhenGroupIsEmpty()
        {
            // Arrange
            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Test Description",
                Guid = "test-guid",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>
                {
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 0 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>
                {
                    new QuestionGroup { Id = 1, SurveyId = 1, GroupNumber = 0, GroupName = "Default" },
                    new QuestionGroup { Id = 2, SurveyId = 1, GroupNumber = 1, GroupName = "Empty Group" }
                }
            };
            MockDbContext.Surveys.Add(survey);
            MockDbContext.SaveChanges();

            // Act
            var (isValid, errorMessage) = await _service.ValidateSurveyForPublishingAsync(survey.Id);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Does.Contain("empty group").IgnoreCase);
            Assert.That(errorMessage, Does.Contain("Empty Group"));
        }

        [Test]
        public async Task ValidateSurveyForPublishing_ReturnsInvalid_WhenGroupIsOrphaned()
        {
            // Arrange
            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Test Description",
                Guid = "test-guid",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>
                {
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 0 },
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 1 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>
                {
                    new QuestionGroup { Id = 1, SurveyId = 1, GroupNumber = 0, GroupName = "Default" },
                    new QuestionGroup { Id = 2, SurveyId = 1, GroupNumber = 1, GroupName = "Orphaned Group" }
                }
            };
            MockDbContext.Surveys.Add(survey);
            MockDbContext.SaveChanges();

            // Act
            var (isValid, errorMessage) = await _service.ValidateSurveyForPublishingAsync(survey.Id);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Does.Contain("unreachable group").IgnoreCase);
            Assert.That(errorMessage, Does.Contain("Orphaned Group"));
        }

        [Test]
        public async Task ValidateSurveyForPublishing_ReturnsValid_WhenGroupConnectedViaNextGroupId()
        {
            // Arrange
            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Test Description",
                Guid = "test-guid",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>
                {
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 0 },
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 1 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>
                {
                    new QuestionGroup { Id = 1, SurveyId = 1, GroupNumber = 0, GroupName = "Default", NextGroupId = 1 },
                    new QuestionGroup { Id = 2, SurveyId = 1, GroupNumber = 1, GroupName = "Connected Group" }
                }
            };
            MockDbContext.Surveys.Add(survey);
            MockDbContext.SaveChanges();

            // Act
            var (isValid, errorMessage) = await _service.ValidateSurveyForPublishingAsync(survey.Id);

            // Assert
            Assert.That(isValid, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public async Task ValidateSurveyForPublishing_ReturnsValid_WhenGroupConnectedViaTrueFalseBranching()
        {
            // Arrange
            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Test Description",
                Guid = "test-guid",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>
                {
                    new TrueFalseQuestion 
                    { 
                        Id = 1, 
                        Text = "Q1", 
                        QuestionNumber = 1, 
                        QuestionType = QuestionType.TrueFalse, 
                        GroupId = 0,
                        BranchToGroupIdOnTrue = 1
                    },
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 1 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>
                {
                    new QuestionGroup { Id = 1, SurveyId = 1, GroupNumber = 0, GroupName = "Default" },
                    new QuestionGroup { Id = 2, SurveyId = 1, GroupNumber = 1, GroupName = "Connected via True" }
                }
            };
            MockDbContext.Surveys.Add(survey);
            MockDbContext.SaveChanges();

            // Act
            var (isValid, errorMessage) = await _service.ValidateSurveyForPublishingAsync(survey.Id);

            // Assert
            Assert.That(isValid, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public async Task ValidateSurveyForPublishing_ReturnsValid_WhenGroupConnectedViaMultipleChoiceBranching()
        {
            // Arrange
            var mcQuestion = new MultipleChoiceQuestion 
            { 
                Id = 1, 
                Text = "Q1", 
                QuestionNumber = 1, 
                QuestionType = QuestionType.MultipleChoice, 
                GroupId = 0
            };

            var choiceOption = new ChoiceOption 
            { 
                Id = 1, 
                OptionText = "Option 1", 
                MultipleChoiceQuestionId = 1, 
                BranchToGroupId = 1 
            };

            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Test Description",
                Guid = "test-guid",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>
                {
                    mcQuestion,
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 1 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>
                {
                    new QuestionGroup { Id = 1, SurveyId = 1, GroupNumber = 0, GroupName = "Default" },
                    new QuestionGroup { Id = 2, SurveyId = 1, GroupNumber = 1, GroupName = "Connected via MC" }
                }
            };
            
            MockDbContext.Surveys.Add(survey);
            MockDbContext.ChoiceOptions.Add(choiceOption);
            MockDbContext.SaveChanges();

            // Act
            var (isValid, errorMessage) = await _service.ValidateSurveyForPublishingAsync(survey.Id);

            // Assert
            Assert.That(isValid, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public async Task ValidateSurveyForPublishing_RealWorldScenario_KitchenReachableViaSelectAll()
        {
            // This mimics the real scenario from the screenshots
            // Default group has multiple questions including a SelectAllThatApply that branches to Kitchen
            // Kitchen has a question
            // Bathroom has a question but is orphaned
            
            var survey = new Survey
            {
                Title = "Customer Satisfaction Survey",
                Description = "Test",
                Guid = "test-guid",
                Published = false,
                Questions = new System.Collections.Generic.List<Question>
                {
                    // Default group questions
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 0 },
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 0 },
                    // Q8 - The last question in default group with branching to Kitchen
                    new SelectAllThatApplyQuestion 
                    { 
                        Id = 8, 
                        Text = "Q8: Which areas of your home were included in this remodel?", 
                        QuestionNumber = 8, 
                        QuestionType = QuestionType.SelectAllThatApply, 
                        GroupId = 0
                    },
                    // Kitchen group question
                    new TextQuestion { Id = 10, Text = "Q10: What did you have done in the kitchen?", QuestionNumber = 10, QuestionType = QuestionType.Text, GroupId = 1 },
                    // Bathroom group question (orphaned)
                    new TextQuestion { Id = 9, Text = "Q9: What did you have done in the Bathroom?", QuestionNumber = 9, QuestionType = QuestionType.Text, GroupId = 2 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>
                {
                    new QuestionGroup { Id = 1, SurveyId = 1, GroupNumber = 0, GroupName = "Default Group" },
                    new QuestionGroup { Id = 2, SurveyId = 1, GroupNumber = 1, GroupName = "Kitchen" },
                    new QuestionGroup { Id = 3, SurveyId = 1, GroupNumber = 2, GroupName = "Bathroom" }
                }
            };
            
            // Add choice option for Q8 that branches to Kitchen (GroupNumber = 1)
            var kitchenOption = new ChoiceOption 
            { 
                Id = 1, 
                OptionText = "Kitchen", 
                SelectAllThatApplyQuestionId = 8, 
                BranchToGroupId = 1  // Branches to Kitchen group (GroupNumber 1)
            };
            
            MockDbContext.Surveys.Add(survey);
            MockDbContext.ChoiceOptions.Add(kitchenOption);
            MockDbContext.SaveChanges();

            // Act
            var (isValid, errorMessage) = await _service.ValidateSurveyForPublishingAsync(survey.Id);

            // Assert
            Assert.That(isValid, Is.False, "Should fail because Bathroom is orphaned");
            Assert.That(errorMessage, Does.Contain("unreachable").IgnoreCase);
            Assert.That(errorMessage, Does.Contain("Bathroom"), "Should only mention Bathroom, not Kitchen");
            Assert.That(errorMessage, Does.Not.Contain("Kitchen"), "Kitchen should NOT be in error since it's reachable");
        }
    }
}
