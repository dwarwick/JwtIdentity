using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using JwtIdentity.Controllers;
using JwtIdentity.Data;
using JwtIdentity.Models;
using JwtIdentity.Common.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using NUnit.Framework;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class ApplicationUsersControllerTests : TestBase
    {
        private ApplicationUserController _controller;
        private List<ApplicationUser> _users;

        [SetUp]
        public override void BaseSetUp()
        {
            // Call base setup to initialize base mocks and in-memory database
            base.BaseSetUp();

            // Create test data
            _users = new List<ApplicationUser>
            {
                new ApplicationUser 
                { 
                    Id = 1, 
                    UserName = "admin", 
                    Email = "admin@example.com", 
                    Theme = "dark", 
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                },
                new ApplicationUser 
                { 
                    Id = 2, 
                    UserName = "user", 
                    Email = "user@example.com", 
                    Theme = "light",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                },
                new ApplicationUser 
                { 
                    Id = 3, 
                    UserName = "anonymous", 
                    Email = "anonymous@example.com", 
                    Theme = "light",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                }
            };

            // Add test users to the in-memory database
            MockDbContext.ApplicationUsers.AddRange(_users);
            MockDbContext.SaveChanges();
            
            // Set up mapper mock
            MockMapper.Setup(m => m.Map<ApplicationUserViewModel>(It.IsAny<ApplicationUser>()))
                .Returns<ApplicationUser>(user => new ApplicationUserViewModel 
                { 
                    Id = user.Id, 
                    UserName = user.UserName, 
                    Email = user.Email, 
                    Theme = user.Theme 
                });
            
            MockMapper.Setup(m => m.Map<ApplicationUser>(It.IsAny<ApplicationUserViewModel>()))
                .Returns<ApplicationUserViewModel>(viewModel => new ApplicationUser 
                { 
                    Id = viewModel.Id, 
                    UserName = viewModel.UserName, 
                    Email = viewModel.Email, 
                    Theme = viewModel.Theme,
                    // Add required fields to avoid null reference exceptions
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });

            MockMapper.Setup(m => m.Map(It.IsAny<ApplicationUserViewModel>(), It.IsAny<ApplicationUser>()))
                .Callback((ApplicationUserViewModel src, ApplicationUser dest) => {
                    dest.UserName = src.UserName;
                    dest.Email = src.Email;
                    dest.Theme = src.Theme;
                });

            // Set up controller with real in-memory DbContext and mock mapper
            _controller = new ApplicationUserController(MockDbContext, MockMapper.Object, MockApiAuthService.Object);
        }

        [TearDown]
        public override void BaseTearDown()
        {
            // Clean up the database after each test
            if (MockDbContext != null)
            {
                var entities = MockDbContext.ApplicationUsers.ToList();
                if (entities.Any())
                {
                    MockDbContext.ApplicationUsers.RemoveRange(entities);
                    MockDbContext.SaveChanges();
                }
            }
            
            base.BaseTearDown();
        }

        [Test]
        public async Task GetApplicationUsers_ReturnsAllUsers()
        {
            // Act
            var result = await _controller.GetApplicationUsers();

            // Assert
            Assert.That(result.Value, Is.Not.Null, "Result value should not be null");
            Assert.That(result.Value.Count(), Is.EqualTo(_users.Count), "Should return all users");
            Assert.That(_users.All(u => result.Value.Any(r => r.Id == u.Id)), Is.True, "All expected users should be present in the result");
        }

        [Test]
        public async Task GetApplicationUser_WithValidId_ReturnsUser()
        {
            // Arrange
            int userId = 1;

            // Act
            var result = await _controller.GetApplicationUser(userId);

            // Assert
            Assert.That(result.Value, Is.Not.Null, "Result value should not be null");
            Assert.That(result.Value.Id, Is.EqualTo(userId), "User ID should match the requested ID");
            Assert.That(result.Value.UserName, Is.EqualTo("admin"), "Username should match expected value");
            Assert.That(result.Value.Email, Is.EqualTo("admin@example.com"), "Email should match expected value");
        }

        [Test]
        public async Task GetApplicationUser_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int userId = 999; // Non-existent ID

            // Act
            var result = await _controller.GetApplicationUser(userId);

            // Assert
            Assert.That(result.Result, Is.TypeOf<NotFoundResult>(), "Should return NotFound for invalid user ID");
        }

        [Test]
        public async Task PutApplicationUser_WithValidModel_UpdatesUser()
        {
            // Arrange
            int userId = 2;
            var model = new ApplicationUserViewModel
            {
                Id = userId,
                UserName = "updateduser",
                Email = "updateduser@example.com",
                Theme = "dark"
            };

            // Act
            var result = await _controller.PutApplicationUser(userId, model);

            // Assert
            Assert.That(result, Is.TypeOf<NoContentResult>(), "Should return NoContent for successful update");

            // Verify user was updated in the database
            var userInDb = MockDbContext.ApplicationUsers.FirstOrDefault(u => u.Id == userId);
            Assert.That(userInDb, Is.Not.Null, "User should exist in the database");
            Assert.That(userInDb.UserName, Is.EqualTo("updateduser"), "Username should be updated");
            Assert.That(userInDb.Email, Is.EqualTo("updateduser@example.com"), "Email should be updated");
            Assert.That(userInDb.Theme, Is.EqualTo("dark"), "Theme should be updated");
        }

        [Test]
        public async Task PutApplicationUser_WithNullModel_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.PutApplicationUser(1, null);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestResult>(), "Should return BadRequest when model is null");
        }

        [Test]
        public async Task PutApplicationUser_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            var model = new ApplicationUserViewModel { Id = 2 }; // ID doesn't match route parameter

            // Act
            var result = await _controller.PutApplicationUser(1, model);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestResult>(), "Should return BadRequest when ID in route doesn't match ID in model");
        }

        [Test]
        public async Task PutApplicationUser_WithNonExistentId_ReturnsNotFound()
        {
            // For this test, we'll use the real controller but create a scenario where the save fails
            
            // Arrange
            int userId = 999; // Non-existent ID
            var model = new ApplicationUserViewModel 
            { 
                Id = userId,
                UserName = "nonexistent",
                Email = "nonexistent@example.com",
                Theme = "light"
            };
            
            // Create a new DbContext for this test
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDatabase_{Guid.NewGuid()}")
                .Options;
                
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(HttpContext);
            
            using var dbContext = new ApplicationDbContext(options, httpContextAccessor.Object);
            
            // Create the controller with the test DbContext
            var controller = new ApplicationUserController(dbContext, MockMapper.Object, MockApiAuthService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = HttpContext
                }
            };
            
            // Act
            var result = await controller.PutApplicationUser(userId, model);
            
            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>(), "Should return NotFound when updating a non-existent user");
        }

        [Test]
        public async Task PostApplicationUser_WithValidModel_CreatesUser()
        {
            // Arrange
            var newUser = new ApplicationUser
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Theme = "light"
            };

            // Act
            var result = await _controller.PostApplicationUser(newUser);

            // Assert
            Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>(), "Should return CreatedAtAction result for successful creation");
            
            var createdAtActionResult = result.Result as CreatedAtActionResult;
            Assert.That(createdAtActionResult.ActionName, Is.EqualTo("GetApplicationUser"), "Should use GetApplicationUser action in the response");
            
            // Verify user was added to the database
            var userInDb = MockDbContext.ApplicationUsers.FirstOrDefault(u => u.UserName == "newuser");
            Assert.That(userInDb, Is.Not.Null, "User should be added to the database");
            Assert.That(userInDb.Email, Is.EqualTo("newuser@example.com"), "Email should match");
            Assert.That(userInDb.Theme, Is.EqualTo("light"), "Theme should match");
        }

        [Test]
        public async Task DeleteApplicationUser_WithValidId_DeletesUser()
        {
            // Arrange
            int userId = 3;

            // Act
            var result = await _controller.DeleteApplicationUser(userId);

            // Assert
            Assert.That(result, Is.TypeOf<NoContentResult>(), "Should return NoContent for successful deletion");
            
            // Verify user was removed from the database
            var userInDb = await MockDbContext.ApplicationUsers.FindAsync(userId);
            Assert.That(userInDb, Is.Null, "User should be removed from the database");
        }

        [Test]
        public async Task DeleteApplicationUser_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int userId = 999; // Non-existent ID

            // Act
            var result = await _controller.DeleteApplicationUser(userId);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>(), "Should return NotFound when deleting a non-existent user");
        }
    }
}