using Microsoft.AspNetCore.Authorization;

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
        private const string SettingsCategory = "Feedback";

        public FeedbackController(
            ApplicationDbContext context, 
            IMapper mapper, 
            IApiAuthService authService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            ISettingsService settingsService)
        {
            _context = context;
            _mapper = mapper;
            _authService = authService;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _settingsService = settingsService;
        }

        // GET: api/Feedback
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<FeedbackViewModel>>> GetFeedbacks()
        {
            var feedbacks = await _context.Feedbacks
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
            return Ok(_mapper.Map<IEnumerable<FeedbackViewModel>>(feedbacks));
        }

        // GET: api/Feedback/my
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FeedbackViewModel>>> GetMyFeedbacks()
        {
            int userId = _authService.GetUserId(User);
            if (userId == 0)
            {
                return Unauthorized();
            }

            var feedbacks = await _context.Feedbacks
                .Where(f => f.CreatedById == userId)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
            
            return Ok(_mapper.Map<IEnumerable<FeedbackViewModel>>(feedbacks));
        }

        // GET: api/Feedback/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<FeedbackViewModel>> GetFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);

            if (feedback == null)
            {
                return NotFound();
            }

            // Regular users can only access their own feedback
            int userId = _authService.GetUserId(User);
            if (feedback.CreatedById != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return _mapper.Map<FeedbackViewModel>(feedback);
        }

        // POST: api/Feedback
        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<FeedbackViewModel>> PostFeedback(FeedbackViewModel feedbackViewModel)
        {
            var feedback = _mapper.Map<Feedback>(feedbackViewModel);
            
            // Get authenticated user's ID from claims
            int userId = _authService.GetUserId(User);
            if (userId == 0)
            {
                return Unauthorized();
            }
            
            feedback.CreatedById = userId;

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            // Send notification email to admins
            await SendAdminNotification(feedback);

            feedbackViewModel.Id = feedback.Id;
            return CreatedAtAction("GetFeedback", new { id = feedback.Id }, feedbackViewModel);
        }

        // PUT: api/Feedback/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutFeedback(int id, FeedbackViewModel feedbackViewModel)
        {
            if (id != feedbackViewModel.Id)
            {
                return BadRequest();
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            // Only admins can update any feedback, regular users can only update their own
            int userId = _authService.GetUserId(User);
            bool isAdmin = User.IsInRole("Admin");
            
            if (feedback.CreatedById != userId && !isAdmin)
            {
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
                
                // Send notification if admin responded to the feedback
                if ((adminResponseChanged || statusChanged) && feedback.CreatedById > 0)
                {
                    await SendFeedbackResponseNotification(feedback);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FeedbackExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(feedbackViewModel);
        }

        // DELETE: api/Feedback/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task SendAdminNotification(Feedback feedback)
        {
            try 
            {
                // Check if notifications are enabled (not snoozed)
                if (!await AreNotificationsEnabledAsync())
                {
                    // Notifications are snoozed, don't send emails
                    return;
                }

                // Get the AdminEmail setting that was recently added
                string adminEmail = await _settingsService.GetSettingAsync<string>("AdminEmail", null);                

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
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    await _emailService.SendEmailAsync(adminEmail, subject, messageBody);
                }                
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the feedback submission
                Console.WriteLine($"Error sending admin notification: {ex.Message}");
            }
        }
        
        private async Task SendFeedbackResponseNotification(Feedback feedback)
        {
            try 
            {
                // Get the user who submitted the feedback
                var user = await _userManager.FindByIdAsync(feedback.CreatedById.ToString());
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
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
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the feedback update
                Console.WriteLine($"Error sending user notification: {ex.Message}");
            }
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.Id == id);
        }

        #region Settings Helper Methods       

        private async Task<DateTime?> GetNotificationsSnoozeUntilAsync()
        {
            return await _settingsService.GetSettingAsync<DateTime?>(
                "SnoozeUntilKey", 
                null);
        }

        private async Task<bool> AreNotificationsEnabledAsync()
        {
            var snoozeUntil = await GetNotificationsSnoozeUntilAsync();
            
            if (!snoozeUntil.HasValue)
            {
                return true; // No snooze time set, so notifications are enabled
            }

            return DateTime.Now > snoozeUntil.Value;
        }
        
        #endregion
    }
}