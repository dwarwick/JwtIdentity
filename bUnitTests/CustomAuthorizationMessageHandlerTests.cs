using Blazored.LocalStorage;
using JwtIdentity.Client.Services;
using Microsoft.AspNetCore.Components;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JwtIdentity.BunitTests
{
    [TestFixture]
    public class CustomAuthorizationMessageHandlerTests
    {
        private Mock<ILocalStorageService> _localStorageMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private TestNavigationManager _navigationManager;
        private CustomAuthorizationMessageHandler _handler;
        private HttpMessageInvoker _invoker;

        [SetUp]
        public void Setup()
        {
            _localStorageMock = new Mock<ILocalStorageService>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _navigationManager = new TestNavigationManager();
            
            _handler = new CustomAuthorizationMessageHandler(
                _navigationManager, 
                _serviceProviderMock.Object, 
                _localStorageMock.Object);
            
            // Set up a fake inner handler that returns a response
            _handler.InnerHandler = new FakeHttpMessageHandler();
            _invoker = new HttpMessageInvoker(_handler);
        }

        [Test]
        public async Task SendAsync_WithUnauthorizedResponse_RedirectsToLogin()
        {
            // Arrange
            _navigationManager.NavigateTo("http://localhost/survey/my-survey");
            
            var fakeHandler = (FakeHttpMessageHandler)_handler.InnerHandler;
            fakeHandler.StatusCodeToReturn = HttpStatusCode.Unauthorized;
            
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            
            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            
            // Note: Token clearing only happens when OperatingSystem.IsBrowser() is true,
            // which is false in test environments. In actual browser, tokens would be cleared.
            
            // Verify navigation to login with return URL
            Assert.That(_navigationManager.NavigationHistory.Count, Is.EqualTo(2)); // Initial navigation + redirect
            Assert.That(_navigationManager.Uri, Does.Contain("login?returnUrl="));
            Assert.That(_navigationManager.Uri, Does.Contain("survey/my-survey"));
        }

        [Test]
        public async Task SendAsync_WithUnauthorizedResponse_OnHomePage_RedirectsToLoginWithEmptyReturnUrl()
        {
            // Arrange - navigation manager starts at home by default
            
            var fakeHandler = (FakeHttpMessageHandler)_handler.InnerHandler;
            fakeHandler.StatusCodeToReturn = HttpStatusCode.Unauthorized;
            
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            
            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            
            // Verify navigation to login
            Assert.That(_navigationManager.NavigationHistory.Count, Is.EqualTo(1));
            Assert.That(_navigationManager.Uri, Does.Contain("login?returnUrl="));
        }

        [Test]
        public async Task SendAsync_WithOkResponse_DoesNotClearTokensOrNavigate()
        {
            // Arrange
            _navigationManager.NavigateTo("http://localhost/survey/my-survey");
            var initialHistoryCount = _navigationManager.NavigationHistory.Count;
            
            var fakeHandler = (FakeHttpMessageHandler)_handler.InnerHandler;
            fakeHandler.StatusCodeToReturn = HttpStatusCode.OK;
            
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            
            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            // Verify tokens were NOT cleared
            _localStorageMock.Verify(x => x.RemoveItemAsync(It.IsAny<string>(), default), Times.Never);
            
            // Verify no navigation occurred (count should be same as before)
            Assert.That(_navigationManager.NavigationHistory.Count, Is.EqualTo(initialHistoryCount));
        }

        [Test]
        public async Task SendAsync_WithNotFoundResponse_NavigatesToHomeAndDoesNotClearTokens()
        {
            // Arrange
            _navigationManager.NavigateTo("http://localhost/survey/my-survey");
            
            var fakeHandler = (FakeHttpMessageHandler)_handler.InnerHandler;
            fakeHandler.StatusCodeToReturn = HttpStatusCode.NotFound;
            
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            
            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            
            // Verify tokens were NOT cleared (404 is not an auth issue)
            _localStorageMock.Verify(x => x.RemoveItemAsync(It.IsAny<string>(), default), Times.Never);
            
            // Verify navigation to home page
            Assert.That(_navigationManager.NavigationHistory.Count, Is.EqualTo(2)); // Initial + redirect
            Assert.That(_navigationManager.Uri, Does.EndWith("/"));
        }

        // Fake handler that returns a specified status code
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            public HttpStatusCode StatusCodeToReturn { get; set; } = HttpStatusCode.OK;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(StatusCodeToReturn)
                {
                    Content = new StringContent("Test response")
                });
            }
        }

        // Test navigation manager that tracks navigation history
        private class TestNavigationManager : NavigationManager
        {
            public System.Collections.Generic.List<string> NavigationHistory { get; } = new System.Collections.Generic.List<string>();

            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                var absoluteUri = ToAbsoluteUri(uri).ToString();
                NavigationHistory.Add(absoluteUri);
                Uri = absoluteUri;
            }
        }
    }
}
