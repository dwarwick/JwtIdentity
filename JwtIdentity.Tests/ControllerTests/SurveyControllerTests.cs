using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using JwtIdentity.Controllers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Models;
using JwtIdentity.Data;
using JwtIdentity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class SurveyControllerTests : TestBase<SurveyController>
    {
        private SurveyController _controller = null!;
        private List<Survey> _mockSurveys = null!;
        private List<Question> _mockQuestions = null!;
        private Mock<IOpenAi> MockOpenAiService = null!;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            SetupMockData();
            AddDataToDbContext();
            SetupMockMapper();
            SetupMockApiAuthService();
            MockOpenAiService = new Mock<IOpenAi>();
            MockOpenAiService.Setup(x => x.GenerateSurveyAsync(It.IsAny<string>())).ReturnsAsync(new SurveyViewModel { Questions = new List<QuestionViewModel>() });
            _controller = new SurveyController(MockDbContext, MockMapper.Object, MockApiAuthService.Object, MockLogger.Object, MockOpenAiService.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = HttpContext }
            };
        }

        private void SetupMockData()
        {
            _mockSurveys = new List<Survey>
            {
                new Survey
                {
                    Id = 1,
                    Title = "Survey 1",
                    Description = "Description 1",
                    Guid = "guid-1",
                    CreatedById = 1,
                    Published = true,
                    Questions = new List<Question>()
                },
                new Survey
                {
                    Id = 2,
                    Title = "Survey 2",
                    Description = "Description 2",
                    Guid = "guid-2",
                    CreatedById = 2,
                    Published = false,
                    Questions = new List<Question>()
                }
            };
            _mockQuestions = new List<Question>
            {
                new TextQuestion { Id = 1, Text = "Q1", SurveyId = 1, QuestionNumber = 1, QuestionType = QuestionType.Text, CreatedById = 1 },
                new TrueFalseQuestion { Id = 2, Text = "Q2", SurveyId = 1, QuestionNumber = 2, QuestionType = QuestionType.TrueFalse, CreatedById = 1 }
            };
            _mockSurveys[0].Questions.AddRange(_mockQuestions.Where(q => q.SurveyId == 1));
        }

        private void AddDataToDbContext()
        {
            foreach (var survey in _mockSurveys)
                MockDbContext.Surveys.Add(survey);
            foreach (var question in _mockQuestions)
                MockDbContext.Questions.Add(question);
            MockDbContext.SaveChanges();
        }

        private void SetupMockMapper()
        {
            MockMapper.Setup(m => m.Map<IEnumerable<SurveyViewModel>>(It.IsAny<List<Survey>>()))
                .Returns((List<Survey> surveys) => surveys.Select(s => new SurveyViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    Guid = s.Guid,
                    Questions = s.Questions?.Select(q => new TextQuestionViewModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        QuestionType = q.QuestionType,
                        SurveyId = q.SurveyId,
                        QuestionNumber = q.QuestionNumber
                    }).ToList<QuestionViewModel>() ?? new List<QuestionViewModel>()
                }));
            MockMapper.Setup(m => m.Map<SurveyViewModel>(It.IsAny<Survey>()))
                .Returns((Survey s) => new SurveyViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    Guid = s.Guid,
                    Questions = s.Questions?.Select(q => new TextQuestionViewModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        QuestionType = q.QuestionType,
                        SurveyId = q.SurveyId,
                        QuestionNumber = q.QuestionNumber
                    }).ToList<QuestionViewModel>() ?? new List<QuestionViewModel>()
                });
        }

        private void SetupMockApiAuthService()
        {
            MockApiAuthService.Setup(a => a.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(1);
        }

       

        [Test]
        public async Task GetSurvey_ExistingGuid_ReturnsSurvey()
        {
            var result = await _controller.GetSurvey("guid-1");
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            Assert.That(ok!.Value, Is.InstanceOf<SurveyViewModel>());
            var survey = ok.Value as SurveyViewModel;
            Assert.That(survey!.Guid, Is.EqualTo("guid-1"));
        }

        [Test]
        public async Task GetSurvey_NonExistingGuid_ReturnsNotFound()
        {
            var result = await _controller.GetSurvey("notfound");
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GetSurveysICreated_ReturnsSurveysForUser()
        {
            var result = await _controller.GetSurveysICreated();
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            var surveys = ok!.Value as IEnumerable<SurveyViewModel>;
            Assert.That(surveys!.All(s => s.Id == 1));
        }

        [Test]
        public async Task GetSurveysIAnswered_ReturnsSurveysUserAnswered()
        {
            // This test assumes the user has answered at least one question in survey 1
            var result = await _controller.GetSurveysIAnswered();
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task PostSurvey_NewSurvey_ReturnsCreatedSurvey()
        {
            var surveyVm = new SurveyViewModel
            {
                Title = "New Survey",
                Description = "Desc",
                Questions = new List<QuestionViewModel>()
            };
            MockMapper.Setup(m => m.Map<Survey>(It.IsAny<SurveyViewModel>())).Returns(new Survey { Id = 0, Title = "New Survey", Description = "Desc", Questions = new List<Question>() });
            var result = await _controller.PostSurvey(surveyVm);
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task PutSurvey_ExistingSurvey_UpdatesSurvey()
        {
            var surveyVm = new SurveyViewModel { Id = 1, Title = "Updated", Description = "Updated", Published = true };
            var result = await _controller.PutSurvey(surveyVm);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task PutSurvey_NonExistingSurvey_ReturnsNotFound()
        {
            var surveyVm = new SurveyViewModel { Id = 999, Title = "X", Description = "X" };
            var result = await _controller.PutSurvey(surveyVm);
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetSurvey_WithMultipleChoiceAndSelectAllThatApplyQuestions_LoadsOptions()
        {
            // Arrange
            var mcQuestion = new MultipleChoiceQuestion
            {
                Id = 10,
                Text = "MC?",
                SurveyId = 1,
                QuestionNumber = 3,
                QuestionType = QuestionType.MultipleChoice,
                CreatedById = 1,
                Options = new List<ChoiceOption> { new ChoiceOption { Id = 1, OptionText = "A", Order = 1 } }
            };
            var saQuestion = new SelectAllThatApplyQuestion
            {
                Id = 11,
                Text = "SA?",
                SurveyId = 1,
                QuestionNumber = 4,
                QuestionType = QuestionType.SelectAllThatApply,
                CreatedById = 1,
                Options = new List<ChoiceOption> { new ChoiceOption { Id = 2, OptionText = "B", Order = 1 } }
            };
            MockDbContext.Questions.AddRange(mcQuestion, saQuestion);
            MockDbContext.SaveChanges();
            _mockSurveys[0].Questions.Add(mcQuestion);
            _mockSurveys[0].Questions.Add(saQuestion);

            // Act
            var result = await _controller.GetSurvey("guid-1");
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            var survey = ok!.Value as SurveyViewModel;
            Assert.That(survey!.Questions.Any(q => q.QuestionType == QuestionType.MultipleChoice));
            Assert.That(survey.Questions.Any(q => q.QuestionType == QuestionType.SelectAllThatApply));
        }

        [Test]
        public async Task GetSurveysICreated_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange: Remove user from context
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            MockApiAuthService.Setup(a => a.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(0);
            // Act
            var result = await _controller.GetSurveysICreated();
            // Assert
            Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task PostSurvey_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange: Remove user from context
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            MockApiAuthService.Setup(a => a.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(0);
            var surveyVm = new SurveyViewModel { Title = "T", Description = "D", Questions = new List<QuestionViewModel>() };
            // Act
            var result = await _controller.PostSurvey(surveyVm);
            // Assert
            Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
        }
    }
}
