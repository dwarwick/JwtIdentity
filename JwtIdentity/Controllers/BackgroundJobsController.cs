using Hangfire;
using JwtIdentity.Common.Auth;
using JwtIdentity.Services.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.UseHangfire)]
    public class BackgroundJobsController : ControllerBase
    {
        private readonly ILogger<BackgroundJobsController> _logger;
        private readonly BackgroundJobService _backgroundJobService;

        public BackgroundJobsController(
            ILogger<BackgroundJobsController> logger,
            BackgroundJobService backgroundJobService)
        {
            _logger = logger;
            _backgroundJobService = backgroundJobService;
        }

        [HttpPost("cleanuplogs")]
        public IActionResult TriggerLogCleanup()
        {
            _logger.LogInformation("Manually triggering log cleanup job");
            
            // Enqueue a background job to clean up logs
            var jobId = BackgroundJob.Enqueue(() => _backgroundJobService.CleanupOldLogs());
            
            return Ok(new { 
                message = $"Log cleanup job enqueued.",
                jobId = jobId
            });
        }

        [HttpPost("sendreport")]
        public IActionResult TriggerSummaryReport([FromBody] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email address is required");
            }
            
            _logger.LogInformation("Manually triggering summary report job for {Email}", email);
            
            // Enqueue a background job to send the summary report
            var jobId = BackgroundJob.Enqueue(() => _backgroundJobService.SendDailySummaryReport(email));
            
            return Ok(new { 
                message = $"Summary report job enqueued. A report will be sent to {email}.",
                jobId = jobId
            });
        }

        [HttpGet("info")]
        public IActionResult GetScheduledJobs()
        {
            return Ok(new
            {
                message = "Hangfire recurring jobs information",
                scheduledJobs = new[]
                {
                    new {
                        name = "cleanup-old-logs",
                        description = "Cleans up log entries older than 30 days",
                        schedule = "Daily at 3:00 AM local time"
                    },
                    new {
                        name = "daily-summary-report",
                        description = "Sends a daily summary report email to the administrator",
                        schedule = "Daily at 7:00 AM local time"
                    }
                }
            });
        }
    }
}