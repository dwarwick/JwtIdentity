using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtIdentity.Controllers;
using JwtIdentity.Models;
using JwtIdentity.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class OdataQuestionControllerTests : TestBase<OdataQuestionController>
    {
        private OdataQuestionController _controller = null!;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            // Seed the in-memory database with test questions
            MockDbContext.Questions.AddRange(new List<Question>
            {
                new TextQuestion
                {
                    Id = 1,
                    SurveyId = 10,
                    Text = "What is your name?",
                    QuestionNumber = 1,
                    QuestionType = QuestionType.Text,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new TrueFalseQuestion
                {
                    Id = 2,
                    SurveyId = 10,
                    Text = "Is this a test?",
                    QuestionNumber = 2,
                    QuestionType = QuestionType.TrueFalse,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddDays(-1)
                }
            });
            MockDbContext.SaveChanges();
            _controller = new OdataQuestionController(MockDbContext);
        }

        [Test]
        public void Get_ReturnsAllQuestionsAsBaseQuestionDto()
        {
            // Act
            var result = _controller.Get().ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[0].Text, Is.EqualTo("What is your name?"));
            Assert.That(result[1].Id, Is.EqualTo(2));
            Assert.That(result[1].Text, Is.EqualTo("Is this a test?"));
        }

        [Test]
        public void Get_ReturnsEmptyList_WhenNoQuestionsExist()
        {
            // Arrange
            foreach (var q in MockDbContext.Questions.ToList())
                MockDbContext.Questions.Remove(q);
            MockDbContext.SaveChanges();

            // Act
            var result = _controller.Get().ToList();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Get_FieldsMatchBaseQuestionDto()
        {
            // Act
            var dto = _controller.Get().First();

            // Assert
            Assert.That(dto.Id, Is.EqualTo(1));
            Assert.That(dto.SurveyId, Is.EqualTo(10));
            Assert.That(dto.Text, Is.EqualTo("What is your name?"));
            Assert.That(dto.QuestionNumber, Is.EqualTo(1));
            Assert.That(dto.QuestionType, Is.EqualTo(QuestionType.Text));
            Assert.That(dto.CreatedDate, Is.Not.EqualTo(default(DateTime)));
            Assert.That(dto.UpdatedDate, Is.Not.EqualTo(default(DateTime)));
        }
    }
}
