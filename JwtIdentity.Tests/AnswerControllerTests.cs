using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using JwtIdentity.Controllers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Net;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using JwtIdentity.Common.Helpers;
using JwtIdentity.Data;
using System.Security.Claims;
using JwtIdentity.Services;

namespace JwtIdentity.Tests
{
    [TestFixture]
    public class AnswerControllerTests : TestBase
    {
        private AnswerController _controller = null!;
        private Mock<DbSet<Answer>> _mockAnswersDbSet = null!;
        private Mock<DbSet<Survey>> _mockSurveysDbSet = null!;
        private Mock<DbSet<Question>> _mockQuestionsDbSet = null!;
        private Mock<DbSet<MultipleChoiceQuestion>> _mockMultipleChoiceQuestionsDbSet = null!;
        private Mock<DbSet<SelectAllThatApplyQuestion>> _mockSelectAllThatApplyQuestionsDbSet = null!;
        private List<Answer> _mockAnswers = null!;
        private List<Survey> _mockSurveys = null!;
        private List<Question> _mockQuestions = null!;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();

            // Initialize mock data
            SetupMockData();
            SetupMockDbSets();
            
            // Add data to the in-memory database
            AddDataToDbContext();
            
            SetupMockMapper();
            SetupMockApiAuthService();
            
            // Create controller
            _controller = new AnswerController(MockDbContext, MockMapper.Object, MockApiAuthService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = HttpContext
            };
        }

        #region Test Setup

        private void SetupMockData()
        {
            // Setup test users
            var user1 = new ApplicationUser { Id = 1, UserName = "testuser1", Email = "test1@example.com" };
            var user2 = new ApplicationUser { Id = 2, UserName = "testuser2", Email = "test2@example.com" };
            
            // Setup test surveys
            _mockSurveys = new List<Survey>
            {
                new Survey 
                { 
                    Id = 1, 
                    Title = "Test Survey 1", 
                    Description = "Survey for testing", 
                    Guid = "survey-1-guid", 
                    CreatedById = 1, 
                    Published = true,
                    Questions = new List<Question>()
                },
                new Survey 
                { 
                    Id = 2, 
                    Title = "Test Survey 2", 
                    Description = "Another survey for testing", 
                    Guid = "survey-2-guid", 
                    CreatedById = 1, 
                    Published = false,
                    Questions = new List<Question>()
                }
            };

            // Setup test questions
            _mockQuestions = new List<Question>
            {
                new TextQuestion 
                { 
                    Id = 1, 
                    Text = "What is your name?", 
                    SurveyId = 1, 
                    QuestionNumber = 1,
                    QuestionType = QuestionType.Text,
                    CreatedById = 1
                },
                new TrueFalseQuestion 
                { 
                    Id = 2, 
                    Text = "Is this a test?", 
                    SurveyId = 1, 
                    QuestionNumber = 2,
                    QuestionType = QuestionType.TrueFalse,
                    CreatedById = 1
                },
                new Rating1To10Question 
                { 
                    Id = 3, 
                    Text = "Rate this survey", 
                    SurveyId = 1, 
                    QuestionNumber = 3,
                    QuestionType = QuestionType.Rating1To10,
                    CreatedById = 1
                },
                new MultipleChoiceQuestion
                {
                    Id = 4,
                    Text = "Which color do you prefer?",
                    SurveyId = 1,
                    QuestionNumber = 4,
                    QuestionType = QuestionType.MultipleChoice,
                    CreatedById = 1,
                    Options = new List<ChoiceOption>
                    {
                        new ChoiceOption { Id = 1, OptionText = "Red", Order = 1 },
                        new ChoiceOption { Id = 2, OptionText = "Green", Order = 2 },
                        new ChoiceOption { Id = 3, OptionText = "Blue", Order = 3 }
                    }
                },
                new SelectAllThatApplyQuestion
                {
                    Id = 5,
                    Text = "Which programming languages do you know?",
                    SurveyId = 1,
                    QuestionNumber = 5,
                    QuestionType = QuestionType.SelectAllThatApply,
                    CreatedById = 1,
                    Options = new List<ChoiceOption>
                    {
                        new ChoiceOption { Id = 4, OptionText = "C#", Order = 1 },
                        new ChoiceOption { Id = 5, OptionText = "JavaScript", Order = 2 },
                        new ChoiceOption { Id = 6, OptionText = "Python", Order = 3 }
                    }
                }
            };

            // Link questions to surveys
            foreach (var question in _mockQuestions)
            {
                var survey = _mockSurveys.FirstOrDefault(s => s.Id == question.SurveyId);
                if (survey != null)
                {
                    survey.Questions.Add(question);
                }
            }

            // Setup test answers
            _mockAnswers = new List<Answer>
            {
                new TextAnswer
                {
                    Id = 1,
                    QuestionId = 1,
                    Text = "John Doe",
                    CreatedById = 2,
                    IpAddress = "192.168.1.1",
                    Complete = true,
                    AnswerType = AnswerType.Text,
                    Question = _mockQuestions.FirstOrDefault(q => q.Id == 1)!
                },
                new TrueFalseAnswer
                {
                    Id = 2,
                    QuestionId = 2,
                    Value = true,
                    CreatedById = 2,
                    IpAddress = "192.168.1.1",
                    Complete = true,
                    AnswerType = AnswerType.TrueFalse,
                    Question = _mockQuestions.FirstOrDefault(q => q.Id == 2)!
                },
                new Rating1To10Answer
                {
                    Id = 3,
                    QuestionId = 3,
                    SelectedOptionId = 8,
                    CreatedById = 2,
                    IpAddress = "192.168.1.1",
                    Complete = true,
                    AnswerType = AnswerType.Rating1To10,
                    Question = _mockQuestions.FirstOrDefault(q => q.Id == 3)!
                },
                new MultipleChoiceAnswer
                {
                    Id = 4,
                    QuestionId = 4,
                    SelectedOptionId = 2,
                    CreatedById = 2,
                    IpAddress = "192.168.1.1",
                    Complete = true,
                    AnswerType = AnswerType.MultipleChoice,
                    Question = _mockQuestions.FirstOrDefault(q => q.Id == 4)!
                },
                new SelectAllThatApplyAnswer
                {
                    Id = 5,
                    QuestionId = 5,
                    SelectedOptionIds = "4,6",
                    CreatedById = 2,
                    IpAddress = "192.168.1.1",
                    Complete = true,
                    AnswerType = AnswerType.SelectAllThatApply,
                    Question = _mockQuestions.FirstOrDefault(q => q.Id == 5)!
                }
            };

            // Setup IPAddress for HttpContext
            HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        }

        private void SetupMockDbSets()
        {
            // Setup mock DbSets
            _mockAnswersDbSet = MockDbSetFactory.CreateMockDbSet(_mockAnswers);
            _mockSurveysDbSet = MockDbSetFactory.CreateMockDbSet(_mockSurveys);
            _mockQuestionsDbSet = MockDbSetFactory.CreateMockDbSet(_mockQuestions);
            _mockMultipleChoiceQuestionsDbSet = MockDbSetFactory.CreateMockDbSet(_mockQuestions.OfType<MultipleChoiceQuestion>().ToList());
            _mockSelectAllThatApplyQuestionsDbSet = MockDbSetFactory.CreateMockDbSet(_mockQuestions.OfType<SelectAllThatApplyQuestion>().ToList());
        }

        private void AddDataToDbContext()
        {
            // Add data to the in-memory database
            foreach (var survey in _mockSurveys)
            {
                MockDbContext.Surveys.Add(survey);
            }
            
            foreach (var question in _mockQuestions)
            {
                MockDbContext.Questions.Add(question);
            }
            
            foreach (var answer in _mockAnswers)
            {
                MockDbContext.Answers.Add(answer);
            }
            
            MockDbContext.SaveChanges();
        }

        private void SetupMockMapper()
        {
            // Setup mapper for Answer to AnswerViewModel
            MockMapper.Setup(m => m.Map<AnswerViewModel>(It.IsAny<Answer>()))
                .Returns((Answer answer) => {
                    AnswerViewModel viewModel;
                    switch (answer.AnswerType)
                    {
                        case AnswerType.Text:
                            viewModel = new TextAnswerViewModel 
                            {
                                Text = ((TextAnswer)answer).Text 
                            };
                            break;
                        case AnswerType.TrueFalse:
                            viewModel = new TrueFalseAnswerViewModel 
                            {
                                Value = ((TrueFalseAnswer)answer).Value 
                            };
                            break;
                        case AnswerType.Rating1To10:
                            viewModel = new Rating1To10AnswerViewModel 
                            {
                                SelectedOptionId = ((Rating1To10Answer)answer).SelectedOptionId 
                            };
                            break;
                        case AnswerType.MultipleChoice:
                            viewModel = new MultipleChoiceAnswerViewModel 
                            {
                                SelectedOptionId = ((MultipleChoiceAnswer)answer).SelectedOptionId 
                            };
                            break;
                        case AnswerType.SelectAllThatApply:
                            viewModel = new SelectAllThatApplyAnswerViewModel 
                            {
                                SelectedOptionIds = ((SelectAllThatApplyAnswer)answer).SelectedOptionIds 
                            };
                            break;
                        default:
                            viewModel = new TextAnswerViewModel();
                            break;
                    }
                    
                    viewModel.Id = answer.Id;
                    viewModel.QuestionId = answer.QuestionId;
                    viewModel.CreatedById = answer.CreatedById;
                    viewModel.Complete = answer.Complete;
                    viewModel.AnswerType = answer.AnswerType;
                    
                    return viewModel;
                });

            // Setup mapper for List of Answers to List of AnswerViewModels
            MockMapper.Setup(m => m.Map<IEnumerable<AnswerViewModel>>(It.IsAny<List<Answer>>()))
                .Returns((List<Answer> answers) => answers.Select(a => {
                    AnswerViewModel viewModel;
                    switch (a.AnswerType)
                    {
                        case AnswerType.Text:
                            viewModel = new TextAnswerViewModel 
                            {
                                Text = ((TextAnswer)a).Text 
                            };
                            break;
                        case AnswerType.TrueFalse:
                            viewModel = new TrueFalseAnswerViewModel 
                            {
                                Value = ((TrueFalseAnswer)a).Value 
                            };
                            break;
                        case AnswerType.Rating1To10:
                            viewModel = new Rating1To10AnswerViewModel 
                            {
                                SelectedOptionId = ((Rating1To10Answer)a).SelectedOptionId 
                            };
                            break;
                        case AnswerType.MultipleChoice:
                            viewModel = new MultipleChoiceAnswerViewModel 
                            {
                                SelectedOptionId = ((MultipleChoiceAnswer)a).SelectedOptionId 
                            };
                            break;
                        case AnswerType.SelectAllThatApply:
                            viewModel = new SelectAllThatApplyAnswerViewModel 
                            {
                                SelectedOptionIds = ((SelectAllThatApplyAnswer)a).SelectedOptionIds 
                            };
                            break;
                        default:
                            viewModel = new TextAnswerViewModel();
                            break;
                    }
                    
                    viewModel.Id = a.Id;
                    viewModel.QuestionId = a.QuestionId;
                    viewModel.CreatedById = a.CreatedById;
                    viewModel.Complete = a.Complete;
                    viewModel.AnswerType = a.AnswerType;
                    
                    return viewModel;
                }));

            // Setup mapper for AnswerViewModel to Answer
            MockMapper.Setup(m => m.Map<Answer>(It.IsAny<AnswerViewModel>()))
                .Returns((AnswerViewModel model) =>
                {
                    Answer answer;
                    switch (model.AnswerType)
                    {
                        case AnswerType.Text:
                            answer = new TextAnswer { Text = ((TextAnswerViewModel)model).Text };
                            break;
                        case AnswerType.TrueFalse:
                            answer = new TrueFalseAnswer { Value = ((TrueFalseAnswerViewModel)model).Value };
                            break;
                        case AnswerType.Rating1To10:
                            answer = new Rating1To10Answer { SelectedOptionId = ((Rating1To10AnswerViewModel)model).SelectedOptionId };
                            break;
                        case AnswerType.MultipleChoice:
                            answer = new MultipleChoiceAnswer { SelectedOptionId = ((MultipleChoiceAnswerViewModel)model).SelectedOptionId };
                            break;
                        case AnswerType.SelectAllThatApply:
                            answer = new SelectAllThatApplyAnswer { SelectedOptionIds = ((SelectAllThatApplyAnswerViewModel)model).SelectedOptionIds };
                            break;
                        default:
                            answer = new TextAnswer();
                            break;
                    }
                    
                    answer.Id = model.Id;
                    answer.QuestionId = model.QuestionId;
                    answer.CreatedById = model.CreatedById;
                    answer.Complete = model.Complete;
                    answer.AnswerType = model.AnswerType;
                    
                    return answer;
                });

            // Setup mapper for Survey to SurveyViewModel
            MockMapper.Setup(m => m.Map<SurveyViewModel>(It.IsAny<Survey>()))
                .Returns((Survey survey) => new SurveyViewModel
                {
                    Id = survey.Id,
                    Title = survey.Title,
                    Description = survey.Description,
                    Guid = survey.Guid,
                    Questions = survey.Questions?.Select(q => {
                        QuestionViewModel questionVm;
                        switch (q.QuestionType)
                        {
                            case QuestionType.Text:
                                questionVm = new TextQuestionViewModel();
                                break;
                            case QuestionType.TrueFalse:
                                questionVm = new TrueFalseQuestionViewModel();
                                break;
                            case QuestionType.Rating1To10:
                                questionVm = new Rating1To10QuestionViewModel();
                                break;
                            case QuestionType.MultipleChoice:
                                questionVm = new MultipleChoiceQuestionViewModel
                                {
                                    Options = ((MultipleChoiceQuestion)q).Options?.Select(o => new ChoiceOptionViewModel
                                    {
                                        Id = o.Id,
                                        OptionText = o.OptionText,
                                        Order = o.Order
                                    }).ToList()
                                };
                                break;
                            case QuestionType.SelectAllThatApply:
                                questionVm = new SelectAllThatApplyQuestionViewModel
                                {
                                    Options = ((SelectAllThatApplyQuestion)q).Options?.Select(o => new ChoiceOptionViewModel
                                    {
                                        Id = o.Id,
                                        OptionText = o.OptionText,
                                        Order = o.Order
                                    }).ToList()
                                };
                                break;
                            default:
                                questionVm = new TextQuestionViewModel();
                                break;
                        }
                        
                        questionVm.Id = q.Id;
                        questionVm.Text = q.Text;
                        questionVm.QuestionType = q.QuestionType;
                        questionVm.QuestionNumber = q.QuestionNumber;
                        questionVm.SurveyId = q.SurveyId;
                        
                        return questionVm;
                    }).ToList<QuestionViewModel>()
                });

            // Setup mapper for Question to QuestionViewModel
            MockMapper.Setup(m => m.Map<QuestionViewModel>(It.IsAny<Question>()))
                .Returns((Question question) => {
                    QuestionViewModel questionVm;
                    switch (question.QuestionType)
                    {
                        case QuestionType.Text:
                            questionVm = new TextQuestionViewModel();
                            break;
                        case QuestionType.TrueFalse:
                            questionVm = new TrueFalseQuestionViewModel();
                            break;
                        case QuestionType.Rating1To10:
                            questionVm = new Rating1To10QuestionViewModel();
                            break;
                        case QuestionType.MultipleChoice:
                            questionVm = new MultipleChoiceQuestionViewModel();
                            break;
                        case QuestionType.SelectAllThatApply:
                            questionVm = new SelectAllThatApplyQuestionViewModel();
                            break;
                        default:
                            questionVm = new TextQuestionViewModel();
                            break;
                    }
                    
                    questionVm.Id = question.Id;
                    questionVm.Text = question.Text;
                    questionVm.QuestionType = question.QuestionType;
                    questionVm.QuestionNumber = question.QuestionNumber;
                    questionVm.SurveyId = question.SurveyId;
                    
                    return questionVm;
                });

            // Additional mappers for specialized question types
            MockMapper.Setup(m => m.Map<TextQuestionViewModel>(It.IsAny<Question>()))
                .Returns((Question question) => new TextQuestionViewModel
                {
                    Id = question.Id,
                    Text = question.Text,
                    QuestionType = QuestionType.Text,
                    QuestionNumber = question.QuestionNumber,
                    SurveyId = question.SurveyId
                });

            MockMapper.Setup(m => m.Map<TrueFalseQuestionViewModel>(It.IsAny<Question>()))
                .Returns((Question question) => new TrueFalseQuestionViewModel
                {
                    Id = question.Id,
                    Text = question.Text,
                    QuestionType = QuestionType.TrueFalse,
                    QuestionNumber = question.QuestionNumber,
                    SurveyId = question.SurveyId
                });

            MockMapper.Setup(m => m.Map<Rating1To10QuestionViewModel>(It.IsAny<Question>()))
                .Returns((Question question) => new Rating1To10QuestionViewModel
                {
                    Id = question.Id,
                    Text = question.Text,
                    QuestionType = QuestionType.Rating1To10,
                    QuestionNumber = question.QuestionNumber,
                    SurveyId = question.SurveyId
                });

            MockMapper.Setup(m => m.Map<MultipleChoiceQuestionViewModel>(It.IsAny<MultipleChoiceQuestion>()))
                .Returns((MultipleChoiceQuestion question) => new MultipleChoiceQuestionViewModel
                {
                    Id = question.Id,
                    Text = question.Text,
                    QuestionType = QuestionType.MultipleChoice,
                    QuestionNumber = question.QuestionNumber,
                    SurveyId = question.SurveyId,
                    Options = question.Options?.Select(o => new ChoiceOptionViewModel
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        Order = o.Order
                    }).ToList()
                });

            MockMapper.Setup(m => m.Map<SelectAllThatApplyQuestionViewModel>(It.IsAny<SelectAllThatApplyQuestion>()))
                .Returns((SelectAllThatApplyQuestion question) => new SelectAllThatApplyQuestionViewModel
                {
                    Id = question.Id,
                    Text = question.Text,
                    QuestionType = QuestionType.SelectAllThatApply,
                    QuestionNumber = question.QuestionNumber,
                    SurveyId = question.SurveyId,
                    Options = question.Options?.Select(o => new ChoiceOptionViewModel
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        Order = o.Order
                    }).ToList()
                });
        }

        private void SetupMockApiAuthService()
        {
            // Setup GetUserId method to return user ID
            MockApiAuthService.Setup(a => a.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns((System.Security.Claims.ClaimsPrincipal principal) =>
                {
                    var claim = principal?.FindFirst("uid");
                    return claim != null ? int.Parse(claim.Value) : 0;
                });
        }

        #endregion

        #region Tests for GetAnswers()

        [Test]
        public async Task GetAnswers_ReturnsOkWithAllAnswers()
        {
            // Act
            var result = await _controller.GetAnswers();
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.Value, Is.InstanceOf<IEnumerable<AnswerViewModel>>());
            
            var answers = okResult.Value as IEnumerable<AnswerViewModel>;
            Assert.That(answers, Is.Not.Null);
            Assert.That(answers!.Count(), Is.EqualTo(_mockAnswers.Count));
        }

        #endregion

        #region Tests for GetAnswer(int id)

        [Test]
        public async Task GetAnswer_WithValidId_ReturnsOkWithAnswer()
        {
            // Arrange
            int answerId = 1;
            
            // Act
            var result = await _controller.GetAnswer(answerId);
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.Value, Is.InstanceOf<AnswerViewModel>());
            
            var answer = okResult.Value as AnswerViewModel;
            Assert.That(answer, Is.Not.Null);
            Assert.That(answer!.Id, Is.EqualTo(answerId));
        }
        
        [Test]
        public async Task GetAnswer_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int invalidAnswerId = 99;
            
            // Act
            var result = await _controller.GetAnswer(invalidAnswerId);
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        #endregion

        #region Tests for GetAnswersForSurveyForLoggedInUser

        [Test]
        public async Task GetAnswersForSurveyForLoggedInUser_WithValidGuidAndUser_ReturnsSurveyWithQuestions()
        {
            // Arrange - Create test user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user2"),
                new Claim("uid", "2")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            
            // Create an in-memory database for testing
            var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
                
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            
            // Create a real DbContext with in-memory database
            using var context = new ApplicationDbContext(dbContextOptions, httpContextAccessor.Object);
            
            // Create test data
            string surveyGuid = "survey-1-guid";
            int userId = 2;
            
            // Create and add survey with questions to the in-memory database
            var survey = new Survey 
            { 
                Id = 1, 
                Title = "Test Survey 1", 
                Description = "Survey for testing", 
                Guid = surveyGuid, 
                CreatedById = 1, 
                Published = true,
                Questions = new List<Question>()
            };
            
            // Create and add questions to the survey
            var questions = new List<Question>
            {
                new TextQuestion 
                { 
                    Id = 1, 
                    Text = "What is your name?", 
                    SurveyId = 1, 
                    QuestionNumber = 1,
                    QuestionType = QuestionType.Text,
                    CreatedById = 1
                },
                new TrueFalseQuestion 
                { 
                    Id = 2, 
                    Text = "Is this a test?", 
                    SurveyId = 1, 
                    QuestionNumber = 2,
                    QuestionType = QuestionType.TrueFalse,
                    CreatedById = 1
                },
                new Rating1To10Question 
                { 
                    Id = 3, 
                    Text = "Rate this survey", 
                    SurveyId = 1, 
                    QuestionNumber = 3,
                    QuestionType = QuestionType.Rating1To10,
                    CreatedById = 1
                },
                new MultipleChoiceQuestion
                {
                    Id = 4,
                    Text = "Which color do you prefer?",
                    SurveyId = 1,
                    QuestionNumber = 4,
                    QuestionType = QuestionType.MultipleChoice,
                    CreatedById = 1,
                    Options = new List<ChoiceOption>
                    {
                        new ChoiceOption { Id = 1, OptionText = "Red", Order = 1 },
                        new ChoiceOption { Id = 2, OptionText = "Green", Order = 2 },
                        new ChoiceOption { Id = 3, OptionText = "Blue", Order = 3 }
                    }
                },
                new SelectAllThatApplyQuestion
                {
                    Id = 5,
                    Text = "Which programming languages do you know?",
                    SurveyId = 1,
                    QuestionNumber = 5,
                    QuestionType = QuestionType.SelectAllThatApply,
                    CreatedById = 1,
                    Options = new List<ChoiceOption>
                    {
                        new ChoiceOption { Id = 4, OptionText = "C#", Order = 1 },
                        new ChoiceOption { Id = 5, OptionText = "JavaScript", Order = 2 },
                        new ChoiceOption { Id = 6, OptionText = "Python", Order = 3 }
                    }
                }
            };
            
            foreach (var question in questions)
            {
                survey.Questions.Add(question);
            }
            
            // Add survey to in-memory database
            context.Surveys.Add(survey);
            await context.SaveChangesAsync();
            
            // Create mocks for other dependencies
            var mockMapper = new Mock<IMapper>();
            var mockApiAuthService = new Mock<IApiAuthService>();
            
            // Setup mapper to return correct SurveyViewModel
            mockMapper.Setup(m => m.Map<SurveyViewModel>(It.IsAny<Survey>()))
                .Returns(new SurveyViewModel 
                { 
                    Id = survey.Id,
                    Title = survey.Title,
                    Description = survey.Description,
                    Guid = survey.Guid,
                    Questions = survey.Questions.Select(q => new TextQuestionViewModel 
                    { 
                        Id = q.Id,
                        Text = q.Text,
                        QuestionType = q.QuestionType,
                        SurveyId = q.SurveyId,
                        QuestionNumber = q.QuestionNumber
                    }).ToList<QuestionViewModel>()
                });
                
            // Setup API auth service
            mockApiAuthService.Setup(a => a.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(userId);
                
            // Create controller with real DbContext and mocked dependencies
            var controller = new AnswerController(context, mockMapper.Object, mockApiAuthService.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            
            // Act
            var result = await controller.GetAnswersForSurveyForLoggedInUser(surveyGuid, true); // Use Preview=true to simplify test
            
            // Debug - Check if it's a bad request and inspect the error message
            if (result.Result is BadRequestObjectResult badResult)
            {
                Assert.Fail($"BadRequest returned with message: {badResult.Value}");
            }
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.Value, Is.InstanceOf<SurveyViewModel>());
            
            var surveyViewModel = okResult.Value as SurveyViewModel;
            Assert.That(surveyViewModel, Is.Not.Null);
            Assert.That(surveyViewModel!.Guid, Is.EqualTo(surveyGuid));
            Assert.That(surveyViewModel.Questions, Is.Not.Null);
            Assert.That(surveyViewModel.Questions!.Count, Is.EqualTo(5));
        }
        
        [Test]
        public async Task GetAnswersForSurveyForLoggedInUser_WithInvalidGuid_ReturnsBadRequest()
        {
            // Arrange
            string invalidGuid = "invalid-guid";
            
            // Act
            var result = await _controller.GetAnswersForSurveyForLoggedInUser(invalidGuid, false);
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        #endregion

        #region Tests for PostAnswer


        #endregion

        #region Tests for PutAnswer


        #endregion

        #region Tests for DeleteAnswer


        #endregion
    }
}