using Hangfire;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Services.BackgroundJobs
{
    /// <summary>
    /// Service for handling background jobs using Hangfire
    /// </summary>
    public class BackgroundJobService
    {
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ISettingsService _settingsService;  

        public BackgroundJobService(
            ILogger<BackgroundJobService> logger,
            ApplicationDbContext dbContext,
            IEmailService emailService,
            IConfiguration configuration,
            ISettingsService settingsService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _emailService = emailService;
            _configuration = configuration;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Initializes and schedules all recurring Hangfire jobs
        /// </summary>
        public void InitializeRecurringJobs()
        {
            _logger.LogInformation("Initializing recurring Hangfire jobs");
            
            try
            {
                var customerServiceEmail = _configuration["EmailSettings:CustomerServiceEmail"] ?? "admin@example.com";
                
                // Schedule daily cleanup of old logs (keeping 30 days of logs)
                RecurringJob.AddOrUpdate(
                    "cleanup-old-logs",
                    () => CleanupOldLogs(),
                    Cron.Daily(3, 0)); // Run at 3:00 AM every day
                
                // Schedule daily summary email report
                RecurringJob.AddOrUpdate(
                    "daily-summary-report",
                    () => SendDailySummaryReport(customerServiceEmail),
                    Cron.Daily(7, 0)); // Run at 7:00 AM every day

                RecurringJob.AddOrUpdate(
                    "demo-cleanup",
                    () => DemoCleanup(),
                    Cron.Daily());

                RecurringJob.AddOrUpdate(
                    "playwright-cleanup",
                    () => PlaywrightCleanup(),
                    Cron.Daily());

                _logger.LogInformation("Hangfire recurring jobs scheduled successfully");
                
                // Add direct database log entry for successful initialization
                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = "Hangfire recurring jobs initialized successfully",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Hangfire recurring jobs");
                throw;
            }
        }

        /// <summary>
        /// Cleans up old log entries (older than specified days)
        /// </summary>
        /// <param name="daysToKeep">Number of days of logs to keep</param>
        public async Task CleanupOldLogs()
        {            
            try
            {
                var daysToKeep = await _settingsService.GetSettingAsync<int>("OldDbLogDays", 730);
                
                _logger.LogInformation("Starting cleanup of old log entries (older than {DaysToKeep} days)", daysToKeep);
               
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                
                var oldLogs = await _dbContext.LogEntries
                    .Where(log => log.LoggedAt < cutoffDate)
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} log entries older than {CutoffDate} to delete", 
                    oldLogs.Count, cutoffDate);
                
                if (oldLogs.Any())
                {
                    _dbContext.LogEntries.RemoveRange(oldLogs);
                    _dbContext.SaveChanges();
                    
                    // Log the cleanup itself
                    _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = $"Cleaned up {oldLogs.Count} log entries older than {daysToKeep} days",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });
                    _dbContext.SaveChanges();
                    
                    _logger.LogInformation("Successfully deleted {Count} old log entries", oldLogs.Count);
                }
                else
                {
                    // Log when no entries need cleanup
                    _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = $"No log entries older than {daysToKeep} days found for cleanup",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });
                    _dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old log entries");
                
                // Ensure error is logged to database
                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Error cleaning up old log entries: {ex.Message}",
                    Level = "Error",
                    LoggedAt = DateTime.UtcNow
                });
                _dbContext.SaveChanges();
                
                throw;
            }
        }

        /// <summary>
        /// Sends a summary email report
        /// </summary>
        /// <param name="recipientEmail">Email address to send the report to</param>
        public async Task SendDailySummaryReport(string recipientEmail)
        {
            _logger.LogInformation("Generating daily summary report for {Email}", recipientEmail);
            
            try
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);
                
                var logsCount = _dbContext.LogEntries
                    .Count(log => log.LoggedAt >= yesterday && log.LoggedAt < today);
                
                var subject = "Daily System Summary Report";
                var body = $@"
                    <h1>Daily System Summary Report</h1>
                    <p>Report for: {yesterday:yyyy-MM-dd}</p>
                    <ul>
                        <li>New log entries: {logsCount}</li>
                    </ul>
                    <p>This is an automated message from the Hangfire background job system.</p>
                ";
                
                var emailResult = await _emailService.SendEmailAsync(recipientEmail, subject, body);
                _logger.LogInformation("Daily summary report sent to {Email}", recipientEmail);
                
                // Log the email result to database
                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = emailResult 
                        ? $"Daily summary report successfully sent to {recipientEmail}" 
                        : $"Failed to send daily summary report to {recipientEmail}",
                    Level = emailResult ? "Info" : "Warning",
                    LoggedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending daily summary report to {Email}", recipientEmail);
                
                // Ensure error is logged to database
                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Error sending daily summary report to {recipientEmail}: {ex.Message}",
                    Level = "Error",
                    LoggedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();
                
                throw;
            }
        }

        public async Task DemoCleanup()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);
                var demoUsers = await _dbContext.ApplicationUsers
                    .Where(u => u.UserName.StartsWith("DemoUser_") && u.CreatedDate < cutoff)
                    .ToListAsync();

                if (demoUsers.Any())
                {
                    var userIds = demoUsers.Select(u => u.Id).ToList();
                    var surveys = await _dbContext.Surveys
                        .Where(s => userIds.Contains(s.CreatedById))
                        .ToListAsync();

                    if (surveys.Any())
                    {
                        var surveyIds = surveys.Select(s => s.Id).ToList();

                        var mcQuestions = await _dbContext.Questions
                            .OfType<MultipleChoiceQuestion>()
                            .Where(q => surveyIds.Contains(q.SurveyId))
                            .Include(q => q.Options)
                            .ToListAsync();

                        var selectQuestions = await _dbContext.Questions
                            .OfType<SelectAllThatApplyQuestion>()
                            .Where(q => surveyIds.Contains(q.SurveyId))
                            .Include(q => q.Options)
                            .ToListAsync();

                        _dbContext.ChoiceOptions.RemoveRange(mcQuestions.SelectMany(q => q.Options));
                        _dbContext.ChoiceOptions.RemoveRange(selectQuestions.SelectMany(q => q.Options));

                        var questions = await _dbContext.Questions
                            .Where(q => surveyIds.Contains(q.SurveyId))
                            .ToListAsync();

                        _dbContext.Questions.RemoveRange(questions);

                        _dbContext.Surveys.RemoveRange(surveys);
                    }

                    _dbContext.ApplicationUsers.RemoveRange(demoUsers);
                    await _dbContext.SaveChangesAsync();

                    _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = $"DemoCleanup removed {demoUsers.Count} users and {surveys.Count} surveys",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during demo cleanup");
                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Error during demo cleanup: {ex.Message}",
                    Level = "Error",
                    LoggedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();
                throw;
            }
        }

        public async Task PlaywrightCleanup()
        {
            try
            {
                var playwrightUser = await _dbContext.ApplicationUsers
                    .FirstOrDefaultAsync(u => u.UserName == "playwrightuser@example.com");

                if (playwrightUser == null)
                {
                    _logger.LogWarning("Playwright user not found for cleanup");
                    return;
                }

                var surveys = await _dbContext.Surveys
                    .Where(s => s.CreatedById == playwrightUser.Id)
                    .ToListAsync();

                if (!surveys.Any())
                {
                    _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = "PlaywrightCleanup found no surveys to remove",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });
                    await _dbContext.SaveChangesAsync();
                    return;
                }

                var surveyIds = surveys.Select(s => s.Id).ToList();

                var questions = await _dbContext.Questions
                    .Where(q => surveyIds.Contains(q.SurveyId))
                    .ToListAsync();

                var questionIds = questions.Select(q => q.Id).ToList();

                var answers = questionIds.Any()
                    ? await _dbContext.Answers
                        .Where(a => questionIds.Contains(a.QuestionId))
                        .ToListAsync()
                    : new List<Answer>();

                var choiceOptions = questionIds.Any()
                    ? await _dbContext.ChoiceOptions
                        .Where(option =>
                            (option.MultipleChoiceQuestionId.HasValue && questionIds.Contains(option.MultipleChoiceQuestionId.Value)) ||
                            (option.SelectAllThatApplyQuestionId.HasValue && questionIds.Contains(option.SelectAllThatApplyQuestionId.Value)))
                        .ToListAsync()
                    : new List<ChoiceOption>();

                if (answers.Any())
                {
                    _dbContext.Answers.RemoveRange(answers);
                }

                if (choiceOptions.Any())
                {
                    _dbContext.ChoiceOptions.RemoveRange(choiceOptions);
                }

                if (questions.Any())
                {
                    _dbContext.Questions.RemoveRange(questions);
                }

                _dbContext.Surveys.RemoveRange(surveys);

                await _dbContext.SaveChangesAsync();

                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"PlaywrightCleanup removed {surveys.Count} surveys, {questions.Count} questions, {choiceOptions.Count} options, and {answers.Count} answers for Playwright user",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Playwright cleanup");

                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Error during Playwright cleanup: {ex.Message}",
                    Level = "Error",
                    LoggedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();
                throw;
            }
        }
    }
}
