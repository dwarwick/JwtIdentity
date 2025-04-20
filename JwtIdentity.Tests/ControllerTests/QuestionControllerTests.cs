using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using JwtIdentity.Controllers;
using JwtIdentity.Common.ViewModels;
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
    public class QuestionControllerTests : TestBase
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
            _controller = new QuestionController(MockDbContext, MockMapper.Object);
            _controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
        }

        [Test]
        public async Task GetQuestions_ReturnsAllQuestions()
        {
            // Arrange
            var question = new TextQuestion { Id = 1, Text = "Sample?", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            MockDbContext.Questions.Add(question);
            await MockDbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetQuestions();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var questions = okResult!.Value as IEnumerable<QuestionViewModel>;
            Assert.That(questions, Is.Not.Null);
            Assert.That(questions!.Any(q => q.Id == question.Id));
        }

        [Test]
        public async Task GetQuestion_WithValidId_ReturnsQuestion()
        {
            // Arrange
            var question = new TextQuestion { Id = 2, Text = "What is your name?", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            MockDbContext.Questions.Add(question);
            await MockDbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetQuestion(2);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var returned = okResult!.Value as QuestionViewModel;
            Assert.That(returned, Is.Not.Null);
            Assert.That(returned!.Id, Is.EqualTo(2));
        }

        [Test]
        public async Task GetQuestion_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetQuestion(999);
            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
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
        public async Task PostQuestion_CreatesQuestion()
        {
            var vm = new TextQuestionViewModel { Id = 0, Text = "New Q", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            var result = await _controller.PostQuestion(vm);
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
            var returned = created!.Value as QuestionViewModel;
            Assert.That(returned, Is.Not.Null);
            Assert.That(returned!.Text, Is.EqualTo("New Q"));
        }

        [Test]
        public async Task PutQuestion_Valid_UpdatesQuestion()
        {
            var question = new TextQuestion { Id = 20, Text = "Old", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            MockDbContext.Questions.Add(question);
            await MockDbContext.SaveChangesAsync();
            MockDbContext.Entry(question).State = EntityState.Detached;
            var vm = new TextQuestionViewModel { Id = 20, Text = "Updated", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            var result = await _controller.PutQuestion(20, vm);
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task PutQuestion_BadRequest_WhenIdMismatch()
        {
            var vm = new TextQuestionViewModel { Id = 21, Text = "Mismatch", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            var result = await _controller.PutQuestion(99, vm);
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task PutQuestion_NotFound_WhenMissing()
        {
            var vm = new TextQuestionViewModel { Id = 22, Text = "Missing", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            var result = await _controller.PutQuestion(22, vm);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task UpdateQuestionNumbers_UpdatesNumbers()
        {
            var q1 = new TextQuestion { Id = 30, Text = "Q1", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 2 };
            var q2 = new TextQuestion { Id = 31, Text = "Q2", SurveyId = 1, QuestionType = QuestionType.Text, QuestionNumber = 1 };
            MockDbContext.Questions.AddRange(q1, q2);
            await MockDbContext.SaveChangesAsync();
            var vms = new List<QuestionViewModel> {
                new TextQuestionViewModel { Id = 30, QuestionNumber = 2 },
                new TextQuestionViewModel { Id = 31, QuestionNumber = 1 }
            };
            var result = await _controller.UpdateQuestionNumbers(vms);
            Assert.That(result, Is.InstanceOf<NoContentResult>());
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
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }
    }
}
