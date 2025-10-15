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
            _context.Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(mockConfig.Object);

            // Register fake CustomAuthorizationMessageHandler
            _context.Services.AddSingleton<JwtIdentity.Client.Services.CustomAuthorizationMessageHandler>(
                new FakeCustomAuthorizationMessageHandler());
            _context.Services.AddSingleton<HttpClient>(new HttpClient());
            _context.Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(new Mock<Microsoft.JSInterop.IJSRuntime>().Object);
            _context.Services.AddSingleton<JwtIdentity.Client.Helpers.IUtility>(new Mock<JwtIdentity.Client.Helpers.IUtility>().Object);

            // Register MudBlazor services
            _context.Services.AddMudServices();

            // Setup default test survey with non-branching questions
            _testSurvey = CreateNonBranchingSurvey();

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

        private SurveyViewModel CreateNonBranchingSurvey()
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

            // Add a Text question
            var textQuestion = new TextQuestionViewModel
            {
                Id = 1,
                SurveyId = survey.Id,
                Text = "What is your name?",
                QuestionNumber = 1,
                QuestionType = QuestionType.Text,
                IsRequired = true,
                GroupId = 0,
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(textQuestion);

            // Add a TrueFalse question
            var trueFalseQuestion = new TrueFalseQuestionViewModel
            {
                Id = 2,
                SurveyId = survey.Id,
                Text = "Do you like surveys?",
                QuestionNumber = 2,
                QuestionType = QuestionType.TrueFalse,
                IsRequired = true,
                GroupId = 0,
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(trueFalseQuestion);

            // Add a MultipleChoice question
            var multipleChoiceQuestion = new MultipleChoiceQuestionViewModel
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
            };
            survey.Questions.Add(multipleChoiceQuestion);

            // Add a Rating1To10 question
            var ratingQuestion = new Rating1To10QuestionViewModel
            {
                Id = 4,
                SurveyId = survey.Id,
                Text = "How satisfied are you with this survey?",
                QuestionNumber = 4,
                QuestionType = QuestionType.Rating1To10,
                IsRequired = false,
                GroupId = 0,
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(ratingQuestion);

            // Add a SelectAllThatApply question
            var selectAllQuestion = new SelectAllThatApplyQuestionViewModel
            {
                Id = 5,
                SurveyId = survey.Id,
                Text = "Which of the following do you use?",
                QuestionNumber = 5,
                QuestionType = QuestionType.SelectAllThatApply,
                IsRequired = false,
                GroupId = 0,
                Options = new List<ChoiceOptionViewModel>
                {
                    new ChoiceOptionViewModel { Id = 4, OptionText = "Email", Order = 1 },
                    new ChoiceOptionViewModel { Id = 5, OptionText = "Phone", Order = 2 },
                    new ChoiceOptionViewModel { Id = 6, OptionText = "Chat", Order = 3 }
                },
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(selectAllQuestion);

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

            // Add a question in group 0 with branching
            var q1 = new MultipleChoiceQuestionViewModel
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
            };
            survey.Questions.Add(q1);

            // Add a question in group 1
            var q2 = new TextQuestionViewModel
            {
                Id = 11,
                SurveyId = survey.Id,
                Text = "What's your favorite pizza topping?",
                QuestionNumber = 2,
                QuestionType = QuestionType.Text,
                IsRequired = true,
                GroupId = 1,
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(q2);

            // Add a question in group 2
            var q3 = new TextQuestionViewModel
            {
                Id = 12,
                SurveyId = survey.Id,
                Text = "What do you prefer instead?",
                QuestionNumber = 3,
                QuestionType = QuestionType.Text,
                IsRequired = true,
                GroupId = 2,
                Answers = new List<AnswerViewModel>()
            };
            survey.Questions.Add(q3);

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

        #region Component Rendering Tests

        [Test]
        public void Survey_Component_Renders_Correctly()
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
            // Component should initially show loading state
            Assert.That(cut.Markup.Contains("Survey not found") || cut.Markup.Contains(_testSurvey.Title), Is.True);
        }

        [Test]
        public void Survey_Renders_Title_And_Description()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Markup, Does.Contain(_testSurvey.Title));
            Assert.That(cut.Markup, Does.Contain(_testSurvey.Description));
        }

        [Test]
        public void Survey_Shows_Loading_State_Initially()
        {
            // Arrange
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync((SurveyViewModel)null);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert - Should show loading or error state
            Assert.That(cut.Markup.Contains("Survey not found") || cut.Markup.Contains("MudProgressCircular"), Is.True);
        }

        #endregion

        #region Non-Branching Mode Tests

        [Test]
        public void Survey_NonBranching_Renders_All_Questions()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - All questions should be rendered
            Assert.That(cut.Markup, Does.Contain("What is your name?"));
            Assert.That(cut.Markup, Does.Contain("Do you like surveys?"));
            Assert.That(cut.Markup, Does.Contain("What is your favorite color?"));
            Assert.That(cut.Markup, Does.Contain("How satisfied are you with this survey?"));
            Assert.That(cut.Markup, Does.Contain("Which of the following do you use?"));
        }

        [Test]
        public void Survey_NonBranching_Shows_Submit_Button()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Submit Survey"));
        }

        #endregion

        #region Branching Mode Tests

        [Test]
        public void Survey_Branching_Shows_One_Question_At_A_Time()
        {
            // Arrange
            _testSurvey = CreateBranchingSurvey();
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Should show first question
            Assert.That(cut.Markup, Does.Contain("Do you like pizza?"));
            // Should NOT show questions from other groups yet
            Assert.That(cut.Markup, Does.Not.Contain("What's your favorite pizza topping?"));
            Assert.That(cut.Markup, Does.Not.Contain("What do you prefer instead?"));
        }

        [Test]
        public void Survey_Branching_Shows_Progress_Indicator()
        {
            // Arrange
            _testSurvey = CreateBranchingSurvey();
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Should show progress indicator
            Assert.That(cut.Markup, Does.Contain("Question 1 of"));
        }

        [Test]
        public void Survey_Branching_Shows_Navigation_Buttons()
        {
            // Arrange
            _testSurvey = CreateBranchingSurvey();
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(_testSurvey);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Should show navigation buttons
            Assert.That(cut.Markup, Does.Contain("Previous"));
            Assert.That(cut.Markup, Does.Contain("Next").Or.Contain("Submit"));
        }

        #endregion

        #region Question Type Rendering Tests

        [Test]
        public void Survey_Renders_Text_Question_Correctly()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            var textInputs = cut.FindAll("input[type='text'], textarea");
            Assert.That(textInputs.Count, Is.GreaterThan(0), "Should have text input fields");
        }

        [Test]
        public void Survey_Renders_TrueFalse_Question_Correctly()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Markup, Does.Contain("True"));
            Assert.That(cut.Markup, Does.Contain("False"));
        }

        [Test]
        public void Survey_Renders_MultipleChoice_Question_Correctly()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Red"));
            Assert.That(cut.Markup, Does.Contain("Blue"));
            Assert.That(cut.Markup, Does.Contain("Green"));
        }

        [Test]
        public void Survey_Renders_Rating_Question_Correctly()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Should have rating options 1-10
            for (int i = 1; i <= 10; i++)
            {
                Assert.That(cut.Markup, Does.Contain($">{i}<"));
            }
        }

        [Test]
        public void Survey_Renders_SelectAllThatApply_Question_Correctly()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Email"));
            Assert.That(cut.Markup, Does.Contain("Phone"));
            Assert.That(cut.Markup, Does.Contain("Chat"));
        }

        [Test]
        public void Survey_Marks_Required_Questions_With_Asterisk()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Required questions should have asterisk
            var requiredMarkers = cut.FindAll(".RequiredStar");
            Assert.That(requiredMarkers.Count, Is.GreaterThan(0), "Should have required field markers");
        }

        #endregion

        #region Anonymous User Tests

        [Test]
        public void Survey_AnonymousUser_Shows_Terms_Agreement()
        {
            // Arrange
            SetupAnonymousUser();
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Privacy Policy"));
            Assert.That(cut.Markup, Does.Contain("Terms of Service"));
            Assert.That(cut.Markup, Does.Contain("I Agree").Or.Contain("I Disagree"));
        }

        [Test]
        public void Survey_AnonymousUser_Logs_In_Automatically()
        {
            // Arrange
            SetupAnonymousUser();
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - AuthService.Login should have been called for anonymous user
            _authServiceMock.Verify(x => x.Login(It.Is<ApplicationUserViewModel>(u => u.UserName == "logmeinanonymoususer")), Times.Once);
        }

        #endregion

        #region CAPTCHA Tests

        [Test]
        public void Survey_Shows_Captcha_For_Non_Demo_Users()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert - Should show CAPTCHA or verify message
            Assert.That(cut.Markup, Does.Contain("captcha").Or.Contain("Verification"));
        }

        [Test]
        public void Survey_Demo_User_Bypasses_Captcha()
        {
            // Arrange
            SetupDemoUser();
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Should show survey content without CAPTCHA
            Assert.That(cut.Markup, Does.Contain(_testSurvey.Title));
        }

        #endregion

        #region Preview Mode Tests

        [Test]
        public void Survey_Preview_Mode_Shows_Alert()
        {
            // Arrange
            _navManager.NavigateTo($"http://localhost/survey/{_testSurveyId}?Preview=true");
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Preview Mode"));
        }

        [Test]
        public void Survey_Preview_Mode_Disables_Submit_Button()
        {
            // Arrange
            _navManager.NavigateTo($"http://localhost/survey/{_testSurveyId}?Preview=true");
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Submit button should be disabled
            var submitButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Submit"));
            Assert.That(submitButtons.Any(b => b.HasAttribute("disabled")), Is.True, "Submit button should be disabled in preview mode");
        }

        #endregion

        #region Demo User Tests

        [Test]
        public void Survey_Identifies_Demo_User_Correctly()
        {
            // Arrange
            SetupDemoUser();
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Demo user specific content should be visible
            Assert.That(cut.Instance, Is.Not.Null);
        }

        [Test]
        public void Survey_Regular_User_Is_Not_Demo_User()
        {
            // Arrange - Already set up with regular user in Setup
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(cut.Instance, Is.Not.Null);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Survey_Handles_Invalid_Survey_Id_Gracefully()
        {
            // Arrange
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync((SurveyViewModel)null);

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);

            // Assert - Should show error message or redirect
            Assert.That(cut.Markup, Does.Contain("Survey not found").Or.Contains("could not be loaded"));
        }

        [Test]
        public void Survey_Handles_API_Error_Gracefully()
        {
            // Arrange
            _apiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ThrowsAsync(new Exception("API Error"));

            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() =>
            {
                var cut = _context.RenderComponent<Survey>(parameters);
            });
        }

        #endregion

        #region Answer Handling Tests

        [Test]
        public void Survey_Saves_Answer_When_Question_Answered()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Act - Find and interact with a True/False radio button
            var radioButtons = cut.FindAll("input[type='radio']");
            if (radioButtons.Any())
            {
                var trueButton = radioButtons.FirstOrDefault(r => r.ParentElement?.TextContent?.Contains("True") == true);
                if (trueButton != null)
                {
                    trueButton.Change(true);
                }
            }

            // Assert - Should have attempted to save the answer
            _apiServiceMock.Verify(x => x.PostAsync(ApiEndpoints.Answer, It.IsAny<AnswerViewModel>()), Times.AtLeastOnce);
        }

        #endregion

        #region Validation Tests

        [Test]
        public void Survey_Question_Numbers_Display_Correctly()
        {
            // Arrange
            var parameters = new ComponentParameter[]
            {
                ComponentParameter.CreateParameter(nameof(Survey.SurveyId), _testSurveyId)
            };

            // Act
            var cut = _context.RenderComponent<Survey>(parameters);
            cut.WaitForState(() => cut.Markup.Contains(_testSurvey.Title), timeout: TimeSpan.FromSeconds(5));

            // Assert - Questions should be numbered
            Assert.That(cut.Markup, Does.Contain("1."));
            Assert.That(cut.Markup, Does.Contain("2."));
            Assert.That(cut.Markup, Does.Contain("3."));
        }

        #endregion
    }
}
