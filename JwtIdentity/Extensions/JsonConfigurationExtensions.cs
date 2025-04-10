using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JwtIdentity.Extensions
{
    public static class JsonConfigurationExtensions
    {
        public static IMvcBuilder AddCyclicalReferenceHandling(this IServiceCollection services)
        {
            return services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        }

        /// <summary>
        /// Updates a section in a JSON configuration file
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <param name="sectionName">Name of the section to update</param>
        /// <param name="sectionJson">New JSON content for the section</param>
        public static void UpdateSection(string filePath, string sectionName, string sectionJson)
        {
            try
            {
                // Read the existing JSON file
                var json = File.ReadAllText(filePath);
                var jsonObj = JObject.Parse(json);

                // Parse the new section content
                var newSection = JObject.Parse(sectionJson);

                // Update or add the section
                if (jsonObj[sectionName] != null)
                {
                    jsonObj[sectionName] = newSection;
                }
                else
                {
                    jsonObj.Add(sectionName, newSection);
                }

                // Write back to the file with formatting
                File.WriteAllText(filePath, jsonObj.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - this is a non-critical operation
                Console.WriteLine($"Error updating configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds an email address to the admin notification list
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <param name="email">Email address to add</param>
        public static void AddAdminEmail(string filePath, string email)
        {
            if (string.IsNullOrEmpty(email))
                return;

            try
            {
                // Read the existing JSON file
                var json = File.ReadAllText(filePath);
                var jsonObj = JObject.Parse(json);

                // Get the FeedbackSettings section or create it if it doesn't exist
                JToken? feedbackSettings = jsonObj["FeedbackSettings"];
                if (feedbackSettings == null)
                {
                    feedbackSettings = new JObject();
                    jsonObj["FeedbackSettings"] = feedbackSettings;
                }

                // Get the AdminNotificationEmails array or create it if it doesn't exist
                JArray? emails;
                if (feedbackSettings["AdminNotificationEmails"] != null)
                {
                    emails = (JArray)feedbackSettings["AdminNotificationEmails"];
                }
                else
                {
                    emails = new JArray();
                    ((JObject)feedbackSettings)["AdminNotificationEmails"] = emails;
                }

                // Check if the email already exists
                bool exists = false;
                foreach (var existingEmail in emails)
                {
                    if (existingEmail.ToString().Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                // Add the email if it doesn't exist
                if (!exists)
                {
                    emails.Add(email);
                }

                // Write back to the file with formatting
                File.WriteAllText(filePath, jsonObj.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - this is a non-critical operation
                Console.WriteLine($"Error updating admin emails: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes an email address from the admin notification list
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <param name="email">Email address to remove</param>
        public static void RemoveAdminEmail(string filePath, string email)
        {
            if (string.IsNullOrEmpty(email))
                return;

            try
            {
                // Read the existing JSON file
                var json = File.ReadAllText(filePath);
                var jsonObj = JObject.Parse(json);

                // Get the FeedbackSettings section
                JToken? feedbackSettings = jsonObj["FeedbackSettings"];
                if (feedbackSettings == null)
                    return;

                // Get the AdminNotificationEmails array
                JArray? emails = (JArray)feedbackSettings["AdminNotificationEmails"];
                if (emails == null)
                    return;

                // Find and remove the email
                for (int i = emails.Count - 1; i >= 0; i--)
                {
                    if (emails[i].ToString().Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        emails.RemoveAt(i);
                        break;
                    }
                }

                // Write back to the file with formatting
                File.WriteAllText(filePath, jsonObj.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - this is a non-critical operation
                Console.WriteLine($"Error removing admin email: {ex.Message}");
            }
        }
    }
}
