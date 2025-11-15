using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using JwtIdentity.Client.Pages.Auth;
using JwtIdentity.Client.Services;
using JwtIdentity.Client.Services.Base;
using JwtIdentity.Client.Tests.Stubs; // adjust namespace if different
using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using NUnit.Framework;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Diagram;
using System;
using System.Collections.Generic;
using System.Linq;


namespace JwtIdentity.BunitTests
{
    /// <summary>
    /// Base class for bUnit tests providing test context and commonly used mocks
    /// </summary>
    public class BUnitTestBase : IDisposable
    {
        private IRenderedComponent<MudPopoverProvider> popoverProvider;

        protected Bunit.TestContext Context { get; private set; }
        protected MockNavigationManager NavManager { get; private set; }
        
        // Core services for tests
        protected Mock<IAuthService> AuthServiceMock { get; private set; }
        protected Mock<ILocalStorageService> LocalStorageMock { get; private set; }
        protected Mock<ISnackbar> SnackbarMock { get; private set; }
        protected Mock<IDialogService> DialogServiceMock { get; private set; }
        protected Mock<IApiService> ApiServiceMock { get; private set; }
        protected Mock<IHttpClientFactory> HttpClientFactoryMock { get; private set; }
        protected Mock<AuthenticationStateProvider> AuthStateProviderMock { get; private set; }
        protected Mock<Microsoft.Extensions.Configuration.IConfiguration> ConfigMock { get; private set; }
        
        public BUnitTestBase()
        {
            // Create test context
            Context = new Bunit.TestContext();

            // Substitute all SfDiagramComponent instances with our stub
            Context.ComponentFactories.Add<SfDiagramComponent, SfDiagramComponentStub>();

            // Register MockNavigationManager for NavigationManager
            NavManager = new MockNavigationManager();
            Context.Services.AddSingleton<NavigationManager>(NavManager);

            // Create mock IConfiguration
            ConfigMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            ConfigMock.Setup(c => c["ReCaptcha:SiteKey"]).Returns("test-site-key");
            Context.Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(ConfigMock.Object);

            // Create core service mocks
            AuthServiceMock = new Mock<IAuthService>();
            LocalStorageMock = new Mock<ILocalStorageService>();
            SnackbarMock = new Mock<ISnackbar>();
            DialogServiceMock = new Mock<IDialogService>();
            ApiServiceMock = new Mock<IApiService>();
            HttpClientFactoryMock = new Mock<IHttpClientFactory>();
            HttpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            AuthStateProviderMock = new Mock<AuthenticationStateProvider>();            
            
            // Register services to the test context
            Context.Services.AddSingleton<IAuthService>(AuthServiceMock.Object);
            Context.Services.AddSingleton<ILocalStorageService>(LocalStorageMock.Object);
            Context.Services.AddSingleton<ISnackbar>(SnackbarMock.Object);
            Context.Services.AddSingleton<IDialogService>(DialogServiceMock.Object);
            Context.Services.AddSingleton<IApiService>(ApiServiceMock.Object);
            Context.Services.AddSingleton<AuthenticationStateProvider>(AuthStateProviderMock.Object);
            Context.Services.AddSingleton<IHttpClientFactory>(HttpClientFactoryMock.Object);

            // Register a fake for CustomAuthorizationMessageHandler
            Context.Services.AddSingleton<JwtIdentity.Client.Services.CustomAuthorizationMessageHandler>(new FakeCustomAuthorizationMessageHandler());
            Context.Services.AddSingleton<System.Net.Http.HttpClient>(new System.Net.Http.HttpClient());
            Context.Services.AddSingleton<JwtIdentity.Client.Helpers.IUtility>(new Mock<JwtIdentity.Client.Helpers.IUtility>().Object);
            Context.Services.AddSingleton<MudBlazor.IDialogService>(new Mock<MudBlazor.IDialogService>().Object);

            // Let unconfigured JS calls return default values instead of throwing
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            // Register all MudBlazor services (including InternalMudLocalizer) for bUnit
            Context.Services.AddMudServices();            

            // Register Syncfusion Blazor services
            Context.Services.AddSyncfusionBlazor()
                .Replace(ServiceDescriptor.Transient<IComponentActivator, SfComponentActivator>());
            Context.Services.AddOptions();

            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            // 2. Render the global popover provider
            popoverProvider = Context.RenderComponent<MudPopoverProvider>();
        }

        public void Dispose()
        {
            Context?.Dispose();
        }

        /// <summary>
        /// Helper method to setup standard mocks for survey and question loading.
        /// This includes mocking the survey, question groups, and QuestionAndOptions API calls.
        /// </summary>
        /// <param name="survey">The survey to use for mocking</param>
        /// <param name="groups">The question groups to use for mocking</param>
        protected void SetupSurveyMocks(SurveyViewModel survey, List<QuestionGroupViewModel> groups)
        {
            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);
            ApiServiceMock.Setup(x => x.GetAsync<List<QuestionGroupViewModel>>(It.IsAny<string>()))
                .ReturnsAsync(groups);
            
            // Mock QuestionAndOptions API calls for each multiple choice question
            foreach (var question in survey.Questions.Where(q => q.QuestionType == QuestionType.MultipleChoice))
            {
                var mcQuestion = question as MultipleChoiceQuestionViewModel;
                ApiServiceMock.Setup(x => x.GetAsync<MultipleChoiceQuestionViewModel>(
                    It.Is<string>(s => s.Contains($"/QuestionAndOptions/{question.Id}"))))
                    .ReturnsAsync(mcQuestion);
            }
            
            // Mock QuestionAndOptions API calls for SelectAllThatApply questions
            foreach (var question in survey.Questions.Where(q => q.QuestionType == QuestionType.SelectAllThatApply))
            {
                var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                ApiServiceMock.Setup(x => x.GetAsync<SelectAllThatApplyQuestionViewModel>(
                    It.Is<string>(s => s.Contains($"/QuestionAndOptions/{question.Id}"))))
                    .ReturnsAsync(saQuestion);
            }
            
            // Mock QuestionAndOptions API calls for TrueFalse questions
            foreach (var question in survey.Questions.Where(q => q.QuestionType == QuestionType.TrueFalse))
            {
                var tfQuestion = question as TrueFalseQuestionViewModel;
                ApiServiceMock.Setup(x => x.GetAsync<TrueFalseQuestionViewModel>(
                    It.Is<string>(s => s.Contains($"/QuestionAndOptions/{question.Id}"))))
                    .ReturnsAsync(tfQuestion);
            }
        }

        protected void AssertPopoverText(string expectedText)
        {
            // Wait for the popover to show up in the provider
            popoverProvider.WaitForAssertion(() =>
            {
                Assert.That(
                    popoverProvider.Markup,
                    Does.Contain(expectedText)
                );
            }, timeout: TimeSpan.FromSeconds(5));

        }

        // Fake implementation for DI
        private class FakeCustomAuthorizationMessageHandler : JwtIdentity.Client.Services.CustomAuthorizationMessageHandler
        {
            public FakeCustomAuthorizationMessageHandler()
                : base(new MockNavigationManager(), new ServiceCollection().BuildServiceProvider(), new Mock<ILocalStorageService>().Object)
            {
            }
        }
    }

    public class MockNavigationManager : NavigationManager
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
}