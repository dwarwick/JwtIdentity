using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using JwtIdentity.Client.Pages.Auth;
using JwtIdentity.Client.Services;
using JwtIdentity.Client.Services.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Syncfusion.Blazor;
using System;
using System.Collections.Generic;

namespace JwtIdentity.BunitTests
{
    /// <summary>
    /// Base class for bUnit tests providing test context and commonly used mocks
    /// </summary>
    public class BUnitTestBase : IDisposable
    {
        protected TestContext Context { get; private set; }
        protected MockNavigationManager NavManager { get; private set; }

        // Core services for tests
        protected Mock<IAuthService> AuthServiceMock { get; private set; }
        protected Mock<ILocalStorageService> LocalStorageMock { get; private set; }
        protected Mock<ISnackbar> SnackbarMock { get; private set; }
        protected Mock<IDialogService> DialogServiceMock { get; private set; }
        protected Mock<IApiService> ApiServiceMock { get; private set; }
        protected Mock<IHttpClientFactory> HttpClientFactoryMock { get; private set; }
        protected Mock<AuthenticationStateProvider> AuthStateProviderMock { get; private set; }
        protected Mock<Microsoft.JSInterop.IJSRuntime> JSRuntimeMock { get; private set; }
        protected Mock<Microsoft.Extensions.Configuration.IConfiguration> ConfigMock { get; private set; }
        
        public BUnitTestBase()
        {
            // Create test context
            Context = new TestContext();

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
            JSRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            
            // Register services to the test context
            Context.Services.AddSingleton<IAuthService>(AuthServiceMock.Object);
            Context.Services.AddSingleton<ILocalStorageService>(LocalStorageMock.Object);
            Context.Services.AddSingleton<ISnackbar>(SnackbarMock.Object);
            Context.Services.AddSingleton<IDialogService>(DialogServiceMock.Object);
            Context.Services.AddSingleton<IApiService>(ApiServiceMock.Object);
            Context.Services.AddSingleton<AuthenticationStateProvider>(AuthStateProviderMock.Object);
            Context.Services.AddSingleton<IHttpClientFactory>(HttpClientFactoryMock.Object);
            Context.Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(JSRuntimeMock.Object);

            // Register a fake for CustomAuthorizationMessageHandler
            Context.Services.AddSingleton<JwtIdentity.Client.Services.CustomAuthorizationMessageHandler>(new FakeCustomAuthorizationMessageHandler());
            Context.Services.AddSingleton<System.Net.Http.HttpClient>(new System.Net.Http.HttpClient());
            Context.Services.AddSingleton<JwtIdentity.Client.Helpers.IUtility>(new Mock<JwtIdentity.Client.Helpers.IUtility>().Object);
            Context.Services.AddSingleton<MudBlazor.IDialogService>(new Mock<MudBlazor.IDialogService>().Object);

            // Register all MudBlazor services (including InternalMudLocalizer) for bUnit
            Context.Services.AddMudServices();
            
            // Register Syncfusion Blazor services
            Context.Services.AddSyncfusionBlazor();
        }

        public void Dispose()
        {
            Context?.Dispose();
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