using System;
using System.Threading.Tasks;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class EmailTokenAuthorizationProviderTests : TestBase<EmailTokenAuthorizationProvider<ApplicationUser>>
    {
        private Mock<IDataProtectionProvider> _mockDataProtectionProvider;
        private Mock<IDataProtector> _mockDataProtector;
        private Mock<IOptions<DataProtectionTokenProviderOptions>> _mockOptions;
        private DataProtectionTokenProviderOptions _options;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            _mockDataProtector = new Mock<IDataProtector>();
            _mockOptions = new Mock<IOptions<DataProtectionTokenProviderOptions>>();
            MockLogger = new Mock<ILogger<EmailTokenAuthorizationProvider<ApplicationUser>>>();
            _options = new DataProtectionTokenProviderOptions { TokenLifespan = TimeSpan.FromHours(1) };
            _mockOptions.Setup(o => o.Value).Returns(_options);
            _mockDataProtectionProvider.Setup(p => p.CreateProtector(It.IsAny<string>())).Returns(_mockDataProtector.Object);
        }

        [Test]
        public void Constructor_Succeeds()
        {
            Assert.DoesNotThrow(() =>
            {
                var provider = new EmailTokenAuthorizationProvider<ApplicationUser>(
                    _mockDataProtectionProvider.Object,
                    _mockOptions.Object,
                    MockLogger.Object);
            });
        }

        [Test]
        public async Task Can_Generate_And_Validate_Token()
        {
            // Arrange
            var provider = new EmailTokenAuthorizationProvider<ApplicationUser>(
                _mockDataProtectionProvider.Object,
                _mockOptions.Object,
                MockLogger.Object);
            var user = new ApplicationUser { Id = 1, UserName = "testuser", Email = "test@example.com" };
            var userManager = MockUserManager.Object;

            // Setup required UserManager methods to avoid nulls
            MockUserManager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id.ToString());
            MockUserManager.Setup(m => m.GetUserNameAsync(user)).ReturnsAsync(user.UserName);
            MockUserManager.Setup(m => m.GetEmailAsync(user)).ReturnsAsync(user.Email);

            // Use a real DataProtectionProvider for integration test
            var realProvider = DataProtectionProvider.Create(Guid.NewGuid().ToString());
            var realLogger = new Mock<ILogger<EmailTokenAuthorizationProvider<ApplicationUser>>>();
            var realOptions = Options.Create(new DataProtectionTokenProviderOptions { TokenLifespan = TimeSpan.FromMinutes(5) });
            var realTokenProvider = new EmailTokenAuthorizationProvider<ApplicationUser>(realProvider, realOptions, realLogger.Object);
            // Act
            var token = await realTokenProvider.GenerateAsync("Purpose", userManager, user);
            var isValid = await realTokenProvider.ValidateAsync("Purpose", token, userManager, user);
            // Assert
            Assert.That(token, Is.Not.Null.And.Not.Empty);
            Assert.That(isValid, Is.True);
        }
    }
}
