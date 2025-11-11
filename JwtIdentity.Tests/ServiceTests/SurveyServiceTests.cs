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
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 1 }
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
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 1 },
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 2 }
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
                    new TextQuestion { Id = 1, Text = "Q1", QuestionNumber = 1, QuestionType = QuestionType.Text, GroupId = 1 },
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 2 }
                },
                QuestionGroups = new System.Collections.Generic.List<QuestionGroup>
                {
                    new QuestionGroup { Id = 1, SurveyId = 1, GroupNumber = 0, GroupName = "Default", NextGroupId = 2 },
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
                        GroupId = 1,
                        BranchToGroupIdOnTrue = 2
                    },
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 2 }
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
                GroupId = 1
            };

            var choiceOption = new ChoiceOption 
            { 
                Id = 1, 
                OptionText = "Option 1", 
                MultipleChoiceQuestionId = 1, 
                BranchToGroupId = 2 
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
                    new TextQuestion { Id = 2, Text = "Q2", QuestionNumber = 2, QuestionType = QuestionType.Text, GroupId = 2 }
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
    }
}
