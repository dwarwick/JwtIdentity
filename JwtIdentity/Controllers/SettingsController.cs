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
        [Authorize(Policy = Permissions.ManageSettings)]
        public async Task<ActionResult<List<SettingViewModel>>> GetSettings(string category = null)
        {
            try
            {
                _logger.LogInformation("Retrieving settings with category filter: {Category}", category ?? "All");
                var settings = await _settingsService.GetAllSettingsAsync(category);
                _logger.LogInformation("Retrieved {Count} settings", settings.Count);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings with category: {Category}", category ?? "All");
                return StatusCode(500, "An error occurred while retrieving settings");
            }
        }

        // GET api/settings/categories
        [HttpGet("categories")]
        [Authorize(Policy = Permissions.ManageSettings)]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            try
            {
                _logger.LogInformation("Retrieving all setting categories");
                var categories = await _settingsService.GetCategoriesAsync();
                _logger.LogInformation("Retrieved {Count} categories", categories.Count);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting categories");
                return StatusCode(500, "An error occurred while retrieving setting categories");
            }
        }

        // GET api/settings/{key}
        [HttpGet("{key}")]
        [Authorize(Policy = Permissions.ManageSettings)]
        public async Task<ActionResult<SettingViewModel>> GetSetting(string key)
        {
            try
            {
                _logger.LogInformation("Retrieving setting with key: {Key}", key);
                var setting = await _settingsService.GetSettingEntityAsync(key);
                if (setting == null)
                {
                    _logger.LogWarning("Setting with key {Key} not found", key);
                    return NotFound();
                }

                _logger.LogDebug("Retrieved setting with key: {Key}, category: {Category}", key, setting.Category);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting with key: {Key}", key);
                return StatusCode(500, "An error occurred while retrieving the setting");
            }
        }

        // PUT api/settings/{key}
        [HttpPut("{key}")]
        [Authorize(Policy = Permissions.ManageSettings)]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] SettingViewModel model)
        {
            try
            {
                _logger.LogInformation("Attempting to update setting with key: {Key}", key);
                
                if (key != model.Key)
                {
                    _logger.LogWarning("Setting key mismatch: URL key {UrlKey} doesn't match body key {BodyKey}", key, model.Key);
                    return BadRequest("Setting key mismatch");
                }

                var existingSetting = await _settingsService.GetSettingEntityAsync(key);
                if (existingSetting == null)
                {
                    _logger.LogWarning("Setting with key {Key} not found for update", key);
                    return NotFound();
                }

                if (!existingSetting.IsEditable)
                {
                    _logger.LogWarning("Attempted to update non-editable setting with key: {Key}", key);
                    return BadRequest("This setting cannot be edited");
                }

                // Based on DataType, convert and save
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
                    _logger.LogInformation("Successfully updated setting with key: {Key}", key);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("Failed to update setting with key: {Key}", key);
                    return StatusCode(500, "Failed to update setting");
                }
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Format error updating setting {Key}: Invalid value format for type {DataType}", key, model.DataType);
                return BadRequest($"Invalid format for {model.DataType} value: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {Key}", key);
                return StatusCode(500, "An error occurred while updating the setting");
            }
        }

        // DELETE api/settings/{key}
        [HttpDelete("{key}")]
        [Authorize(Policy = Permissions.ManageSettings)]
        public async Task<IActionResult> DeleteSetting(string key)
        {
            try
            {
                _logger.LogInformation("Attempting to delete setting with key: {Key}", key);
                var setting = await _settingsService.GetSettingEntityAsync(key);
                if (setting == null)
                {
                    _logger.LogWarning("Setting with key {Key} not found for deletion", key);
                    return NotFound();
                }

                if (!setting.IsEditable)
                {
                    _logger.LogWarning("Attempted to delete non-editable setting with key: {Key}", key);
                    return BadRequest("This setting cannot be deleted");
                }

                var success = await _settingsService.DeleteSettingAsync(key);
                if (success)
                {
                    _logger.LogInformation("Successfully deleted setting with key: {Key}", key);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("Failed to delete setting with key: {Key}", key);
                    return StatusCode(500, "Failed to delete setting");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting setting with key: {Key}", key);
                return StatusCode(500, "An error occurred while deleting the setting");
            }
        }

        // POST api/settings
        [HttpPost]
        [Authorize(Policy = Permissions.ManageSettings)]
        public async Task<ActionResult<SettingViewModel>> CreateSetting([FromBody] SettingViewModel model)
        {
            try
            {
                _logger.LogInformation("Attempting to create new setting with key: {Key}", model.Key);
                
                // Check if the setting already exists
                var existingSetting = await _settingsService.GetSettingEntityAsync(model.Key);
                if (existingSetting != null)
                {
                    _logger.LogWarning("Attempted to create setting with existing key: {Key}", model.Key);
                    return Conflict("A setting with this key already exists");
                }

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
                    _logger.LogInformation("Successfully created setting with key: {Key}", model.Key);
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
                    _logger.LogWarning("Failed to create setting with key: {Key}", model.Key);
                    return StatusCode(500, "Failed to create setting");
                }
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Format error creating setting {Key}: Invalid value format for type {DataType}", model.Key, model.DataType);
                return BadRequest($"Invalid format for {model.DataType} value: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating setting {Key}: {Message}", model.Key, ex.Message);
                return StatusCode(500, "An error occurred while creating the setting");
            }
        }

        // GET api/settings/seed
        [HttpGet("seed")]
        [Authorize(Policy = Permissions.ManageSettings)]
        public async Task<IActionResult> SeedTestSetting()
        {
            try
            {
                _logger.LogInformation("Attempting to seed test settings");
                // Create a test setting if none exist
                var settings = await _settingsService.GetAllSettingsAsync();
                if (!settings.Any())
                {
                    await _settingsService.SetSettingAsync(
                        "Test.Setting", 
                        "This is a test setting value", 
                        "A test setting to verify the settings system is working", 
                        "Test");
                    
                    _logger.LogInformation("Created test setting successfully");
                    return Ok("Test setting created");
                }
                
                _logger.LogInformation("Seeding skipped - {Count} settings already exist", settings.Count);
                return Ok($"Settings already exist: {settings.Count} settings found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding test setting");
                return StatusCode(500, "An error occurred while seeding test setting");
            }
        }
    }
}