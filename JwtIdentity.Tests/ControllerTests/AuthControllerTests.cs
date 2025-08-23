using JwtIdentity.Common.Auth;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Controllers;
using JwtIdentity.Data;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Text;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class AuthControllerTests : TestBase<AuthController>
    {
        private AuthController _controller;
        private Mock<IEmailService> _mockEmailService;

        [SetUp]
        public override void BaseSetUp()
        {
            // Call the base setup to initialize common mock objects
            base.BaseSetUp();

            // Setup email service mock
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _mockEmailService.Setup(e => e.SendEmailVerificationMessage(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            MockEmailService = _mockEmailService;

            MockConfiguration.Setup(c => c["EmailSettings:CustomerServiceEmail"]).Returns("admin@example.com");

            // Create the controller with the mocked dependencies
            _controller = new AuthController(
                MockUserManager.Object,
                MockSignInManager.Object,
                MockConfiguration.Object,
                MockMapper.Object,
                MockDbContext,
                _mockEmailService.Object,
                MockApiAuthService.Object,
                MockLogger.Object
            );

            // Set controller context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = HttpContext
            };
        }

        #region Login Tests

        [Test]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
        {
            // Arrange
            var model = new LoginModel
            {
                Username = "testuser",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = 1, // Using int instead of string
                UserName = model.Username,
                Email = "test@example.com"
            };

            // Setup UserManager to return our test user when FindByNameAsync is called
            MockUserManager.Setup(um => um.FindByNameAsync(model.Username))
                .ReturnsAsync(user);

            // Setup UserManager to return true when CheckPasswordAsync is called
            MockUserManager.Setup(um => um.CheckPasswordAsync(user, model.Password))
                .ReturnsAsync(true);

            // Setup ApiAuthService to generate a token
            MockApiAuthService.Setup(a => a.GenerateJwtToken(user))
                .ReturnsAsync("test-jwt-token");

            // Setup Mapper to map user to view model
            MockMapper.Setup(m => m.Map<ApplicationUserViewModel>(user))
                .Returns(new ApplicationUserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                });

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<ApplicationUserViewModel>());

            var userViewModel = okResult.Value as ApplicationUserViewModel;
            Assert.That(userViewModel, Is.Not.Null);
            Assert.That(userViewModel.UserName, Is.EqualTo(user.UserName));
            Assert.That(userViewModel.Token, Is.EqualTo("test-jwt-token"));

            _mockEmailService.Verify(e => e.SendEmailAsync("admin@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("testuser"))), Times.Once);
        }

        [Test]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var model = new LoginModel
            {
                Username = "wronguser",
                Password = "WrongPassword123!"
            };

            // Setup UserManager to return null (user not found)
            MockUserManager.Setup(um => um.FindByNameAsync(model.Username))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            var model = new LoginModel
            {
                Username = "testuser",
                Password = "WrongPassword123!"
            };

            var user = new ApplicationUser
            {
                Id = 1,
                UserName = model.Username,
                Email = "test@example.com"
            };

            // Setup UserManager to return our test user
            MockUserManager.Setup(um => um.FindByNameAsync(model.Username))
                .ReturnsAsync(user);

            // Setup UserManager to return false for wrong password
            MockUserManager.Setup(um => um.CheckPasswordAsync(user, model.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Login_WithEmptyCredentials_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new LoginModel
            {
                Username = "",
                Password = ""
            };

            // Add model error to make ModelState invalid
            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Login_WithAnonymousUser_ShouldUseAnonymousCredentials()
        {
            // Arrange
            var model = new LoginModel
            {
                Username = "logmein",
                Password = "anypassword" // Will be ignored and replaced with anonymous password
            };

            var anonymousPassword = "anonymous123";
            MockConfiguration.Setup(c => c["AnonymousPassword"]).Returns(anonymousPassword);

            var anonymousUser = new ApplicationUser
            {
                Id = 999,
                UserName = "anonymous",
                Email = "anonymous@example.com"
            };

            // Setup UserManager to return the anonymous user
            MockUserManager.Setup(um => um.FindByNameAsync("anonymous"))
                .ReturnsAsync(anonymousUser);

            // Setup UserManager to return true for anonymous password
            MockUserManager.Setup(um => um.CheckPasswordAsync(anonymousUser, anonymousPassword))
                .ReturnsAsync(true);

            // Setup ApiAuthService to generate a token
            MockApiAuthService.Setup(a => a.GenerateJwtToken(anonymousUser))
                .ReturnsAsync("anonymous-jwt-token");

            // Setup Mapper to map user to view model
            MockMapper.Setup(m => m.Map<ApplicationUserViewModel>(anonymousUser))
                .Returns(new ApplicationUserViewModel
                {
                    Id = anonymousUser.Id,
                    UserName = anonymousUser.UserName,
                    Email = anonymousUser.Email
                });

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<ApplicationUserViewModel>());

            var userViewModel = okResult.Value as ApplicationUserViewModel;
            Assert.That(userViewModel, Is.Not.Null);
            Assert.That(userViewModel.UserName, Is.EqualTo("anonymous"));
        }

        [Test]
        public async Task Login_ValidCredentials()
        {
            // Arrange
            var model = new LoginModel
            {
                Username = "test@example.com",
                Password = "P@ssw0rd"
            };

            var userId = 123; // Using int instead of string
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            MockUserManager.Setup(m => m.FindByNameAsync(model.Username))
                .ReturnsAsync(user);
            MockUserManager.Setup(m => m.CheckPasswordAsync(user, model.Password))
                .ReturnsAsync(true);
            MockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            // Setup ApiAuthService to generate a token
            MockApiAuthService.Setup(a => a.GenerateJwtToken(user))
                .ReturnsAsync("test-jwt-token");

            // Setup Mapper to map user to view model
            MockMapper.Setup(m => m.Map<ApplicationUserViewModel>(user))
                .Returns(new ApplicationUserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                });

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<ApplicationUserViewModel>());

            var userViewModel = okResult.Value as ApplicationUserViewModel;
            Assert.That(userViewModel, Is.Not.Null);
            Assert.That(userViewModel.UserName, Is.EqualTo(user.UserName));
            Assert.That(userViewModel.Token, Is.EqualTo("test-jwt-token"));
        }

        #endregion

        #region Register Tests

        [Test]
        public async Task Register_WithValidModel_ShouldCreateUser()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Setup UserManager to return null (user doesn't exist yet)
            MockUserManager.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Setup UserManager to return success for CreateAsync
            MockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Setup UserManager to return success for AddToRoleAsync
            MockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "UnconfirmedUser"))
                .ReturnsAsync(IdentityResult.Success);

            // Setup ApiAuthService to generate verification link
            MockApiAuthService.Setup(a => a.GenerateEmailVerificationLink(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("https://example.com/verify?token=abc123");

            // Act
            var result = await _controller.Register(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<RegisterViewModel>());

            var returnedModel = okResult.Value as RegisterViewModel;
            Assert.That(returnedModel, Is.Not.Null);
            Assert.That(returnedModel.Response, Is.EqualTo("User created successfully"));

            // Verify email was sent
            _mockEmailService.Verify(e => e.SendEmailVerificationMessage(
                model.Email,
                It.IsAny<string>()),
                Times.Once);

            _mockEmailService.Verify(e => e.SendEmailAsync("admin@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains(model.Email))), Times.Once);
        }

        [Test]
        public async Task Register_WithExistingEmail_ShouldReturnError()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Email = "existing@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var existingUser = new ApplicationUser
            {
                Id = 2,
                UserName = model.Email,
                Email = model.Email
            };

            // Setup UserManager to return existing user
            MockUserManager.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _controller.Register(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<RegisterViewModel>());

            var returnedModel = okResult.Value as RegisterViewModel;
            Assert.That(returnedModel, Is.Not.Null);
            Assert.That(returnedModel.Response, Is.EqualTo("Email already exists"));
        }

        [Test]
        public async Task Register_WithPasswordMismatch_ShouldReturnError()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "DifferentPassword123!"
            };

            // Add model error to make ModelState invalid
            _controller.ModelState.AddModelError("ConfirmPassword", "Passwords do not match");

            // Act
            var result = await _controller.Register(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<RegisterViewModel>());

            var returnedModel = okResult.Value as RegisterViewModel;
            Assert.That(returnedModel, Is.Not.Null);
            Assert.That(returnedModel.Response, Is.EqualTo("Invalid client request"));
        }

        #endregion

        #region Email Confirmation Tests

        [Test]
        public async Task ConfirmEmail_WithValidToken_ShouldConfirmEmailAndReturnRedirect()
        {
            // Arrange
            var token = "validToken";
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var email = "user@example.com";

            var user = new ApplicationUser
            {
                Id = 3,
                UserName = email,
                Email = email
            };

            // Setup UserManager to return test user
            MockUserManager.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);

            // Setup UserManager to return success for ConfirmEmailAsync
            MockUserManager.Setup(um => um.ConfirmEmailAsync(user, token))
                .ReturnsAsync(IdentityResult.Success);

            // Setup UserManager to return success for role management
            MockUserManager.Setup(um => um.RemoveFromRoleAsync(user, "UnconfirmedUser"))
                .ReturnsAsync(IdentityResult.Success);

            MockUserManager.Setup(um => um.AddToRoleAsync(user, "User"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ConfirmEmail(encodedToken, email);

            // Assert
            Assert.That(result, Is.InstanceOf<LocalRedirectResult>());
            var redirectResult = result as LocalRedirectResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.Url, Is.EqualTo("/users/emailconfirmed"));
        }

        [Test]
        public async Task ConfirmEmail_WithInvalidToken_ShouldReturnErrorRedirect()
        {
            // Arrange
            var token = "invalidToken";
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var email = "user@example.com";

            var user = new ApplicationUser
            {
                Id = 3,
                UserName = email,
                Email = email
            };

            // Setup UserManager to return test user
            MockUserManager.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);

            // Setup UserManager to return failure for ConfirmEmailAsync
            MockUserManager.Setup(um => um.ConfirmEmailAsync(user, token))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

            // Act
            var result = await _controller.ConfirmEmail(encodedToken, email);

            // Assert
            Assert.That(result, Is.InstanceOf<LocalRedirectResult>());
            var redirectResult = result as LocalRedirectResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.Url, Is.EqualTo("/users/emailnotconfirmed"));
        }

        [Test]
        public async Task ConfirmEmail_ValidData()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var userId = 123;
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                Email = email
            };

            MockUserManager.Setup(m => m.FindByEmailAsync(email))
                .ReturnsAsync(user);
            MockUserManager.Setup(m => m.ConfirmEmailAsync(user, token))
                .ReturnsAsync(IdentityResult.Success);
            MockUserManager.Setup(m => m.RemoveFromRoleAsync(user, "UnconfirmedUser"))
                .ReturnsAsync(IdentityResult.Success);
            MockUserManager.Setup(m => m.AddToRoleAsync(user, "User"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ConfirmEmail(encodedToken, email);

            // Assert
            Assert.That(result, Is.InstanceOf<LocalRedirectResult>());
            var redirectResult = result as LocalRedirectResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.Url, Is.EqualTo("/users/emailconfirmed"));
        }

        #endregion

        #region Password Reset Tests

        [Test]
        public async Task ForgotPassword_WithValidEmail_ShouldSendResetEmail()
        {
            // Arrange
            var model = new AuthController.ForgotPasswordViewModel
            {
                Email = "user@example.com"
            };

            var user = new ApplicationUser
            {
                Id = 4,
                UserName = model.Email,
                Email = model.Email
            };

            // Setup UserManager to return test user
            MockUserManager.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            // Setup UserManager to generate reset token
            MockUserManager.Setup(um => um.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("reset-token-123");

            // Setup Configuration to return base address
            MockConfiguration.Setup(c => c["ApiBaseAddress"])
                .Returns("https://example.com");

            // Act
            var result = await _controller.ForgotPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value.ToString(), Does.Contain("Success"));

            // Verify email was sent
            _mockEmailService.Verify(e => e.SendPasswordResetEmail(
                model.Email,
                It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task ResetPassword_WithValidData_ShouldResetPassword()
        {
            // Arrange
            var token = "reset-token-123";
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var model = new AuthController.ResetPasswordViewModel
            {
                Email = "user@example.com",
                Token = encodedToken,
                Password = "NewPassword123!"
            };

            var user = new ApplicationUser
            {
                Id = 4,
                UserName = model.Email,
                Email = model.Email
            };

            // Setup UserManager to return test user
            MockUserManager.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            // Setup UserManager to return success for ResetPasswordAsync
            MockUserManager.Setup(um => um.ResetPasswordAsync(user, token, model.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            // Access the anonymous object properties using reflection to avoid runtime binding errors
            var responseObj = okResult.Value;

            // Extract properties using reflection
            var successProperty = responseObj.GetType().GetProperty("Success");
            var messageProperty = responseObj.GetType().GetProperty("Message");

            Assert.That(successProperty, Is.Not.Null, "Success property not found in response");
            Assert.That(messageProperty, Is.Not.Null, "Message property not found in response");

            var isSuccess = (bool)successProperty.GetValue(responseObj);
            var message = (string)messageProperty.GetValue(responseObj);

            Assert.That(isSuccess, Is.True);
            Assert.That(message, Does.Contain("reset successfully"));
        }

        [Test]
        public async Task ResetPassword_ValidCode()
        {
            // Arrange
            var token = "valid-token";
            // Encode the token for URL transmission
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var model = new AuthController.ResetPasswordViewModel
            {
                Email = "test@example.com",
                Password = "NewP@ssw0rd",
                Token = encodedToken
            };

            var userId = 123;
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            MockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);
            MockUserManager.Setup(m => m.ResetPasswordAsync(user, token, model.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            // Access the anonymous object properties using a cast to expand object
            var responseObj = okResult.Value;

            // Extract properties using reflection to avoid runtime binding errors
            var successProperty = responseObj.GetType().GetProperty("Success");
            var messageProperty = responseObj.GetType().GetProperty("Message");

            Assert.That(successProperty, Is.Not.Null, "Success property not found in response");
            Assert.That(messageProperty, Is.Not.Null, "Message property not found in response");

            var isSuccess = (bool)successProperty.GetValue(responseObj);
            var message = (string)messageProperty.GetValue(responseObj);

            Assert.That(isSuccess, Is.True);
            Assert.That(message, Does.Contain("reset successfully"));
        }

        #endregion

        #region Role Management Tests

        [Test]
        public async Task GetRolesAndPermissions_ShouldReturnRolesWithClaims()
        {
            // Arrange
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole
                {
                    Id = 1,
                    Name = "Admin",
                    Claims = new List<RoleClaim>
                    {
                        new RoleClaim { Id = 1, RoleId = 1, ClaimType = "Permission", ClaimValue = "ManageUsers" }
                    }
                },
                new ApplicationRole
                {
                    Id = 2,
                    Name = "User",
                    Claims = new List<RoleClaim>
                    {
                        new RoleClaim { Id = 2, RoleId = 2, ClaimType = "Permission", ClaimValue = "ViewReports" }
                    }
                }
            };

            // Setup our mock DbContext with ApplicationRoles
            var mockRolesDbSet = MockDbSetFactory.CreateMockDbSet(roles);

            // Create a new DbContext instance to use for this test
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"RolesTest_{Guid.NewGuid()}")
                .Options;

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(HttpContext);

            var testDbContext = new ApplicationDbContext(options, httpContextAccessor.Object);

            // Add our test data to the in-memory database
            await testDbContext.ApplicationRoles.AddRangeAsync(roles);
            await testDbContext.SaveChangesAsync();

            // Create a controller with our test DbContext
            var controller = new AuthController(
                MockUserManager.Object,
                MockSignInManager.Object,
                MockConfiguration.Object,
                MockMapper.Object,
                testDbContext,
                MockEmailService.Object,
                MockApiAuthService.Object,
                MockLogger.Object
            );

            // Set up the mapper to map ApplicationRole to ApplicationRoleViewModel
            MockMapper.Setup(m => m.Map<List<ApplicationRoleViewModel>>(It.IsAny<List<ApplicationRole>>()))
                .Returns((List<ApplicationRole> sourceRoles) =>
                    sourceRoles.Select(r => new ApplicationRoleViewModel
                    {
                        Id = r.Id.ToString(), // Convert int to string
                        Name = r.Name,
                        Claims = r.Claims.Select(c => new RoleClaimViewModel
                        {
                            Id = c.Id,
                            RoleId = c.RoleId.ToString(), // Convert int to string
                            ClaimType = c.ClaimType,
                            ClaimValue = c.ClaimValue
                        }).ToList()
                    }).ToList()
                );

            // Add a mock user with the required permission
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "admin@example.com"),
                new Claim("Permission", "ManageUsers")
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await controller.GetRolesAndPermissions();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<List<ApplicationRoleViewModel>>());

            var viewModels = okResult.Value as List<ApplicationRoleViewModel>;
            Assert.That(viewModels, Is.Not.Null);
            Assert.That(viewModels.Count, Is.EqualTo(2));

            var adminRole = viewModels.FirstOrDefault(r => r.Name == "Admin");
            Assert.That(adminRole, Is.Not.Null);
            Assert.That(adminRole.Claims.Count, Is.EqualTo(1));
            Assert.That(adminRole.Claims[0].ClaimValue, Is.EqualTo("ManageUsers"));
        }

        #endregion

        // Fix for CreateUser_ShouldSucceed (around line 94)
        [Test]
        public async Task CreateUser_ShouldSucceed()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Email = "testuser@example.com",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };

            var userId = 123; // Using int instead of string
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = model.Email,
                Email = model.Email
            };

            // First FindByEmailAsync should return null (user doesn't exist yet)
            MockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Setup CreateAsync to succeed
            MockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<ApplicationUser, string>((createdUser, password) =>
                {
                    // Update the user reference for subsequent calls
                    createdUser.Id = userId;
                    createdUser.UserName = model.Email;
                    createdUser.Email = model.Email;

                    // Now we can update the mock to return this user
                    MockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                        .ReturnsAsync(createdUser);
                });

            MockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "UnconfirmedUser"))
                .ReturnsAsync(IdentityResult.Success);

            // Setup for email verification link generation
            MockApiAuthService.Setup(a => a.GenerateEmailVerificationLink(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("https://example.com/verify?token=abc123");

            // Act
            var result = await _controller.Register(model);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<RegisterViewModel>());

            var returnedModel = okResult.Value as RegisterViewModel;
            Assert.That(returnedModel, Is.Not.Null);
            Assert.That(returnedModel.Response, Is.EqualTo("User created successfully"));

            // Verify email was sent
            _mockEmailService.Verify(e => e.SendEmailVerificationMessage(
                model.Email,
                It.IsAny<string>()),
                Times.Once);

            _mockEmailService.Verify(e => e.SendEmailAsync("admin@example.com", It.IsAny<string>(), It.Is<string>(b => b.Contains(model.Email))), Times.Once);
        }
    }

    // Missing model classes for tests
    public class UserManagerResponse
    {
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }

    public class ConfirmEmailModel
    {
        public string Token { get; set; }
        public string Email { get; set; }
    }

    public class ResetPasswordModel
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}