using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class SettingsServiceTests : TestBase<SettingsService>
    {
        private SettingsService _service;
        private Mock<ILogger<SettingsService>> _mockLogger;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _mockLogger = new Mock<ILogger<SettingsService>>();
            _service = new SettingsService(MockDbContext, _mockLogger.Object);
        }

        [Test]
        public async Task GetSettingAsync_ReturnsDefault_WhenNotFound()
        {
            var result = await _service.GetSettingAsync("nonexistent", 42);
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public async Task GetSettingAsync_ReturnsValue_WhenFound()
        {
            MockDbContext.Settings.Add(new Setting { Key = "TestInt", Value = "123", DataType = "Int" });
            await MockDbContext.SaveChangesAsync();
            var result = await _service.GetSettingAsync("TestInt", 0);
            Assert.That(result, Is.EqualTo(123));
        }

        [Test]
        public async Task GetSettingEntityAsync_ReturnsEntity_WhenFound()
        {
            var setting = new Setting { Key = "EntityKey", Value = "abc", DataType = "String" };
            MockDbContext.Settings.Add(setting);
            await MockDbContext.SaveChangesAsync();
            var result = await _service.GetSettingEntityAsync("EntityKey");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Key, Is.EqualTo("EntityKey"));
        }

        [Test]
        public async Task GetSettingEntityAsync_ReturnsNull_WhenNotFound()
        {
            var result = await _service.GetSettingEntityAsync("missing");
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SetSettingAsync_CreatesNewSetting()
        {
            var success = await _service.SetSettingAsync("NewKey", 99, "desc", "cat", true);
            Assert.That(success, Is.True);
            var setting = await MockDbContext.Settings.FirstOrDefaultAsync(s => s.Key == "NewKey");
            Assert.That(setting, Is.Not.Null);
            Assert.That(setting.Value, Is.EqualTo("99"));
            Assert.That(setting.Description, Is.EqualTo("desc"));
            Assert.That(setting.Category, Is.EqualTo("cat"));
            Assert.That(setting.IsEditable, Is.True);
        }

        [Test]
        public async Task SetSettingAsync_UpdatesExistingSetting()
        {
            var setting = new Setting { Key = "UpdateKey", Value = "old", DataType = "String", Description = "old", Category = "old", IsEditable = false };
            MockDbContext.Settings.Add(setting);
            await MockDbContext.SaveChangesAsync();
            var success = await _service.SetSettingAsync("UpdateKey", "new", "newdesc", "newcat", true);
            Assert.That(success, Is.True);
            var updated = await MockDbContext.Settings.FirstOrDefaultAsync(s => s.Key == "UpdateKey");
            Assert.That(updated.Value, Is.EqualTo("new"));
            Assert.That(updated.Description, Is.EqualTo("newdesc"));
            Assert.That(updated.Category, Is.EqualTo("newcat"));
            Assert.That(updated.IsEditable, Is.True);
        }

        [Test]
        public async Task GetAllSettingsAsync_ReturnsAll()
        {
            MockDbContext.Settings.AddRange(
                new Setting { Key = "A", Value = "1", DataType = "Int", Category = "Cat1" },
                new Setting { Key = "B", Value = "2", DataType = "Int", Category = "Cat2" }
            );
            await MockDbContext.SaveChangesAsync();
            var all = await _service.GetAllSettingsAsync();
            Assert.That(all.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetAllSettingsAsync_FiltersByCategory()
        {
            MockDbContext.Settings.AddRange(
                new Setting { Key = "A", Value = "1", DataType = "Int", Category = "Cat1" },
                new Setting { Key = "B", Value = "2", DataType = "Int", Category = "Cat2" }
            );
            await MockDbContext.SaveChangesAsync();
            var filtered = await _service.GetAllSettingsAsync("Cat1");
            Assert.That(filtered.Count, Is.EqualTo(1));
            Assert.That(filtered[0].Category, Is.EqualTo("Cat1"));
        }

        [Test]
        public async Task DeleteSettingAsync_RemovesSetting()
        {
            var setting = new Setting { Key = "DelKey", Value = "x", DataType = "String" };
            MockDbContext.Settings.Add(setting);
            await MockDbContext.SaveChangesAsync();
            var result = await _service.DeleteSettingAsync("DelKey");
            Assert.That(result, Is.True);
            var exists = await MockDbContext.Settings.AnyAsync(s => s.Key == "DelKey");
            Assert.That(exists, Is.False);
        }

        [Test]
        public async Task DeleteSettingAsync_ReturnsFalse_WhenNotFound()
        {
            var result = await _service.DeleteSettingAsync("nope");
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetCategoriesAsync_ReturnsDistinctCategories()
        {
            MockDbContext.Settings.AddRange(
                new Setting { Key = "A", Value = "1", DataType = "Int", Category = "Cat1" },
                new Setting { Key = "B", Value = "2", DataType = "Int", Category = "Cat2" },
                new Setting { Key = "C", Value = "3", DataType = "Int", Category = "Cat1" }
            );
            await MockDbContext.SaveChangesAsync();
            var cats = await _service.GetCategoriesAsync();
            Assert.That(cats, Is.EquivalentTo(new[] { "Cat1", "Cat2" }));
        }
    }
}
