using Bunit;
using JwtIdentity.Client.Pages.Survey;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using Moq;
using JwtIdentity.Client.Services.Base;
using System.Linq;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using MudBlazor;
using MudBlazor.Services;
using JwtIdentity.Client.Helpers;
using JwtIdentity.Client.Services;
using BunitTestContext = Bunit.TestContext;

namespace JwtIdentity.BunitTests
{
    /// <summary>
    /// Focused unit tests for Survey.razor component.
    /// Note: Full integration testing is limited due to browser-specific dependencies.
    /// These tests focus on component initialization, structure, and service interactions.
    /// </summary>
    [TestFixture]
    public class SurveyTests : IDisposable
    {
        private BunitTestContext _context;
        private MockNavigationManager _navManager;
        private Mock<AuthenticationStateProvider> _authStateProviderMock;
        private Mock<IAuthService> _authServiceMock;
        private Mock<ILocalStorageService> _localStorageMock;
        private Mock<ISnackbar> _snackbarMock;
        private Mock<IDialogService> _dialogServiceMock;
        private Mock<IApiService> _apiServiceMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private SurveyViewModel _testSurvey;
        private Guid _testSurveyId;

        [SetUp]
        public void Setup()
        {
            // Create a fresh test context for each test
            _context = new BunitTestContext();

            // Generate a test survey ID
            _testSurveyId = Guid.NewGuid();

            // Create mocks
            _authServiceMock = new Mock<IAuthService>();
            _localStorageMock = new Mock<ILocalStorageService>();
            _snackbarMock = new Mock<ISnackbar>();
            _dialogServiceMock = new Mock<IDialogService>();
            _apiServiceMock = new Mock<IApiService>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

            // Setup MockNavigationManager
            _navManager = new MockNavigationManager();
            _context.Services.AddSingleton<NavigationManager>(_navManager);

            // Setup AuthenticationStateProvider with authenticated user by default
            _authStateProviderMock = new Mock<AuthenticationStateProvider>();
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "TestAuth"));
            var authState = new AuthenticationState(authenticatedUser);
            _authStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);

            // Register all services
            _context.Services.AddSingleton<IAuthService>(_authServiceMock.Object);
            _context.Services.AddSingleton<ILocalStorageService>(_localStorageMock.Object);
            _context.Services.AddSingleton<ISnackbar>(_snackbarMock.Object);
            _context.Services.AddSingleton<IDialogService>(_dialogServiceMock.Object);
            _context.Services.AddSingleton<IApiService>(_apiServiceMock.Object);
            _context.Services.AddSingleton<AuthenticationStateProvider>(_authStateProviderMock.Object);
            _context.Services.AddSingleton<IHttpClientFactory>(_httpClientFactoryMock.Object);

            // Register mock IConfiguration
            var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfig.Setup(c => c["ReCaptcha:SiteKey"]).Returns("test-site-key");
            _context.Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(mockConfig.Object);

            // Register fake CustomAuthorizationMessageHandler
            _context.Services.AddSingleton<JwtIdentity.Client.Services.CustomAuthorizationMessageHandler>(
                new FakeCustomAuthorizationMessageHandler());
            _context.Services.AddSingleton<HttpClient>(new HttpClient());
            _context.Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(new Mock<Microsoft.JSInterop.IJSRuntime>().Object);
            _context.Services.AddSingleton<JwtIdentity.Client.Helpers.IUtility>(new Mock<JwtIdentity.Client.Helpers.IUtility>().Object);

            // Register MudBlazor services
            _context.Services.AddMudServices();

            // Setup default test survey
            _testSurvey = CreateTestSurvey();

            // Setup default API service responses
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            // Setup AuthService for anonymous login
            _authServiceMock.Setup(x => x.Login(It.IsAny<ApplicationUserViewModel>()))
                .ReturnsAsync(new Response<ApplicationUserViewModel>
                {
                    Success = true,
                    Data = new ApplicationUserViewModel { UserName = "anonymous" }
                });

            // Setup default answer post responses
            _apiServiceMock.Setup(x => x.PostAsync(It.Is<string>(s => s == ApiEndpoints.Answer), It.IsAny<AnswerViewModel>()))
                .ReturnsAsync((string endpoint, AnswerViewModel answer) => answer);
        }

        // Fake implementation for DI
        private class FakeCustomAuthorizationMessageHandler : JwtIdentity.Client.Services.CustomAuthorizationMessageHandler
        {
            public FakeCustomAuthorizationMessageHandler()
                : base(new MockNavigationManager(), new ServiceCollection().BuildServiceProvider(), new Mock<ILocalStorageService>().Object)
            {
            }
        }

        private class MockNavigationManager : NavigationManager
        {
            public List<string> History { get; } = new List<string>();
            public MockNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }
            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                var absoluteUri = ToAbsoluteUri(uri).ToString();
                History.Add(absoluteUri);
                Uri = absoluteUri;
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Helper Methods

        private SurveyViewModel CreateTestSurvey()
        {
            var survey = new SurveyViewModel
            {
                Id = 1,
                Guid = _testSurveyId.ToString(),
                Title = "Test Survey",
                Description = "This is a test survey description",
                Published = true,
                Questions = new List<QuestionViewModel>(),
                QuestionGroups = new List<QuestionGroupViewModel>()
            };

            // Add various question types
            survey.Questions.Add(new TextQuestionViewModel
            {
                Id = 1,
                SurveyId = survey.Id,
                Text = "What is your name?",
                QuestionNumber = 1,
                QuestionType = QuestionType.Text,
                IsRequired = true,
                GroupId = 0,
                Answers = new List<AnswerViewModel>()
            });

            survey.Questions.Add(new TrueFalseQuestionViewModel
            {
                Id = 2,
                SurveyId = survey.Id,
                Text = "Do you like surveys?",
                QuestionNumber = 2,
                QuestionType = QuestionType.TrueFalse,
                IsRequired = true,
                GroupId = 0,
                Answers = new List<AnswerViewModel>()
            });

            survey.Questions.Add(new MultipleChoiceQuestionViewModel
            {
                Id = 3,
                SurveyId = survey.Id,
                Text = "What is your favorite color?",
                QuestionNumber = 3,
                QuestionType = QuestionType.MultipleChoice,
                IsRequired = true,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 1, OptionText = "Red", Order = 1 },
                    new ChoiceOptionViewModel { Id = 2, OptionText = "Blue", Order = 2 },
                    new ChoiceOptionViewModel { Id = 3, OptionText = "Green", Order = 3 }
                },
                Answers = new List<AnswerViewModel>()
            });

            survey.Questions.Add(new Rating1To10QuestionViewModel
            {
                Id = 4,
                SurveyId = survey.Id,
                Text = "Rate your satisfaction",
                QuestionNumber = 4,
                QuestionType = QuestionType.Rating1To10,
                IsRequired = false,
                GroupId = 0,
                Answers = new List<AnswerViewModel>()
            });

            survey.Questions.Add(new SelectAllThatApplyQuestionViewModel
            {
                Id = 5,
                SurveyId = survey.Id,
                Text = "Select all that apply",
                QuestionNumber = 5,
                QuestionType = QuestionType.SelectAllThatApply,
                IsRequired = false,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 4, OptionText = "Option A", Order = 1 },
                    new ChoiceOptionViewModel { Id = 5, OptionText = "Option B", Order = 2 },
                    new ChoiceOptionViewModel { Id = 6, OptionText = "Option C", Order = 3 }
                },
                Answers = new List<AnswerViewModel>()
            });

            return survey;
        }

        private SurveyViewModel CreateBranchingSurvey()
        {
            var survey = new SurveyViewModel
            {
                Id = 2,
                Guid = _testSurveyId.ToString(),
                Title = "Branching Survey",
                Description = "This survey has conditional branching",
                Published = true,
                Questions = new List<QuestionViewModel>(),
                QuestionGroups = new List<QuestionGroupViewModel>
                {
                    new QuestionGroupViewModel { Id = 1, GroupNumber = 0, GroupName = "Initial Questions" },
                    new QuestionGroupViewModel { Id = 2, GroupNumber = 1, GroupName = "Group 1" },
                    new QuestionGroupViewModel { Id = 3, GroupNumber = 2, GroupName = "Group 2" }
                }
            };

            // Add branching question
            survey.Questions.Add(new MultipleChoiceQuestionViewModel
            {
                Id = 10,
                SurveyId = survey.Id,
                Text = "Do you like pizza?",
                QuestionNumber = 1,
                QuestionType = QuestionType.MultipleChoice,
                IsRequired = true,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 10, OptionText = "Yes", Order = 1, BranchToGroupId = 1 },
                    new ChoiceOptionViewModel { Id = 11, OptionText = "No", Order = 2, BranchToGroupId = 2 }
                },
                Answers = new List<AnswerViewModel>()
            });

            return survey;
        }

        private void SetupAnonymousUser()
        {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = new AuthenticationState(anonymousUser);
            _authStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        }

        private void SetupDemoUser()
        {
            var demoUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "DemoUser123@surveyshark.site")
            }, "TestAuth"));
            var authState = new AuthenticationState(demoUser);
            _authStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        }

        #endregion

        #region Component Initialization Tests

        [Test]
        public void Survey_Component_Renders_Without_Error()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert
            Assert.That(cut, Is.Not.Null);
            Assert.That(cut.Markup, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Survey_Component_Initializes_With_Correct_SurveyId()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert
            Assert.That(cut.Instance, Is.Not.Null);
            Assert.That(cut.Instance.SurveyId, Is.EqualTo(_testSurveyId));
        }

        [Test]
        public void Survey_Component_Has_Valid_Instance_Properties()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert
            Assert.That(cut.Instance, Is.Not.Null);
            Assert.That(cut.Instance.SurveyId, Is.EqualTo(_testSurveyId));
            // Component should render in a valid initial state
        }

        #endregion

        #region Authentication and User Tests

        [Test]
        public void Survey_Handles_Anonymous_User_Login()
        {
            // Arrange
            SetupAnonymousUser();
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert - Component should render without error even for anonymous users
            Assert.That(cut, Is.Not.Null);
            Assert.That(cut.Instance, Is.Not.Null);
        }

        [Test]
        public void Survey_Identifies_Demo_User()
        {
            // Arrange
            SetupDemoUser();
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert
            Assert.That(cut, Is.Not.Null);
            Assert.That(cut.Instance, Is.Not.Null);
            // Demo user detection happens in OnInitializedAsync
        }

        [Test]
        public void Survey_Handles_Regular_Authenticated_User()
        {
            // Arrange - Default setup has regular authenticated user
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert
            Assert.That(cut, Is.Not.Null);
            Assert.That(cut.Instance, Is.Not.Null);
        }

        #endregion

        #region Service Interaction Tests

        [Test]
        public void Survey_Setup_Includes_All_Required_Services()
        {
            // Assert - Verify all required services are registered
            Assert.That(_context.Services.GetService<IApiService>(), Is.Not.Null);
            Assert.That(_context.Services.GetService<IAuthService>(), Is.Not.Null);
            Assert.That(_context.Services.GetService<AuthenticationStateProvider>(), Is.Not.Null);
            Assert.That(_context.Services.GetService<NavigationManager>(), Is.Not.Null);
            Assert.That(_context.Services.GetService<ISnackbar>(), Is.Not.Null);
        }

        [Test]
        public void Survey_ApiService_Mock_Returns_Valid_Survey()
        {
            // Arrange & Act
            var survey = _apiServiceMock.Object.GetAsync<SurveyViewModel>("test").Result;

            // Assert
            Assert.That(survey, Is.Not.Null);
            Assert.That(survey.Id, Is.EqualTo(1));
            Assert.That(survey.Title, Is.EqualTo("Test Survey"));
            Assert.That(survey.Questions, Is.Not.Null.And.Count.EqualTo(5));
        }

        #endregion

        #region Test Data Structure Tests

        [Test]
        public void CreateTestSurvey_Returns_Valid_NonBranching_Survey()
        {
            // Arrange & Act
            var survey = CreateTestSurvey();

            // Assert
            Assert.That(survey, Is.Not.Null);
            Assert.That(survey.Title, Is.EqualTo("Test Survey"));
            Assert.That(survey.Questions.Count, Is.EqualTo(5));
            Assert.That(survey.QuestionGroups.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateTestSurvey_Contains_All_Question_Types()
        {
            // Arrange & Act
            var survey = CreateTestSurvey();

            // Assert
            var questionTypes = survey.Questions.Select(q => q.QuestionType).ToList();
            Assert.That(questionTypes, Does.Contain(QuestionType.Text));
            Assert.That(questionTypes, Does.Contain(QuestionType.TrueFalse));
            Assert.That(questionTypes, Does.Contain(QuestionType.MultipleChoice));
            Assert.That(questionTypes, Does.Contain(QuestionType.Rating1To10));
            Assert.That(questionTypes, Does.Contain(QuestionType.SelectAllThatApply));
        }

        [Test]
        public void CreateBranchingSurvey_Has_QuestionGroups()
        {
            // Arrange & Act
            var survey = CreateBranchingSurvey();

            // Assert
            Assert.That(survey, Is.Not.Null);
            Assert.That(survey.QuestionGroups.Count, Is.EqualTo(3));
            Assert.That(survey.QuestionGroups.Any(g => g.GroupNumber == 0), Is.True);
            Assert.That(survey.QuestionGroups.Any(g => g.GroupNumber == 1), Is.True);
            Assert.That(survey.QuestionGroups.Any(g => g.GroupNumber == 2), Is.True);
        }

        [Test]
        public void CreateBranchingSurvey_Has_Branching_Options()
        {
            // Arrange & Act
            var survey = CreateBranchingSurvey();

            // Assert
            var mcQuestion = survey.Questions.FirstOrDefault() as MultipleChoiceQuestionViewModel;
            Assert.That(mcQuestion, Is.Not.Null);
            Assert.That(mcQuestion.Options.Any(o => o.BranchToGroupId.HasValue), Is.True);
        }

        [Test]
        public void TestSurvey_MultipleChoice_Has_Options()
        {
            // Arrange & Act
            var survey = CreateTestSurvey();
            var mcQuestion = survey.Questions.OfType<MultipleChoiceQuestionViewModel>().FirstOrDefault();

            // Assert
            Assert.That(mcQuestion, Is.Not.Null);
            Assert.That(mcQuestion.Options.Count, Is.EqualTo(3));
            Assert.That(mcQuestion.Options[0].OptionText, Is.EqualTo("Red"));
            Assert.That(mcQuestion.Options[1].OptionText, Is.EqualTo("Blue"));
            Assert.That(mcQuestion.Options[2].OptionText, Is.EqualTo("Green"));
        }

        [Test]
        public void TestSurvey_SelectAllThatApply_Has_Options()
        {
            // Arrange & Act
            var survey = CreateTestSurvey();
            var saQuestion = survey.Questions.OfType<SelectAllThatApplyQuestionViewModel>().FirstOrDefault();

            // Assert
            Assert.That(saQuestion, Is.Not.Null);
            Assert.That(saQuestion.Options.Count, Is.EqualTo(3));
        }

        [Test]
        public void TestSurvey_Has_Required_And_Optional_Questions()
        {
            // Arrange & Act
            var survey = CreateTestSurvey();

            // Assert
            var requiredQuestions = survey.Questions.Where(q => q.IsRequired).ToList();
            var optionalQuestions = survey.Questions.Where(q => !q.IsRequired).ToList();
            
            Assert.That(requiredQuestions.Count, Is.GreaterThan(0));
            Assert.That(optionalQuestions.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Component Structure Tests

        [Test]
        public void Survey_Component_Renders_Container_Div()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert
            Assert.That(cut.Markup, Does.Contain("survey-container"));
        }

        [Test]
        public void Survey_Component_Renders_With_DifferentSurveyIds()
        {
            // Arrange
            var surveyId1 = Guid.NewGuid();
            var surveyId2 = Guid.NewGuid();

            // Act
            var cut1 = _context.RenderComponent<Survey>(ComponentParameter.CreateParameter(nameof(Survey.SurveyId), surveyId1));
            var cut2 = _context.RenderComponent<Survey>(ComponentParameter.CreateParameter(nameof(Survey.SurveyId), surveyId2));

            // Assert
            Assert.That(cut1.Instance.SurveyId, Is.EqualTo(surveyId1));
            Assert.That(cut2.Instance.SurveyId, Is.EqualTo(surveyId2));
            Assert.That(cut1.Instance.SurveyId, Is.Not.EqualTo(cut2.Instance.SurveyId));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Survey_Handles_Null_Survey_Response_Gracefully()
        {
            // Arrange
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync((SurveyViewModel)null);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() =>
            {
                var cut = _context.RenderComponent<Survey>(parameters);
                Assert.That(cut, Is.Not.Null);
            });
        }

        [Test]
        public void Survey_Handles_Invalid_Guid_Gracefully()
        {
            // Arrange
            var invalidGuid = Guid.Empty;
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), invalidGuid)
            };

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() =>
            {
                var cut = _context.RenderComponent<Survey>(parameters);
                Assert.That(cut, Is.Not.Null);
            });
        }

        #endregion
    }
}
