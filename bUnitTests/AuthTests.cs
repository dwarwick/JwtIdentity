using Bunit;
using JwtIdentity.Client.Pages.Auth;
using JwtIdentity.Common.ViewModels;
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

namespace JwtIdentity.BunitTests
{
    [TestFixture]
    public class AuthTests : BUnitTestBase, IDisposable
    {
        private MockNavigationManager _navManager;

        [SetUp]
        public void Setup()
        {
            // Setup default login behavior
            AuthServiceMock.Setup(x => x.Login(It.IsAny<ApplicationUserViewModel>()))
                .ReturnsAsync(new Response<ApplicationUserViewModel>
                {
                    Success = true,
                    Data = new ApplicationUserViewModel { UserName = "test@example.com" }
                });
                
            // Setup localStorage defaults
            LocalStorageMock.Setup(x => x.ContainKeyAsync("survey id", default))
                .ReturnsAsync(false);

            // Get MockNavigationManager from DI
            _navManager = (MockNavigationManager)Context.Services.GetRequiredService<NavigationManager>();
            _navManager.History.Clear(); // Clear navigation history before each test
        }

        [Test]
        public void Login_Component_Renders_Correctly()
        {
            // Arrange & Act
            var cut = Context.RenderComponent<Login>();

            // Assert
            Assert.That(cut, Is.Not.Null);
            Assert.That(cut.FindAll("input").Count, Is.GreaterThanOrEqualTo(2), "Component should contain at least two input fields");
            Assert.That(cut.Find("button"), Is.Not.Null, "Component should contain a button");
            Assert.That(cut.Markup.Contains("Login"), Is.True, "Component should contain 'Login' text");
        }

        [Test]
        public void Login_Form_CanSubmit()
        {
            // Arrange - setup is done in the Setup() method
            
            // Act - render component and submit form
            var cut = Context.RenderComponent<Login>();
            cut.Find("#username").Change("test@example.com");
            cut.Find("#password").Change("password123");
            cut.Find("form").Submit();
            
            // Assert - check that the service was called
            AuthServiceMock.Verify(x => x.Login(It.IsAny<ApplicationUserViewModel>()), Times.AtLeast(2));
            
            // Check navigation occurred
            Assert.That(_navManager.History.Count, Is.GreaterThan(0), "Navigation should have occurred");
            Assert.That(_navManager.Uri, Does.EndWith("/"), "Should navigate to home page after successful login");
        }

        [Test]
        public void Login_WithSurveyId_ChecksLocalStorage()
        {
            // Arrange
            // Override the default setup to have survey ID in localStorage
            LocalStorageMock.Setup(x => x.ContainKeyAsync("survey id", default))
                .ReturnsAsync(true);
                
            string surveyId = "test-survey-id";
            LocalStorageMock.Setup(x => x.GetItemAsStringAsync("survey id", default))
                .ReturnsAsync(surveyId);

            LocalStorageMock.Setup(x => x.RemoveItemAsync("survey id", default))
                .Returns(ValueTask.CompletedTask);

            // Act
            var cut = Context.RenderComponent<Login>();
            cut.Find("#username").Change("test@example.com");
            cut.Find("#password").Change("password123");
            cut.Find("form").Submit();
            
            // Assert
            LocalStorageMock.Verify(x => x.ContainKeyAsync("survey id", default), Times.AtLeast(2));
            LocalStorageMock.Verify(x => x.RemoveItemAsync("survey id", default), Times.Once);
            
            Assert.That(_navManager.History.Count, Is.GreaterThan(0), "Navigation should have occurred");
            Assert.That(_navManager.Uri, Does.EndWith($"/survey/{surveyId}"), "Should navigate to survey page");
        }

        [Test]
        public void Login_FailedLogin_DoesNotNavigate()
        {
            // Arrange - override the default login response
            AuthServiceMock.Setup(x => x.Login(It.IsAny<ApplicationUserViewModel>()))
                .ReturnsAsync(new Response<ApplicationUserViewModel>
                {
                    Success = false,
                    Message = "Invalid username or password"
                });

            // Act
            var cut = Context.RenderComponent<Login>();
            cut.Find("#username").Change("test@example.com");
            cut.Find("#password").Change("wrongpassword");
            cut.Find("form").Submit();
            
            // Assert
            Assert.That(_navManager.History.Count, Is.EqualTo(0), "No navigation should have occurred");
        }

        [Test]
        public void Login_Form_HasRequiredFields()
        {
            // Arrange & Act
            var cut = Context.RenderComponent<Login>();
            
            // Assert
            var usernameField = cut.Find("#username");
            var passwordField = cut.Find("#password");
            
            Assert.That(usernameField, Is.Not.Null, "Username field should be present");
            Assert.That(passwordField, Is.Not.Null, "Password field should be present");
            
            Assert.That(usernameField.GetAttribute("autocomplete"), Is.EqualTo("username"), 
                "Username field should have autocomplete set to 'username'");
            Assert.That(passwordField.GetAttribute("autocomplete"), Is.EqualTo("current-password"), 
                "Password field should have autocomplete set to 'current-password'");
        }
    }
}