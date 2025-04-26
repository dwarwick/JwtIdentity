using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IApiAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ISettingsService _settingsService;        
        private readonly ILogger<FeedbackController> _logger;
        private const string SettingsCategory = "Feedback";

        public FeedbackController(
            ApplicationDbContext context, 
            IMapper mapper, 
            IApiAuthService authService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            ISettingsService settingsService,
            ILogger<FeedbackController> logger)
        {
            _context = context;
            _mapper = mapper;
            _authService = authService;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _settingsService = settingsService;
            _logger = logger;
        }

        // GET: api/Feedback
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<FeedbackViewModel>>> GetFeedbacks()
        {
            try
            {
                _logger.LogInformation("Getting all feedback items");
                var feedbacks = await _context.Feedbacks
                    .OrderByDescending(f => f.CreatedDate)
                    .ToListAsync();
                return Ok(_mapper.Map<IEnumerable<FeedbackViewModel>>(feedbacks));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all feedback items");
                return StatusCode(500, "An error occurred while retrieving feedback items");
            }
        }

        // GET: api/Feedback/my
        [HttpGet("my")]
        [Authorize(Policy = Permissions.LeaveFeedback)]
        public async Task<ActionResult<IEnumerable<FeedbackViewModel>>> GetMyFeedbacks()
        {
            try
            {
                int userId = _authService.GetUserId(User);
                if (userId == 0)
                {
                    _logger.LogWarning("Unauthorized attempt to access personal feedback");
                    return Unauthorized();
                }

                _logger.LogInformation("User {UserId} retrieving their feedback items", userId);
                var feedbacks = await _context.Feedbacks
                    .Where(f => f.CreatedById == userId)
                    .OrderByDescending(f => f.CreatedDate)
                    .ToListAsync();
                
                return Ok(_mapper.Map<IEnumerable<FeedbackViewModel>>(feedbacks));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user feedback items");
                return StatusCode(500, "An error occurred while retrieving your feedback items");
            }
        }

        // GET: api/Feedback/5
        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.LeaveFeedback)]
        public async Task<ActionResult<FeedbackViewModel>> GetFeedback(int id)
        {
            try 
            {
                _logger.LogInformation("Getting feedback with ID: {FeedbackId}", id);
                var feedback = await _context.Feedbacks.FindAsync(id);

                if (feedback == null)
                {
                    _logger.LogWarning("Feedback with ID {FeedbackId} not found", id);
                    return NotFound();
                }

                // Regular users can only access their own feedback
                int userId = _authService.GetUserId(User);
                if (feedback.CreatedById != userId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("Unauthorized access attempt to feedback {FeedbackId} by user {UserId}", id, userId);
                    return Forbid();
                }

                _logger.LogInformation("Successfully retrieved feedback {FeedbackId}", id);
                return _mapper.Map<FeedbackViewModel>(feedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with ID {FeedbackId}", id);
                return StatusCode(500, "An error occurred while retrieving the feedback item");
            }
        }

        // POST: api/Feedback
        [HttpPost]
        [Authorize(Policy = Permissions.LeaveFeedback)]
        public async Task<ActionResult<FeedbackViewModel>> PostFeedback(FeedbackViewModel feedbackViewModel)
        {
            try
            {
                _logger.LogInformation("Creating new feedback entry with title: {Title}", feedbackViewModel.Title);
                
                var feedback = _mapper.Map<Feedback>(feedbackViewModel);
                
                // Get authenticated user's ID from claims
                int userId = _authService.GetUserId(User);
                if (userId == 0)
                {
                    _logger.LogWarning("Unauthorized attempt to create feedback");
                    return Unauthorized();
                }
                
                feedback.CreatedById = userId;

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Feedback created successfully with ID: {FeedbackId}", feedback.Id);

                // Send notification email to admins
                try
                {
                    await SendAdminNotification(feedback);
                    _logger.LogInformation("Admin notification sent for feedback {FeedbackId}", feedback.Id);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the feedback submission
                    _logger.LogWarning(ex, "Failed to send admin notification for feedback {FeedbackId}", feedback.Id);
                }

                feedbackViewModel.Id = feedback.Id;
                return CreatedAtAction("GetFeedback", new { id = feedback.Id }, feedbackViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feedback entry");
                return StatusCode(500, "An error occurred while creating your feedback");
            }
        }

        // PUT: api/Feedback/5
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.LeaveFeedback)]
        public async Task<IActionResult> PutFeedback(int id, FeedbackViewModel feedbackViewModel)
        {
            try
            {
                _logger.LogInformation("Updating feedback with ID: {FeedbackId}", id);
                
                if (id != feedbackViewModel.Id)
                {
                    _logger.LogWarning("Bad request: ID mismatch when updating feedback. Path ID: {PathId}, Body ID: {BodyId}", id, feedbackViewModel.Id);
                    return BadRequest();
                }

                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback == null)
                {
                    _logger.LogWarning("Feedback with ID {FeedbackId} not found during update", id);
                    return NotFound();
                }

                // Only admins can update any feedback, regular users can only update their own
                int userId = _authService.GetUserId(User);
                bool isAdmin = User.IsInRole("Admin");
                
                if (feedback.CreatedById != userId && !isAdmin)
                {
                    _logger.LogWarning("Unauthorized attempt to update feedback {FeedbackId} by user {UserId}", id, userId);
                    return Forbid();
                }
                
                // Check if admin is adding a response or changing the resolved status
                bool adminResponseChanged = isAdmin && 
                                          !string.IsNullOrEmpty(feedbackViewModel.AdminResponse) &&
                                          feedback.AdminResponse != feedbackViewModel.AdminResponse;
                
                bool statusChanged = isAdmin && feedback.IsResolved != feedbackViewModel.IsResolved;

                // Only allow admins to set admin responses or mark as resolved
                if (!isAdmin && adminResponseChanged)
                {
                    _logger.LogWarning("Non-admin user {UserId} attempted to modify admin response for feedback {FeedbackId}", userId, id);
                    feedbackViewModel.AdminResponse = feedback.AdminResponse;
                    feedbackViewModel.IsResolved = feedback.IsResolved;
                }
                
                // Save old values for comparison
                string oldAdminResponse = feedback.AdminResponse;
                
                // Update the feedback entity
                _mapper.Map(feedbackViewModel, feedback);
                
                // Preserve the original CreatedById
                var originalCreatedById = feedback.CreatedById;
                _context.Entry(feedback).Property(f => f.CreatedById).CurrentValue = originalCreatedById;
                
                _context.Entry(feedback).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated feedback {FeedbackId}", id);
                    
                    // Send notification if admin responded to the feedback
                    if ((adminResponseChanged || statusChanged) && feedback.CreatedById > 0)
                    {
                        try 
                        {
                            await SendFeedbackResponseNotification(feedback);
                            _logger.LogInformation("Response notification sent for feedback {FeedbackId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send response notification for feedback {FeedbackId}", id);
                        }
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!FeedbackExists(id))
                    {
                        _logger.LogWarning("Feedback with ID {FeedbackId} not found after concurrency exception", id);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency exception when updating feedback {FeedbackId}", id);
                        throw;
                    }
                }

                return Ok(feedbackViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feedback with ID {FeedbackId}", id);
                return StatusCode(500, "An error occurred while updating the feedback");
            }
        }

        private async Task SendAdminNotification(Feedback feedback)
        {
            try 
            {
                _logger.LogInformation("Preparing to send admin notification for feedback {FeedbackId}", feedback.Id);
                
                // Check if notifications are enabled (not snoozed)
                if (!await AreNotificationsEnabledAsync())
                {
                    _logger.LogInformation("Admin notifications are currently snoozed, skipping notification for feedback {FeedbackId}", feedback.Id);
                    return;
                }

                // Get the AdminEmail setting that was recently added
                string adminEmail = await _settingsService.GetSettingAsync<string>("AdminEmail", null);                
                if (string.IsNullOrEmpty(adminEmail))
                {
                    _logger.LogWarning("Admin email is not configured, cannot send notification for feedback {FeedbackId}", feedback.Id);
                    return;
                }

                // Get user information for the feedback submitter
                var user = await _userManager.FindByIdAsync(feedback.CreatedById.ToString());
                string userName = user?.UserName ?? "Anonymous";
                string userEmail = user?.Email ?? "No email provided";
                
                string subject = $"New Feedback Submitted: {feedback.Title}";
                
                string messageBody = $@"
                <h2>New Feedback Received</h2>
                <p>A user has submitted new feedback that requires attention.</p>
                
                <h3>Feedback Details:</h3>
                <ul>
                    <li><strong>Title:</strong> {feedback.Title}</li>
                    <li><strong>Type:</strong> {feedback.Type}</li>
                    <li><strong>Description:</strong> {feedback.Description}</li>
                    <li><strong>Submitted By:</strong> {userName} ({userEmail})</li>
                    <li><strong>Date Submitted:</strong> {feedback.CreatedDate.ToString("yyyy-MM-dd HH:mm")}</li>
                </ul>
                
                <p>Please log in to the admin panel to review and respond to this feedback.</p>
                <p>Best regards,<br/>The System</p>
                ";
                
                // Send email to admin
                await _emailService.SendEmailAsync(adminEmail, subject, messageBody);
                _logger.LogInformation("Admin notification email sent to {AdminEmail} for feedback {FeedbackId}", adminEmail, feedback.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin notification for feedback {FeedbackId}", feedback.Id);
                // Don't rethrow - we don't want to fail the feedback submission due to notification failure
            }
        }
        
        private async Task SendFeedbackResponseNotification(Feedback feedback)
        {
            try 
            {
                _logger.LogInformation("Preparing to send response notification to user for feedback {FeedbackId}", feedback.Id);
                
                // Get the user who submitted the feedback
                var user = await _userManager.FindByIdAsync(feedback.CreatedById.ToString());
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("Cannot send response notification for feedback {FeedbackId}: User {UserId} not found or has no email", 
                        feedback.Id, feedback.CreatedById);
                    return;
                }
                
                string statusText = feedback.IsResolved ? "resolved" : "updated";
                string subject = $"Your feedback has been {statusText}";
                
                string messageBody = $@"
                <h2>Your Feedback Has Been {char.ToUpper(statusText[0]) + statusText.Substring(1)}</h2>
                <p>Hello {user.UserName},</p>
                <p>Your feedback titled <strong>""{feedback.Title}""</strong> has been {statusText} by our administrative team.</p>
                
                <h3>Your Feedback:</h3>
                <p>{feedback.Description}</p>
                
                <h3>Admin Response:</h3>
                <p>{(string.IsNullOrEmpty(feedback.AdminResponse) ? "No response provided." : feedback.AdminResponse)}</p>
                
                <p>Thank you for helping us improve our services.</p>
                <p>Best regards,<br/>The Team</p>
                ";
                
                await _emailService.SendEmailAsync(user.Email, subject, messageBody);
                _logger.LogInformation("Response notification email sent to {UserEmail} for feedback {FeedbackId}", user.Email, feedback.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending response notification to user for feedback {FeedbackId}", feedback.Id);
                // Don't rethrow - we don't want to fail the feedback update due to notification failure
            }
        }

        private bool FeedbackExists(int id)
        {
            try
            {
                return _context.Feedbacks.Any(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if feedback {FeedbackId} exists", id);
                throw; // Rethrow to be handled by the calling method
            }
        }

        #region Settings Helper Methods       

        private async Task<DateTime?> GetNotificationsSnoozeUntilAsync()
        {
            try
            {
                var snoozeUntil = await _settingsService.GetSettingAsync<DateTime?>("SnoozeUntilKey", null);
                _logger.LogDebug("Retrieved notification snooze setting: {SnoozeUntil}", 
                    snoozeUntil.HasValue ? snoozeUntil.Value.ToString("yyyy-MM-dd HH:mm:ss") : "No snooze set");
                return snoozeUntil;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification snooze setting");
                return null; // Default to no snooze if there's an error
            }
        }

        private async Task<bool> AreNotificationsEnabledAsync()
        {
            try
            {
                var snoozeUntil = await GetNotificationsSnoozeUntilAsync();
                
                if (!snoozeUntil.HasValue)
                {
                    _logger.LogDebug("Notifications are enabled (no snooze time set)");
                    return true; // No snooze time set, so notifications are enabled
                }

                bool enabled = DateTime.Now > snoozeUntil.Value;
                _logger.LogDebug("Notifications are {Status} (snooze until: {SnoozeUntil}, current time: {CurrentTime})",
                    enabled ? "enabled" : "disabled",
                    snoozeUntil.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                return enabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if notifications are enabled");
                return true; // Default to enabled if there's an error
            }
        }
        
        #endregion
    }
}