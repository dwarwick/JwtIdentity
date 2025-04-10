using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JwtIdentity.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(ApplicationDbContext dbContext, ILogger<SettingsService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default)
        {
            try
            {
                var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
                if (setting == null)
                {
                    return defaultValue;
                }

                return (T)ConvertValue(setting.Value, setting.DataType, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting {Key}", key);
                return defaultValue;
            }
        }

        /// <inheritdoc/>
        public async Task<Setting> GetSettingEntityAsync(string key)
        {
            return await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        }

        /// <inheritdoc/>
        public async Task<bool> SetSettingAsync<T>(string key, T value, string description = null, string category = "General", bool isEditable = true)
        {
            try
            {
                var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
                
                if (setting == null)
                {
                    // Create new setting
                    setting = new Setting
                    {
                        Key = key,
                        Description = description ?? key,
                        Category = category ?? "General",
                        IsEditable = isEditable
                    };
                    
                    _dbContext.Settings.Add(setting);
                }
                else
                {
                    // Update description and category if provided
                    if (description != null)
                    {
                        setting.Description = description;
                    }
                    
                    if (category != null)
                    {
                        setting.Category = category;
                    }
                    
                    setting.IsEditable = isEditable;
                }

                // Determine the type and serialize the value
                var type = typeof(T);
                setting.DataType = DetermineDataType(type);
                setting.Value = SerializeValue(value, type);

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving setting {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Setting>> GetAllSettingsAsync(string category = null)
        {
            try
            {
                var query = _dbContext.Settings.AsQueryable();
                
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(s => s.Category == category);
                }
                
                return await query.OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings");
                return new List<Setting>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteSettingAsync(string key)
        {
            try
            {
                var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
                if (setting == null)
                {
                    return false;
                }
                
                _dbContext.Settings.Remove(setting);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting setting {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                return await _dbContext.Settings
                    .Select(s => s.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting categories");
                return new List<string>();
            }
        }

        #region Helper Methods

        private string DetermineDataType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
                return "Int";
            if (type == typeof(long) || type == typeof(long?))
                return "Long";
            if (type == typeof(decimal) || type == typeof(decimal?))
                return "Decimal";
            if (type == typeof(double) || type == typeof(double?))
                return "Double";
            if (type == typeof(bool) || type == typeof(bool?))
                return "Boolean";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "DateTime";
            if (type == typeof(string))
                return "String";
            if (type.IsEnum)
                return "Enum";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return "List";

            return "Json";
        }

        private string SerializeValue<T>(T value, Type type)
        {
            if (value == null)
                return null;

            if (type == typeof(string))
                return (string)(object)value;
            
            if (type == typeof(int) || type == typeof(int?) ||
                type == typeof(long) || type == typeof(long?) ||
                type == typeof(decimal) || type == typeof(decimal?) ||
                type == typeof(double) || type == typeof(double?) ||
                type == typeof(bool) || type == typeof(bool?))
            {
                return value.ToString();
            }
            
            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return ((DateTime)(object)value).ToString("o");
            }
            
            if (type.IsEnum)
            {
                return value.ToString();
            }

            // For complex types, serialize to JSON
            return JsonSerializer.Serialize(value);
        }

        private object ConvertValue(string storedValue, string dataType, Type targetType)
        {
            if (string.IsNullOrEmpty(storedValue))
                return GetDefaultValue(targetType);

            switch (dataType)
            {
                case "Int":
                    return int.Parse(storedValue);
                case "Long":
                    return long.Parse(storedValue);
                case "Decimal":
                    return decimal.Parse(storedValue);
                case "Double":
                    return double.Parse(storedValue);
                case "Boolean":
                    return bool.Parse(storedValue);
                case "DateTime":
                    return DateTime.Parse(storedValue);
                case "String":
                    return storedValue;
                case "Enum":
                    return Enum.Parse(targetType, storedValue);
                case "Json":
                case "List":
                default:
                    return JsonSerializer.Deserialize(storedValue, targetType);
            }
        }

        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            
            return null;
        }
        
        #endregion
    }
}