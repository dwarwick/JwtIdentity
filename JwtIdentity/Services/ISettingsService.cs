using JwtIdentity.Models;

namespace JwtIdentity.Services
{
    public interface ISettingsService
    {
        /// <summary>
        /// Gets a setting value by its key, deserialized to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="key">The setting key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>The setting value or default</returns>
        Task<T> GetSettingAsync<T>(string key, T defaultValue = default);

        /// <summary>
        /// Gets a setting by its key
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <returns>The setting or null if not found</returns>
        Task<Setting> GetSettingEntityAsync(string key);

        /// <summary>
        /// Sets or updates a setting value
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The setting key</param>
        /// <param name="value">The value to save</param>
        /// <param name="description">Optional description of the setting</param>
        /// <param name="category">Optional category for the setting</param>
        /// <param name="isEditable">Whether this setting can be edited through the UI</param>
        /// <returns>True if successful</returns>
        Task<bool> SetSettingAsync<T>(string key, T value, string description = null, string category = "General", bool isEditable = true);

        /// <summary>
        /// Gets all settings, optionally filtered by category
        /// </summary>
        /// <param name="category">Optional category to filter by</param>
        /// <returns>List of settings</returns>
        Task<List<Setting>> GetAllSettingsAsync(string category = null);

        /// <summary>
        /// Deletes a setting by key
        /// </summary>
        /// <param name="key">The key of the setting to delete</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteSettingAsync(string key);

        /// <summary>
        /// Gets a list of all distinct setting categories
        /// </summary>
        /// <returns>List of category names</returns>
        Task<List<string>> GetCategoriesAsync();
    }
}