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
    public class ApplicationUsersControllerTests : TestBase<ApplicationUserController>
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
            _controller = new ApplicationUserController(MockDbContext, MockMapper.Object, MockApiAuthService.Object, MockLogger.Object);
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
        public async Task GetApplicationUser_WithValidId_ReturnsUser()
        {
            // Arrange
            int userId = 1;
            var mockRoles = new List<string> { "Admin" };
            var mockPermissions = new List<string> { "ManageUsers" };
            
            // Setup mock API auth service to return roles and permissions
            MockApiAuthService.Setup(s => s.GetUserRoles(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(mockRoles);
                
            MockApiAuthService.Setup(s => s.GetUserPermissions(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns(mockPermissions);

            // Act
            var result = await _controller.GetApplicationUser(userId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null, "Result should be an OkObjectResult");
            
            var userViewModel = okResult.Value as ApplicationUserViewModel;
            Assert.That(userViewModel, Is.Not.Null, "Result value should not be null");
            Assert.That(userViewModel.Id, Is.EqualTo(userId), "User ID should match the requested ID");
            Assert.That(userViewModel.UserName, Is.EqualTo("admin"), "Username should match expected value");
            Assert.That(userViewModel.Email, Is.EqualTo("admin@example.com"), "Email should match expected value");
            Assert.That(userViewModel.Roles, Is.EqualTo(mockRoles), "Roles should match expected values");
            Assert.That(userViewModel.Permissions, Is.EqualTo(mockPermissions), "Permissions should match expected values");
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

            // Capture the update date for comparison
            DateTime beforeUpdate = DateTime.UtcNow;
                
            // Act
            var result = await _controller.PutApplicationUser(userId, model);

            // Assert
            Assert.That(result, Is.TypeOf<NoContentResult>(), "Should return NoContent for successful update");

            // Verify user was updated in the database
            var userInDb = await MockDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
            Assert.That(userInDb, Is.Not.Null, "User should exist in the database");
            Assert.That(userInDb.UserName, Is.EqualTo("updateduser"), "Username should be updated");
            Assert.That(userInDb.Email, Is.EqualTo("updateduser@example.com"), "Email should be updated");
            Assert.That(userInDb.Theme, Is.EqualTo("dark"), "Theme should be updated");
            Assert.That(userInDb.UpdatedDate, Is.GreaterThanOrEqualTo(beforeUpdate), "UpdatedDate should be updated");
        }

        [Test]
        public async Task PutApplicationUser_WithNullModel_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.PutApplicationUser(1, null);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Should return BadRequest when model is null");
        }

        [Test]
        public async Task PutApplicationUser_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            var model = new ApplicationUserViewModel { Id = 2 }; // ID doesn't match route parameter

            // Act
            var result = await _controller.PutApplicationUser(1, model);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Should return BadRequest when ID in route doesn't match ID in model");
        }

        [Test]
        public async Task PutApplicationUser_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            int userId = 999; // Non-existent ID
            var model = new ApplicationUserViewModel 
            { 
                Id = userId,
                UserName = "nonexistent",
                Email = "nonexistent@example.com",
                Theme = "light"
            };
                
            // Act
            var result = await _controller.PutApplicationUser(userId, model);
            
            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>(), "Should return NotFound when updating a non-existent user");
        }
    }
}