using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Controllers;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class SurveyControllerTests : TestBase<SurveyController>
    {
        private SurveyController _controller = null!;
        private List<Survey> _mockSurveys = null!;
        private List<Question> _mockQuestions = null!;
        private List<ApplicationUser> _mockUsers = null!;
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
            MockOpenAiService.Setup(x => x.GenerateSurveyAsync(It.IsAny<string>(), "")).ReturnsAsync(new SurveyViewModel { Questions = new List<QuestionViewModel>() });
            MockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            MockConfiguration.Setup(c => c["EmailSettings:CustomerServiceEmail"]).Returns("admin@example.com");
            _controller = new SurveyController(MockDbContext, MockMapper.Object, MockApiAuthService.Object, MockLogger.Object, MockOpenAiService.Object, MockEmailService.Object, MockConfiguration.Object)
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
            _mockUsers = new List<ApplicationUser>
            {
                new ApplicationUser { Id = 1, UserName = "user1", Email = "user1@example.com" },
                new ApplicationUser { Id = 2, UserName = "user2", Email = "user2@example.com" }
            };
        }

        private void AddDataToDbContext()
        {
            foreach (var survey in _mockSurveys)
                MockDbContext.Surveys.Add(survey);
            foreach (var question in _mockQuestions)
                MockDbContext.Questions.Add(question);
            foreach (var user in _mockUsers)
                MockDbContext.ApplicationUsers.Add(user);
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
        public async Task GetSurveysICreated_Admin_ReturnsAllSurveys()
        {
            HttpContext.User = CreateClaimsPrincipal(1, "admin", new[] { "Admin" });
            var result = await _controller.GetSurveysICreated();
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            var surveys = ok!.Value as IEnumerable<SurveyViewModel>;
            Assert.That(surveys!.Count(), Is.EqualTo(_mockSurveys.Count));
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
            MockEmailService.Verify(e => e.SendEmailAsync("admin@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("New Survey"))), Times.Once);
            MockEmailService.Verify(e => e.SendEmailAsync("user1@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("New Survey"))), Times.Once);
        }

        [Test]
        public async Task PutSurvey_PublishSurvey_SendsEmails()
        {
            MockApiAuthService.Setup(a => a.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(2);
            var surveyVm = new SurveyViewModel
            {
                Id = 2,
                Title = "Survey 2",
                Description = "Description 2",
                Published = true
            };
            var result = await _controller.PutSurvey(surveyVm);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            MockEmailService.Verify(e => e.SendEmailAsync("admin@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("Survey 2"))), Times.Once);
            MockEmailService.Verify(e => e.SendEmailAsync("user2@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("Survey 2"))), Times.Once);
        }

        [Test]
        public async Task PostSurvey_RemovedChoiceOption_DeletesOption()
        {
            // Arrange existing multiple choice question with two options
            var mcQuestion = new MultipleChoiceQuestion
            {
                Id = 10,
                Text = "MC?",
                SurveyId = 1,
                QuestionNumber = 3,
                QuestionType = QuestionType.MultipleChoice,
                CreatedById = 1,
                Options = new List<ChoiceOption>
                {
                    new ChoiceOption { Id = 1, OptionText = "A", Order = 1, MultipleChoiceQuestionId = 10 },
                    new ChoiceOption { Id = 2, OptionText = "B", Order = 2, MultipleChoiceQuestionId = 10 }
                }
            };
            MockDbContext.Questions.Add(mcQuestion);
            MockDbContext.ChoiceOptions.AddRange(mcQuestion.Options);
            MockDbContext.SaveChanges();

            var surveyVm = new SurveyViewModel
            {
                Id = 1,
                Title = "Survey 1",
                Description = "Description 1",
                Questions = new List<QuestionViewModel>
                {
                    new MultipleChoiceQuestionViewModel
                    {
                        Id = 10,
                        SurveyId = 1,
                        Text = "MC?",
                        QuestionNumber = 3,
                        QuestionType = QuestionType.MultipleChoice,
                        Options = new List<ChoiceOptionViewModel>
                        {
                            new ChoiceOptionViewModel { Id = 1, OptionText = "A", Order = 1, MultipleChoiceQuestionId = 10 }
                            // Option with Id 2 removed
                        }
                    }
                }
            };

            MockMapper.Setup(m => m.Map<Survey>(It.IsAny<SurveyViewModel>())).Returns((SurveyViewModel svm) =>
            {
                return new Survey
                {
                    Id = svm.Id,
                    Title = svm.Title,
                    Description = svm.Description,
                    Guid = svm.Guid,
                    Questions = svm.Questions.Select(qvm => new MultipleChoiceQuestion
                    {
                        Id = qvm.Id,
                        Text = qvm.Text,
                        SurveyId = svm.Id,
                        QuestionNumber = qvm.QuestionNumber,
                        QuestionType = qvm.QuestionType,
                        Options = ((MultipleChoiceQuestionViewModel)qvm).Options.Select(o => new ChoiceOption
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            Order = o.Order,
                            MultipleChoiceQuestionId = qvm.Id
                        }).ToList()
                    }).ToList<Question>()
                };
            });

            // Act
            var result = await _controller.PostSurvey(surveyVm);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(MockDbContext.ChoiceOptions.Any(o => o.Id == 2), Is.False);
            Assert.That(MockDbContext.ChoiceOptions.Any(o => o.Id == 1), Is.True);
        }

        [Test]
        public async Task PostSurvey_NewChoiceOption_AddsOption()
        {
            // Arrange existing multiple choice question with one option
            var mcQuestion = new MultipleChoiceQuestion
            {
                Id = 20,
                Text = "MC?",
                SurveyId = 1,
                QuestionNumber = 3,
                QuestionType = QuestionType.MultipleChoice,
                CreatedById = 1,
                Options = new List<ChoiceOption>
                {
                    new ChoiceOption { Id = 1, OptionText = "A", Order = 1, MultipleChoiceQuestionId = 20 }
                }
            };
            MockDbContext.Questions.Add(mcQuestion);
            MockDbContext.ChoiceOptions.AddRange(mcQuestion.Options);
            MockDbContext.SaveChanges();

            var surveyVm = new SurveyViewModel
            {
                Id = 1,
                Title = "Survey 1",
                Description = "Description 1",
                Questions = new List<QuestionViewModel>
                {
                    new MultipleChoiceQuestionViewModel
                    {
                        Id = 20,
                        SurveyId = 1,
                        Text = "MC?",
                        QuestionNumber = 3,
                        QuestionType = QuestionType.MultipleChoice,
                        Options = new List<ChoiceOptionViewModel>
                        {
                            new ChoiceOptionViewModel { Id = 1, OptionText = "A", Order = 1, MultipleChoiceQuestionId = 20 },
                            new ChoiceOptionViewModel { Id = 0, OptionText = "C", Order = 2, MultipleChoiceQuestionId = 20 }
                        }
                    }
                }
            };

            MockMapper.Setup(m => m.Map<Survey>(It.IsAny<SurveyViewModel>())).Returns((SurveyViewModel svm) =>
            {
                return new Survey
                {
                    Id = svm.Id,
                    Title = svm.Title,
                    Description = svm.Description,
                    Guid = svm.Guid,
                    Questions = svm.Questions.Select(qvm => new MultipleChoiceQuestion
                    {
                        Id = qvm.Id,
                        Text = qvm.Text,
                        SurveyId = svm.Id,
                        QuestionNumber = qvm.QuestionNumber,
                        QuestionType = qvm.QuestionType,
                        Options = ((MultipleChoiceQuestionViewModel)qvm).Options.Select(o => new ChoiceOption
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            Order = o.Order,
                            MultipleChoiceQuestionId = qvm.Id
                        }).ToList()
                    }).ToList<Question>()
                };
            });

            // Act
            var result = await _controller.PostSurvey(surveyVm);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(MockDbContext.ChoiceOptions.Count(o => o.MultipleChoiceQuestionId == 20), Is.EqualTo(2));
            Assert.That(MockDbContext.ChoiceOptions.Any(o => o.OptionText == "C"), Is.True);
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
