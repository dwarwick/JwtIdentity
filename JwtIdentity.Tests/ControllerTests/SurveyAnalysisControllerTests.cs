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
    public class SurveyAnalysisControllerTests : TestBase<SurveyAnalysisController>
    {
        private SurveyAnalysisController _controller = null!;
        private List<Survey> _mockSurveys = null!;
        private List<SurveyAnalysis> _mockAnalyses = null!;
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

            _controller = new SurveyAnalysisController(
                MockDbContext,
                MockMapper.Object,
                MockOpenAiService.Object,
                MockLogger.Object,
                MockApiAuthService.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = HttpContext }
            };
        }

        private void SetupMockData()
        {
            _mockUsers = new List<ApplicationUser>
            {
                new ApplicationUser { Id = 1, UserName = "testuser", Email = "test@example.com" },
                new ApplicationUser { Id = 2, UserName = "admin", Email = "admin@example.com" }
            };

            _mockSurveys = new List<Survey>
            {
                new Survey
                {
                    Id = 1,
                    Title = "Test Survey",
                    Description = "Test Description",
                    Guid = Guid.NewGuid().ToString(),
                    Published = true,
                    CreatedById = 1,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                }
            };

            _mockAnalyses = new List<SurveyAnalysis>
            {
                new SurveyAnalysis
                {
                    Id = 1,
                    SurveyId = 1,
                    Analysis = "Test analysis content",
                    CreatedById = 1,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = DateTime.UtcNow.AddDays(-1)
                }
            };
        }

        private void AddDataToDbContext()
        {
            MockDbContext.ApplicationUsers.AddRange(_mockUsers);
            MockDbContext.Surveys.AddRange(_mockSurveys);
            MockDbContext.SurveyAnalyses.AddRange(_mockAnalyses);
            MockDbContext.SaveChanges();
        }

        private void SetupMockMapper()
        {
            MockMapper.Setup(m => m.Map<SurveyAnalysisViewModel>(It.IsAny<SurveyAnalysis>()))
                .Returns((SurveyAnalysis source) => new SurveyAnalysisViewModel
                {
                    Id = source.Id,
                    SurveyId = source.SurveyId,
                    Analysis = source.Analysis,
                    CreatedDate = source.CreatedDate
                });

            MockMapper.Setup(m => m.Map<List<SurveyAnalysisViewModel>>(It.IsAny<List<SurveyAnalysis>>()))
                .Returns((List<SurveyAnalysis> source) => source.Select(s => new SurveyAnalysisViewModel
                {
                    Id = s.Id,
                    SurveyId = s.SurveyId,
                    Analysis = s.Analysis,
                    CreatedDate = s.CreatedDate
                }).ToList());
        }

        private void SetupMockApiAuthService()
        {
            MockApiAuthService.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(1);
        }

        [Test]
        public async Task GetAnalyses_WithValidSurveyId_ReturnsOk()
        {
            // Arrange
            var surveyId = 1;
            HttpContext.User = CreateClaimsPrincipal(1, "testuser", new[] { "User" });

            // Act
            var result = await _controller.GetAnalyses(surveyId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var analyses = okResult.Value as List<SurveyAnalysisViewModel>;
            Assert.That(analyses, Is.Not.Null);
            Assert.That(analyses.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetAnalyses_WithUnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var surveyId = 1;
            MockApiAuthService.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(0);

            // Act
            var result = await _controller.GetAnalyses(surveyId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task GetAnalysis_WithValidId_ReturnsOk()
        {
            // Arrange
            var analysisId = 1;
            HttpContext.User = CreateClaimsPrincipal(1, "testuser", new[] { "User" });

            // Act
            var result = await _controller.GetAnalysis(analysisId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var analysis = okResult.Value as SurveyAnalysisViewModel;
            Assert.That(analysis, Is.Not.Null);
            Assert.That(analysis.Id, Is.EqualTo(analysisId));
        }

        [Test]
        public async Task GetAnalysis_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var analysisId = 999;
            HttpContext.User = CreateClaimsPrincipal(1, "testuser", new[] { "User" });

            // Act
            var result = await _controller.GetAnalysis(analysisId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}
