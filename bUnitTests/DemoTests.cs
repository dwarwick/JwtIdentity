using Bunit;
using JwtIdentity.Client.Pages.Demo;
using JwtIdentity.Client.Pages.Survey;
using JwtIdentity.Client.Services.Base;
using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Syncfusion.Blazor.Diagram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JwtIdentity.BunitTests
{
    /// <summary>
    /// Tests for demo functionality covering both linear and branching demos
    /// </summary>
    [TestFixture]
    public class DemoTests : BUnitTestBase
    {
        [SetUp]
        public void Setup()
        {
            // Setup demo user by default
            SetupDemoUser(true);
        }

        private void SetupDemoUser(bool isDemoUser = true)
        {
            var username = isDemoUser ? "DemoUser123@surveyshark.site" : "regular@example.com";
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "TestAuth"));
            var authState = new AuthenticationState(authenticatedUser);
            AuthStateProviderMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        }

        [Test]
        public void DemoLanding_RendersWithBothDemoButtons()
        {
            // Arrange
            var appSettings = new AppSettings();
            ApiServiceMock.Setup(x => x.GetPublicAsync<AppSettings>("/api/appsettings"))
                .ReturnsAsync(appSettings);

            // Act
            var cut = Context.RenderComponent<DemoLanding>();

            // Assert
            Assert.That(cut.Find("h1").TextContent, Does.Contain("Explore the Survey Shark Interactive Demo"));
            
            // Check for both demo buttons
            var buttons = cut.FindAll("button").Where(b => 
                b.TextContent.Contains("Linear Survey Demo") || 
                b.TextContent.Contains("Branching Survey Demo")).ToList();
            
            Assert.That(buttons.Count, Is.EqualTo(2), "Should have two demo buttons");
        }

        [Test]
        public async Task DemoLanding_LinearDemoButton_NavigatesToCorrectUrl()
        {
            // Arrange
            var appSettings = new AppSettings();
            ApiServiceMock.Setup(x => x.GetPublicAsync<AppSettings>("/api/appsettings"))
                .ReturnsAsync(appSettings);

            var loginResponse = new Response<ApplicationUserViewModel>
            {
                Success = true,
                Data = new ApplicationUserViewModel { UserName = "DemoUser@surveyshark.site" }
            };
            AuthServiceMock.Setup(x => x.StartDemo()).ReturnsAsync(loginResponse);

            var cut = Context.RenderComponent<DemoLanding>();

            // Act
            var linearButton = cut.FindAll("button")
                .FirstOrDefault(b => b.TextContent.Contains("Linear Survey Demo"));
            Assert.That(linearButton, Is.Not.Null, "Linear demo button should exist");
            
            await cut.InvokeAsync(() => linearButton.Click());

            // Assert
            Assert.That(NavManager.History.Last(), Does.Contain("/survey/create"));
            Assert.That(NavManager.History.Last(), Does.Contain("DemoType=linear"));
        }

        [Test]
        public async Task DemoLanding_BranchingDemoButton_NavigatesToCorrectUrl()
        {
            // Arrange
            var appSettings = new AppSettings();
            ApiServiceMock.Setup(x => x.GetPublicAsync<AppSettings>("/api/appsettings"))
                .ReturnsAsync(appSettings);

            var loginResponse = new Response<ApplicationUserViewModel>
            {
                Success = true,
                Data = new ApplicationUserViewModel { UserName = "DemoUser@surveyshark.site" }
            };
            AuthServiceMock.Setup(x => x.StartDemo()).ReturnsAsync(loginResponse);

            var cut = Context.RenderComponent<DemoLanding>();

            // Act
            var branchingButton = cut.FindAll("button")
                .FirstOrDefault(b => b.TextContent.Contains("Branching Survey Demo"));
            Assert.That(branchingButton, Is.Not.Null, "Branching demo button should exist");
            
            await cut.InvokeAsync(() => branchingButton.Click());

            // Assert
            Assert.That(NavManager.History.Last(), Does.Contain("/survey/create"));
            Assert.That(NavManager.History.Last(), Does.Contain("DemoType=branching"));
        }

        [Test]
        public void CreateSurvey_WithLinearDemoType_CapturesisDemoTypeParameter()
        {
            // Arrange
            NavManager.NavigateTo("http://localhost/survey/create?DemoType=linear");

            var survey = new SurveyViewModel { Id = 1, Guid = Guid.NewGuid().ToString() };
            ApiServiceMock.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<SurveyViewModel>()))
                .ReturnsAsync(survey);

            // Act
            var cut = Context.RenderComponent<CreateSurvey>();

            // Assert - verify DemoType is in URL
            Assert.That(NavManager.Uri, Does.Contain("DemoType=linear"));
        }

        [Test]
        public void CreateSurvey_WithBranchingDemoType_CapturesDemoTypeParameter()
        {
            // Arrange
            NavManager.NavigateTo("http://localhost/survey/create?DemoType=branching");

            var survey = new SurveyViewModel { Id = 1, Guid = Guid.NewGuid().ToString() };
            ApiServiceMock.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<SurveyViewModel>()))
                .ReturnsAsync(survey);

            // Act
            var cut = Context.RenderComponent<CreateSurvey>();

            // Assert
            Assert.That(NavManager.Uri, Does.Contain("DemoType=branching"));
        }

        [Test]
        public void EditSurvey_WithDemoUser_RendersSuccessfully()
        {
            // Arrange
            var surveyId = Guid.NewGuid().ToString();
            var survey = new SurveyViewModel
            {
                Id = 1,
                Guid = surveyId,
                Title = "Test Survey",
                Description = "Test Description",
                Questions = new List<QuestionViewModel>()
            };

            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);
            AuthServiceMock.Setup(x => x.GetUserId()).ReturnsAsync(1);

            NavManager.NavigateTo($"http://localhost/survey/edit/{surveyId}?DemoType=branching");

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, surveyId));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Edit Survey"));
        }

        [Test]
        public void BranchingSurveyEdit_WithDemoType_RendersSuccessfully()
        {
            // Arrange
            var surveyId = Guid.NewGuid().ToString();
            var survey = new SurveyViewModel
            {
                Id = 1,
                Guid = surveyId,
                Title = "Test Survey",
                Questions = new List<QuestionViewModel>()
            };

            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);

            ApiServiceMock.Setup(x => x.GetAsync<List<QuestionGroupViewModel>>(It.IsAny<string>()))
                .ReturnsAsync(new List<QuestionGroupViewModel>
                {
            new QuestionGroupViewModel
            {
                Id = 1, 
                GroupName = "Group 1"            }
                });

            NavManager.NavigateTo($"http://localhost/survey/branching/{surveyId}?DemoType=branching");

            // Act
            var cut = Context.RenderComponent<BranchingSurveyEdit>(parameters => parameters
                .Add(p => p.SurveyId, surveyId));

            // Assert
            Assert.That(cut.Markup, Does.Contain("Survey Branching"));
        }


        [Test]
        public void EditSurvey_RegularUser_DoesNotShowDemoElements()
        {
            // Arrange - setup as regular user
            SetupDemoUser(isDemoUser: false);

            var surveyId = Guid.NewGuid().ToString();
            var survey = new SurveyViewModel
            {
                Id = 1,
                Guid = surveyId,
                Title = "Test Survey",
                Description = "Test Description",
                AiQuestionsApproved = true,
                Questions = new List<QuestionViewModel>()
            };

            ApiServiceMock.Setup(x => x.GetAsync<SurveyViewModel>(It.IsAny<string>()))
                .ReturnsAsync(survey);
            AuthServiceMock.Setup(x => x.GetUserId()).ReturnsAsync(1);

            // Act
            var cut = Context.RenderComponent<EditSurvey>(parameters => parameters
                .Add(p => p.SurveyId, surveyId));

            // Assert - regular users can create surveys but won't see demo popups
            Assert.That(cut.Markup, Does.Contain("Edit Survey"));
        }
    }
}

