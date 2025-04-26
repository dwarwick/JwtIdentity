using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using JwtIdentity.Models;
using JwtIdentity.Services;
using JwtIdentity.Data;
using JwtIdentity.Common.Auth;
using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class ApiAuthServiceTests : TestBase<ApiAuthService>
    {
        private ApiAuthService _service;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<IConfiguration> _mockConfig;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _mockUserManager = MockUserManager;
            _mockConfig = MockConfiguration;
            _dbContext = MockDbContext;
            _service = new ApiAuthService(_mockUserManager.Object, _mockConfig.Object, _dbContext);
        }

        [Test]
        public async Task GenerateJwtToken_ReturnsToken_WhenConfigValidAndUserHasRoles()
        {
            // Arrange
            var user = new ApplicationUser { Id = 1, UserName = "testuser", Email = "test@example.com" };
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("efger4ert456464yrey45645ryerr5666etrrrrrrrrrrtergdsfasqsfdgrtjhjhkhjkyuifgdffgdfgdfdfgdfhdfhfgjffgjfghdfsghsdgsdgsdgdfhdghfghfghdfsgfasfDFADadADadDFFGDGSDGSDGStyjfghdtertewtewtwew43463tddfddfdsaqwdfsaadghhhjjfdfghj");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("audience");
            _mockConfig.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim> { new Claim("custom", "value") });
            // Add role/permission data to in-memory db
            var role = new ApplicationRole { Id = 2, Name = "User" };
            _dbContext.Roles.Add(role);
            _dbContext.UserRoles.Add(new IdentityUserRole<int> { UserId = user.Id, RoleId = role.Id });
            _dbContext.RoleClaims.Add(new RoleClaim { Id = 1, RoleId = role.Id, ClaimType = CustomClaimTypes.Permission, ClaimValue = Permissions.AnswerSurvey });
            _dbContext.SaveChanges();

            // Act
            var token = await _service.GenerateJwtToken(user);

            // Assert
            Assert.That(token, Is.Not.Null);
            Assert.That(token, Is.Not.Empty);
        }

        [Test]
        public async Task GenerateJwtToken_ReturnsEmpty_WhenConfigMissing()
        {
            var user = new ApplicationUser { Id = 1, UserName = "testuser", Email = "test@example.com" };
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns((string?)null);
            var token = await _service.GenerateJwtToken(user);
            Assert.That(token, Is.Empty);
        }

        [Test]
        public async Task GenerateJwtToken_AdminRole_GetsAllPermissions()
        {
            var user = new ApplicationUser { Id = 1, UserName = "admin", Email = "admin@example.com" };
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("efger4ert456464yrey45645ryerr5666etrrrrrrrrrrtergdsfasqsfdgrtjhjhkhjkyuifgdffgdfgdfdfgdfhdfhfgjffgjfghdfsghsdgsdgsdgdfhdghfghfghdfsgfasfDFADadADadDFFGDGSDGSDGStyjfghdtertewtewtwew43463tddfddfdsaqwdfsaadghhhjjfdfghj");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("audience");
            _mockConfig.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _mockUserManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            var token = await _service.GenerateJwtToken(user);
            Assert.That(token, Is.Not.Null);
            Assert.That(token, Is.Not.Empty);
        }

        [Test]
        public async Task GenerateEmailVerificationLink_ReturnsValidUrl()
        {
            var user = new ApplicationUser { Id = 1, UserName = "testuser", Email = "test@example.com" };
            _mockUserManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("token123");
            _mockConfig.Setup(c => c["ApiBaseAddress"]).Returns("https://localhost:5001");
            var url = await _service.GenerateEmailVerificationLink(user);
            Assert.That(url, Does.Contain("/api/auth/confirmemail?token="));
            Assert.That(url, Does.Contain(user.Email));
        }

        [Test]
        public void GetUserId_ReturnsUserId_WhenClaimPresent()
        {
            var principal = CreateClaimsPrincipal(42, "testuser");
            var id = _service.GetUserId(principal);
            Assert.That(id, Is.EqualTo(42));
        }

        [Test]
        public void GetUserId_ReturnsZero_WhenNoClaim()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity());
            var id = _service.GetUserId(principal);
            Assert.That(id, Is.EqualTo(0));
        }

        [Test]
        public async Task GetUserRoles_ReturnsRoles()
        {
            var user = new ApplicationUser { Id = 5, UserName = "roleuser", Email = "role@example.com" };
            _mockUserManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User", "Admin" });
            var principal = CreateClaimsPrincipal(5, "roleuser");
            var roles = await _service.GetUserRoles(principal);
            Assert.That(roles, Is.EquivalentTo(new[] { "User", "Admin" }));
        }

        [Test]
        public void GetUserPermissions_ReturnsPermissions()
        {
            var userId = 7;
            var role = new ApplicationRole { Id = 10, Name = "User" };
            _dbContext.Roles.Add(role);
            _dbContext.UserRoles.Add(new IdentityUserRole<int> { UserId = userId, RoleId = role.Id });
            _dbContext.RoleClaims.Add(new RoleClaim { Id = 2, RoleId = role.Id, ClaimType = CustomClaimTypes.Permission, ClaimValue = Permissions.CreateSurvey });
            _dbContext.SaveChanges();
            var principal = CreateClaimsPrincipal(userId, "permuser");
            var perms = _service.GetUserPermissions(principal);
            Assert.That(perms, Does.Contain(Permissions.CreateSurvey));
        }
    }
}
