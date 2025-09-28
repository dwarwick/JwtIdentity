using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using JwtIdentity.Controllers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Interfaces;
using JwtIdentity.Models;
using JwtIdentity.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class QuestionControllerTests : TestBase<QuestionController>
    {
        private QuestionController _controller = null!;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            // Setup mock mapper for Question <-> QuestionViewModel
            MockMapper.Setup(m => m.Map<IEnumerable<QuestionViewModel>>(It.IsAny<List<Question>>()))
                .Returns((List<Question> questions) => questions.Select(q => new TextQuestionViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    SurveyId = q.SurveyId,
                    QuestionNumber = q.QuestionNumber
                }));
            MockMapper.Setup(m => m.Map<QuestionViewModel>(It.IsAny<Question>()))
                .Returns((Question q) => new TextQuestionViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    SurveyId = q.SurveyId,
                    QuestionNumber = q.QuestionNumber
                });
            MockMapper.Setup(m => m.Map<Question>(It.IsAny<QuestionViewModel>()))
                .Returns((QuestionViewModel vm) => new TextQuestion
                {
                    Id = vm.Id,
                    Text = vm.Text,
                    QuestionType = vm.QuestionType,
                    SurveyId = vm.SurveyId,
                    QuestionNumber = vm.QuestionNumber
                });
            var mockQuestionHandlerFactory = new Mock<IQuestionHandlerFactory>();
            var mockQuestionHandler = new Mock<IQuestionHandler>();
            mockQuestionHandler.Setup(h => h.LoadRelatedDataAsync(It.IsAny<List<int>>(), It.IsAny<ApplicationDbContext>()))
                .Returns(Task.CompletedTask);
            mockQuestionHandlerFactory.Setup(f => f.GetHandler(It.IsAny<QuestionType>()))
                .Returns(mockQuestionHandler.Object);
            _controller = new QuestionController(MockDbContext, MockMapper.Object, MockLogger.Object, mockQuestionHandlerFactory.Object)
            {
                // Set the controller context to use the mock HttpContext
                ControllerContext = new ControllerContext { HttpContext = HttpContext }
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
        }

        [Test]
        public async Task GetQuestionsContainingQuestionText_ReturnsQuestion()
        {
            var question = new TextQuestion { Id = 10, Text = "Q?", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            MockDbContext.Questions.Add(question);
            await MockDbContext.SaveChangesAsync();
            var result = await _controller.GetQuestionsContainingQuestionText(10);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var returned = okResult!.Value as QuestionViewModel;
            Assert.That(returned, Is.Not.Null);
            Assert.That(returned!.Id, Is.EqualTo(10));
        }

        [Test]
        public async Task GetQuestionsContainingQuestionText_MultipleChoice_ReturnsWithOptions()
        {
            var mc = new MultipleChoiceQuestion { Id = 11, Text = "MC?", SurveyId = 1, QuestionType = QuestionType.MultipleChoice, QuestionNumber = 1, Options = new List<ChoiceOption> { new ChoiceOption { Id = 1, OptionText = "A", Order = 1 } } };
            MockDbContext.Questions.Add(mc);
            await MockDbContext.SaveChangesAsync();
            var result = await _controller.GetQuestionsContainingQuestionText(11);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task GetQuestionsContainingQuestionText_SelectAllThatApply_ReturnsWithOptions()
        {
            var sa = new SelectAllThatApplyQuestion { Id = 12, Text = "SA?", SurveyId = 1, QuestionType = QuestionType.SelectAllThatApply, QuestionNumber = 1, Options = new List<ChoiceOption> { new ChoiceOption { Id = 2, OptionText = "B", Order = 1 } } };
            MockDbContext.Questions.Add(sa);
            await MockDbContext.SaveChangesAsync();
            var result = await _controller.GetQuestionsContainingQuestionText(12);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task DeleteQuestion_DeletesAndReorders()
        {
            var q1 = new TextQuestion { Id = 40, Text = "Q1", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            var q2 = new TextQuestion { Id = 41, Text = "Q2", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 2 };
            MockDbContext.Questions.AddRange(q1, q2);
            await MockDbContext.SaveChangesAsync();
            var result = await _controller.DeleteQuestion(40);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(MockDbContext.Questions.Count(), Is.EqualTo(1));
            Assert.That(MockDbContext.Questions.First().QuestionNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteQuestion_NotFound()
        {
            var result = await _controller.DeleteQuestion(999);
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}
