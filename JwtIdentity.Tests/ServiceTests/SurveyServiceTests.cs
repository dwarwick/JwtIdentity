using System;
using System.Linq;
using JwtIdentity.Models;
using JwtIdentity.Services;
using NUnit.Framework;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class SurveyServiceTests : TestBase
    {
        private SurveyService _service;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _service = new SurveyService(MockDbContext);
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
        public void DbContext_Property_IsSet()
        {
            Assert.That(_service.DbContext, Is.EqualTo(MockDbContext));
        }
    }
}
