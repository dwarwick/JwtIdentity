using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using JwtIdentity.Services;
using JwtIdentity.Questions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class SurveyServiceTests : TestBase<SurveyService>
    {
        private SurveyService _service;
        private Mock<IQuestionTypeHandlerResolver> MockHandlerResolver = null!;
        private Mock<IQuestionTypeHandler> MockHandler = null!;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            SetupQuestionTypeHandlerMocks();
            _service = new SurveyService(MockDbContext, MockLogger.Object, MockHandlerResolver.Object);
        }

        private void SetupQuestionTypeHandlerMocks()
        {
            MockHandler = new Mock<IQuestionTypeHandler>();
            var definition = QuestionTypeRegistry.GetDefinition(QuestionType.Text);
            MockHandler.SetupGet(h => h.Definition).Returns(definition);
            MockHandler.SetupGet(h => h.QuestionType).Returns(QuestionType.Text);
            MockHandler.SetupGet(h => h.AnswerType).Returns(AnswerType.Text);
            MockHandler.SetupGet(h => h.QuestionEntityType).Returns(typeof(TextQuestion));
            MockHandler.SetupGet(h => h.AnswerEntityType).Returns(typeof(TextAnswer));
            MockHandler.Setup(h => h.EnsureDependenciesLoadedAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<IReadOnlyCollection<Question>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            MockHandler.Setup(h => h.PopulateSurveyDataAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<SurveyDataViewModel>(), It.IsAny<Question>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            MockHandler.Setup(h => h.CreateDemoAnswer(It.IsAny<Question>(), It.IsAny<ApplicationUser>(), It.IsAny<Random>()))
                .Returns((Answer)null);
            MockHandler.Setup(h => h.UpdateQuestionAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<Question>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            MockHandler.Setup(h => h.ShouldUpdateAnswer(It.IsAny<Answer>(), It.IsAny<Answer>())).Returns(false);

            MockHandlerResolver = new Mock<IQuestionTypeHandlerResolver>();
            MockHandlerResolver.SetupGet(r => r.Handlers).Returns(new List<IQuestionTypeHandler> { MockHandler.Object });
            MockHandlerResolver.Setup(r => r.GetHandler(It.IsAny<QuestionType>())).Returns(MockHandler.Object);
            MockHandlerResolver.Setup(r => r.GetHandler(It.IsAny<AnswerType>())).Returns(MockHandler.Object);
            MockHandlerResolver.Setup(r => r.EnsureDependenciesLoadedAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<IEnumerable<Question>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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
    }
}
