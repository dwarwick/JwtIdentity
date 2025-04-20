using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtIdentity.Controllers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class SettingsControllerTests : TestBase
    {
        private SettingsController _controller = null!;
        private Mock<ISettingsService> _mockSettingsService = null!;
        private Mock<ILogger<SettingsController>> _mockLogger = null!;
        private List<Setting> _settings = null!;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _mockSettingsService = new Mock<ISettingsService>();
            _mockLogger = new Mock<ILogger<SettingsController>>();
            _settings = new List<Setting>
            {
                new Setting
                {
                    Id = 1,
                    Key = "Site.Title",
                    Value = "My Site",
                    DataType = "String",
                    Description = "Site title",
                    Category = "General",
                    IsEditable = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new Setting
                {
                    Id = 2,
                    Key = "Max.Users",
                    Value = "100",
                    DataType = "Int",
                    Description = "Maximum users",
                    Category = "Limits",
                    IsEditable = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddDays(-1)
                }
            };
            _controller = new SettingsController(_mockSettingsService.Object, _mockLogger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = HttpContext }
            };
        }

        [Test]
        public async Task GetSettings_ReturnsAllSettings()
        {
            _mockSettingsService.Setup(s => s.GetAllSettingsAsync(null)).ReturnsAsync(_settings);
            var result = await _controller.GetSettings();
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            Assert.That(ok!.Value, Is.InstanceOf<List<SettingViewModel>>());
            var list = ok.Value as List<SettingViewModel>;
            Assert.That(list, Has.Count.EqualTo(_settings.Count));
        }

        [Test]
        public async Task GetSettings_WithCategory_ReturnsFilteredSettings()
        {
            _mockSettingsService.Setup(s => s.GetAllSettingsAsync("General")).ReturnsAsync(_settings.Where(s => s.Category == "General").ToList());
            var result = await _controller.GetSettings("General");
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            var list = ok!.Value as List<SettingViewModel>;
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list![0].Category, Is.EqualTo("General"));
        }

        [Test]
        public async Task GetCategories_ReturnsAllCategories()
        {
            _mockSettingsService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<string> { "General", "Limits" });
            var result = await _controller.GetCategories();
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            var categories = ok!.Value as List<string>;
            Assert.That(categories, Is.EquivalentTo(new[] { "General", "Limits" }));
        }

        [Test]
        public async Task GetSetting_ExistingKey_ReturnsSetting()
        {
            var setting = _settings[0];
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(setting.Key)).ReturnsAsync(setting);
            var result = await _controller.GetSetting(setting.Key);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = result.Result as OkObjectResult;
            var vm = ok!.Value as SettingViewModel;
            Assert.That(vm, Is.Not.Null);
            Assert.That(vm!.Key, Is.EqualTo(setting.Key));
        }

        [Test]
        public async Task GetSetting_NonExistingKey_ReturnsNotFound()
        {
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync("NotFound")).ReturnsAsync((Setting)null!);
            var result = await _controller.GetSetting("NotFound");
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task UpdateSetting_ValidEditable_Succeeds()
        {
            var setting = _settings[1];
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(setting.Key)).ReturnsAsync(setting);
            _mockSettingsService.Setup(s => s.SetSettingAsync<int>(setting.Key, 200, setting.Description, setting.Category, true)).ReturnsAsync(true);
            var model = new SettingViewModel
            {
                Key = setting.Key,
                Value = "200",
                DataType = "Int",
                Description = setting.Description,
                Category = setting.Category,
                IsEditable = true
            };
            var result = await _controller.UpdateSetting(setting.Key, model);
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task UpdateSetting_KeyMismatch_ReturnsBadRequest()
        {
            var model = new SettingViewModel { Key = "A", Value = "1", DataType = "Int" };
            var result = await _controller.UpdateSetting("B", model);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdateSetting_NonExisting_ReturnsNotFound()
        {
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync("Missing")).ReturnsAsync((Setting)null!);
            var model = new SettingViewModel { Key = "Missing", Value = "1", DataType = "Int" };
            var result = await _controller.UpdateSetting("Missing", model);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task UpdateSetting_NotEditable_ReturnsBadRequest()
        {
            var setting = new Setting { Key = "Locked", IsEditable = false, DataType = "String" };
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(setting.Key)).ReturnsAsync(setting);
            var model = new SettingViewModel { Key = setting.Key, Value = "abc", DataType = "String" };
            var result = await _controller.UpdateSetting(setting.Key, model);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdateSetting_ServiceFails_Returns500()
        {
            var setting = _settings[1];
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(setting.Key)).ReturnsAsync(setting);
            _mockSettingsService.Setup(s => s.SetSettingAsync<int>(setting.Key, 123, setting.Description, setting.Category, true)).ReturnsAsync(false);
            var model = new SettingViewModel { Key = setting.Key, Value = "123", DataType = "Int", IsEditable = true };
            var result = await _controller.UpdateSetting(setting.Key, model);
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var obj = result as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task DeleteSetting_Editable_Succeeds()
        {
            var setting = _settings[0];
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(setting.Key)).ReturnsAsync(setting);
            _mockSettingsService.Setup(s => s.DeleteSettingAsync(setting.Key)).ReturnsAsync(true);
            var result = await _controller.DeleteSetting(setting.Key);
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task DeleteSetting_NotFound_ReturnsNotFound()
        {
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync("Missing")).ReturnsAsync((Setting)null!);
            var result = await _controller.DeleteSetting("Missing");
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteSetting_NotEditable_ReturnsBadRequest()
        {
            var setting = new Setting { Key = "Locked", IsEditable = false };
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(setting.Key)).ReturnsAsync(setting);
            var result = await _controller.DeleteSetting(setting.Key);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task DeleteSetting_ServiceFails_Returns500()
        {
            var setting = _settings[0];
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(setting.Key)).ReturnsAsync(setting);
            _mockSettingsService.Setup(s => s.DeleteSettingAsync(setting.Key)).ReturnsAsync(false);
            var result = await _controller.DeleteSetting(setting.Key);
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var obj = result as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task CreateSetting_NewSetting_Succeeds()
        {
            var model = new SettingViewModel
            {
                Key = "New.Setting",
                Value = "true",
                DataType = "Boolean",
                Description = "desc",
                Category = "General",
                IsEditable = true
            };
            _mockSettingsService.SetupSequence(s => s.GetSettingEntityAsync(model.Key))
                .ReturnsAsync((Setting)null!) // First call: does not exist
                .ReturnsAsync(new Setting
                {
                    Id = 3,
                    Key = model.Key,
                    Value = model.Value,
                    DataType = model.DataType,
                    Description = model.Description,
                    Category = model.Category,
                    IsEditable = model.IsEditable,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                }); // Second call: after creation
            _mockSettingsService.Setup(s => s.SetSettingAsync<bool>(model.Key, true, model.Description, model.Category, model.IsEditable)).ReturnsAsync(true);
            var result = await _controller.CreateSetting(model);
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            var created = result.Result as CreatedAtActionResult;
            var vm = created!.Value as SettingViewModel;
            Assert.That(vm, Is.Not.Null);
            Assert.That(vm!.Key, Is.EqualTo(model.Key));
        }

        [Test]
        public async Task CreateSetting_ExistingKey_ReturnsConflict()
        {
            var model = new SettingViewModel { Key = _settings[0].Key, Value = "abc", DataType = "String" };
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(model.Key)).ReturnsAsync(_settings[0]);
            var result = await _controller.CreateSetting(model);
            Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
        }

        [Test]
        public async Task CreateSetting_ServiceFails_Returns500()
        {
            var model = new SettingViewModel { Key = "Fail.Setting", Value = "1", DataType = "Int", IsEditable = true };
            _mockSettingsService.Setup(s => s.GetSettingEntityAsync(model.Key)).ReturnsAsync((Setting)null!);
            _mockSettingsService.Setup(s => s.SetSettingAsync<int>(model.Key, 1, null, null, true)).ReturnsAsync(false);
            var result = await _controller.CreateSetting(model);
            Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
            var obj = result.Result as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task SeedTestSetting_WhenNoSettings_CreatesTestSetting()
        {
            _mockSettingsService.Setup(s => s.GetAllSettingsAsync(null)).ReturnsAsync(new List<Setting>());
            _mockSettingsService.Setup(s => s.SetSettingAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true)).ReturnsAsync(true);
            var result = await _controller.SeedTestSetting();
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = result as OkObjectResult;
            Assert.That(ok!.Value, Is.EqualTo("Test setting created"));
        }

        [Test]
        public async Task SeedTestSetting_WhenSettingsExist_ReturnsCount()
        {
            _mockSettingsService.Setup(s => s.GetAllSettingsAsync(null)).ReturnsAsync(_settings);
            var result = await _controller.SeedTestSetting();
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = result as OkObjectResult;
            Assert.That(ok!.Value!.ToString(), Does.Contain("settings found"));
        }
    }
}
