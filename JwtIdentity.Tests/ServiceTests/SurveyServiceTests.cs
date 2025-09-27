using System;
using System.Linq;
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
    }
}
