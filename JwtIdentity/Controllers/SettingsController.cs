using JwtIdentity.Common.ViewModels;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        // GET api/settings
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<SettingViewModel>>> GetSettings(string category = null)
        {
            var settings = await _settingsService.GetAllSettingsAsync(category);
            return Ok(settings.Select(s => new SettingViewModel
            {
                Id = s.Id,
                Key = s.Key,
                Value = s.Value,
                DataType = s.DataType,
                Description = s.Description,
                Category = s.Category,
                IsEditable = s.IsEditable,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate
            }).ToList());
        }

        // GET api/settings/categories
        [HttpGet("categories")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            var categories = await _settingsService.GetCategoriesAsync();
            return Ok(categories);
        }

        // GET api/settings/{key}
        [HttpGet("{key}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SettingViewModel>> GetSetting(string key)
        {
            var setting = await _settingsService.GetSettingEntityAsync(key);
            if (setting == null)
            {
                return NotFound();
            }

            return Ok(new SettingViewModel
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                DataType = setting.DataType,
                Description = setting.Description,
                Category = setting.Category,
                IsEditable = setting.IsEditable,
                CreatedDate = setting.CreatedDate,
                UpdatedDate = setting.UpdatedDate
            });
        }

        // PUT api/settings/{key}
        [HttpPut("{key}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] SettingViewModel model)
        {
            if (key != model.Key)
            {
                return BadRequest("Setting key mismatch");
            }

            var existingSetting = await _settingsService.GetSettingEntityAsync(key);
            if (existingSetting == null)
            {
                return NotFound();
            }

            if (!existingSetting.IsEditable)
            {
                return BadRequest("This setting cannot be edited");
            }

            // Based on DataType, convert and save
            try
            {
                bool success = false;
                
                switch (existingSetting.DataType)
                {
                    case "Int":
                        success = await _settingsService.SetSettingAsync<int>(
                            key, int.Parse(model.Value), model.Description, model.Category);
                        break;
                        
                    case "Long":
                        success = await _settingsService.SetSettingAsync<long>(
                            key, long.Parse(model.Value), model.Description, model.Category);
                        break;
                        
                    case "Decimal":
                        success = await _settingsService.SetSettingAsync<decimal>(
                            key, decimal.Parse(model.Value), model.Description, model.Category);
                        break;
                        
                    case "Double":
                        success = await _settingsService.SetSettingAsync<double>(
                            key, double.Parse(model.Value), model.Description, model.Category);
                        break;
                        
                    case "Boolean":
                        success = await _settingsService.SetSettingAsync<bool>(
                            key, bool.Parse(model.Value), model.Description, model.Category);
                        break;
                        
                    case "DateTime":
                        success = await _settingsService.SetSettingAsync<DateTime>(
                            key, DateTime.Parse(model.Value), model.Description, model.Category);
                        break;
                        
                    case "String":
                    default:
                        success = await _settingsService.SetSettingAsync<string>(
                            key, model.Value, model.Description, model.Category);
                        break;
                }

                if (success)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(500, "Failed to update setting");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {Key}", key);
                return BadRequest($"Error updating setting: {ex.Message}");
            }
        }

        // DELETE api/settings/{key}
        [HttpDelete("{key}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSetting(string key)
        {
            var setting = await _settingsService.GetSettingEntityAsync(key);
            if (setting == null)
            {
                return NotFound();
            }

            if (!setting.IsEditable)
            {
                return BadRequest("This setting cannot be deleted");
            }

            var success = await _settingsService.DeleteSettingAsync(key);
            if (success)
            {
                return NoContent();
            }
            else
            {
                return StatusCode(500, "Failed to delete setting");
            }
        }

        // POST api/settings
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SettingViewModel>> CreateSetting([FromBody] SettingViewModel model)
        {
            // Check if the setting already exists
            var existingSetting = await _settingsService.GetSettingEntityAsync(model.Key);
            if (existingSetting != null)
            {
                return Conflict("A setting with this key already exists");
            }

            try
            {
                // Create a new setting based on the data type
                bool success = false;
                
                switch (model.DataType)
                {
                    case "Int":
                        success = await _settingsService.SetSettingAsync<int>(
                            model.Key, int.Parse(model.Value), model.Description, model.Category, model.IsEditable);
                        break;
                        
                    case "Long":
                        success = await _settingsService.SetSettingAsync<long>(
                            model.Key, long.Parse(model.Value), model.Description, model.Category, model.IsEditable);
                        break;
                        
                    case "Decimal":
                        success = await _settingsService.SetSettingAsync<decimal>(
                            model.Key, decimal.Parse(model.Value), model.Description, model.Category, model.IsEditable);
                        break;
                        
                    case "Double":
                        success = await _settingsService.SetSettingAsync<double>(
                            model.Key, double.Parse(model.Value), model.Description, model.Category, model.IsEditable);
                        break;
                        
                    case "Boolean":
                        success = await _settingsService.SetSettingAsync<bool>(
                            model.Key, bool.Parse(model.Value), model.Description, model.Category, model.IsEditable);
                        break;
                        
                    case "DateTime":
                        success = await _settingsService.SetSettingAsync<DateTime>(
                            model.Key, DateTime.Parse(model.Value), model.Description, model.Category, model.IsEditable);
                        break;
                        
                    case "String":
                    default:
                        success = await _settingsService.SetSettingAsync<string>(
                            model.Key, model.Value, model.Description, model.Category, model.IsEditable);
                        break;
                }

                if (success)
                {
                    var createdSetting = await _settingsService.GetSettingEntityAsync(model.Key);
                    return CreatedAtAction(
                        nameof(GetSetting), 
                        new { key = model.Key },
                        new SettingViewModel
                        {
                            Id = createdSetting.Id,
                            Key = createdSetting.Key,
                            Value = createdSetting.Value,
                            DataType = createdSetting.DataType,
                            Description = createdSetting.Description,
                            Category = createdSetting.Category,
                            IsEditable = createdSetting.IsEditable,
                            CreatedDate = createdSetting.CreatedDate,
                            UpdatedDate = createdSetting.UpdatedDate
                        });
                }
                else
                {
                    return StatusCode(500, "Failed to create setting");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating setting {Key}", model.Key);
                return BadRequest($"Error creating setting: {ex.Message}");
            }
        }

        // GET api/settings/migrate
        [HttpGet("migrate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MigrateSettings([FromServices] SettingsMigrationService migrationService)
        {
            await migrationService.MigrateAllSettingsAsync();
            return Ok("Settings migration triggered. Please check logs for details.");
        }

        // GET api/settings/seed
        [HttpGet("seed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SeedTestSetting()
        {
            // Create a test setting if none exist
            var settings = await _settingsService.GetAllSettingsAsync();
            if (!settings.Any())
            {
                await _settingsService.SetSettingAsync(
                    "Test.Setting", 
                    "This is a test setting value", 
                    "A test setting to verify the settings system is working", 
                    "Test");
                
                _logger.LogInformation("Created test setting");
                return Ok("Test setting created");
            }
            
            return Ok($"Settings already exist: {settings.Count} settings found");
        }
    }
}