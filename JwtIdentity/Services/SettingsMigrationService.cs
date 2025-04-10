using JwtIdentity.Services;
using JwtIdentity.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JwtIdentity.Services
{
    public class SettingsMigrationService
    {
        private readonly ISettingsService _settingsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsMigrationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SettingsMigrationService(
            ISettingsService settingsService,
            IConfiguration configuration,
            ILogger<SettingsMigrationService> logger,
            IServiceProvider serviceProvider)
        {
            _settingsService = settingsService;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Migrate FeedbackSettings from appsettings.json to the database
        /// </summary>
        public async Task MigrateFeedbackSettingsAsync()
        {
            try
            {
                _logger.LogInformation("Starting migration of FeedbackSettings to database");
                
                // Check if we've already migrated FeedbackSettings
                var adminEmailsSetting = await _settingsService.GetSettingEntityAsync("FeedbackSettings.AdminNotificationEmails");
                if (adminEmailsSetting != null)
                {
                    _logger.LogInformation("FeedbackSettings already migrated to database, skipping");
                    return;
                }

                // Retrieve the FeedbackSettings from appsettings.json
                var adminEmails = _configuration.GetSection("FeedbackSettings:AdminNotificationEmails").Get<List<string>>();
                var snoozeUntil = _configuration.GetValue<DateTime?>("FeedbackSettings:NotificationsSnoozeUntil");

                // Save to database
                await _settingsService.SetSettingAsync(
                    "FeedbackSettings.AdminNotificationEmails", 
                    adminEmails ?? new List<string>(), 
                    "Email addresses to notify about new feedback", 
                    "Feedback");

                await _settingsService.SetSettingAsync(
                    "FeedbackSettings.NotificationsSnoozeUntil", 
                    snoozeUntil, 
                    "Time until notifications are snoozed", 
                    "Feedback");

                _logger.LogInformation("FeedbackSettings successfully migrated to database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating FeedbackSettings to database");
            }
        }

        /// <summary>
        /// Run migrations for all settings
        /// </summary>
        public async Task MigrateAllSettingsAsync()
        {
            await MigrateFeedbackSettingsAsync();
            // Add more migration methods here for other settings types if needed
        }
    }
}