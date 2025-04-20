using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using JwtIdentity.Services;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class EmailServiceTests : TestBase
    {
        private EmailService _service;
        private Mock<IConfiguration> _mockConfig;
        private Mock<ILogger<EmailService>> _mockLogger;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            // Setup default config values
            _mockConfig.Setup(c => c["EmailSettings:CustomerServiceEmail"]).Returns("test@domain.com");
            _mockConfig.Setup(c => c["EmailSettings:Password"]).Returns("password");
            _mockConfig.Setup(c => c["EmailSettings:Server"]).Returns("smtp.domain.com");
            _mockConfig.Setup(c => c["EmailSettings:Domain"]).Returns("domain.com");
            _service = new EmailService(_mockConfig.Object, _mockLogger.Object);
        }

        [Test]
        public void SendEmailVerificationMessage_ValidConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.SendEmailVerificationMessage("user@domain.com", "http://token.url"));
        }

        [Test]
        public void SendEmailVerificationMessage_MissingConfig_LogsError()
        {
            _mockConfig.Setup(c => c["EmailSettings:CustomerServiceEmail"]).Returns((string?)null);
            _service = new EmailService(_mockConfig.Object, _mockLogger.Object);
            _service.SendEmailVerificationMessage("user@domain.com", "http://token.url");
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        [Test]
        public void SendPasswordResetEmail_ValidConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.SendPasswordResetEmail("user@domain.com", "http://token.url"));
        }

        [Test]
        public void SendPasswordResetEmail_MissingConfig_LogsError()
        {
            _mockConfig.Setup(c => c["EmailSettings:Server"]).Returns((string?)null);
            _service = new EmailService(_mockConfig.Object, _mockLogger.Object);
            _service.SendPasswordResetEmail("user@domain.com", "http://token.url");
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task SendEmailAsync_ValidConfig_DoesNotThrow()
        {
            await Task.Run(async () => await _service.SendEmailAsync("user@domain.com", "subject", "body"));
        }

        [Test]
        public async Task SendEmailAsync_MissingConfig_LogsError()
        {
            _mockConfig.Setup(c => c["EmailSettings:Password"]).Returns((string?)null);
            _service = new EmailService(_mockConfig.Object, _mockLogger.Object);
            await _service.SendEmailAsync("user@domain.com", "subject", "body");
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }
    }
}
