using JwtIdentity.Common.ViewModels;
using JwtIdentity.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class AppSettingsControllerTests : TestBase<AppSettingsController>
    {
        private AppSettingsController _controller;
        private Mock<IOptions<AppSettings>> _mockOptions;
        private AppSettings _appSettings;

        [SetUp]
        public override void BaseSetUp()
        {
            // Call base setup to initialize base mocks and in-memory database
            base.BaseSetUp();

            // Create test app settings
            _appSettings = new AppSettings
            {
                ApiBaseAddress = "https://api.example.com",
                DetailedErrors = true,
                Logging = new LoggingOptions
                {
                    LogLevel = new LogLevelOptions
                    {
                        Default = "Information",
                        MicrosoftAspNetCore = "Warning",
                        MicrosoftEntityFrameworkCore = "Warning"
                    }
                },
                EmailSettings = new EmailSettings
                {
                    CustomerServiceEmail = "test@example.com",
                    Domain = "example.com",
                    Server = "smtp.example.com",
                    Password = "test-password"
                }
            };

            // Setup options mock
            _mockOptions = new Mock<IOptions<AppSettings>>();
            _ = _mockOptions.Setup(m => m.Value).Returns(_appSettings);

            // Set up controller with mocked options
            _controller = new AppSettingsController(_mockOptions.Object, MockLogger.Object);
        }

        [TearDown]
        public override void BaseTearDown()
        {
            base.BaseTearDown();
        }

        [Test]
        public void Get_ReturnsAppSettings()
        {
            // Act
            var result = _controller.Get();

            // Assert
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>(), "Should return OkObjectResult");

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null, "OkObjectResult should not be null");

            var returnedSettings = okResult.Value as AppSettings;
            Assert.That(returnedSettings, Is.Not.Null, "Returned value should be AppSettings");
            Assert.That(returnedSettings, Is.SameAs(_appSettings), "Should return the same AppSettings instance");
        }
    }
}