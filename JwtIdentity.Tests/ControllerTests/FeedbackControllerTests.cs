using JwtIdentity.Controllers;
using JwtIdentity.Models;
using JwtIdentity.Common.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JwtIdentity.Services;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class FeedbackControllerTests : TestBase
    {
        private FeedbackController _controller = null!;
        private List<Feedback> _mockFeedbacks = null!;
        private Mock<DbSet<Feedback>> _mockFeedbacksDbSet = null!;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            // Seed the real in-memory DbSet instead of using a mock
            MockDbContext.Feedbacks.AddRange(new List<Feedback>
            {
                new Feedback { Id = 1, Title = "Test1", Description = "Desc1", CreatedById = 1 },
                new Feedback { Id = 2, Title = "Test2", Description = "Desc2", CreatedById = 2 }
            });
            MockDbContext.SaveChanges();

            // Setup mapper for Feedback <-> FeedbackViewModel
            MockMapper.Setup(m => m.Map<FeedbackViewModel>(It.IsAny<Feedback>()))
                .Returns((Feedback f) => new FeedbackViewModel { Id = f.Id, Title = f.Title, Description = f.Description, CreatedById = f.CreatedById });
            MockMapper.Setup(m => m.Map<IEnumerable<FeedbackViewModel>>(It.IsAny<List<Feedback>>()))
                .Returns((List<Feedback> list) => list.Select(f => new FeedbackViewModel { Id = f.Id, Title = f.Title, Description = f.Description, CreatedById = f.CreatedById }));
            MockMapper.Setup(m => m.Map<Feedback>(It.IsAny<FeedbackViewModel>()))
                .Returns((FeedbackViewModel vm) => new Feedback { Id = vm.Id, Title = vm.Title, Description = vm.Description, CreatedById = vm.CreatedById });
            MockMapper.Setup(m => m.Map(It.IsAny<FeedbackViewModel>(), It.IsAny<Feedback>()))
                .Callback((FeedbackViewModel vm, Feedback f) => { f.Title = vm.Title; f.Description = vm.Description; f.IsResolved = vm.IsResolved; f.AdminResponse = vm.AdminResponse; });

            // Setup UserManager and ApiAuthService
            var userManager = MockUserManager.Object;
            var emailService = MockEmailService.Object;
            var apiAuthService = MockApiAuthService.Object;
            var config = MockConfiguration.Object;
            var settingsService = new Mock<ISettingsService>().Object;

            _controller = new FeedbackController(MockDbContext, MockMapper.Object, apiAuthService, userManager, emailService, config, settingsService);
            _controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
        }

        [Test]
        public async Task GetFeedbacks_AdminRole_ReturnsAllFeedbacks()
        {
            // Arrange
            HttpContext.User = CreateClaimsPrincipal(1, "admin", new[] { "Admin" });

            // Act
            var result = await _controller.GetFeedbacks();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            Assert.That(ok!.Value, Is.InstanceOf<IEnumerable<FeedbackViewModel>>());
            Assert.That(((IEnumerable<FeedbackViewModel>)ok.Value!).Count(), Is.EqualTo(MockDbContext.Feedbacks.Count()));
        }

        [Test]
        public async Task GetMyFeedbacks_User_ReturnsOwnFeedbacks()
        {
            // Arrange
            HttpContext.User = CreateClaimsPrincipal(2, "user");
            MockApiAuthService.Setup(s => s.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(2);

            // Act
            var result = await _controller.GetMyFeedbacks();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            var feedbacks = (IEnumerable<FeedbackViewModel>)ok!.Value!;
            Assert.That(feedbacks.All(f => f.CreatedById == 2));
        }

        [Test]
        public async Task PostFeedback_AuthenticatedUser_CreatesFeedback()
        {
            // Arrange
            HttpContext.User = CreateClaimsPrincipal(3, "user");
            MockApiAuthService.Setup(s => s.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(3);
            var vm = new FeedbackViewModel { Title = "New", Description = "Desc" };

            // Act
            var result = await _controller.PostFeedback(vm);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            var created = result.Result as CreatedAtActionResult;
            Assert.That(created!.Value, Is.InstanceOf<FeedbackViewModel>());
        }

        [Test]
        public async Task PutFeedback_UserCanUpdateOwnFeedback()
        {
            // Arrange
            HttpContext.User = CreateClaimsPrincipal(1, "user");
            MockApiAuthService.Setup(s => s.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(1);
            var vm = new FeedbackViewModel { Id = 1, Title = "Updated", Description = "UpdatedDesc", CreatedById = 1 };

            // Act
            var result = await _controller.PutFeedback(1, vm);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task DeleteFeedback_AdminCanDeleteFeedback()
        {
            // Arrange
            HttpContext.User = CreateClaimsPrincipal(1, "admin", new[] { "Admin" });

            // Act
            var result = await _controller.DeleteFeedback(1);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
    }
}
