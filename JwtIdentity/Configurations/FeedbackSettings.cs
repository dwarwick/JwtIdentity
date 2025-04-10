using JwtIdentity.Services;

namespace JwtIdentity.Configurations
{
    public class FeedbackSettings
    {
        private const string AdminNotificationEmailsKey = "FeedbackSettings.AdminNotificationEmails";
        private const string NotificationsSnoozeUntilKey = "FeedbackSettings.NotificationsSnoozeUntil";
        private const string SettingsCategory = "Feedback";
        
        private readonly ISettingsService _settingsService;

        public FeedbackSettings(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Get admin notification emails from settings
        /// </summary>
        public async Task<List<string>> GetAdminNotificationEmailsAsync()
        {
            return await _settingsService.GetSettingAsync(
                AdminNotificationEmailsKey,
                new List<string>());
        }

        /// <summary>
        /// Set admin notification emails in settings
        /// </summary>
        public async Task<bool> SetAdminNotificationEmailsAsync(List<string> emails)
        {
            return await _settingsService.SetSettingAsync(
                AdminNotificationEmailsKey,
                emails,
                "Email addresses to notify about new feedback",
                SettingsCategory);
        }

        /// <summary>
        /// Add an email address to admin notification list
        /// </summary>
        public async Task<bool> AddAdminEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;
                
            var emails = await GetAdminNotificationEmailsAsync();
            
            if (!emails.Contains(email, StringComparer.OrdinalIgnoreCase))
            {
                emails.Add(email);
                return await SetAdminNotificationEmailsAsync(emails);
            }
            
            return true; // Already exists, so technically successful
        }

        /// <summary>
        /// Remove an email address from admin notification list
        /// </summary>
        public async Task<bool> RemoveAdminEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;
                
            var emails = await GetAdminNotificationEmailsAsync();
            
            bool removed = emails.RemoveAll(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                return await SetAdminNotificationEmailsAsync(emails);
            }
            
            return false; // Email not found
        }

        /// <summary>
        /// Get notification snooze time
        /// </summary>
        public async Task<DateTime?> GetNotificationsSnoozeUntilAsync()
        {
            return await _settingsService.GetSettingAsync<DateTime?>(
                NotificationsSnoozeUntilKey, 
                null);
        }

        /// <summary>
        /// Set notification snooze time
        /// </summary>
        public async Task<bool> SetNotificationsSnoozeUntilAsync(DateTime? snoozeUntil)
        {
            return await _settingsService.SetSettingAsync(
                NotificationsSnoozeUntilKey,
                snoozeUntil,
                "Time until notifications are snoozed",
                SettingsCategory);
        }

        /// <summary>
        /// Checks if feedback notifications are enabled (not snoozed)
        /// </summary>
        /// <returns>True if notifications are enabled, false if they are snoozed</returns>
        public async Task<bool> AreNotificationsEnabledAsync()
        {
            var snoozeUntil = await GetNotificationsSnoozeUntilAsync();
            
            if (!snoozeUntil.HasValue)
            {
                return true; // No snooze time set, so notifications are enabled
            }

            return DateTime.Now > snoozeUntil.Value;
        }
    }
}