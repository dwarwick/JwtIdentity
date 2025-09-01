using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

#nullable enable

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly IApiAuthService _apiAuthService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, IMapper mapper, ApplicationDbContext applicationDbContext, IEmailService emailService, IApiAuthService apiAuthService, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _mapper = mapper;
            _dbContext = applicationDbContext;
            _emailService = emailService;
            _apiAuthService = apiAuthService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginModel>> Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("Processing login request for username: {Username}", model?.Username ?? "null");

            try
            {
                if (model == null || !ModelState.IsValid || string.IsNullOrEmpty(model?.Username) || string.IsNullOrEmpty(model?.Password))
                {
                    _logger.LogWarning("Invalid login attempt with invalid model state or empty credentials");
                    return BadRequest("Invalid client request");
                }

                if (model.Username == "logmeinanonymoususer")
                {
                    _logger.LogDebug("Special 'logmeinanonymoususer' username detected, using anonymous user");
                    model.Username = "anonymous";
                    model.Password = _configuration["AnonymousPassword"] ?? string.Empty;
                }
                else if (model.Username == "logmeindemouser")
                {
                    _logger.LogDebug("Special 'logmeindemouser' username detected, using demo user");
                    model.Username = "DemoUser@surveyshark.site";
                    model.Password = _configuration["AnonymousPassword"] ?? string.Empty;
                }

                _logger.LogDebug("Attempting to find user: {Username}", model.Username);
                ApplicationUser? user = await _userManager.FindByNameAsync(model.Username);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found: {Username}", model.Username);
                    return BadRequest("Invalid Username or Password");
                }

                _logger.LogDebug("User found, checking password");
                if (!string.IsNullOrEmpty(model.Password) && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    _logger.LogDebug("Password check successful, generating JWT token");
                    string Token = await _apiAuthService.GenerateJwtToken(user);

                    _logger.LogDebug("Mapping user to view model");
                    ApplicationUserViewModel applicationUserViewModel = _mapper.Map<ApplicationUserViewModel>(user);
                    applicationUserViewModel.Token = Token;

                    // 'Response' is the HttpResponse for the current request
                    var expiration = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out int minutes) ? minutes : 60;
                    _logger.LogDebug("Setting auth cookie with expiration of {ExpirationMinutes} minutes", expiration);

                    Response.Cookies.Append(
                        "authToken",
                        Token,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.Now.AddMinutes(expiration)
                        }
                    );

                    _logger.LogDebug("Adding login entry to audit log");
                    _ = _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = $"User {model.Username} logged in at {DateTime.UtcNow}",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });
                    _ = await _dbContext.SaveChangesAsync();

                    var customerServiceEmail = _configuration["EmailSettings:CustomerServiceEmail"];
                    if (!string.IsNullOrEmpty(customerServiceEmail))
                    {
                        var subject = $"User Login: {user.UserName}";
                        var body = $"<p>User {user.UserName} has logged in.</p>";
                        await _emailService.SendEmailAsync(customerServiceEmail, subject, body);
                    }

                    _logger.LogInformation("User {Username} successfully logged in", model.Username);
                    return Ok(applicationUserViewModel);
                }

                _logger.LogWarning("Login failed: Invalid password for user: {Username}", model.Username);
                return BadRequest("Invalid Username or Password");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred during login for user {Username}: {Message}",
                    model?.Username ?? "unknown", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user {Username}: {Message}",
                    model?.Username ?? "unknown", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost("demo")]
        public async Task<ActionResult<ApplicationUserViewModel>> CreateDemoUser()
        {
            _logger.LogInformation("Creating new demo user");

            try
            {
                var baseUser = await _userManager.FindByNameAsync("DemoUser@surveyshark.site");
                if (baseUser == null)
                {
                    _logger.LogWarning("Base demo user not found");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Base demo user not found");
                }

                var guid = Guid.NewGuid().ToString();
                var userName = $"DemoUser_{guid}@surveyshark.site";
                var now = DateTime.UtcNow;

                var newUser = new ApplicationUser
                {
                    UserName = userName,
                    Email = userName,
                    EmailConfirmed = true,
                    Theme = baseUser.Theme,
                    CreatedDate = now,
                    UpdatedDate = now
                };

                var password = _configuration["AnonymousPassword"] ?? string.Empty;
                var createResult = await _userManager.CreateAsync(newUser, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create demo user: {Errors}", errors);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create demo user");
                }

                var baseRoles = await _userManager.GetRolesAsync(baseUser);
                if (baseRoles.Any())
                {
                    await _userManager.AddToRolesAsync(newUser, baseRoles);
                }

                string token = await _apiAuthService.GenerateJwtToken(newUser);
                var viewModel = _mapper.Map<ApplicationUserViewModel>(newUser);
                viewModel.Token = token;

                var expiration = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out int minutes) ? minutes : 60;
                Response.Cookies.Append(
                    "authToken",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.Now.AddMinutes(expiration)
                    }
                );

                _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Demo user {userName} created at {DateTime.UtcNow}",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();

                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating demo user");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating demo user");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Processing logout request");

            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Response.Cookies.Delete(".AspNetCore.Identity.Application");
                Response.Cookies.Delete("authToken"); // Delete the authToken cookie

                _logger.LogInformation("User successfully logged out");
                return Ok(new { message = "Logged out" });
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation during logout: {Message}", ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during logout. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during logout: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterViewModel>> Register([FromBody] RegisterViewModel model)
        {
            _logger.LogInformation("Processing registration request for email: {Email}", model?.Email ?? "null");

            try
            {
                if (model == null)
                {
                    _logger.LogWarning("Registration attempt with null model");
                    return BadRequest("Invalid client request");
                }

                if (!ModelState.IsValid || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password) || model.Password != model.ConfirmPassword)
                {
                    _logger.LogWarning("Invalid registration attempt with invalid model state or mismatched passwords");
                    model.Response = "Invalid client request";
                    return Ok(model);
                }

                _logger.LogDebug("Checking if email already exists: {Email}", model.Email);
                ApplicationUser? existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email already exists: {Email}", model.Email);
                    model.Response = "Email already exists";
                    return Ok(model);
                }

                DateTime now = DateTime.UtcNow;

                _logger.LogDebug("Creating new user with email: {Email}", model.Email);
                ApplicationUser newUser = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    CreatedDate = now,
                    UpdatedDate = now
                };

                IdentityResult result = await _userManager.CreateAsync(newUser, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user: {Email}. Errors: {Errors}", model.Email, errors);
                    return Problem("Error creating user");
                }

                _logger.LogDebug("User created successfully, adding to UnconfirmedUser role");
                model.Response = "User created successfully";

                var roleResult = await _userManager.AddToRoleAsync(newUser, "UnconfirmedUser");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add user to UnconfirmedUser role: {Email}", model.Email);
                }

                _logger.LogDebug("Generating email verification link for user: {Email}", model.Email);
                string link = await _apiAuthService.GenerateEmailVerificationLink(newUser);

                _logger.LogDebug("Sending email verification message to: {Email}", model.Email);
                bool emailSent = _emailService.SendEmailVerificationMessage(newUser.Email, link);

                if (emailSent)
                {
                    _logger.LogInformation("Email verification message sent successfully to: {Email}", model.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send email verification message to: {Email}", model.Email);
                    // We continue with registration process even if email fails
                    // The user can request a new verification email later
                }

                // log the registration
                _ = _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"User {model.Email} registered at {DateTime.UtcNow}",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });
                _ = await _dbContext.SaveChangesAsync();

                var customerServiceEmail = _configuration["EmailSettings:CustomerServiceEmail"];
                if (!string.IsNullOrEmpty(customerServiceEmail))
                {
                    var subject = $"New User Registration: {newUser.UserName}";
                    var body = $"<p>User {newUser.UserName} has registered for a new account.</p>";
                    await _emailService.SendEmailAsync(customerServiceEmail, subject, body);
                }

                _logger.LogInformation("User {Email} successfully registered", model.Email);
                return Ok(model);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred during registration for email {Email}: {Message}",
                    model?.Email ?? "unknown", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation during registration for email {Email}: {Message}",
                    model?.Email ?? "unknown", ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during registration. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for email {Email}: {Message}",
                    model?.Email ?? "unknown", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpGet]
        [Authorize(Policy = Permissions.ManageUsers)]
        [Route("GetRolesAndPermissions")]
        public async Task<ActionResult<List<ApplicationRoleViewModel>>> GetRolesAndPermissions()
        {
            _logger.LogInformation("Processing get roles and permissions request");

            try
            {
                _logger.LogDebug("Querying application roles from database");
                List<ApplicationRole> applicationRoles = await _dbContext.ApplicationRoles
                    .Include(x => x.Claims)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} application roles with their claims", applicationRoles.Count);

                _logger.LogDebug("Mapping application roles to view models");
                var result = _mapper.Map<List<ApplicationRoleViewModel>>(applicationRoles);

                _logger.LogInformation("Successfully retrieved roles and permissions");
                return Ok(result);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while retrieving roles and permissions: {Message}", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation while retrieving roles and permissions: {Message}", ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving roles and permissions. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving roles and permissions: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost]
        [Route("addpermission")]
        [Authorize(Policy = Permissions.ManageUsers)]
        public async Task<ActionResult<RoleClaimViewModel>> AddPermissionFromRole([FromBody] RoleClaimViewModel model)
        {
            _logger.LogInformation("Processing add permission request for role ID: {RoleId}", model?.RoleId ?? "null");

            try
            {
                if (model == null)
                {
                    _logger.LogWarning("Add permission attempt with null model");
                    return BadRequest("Invalid client request");
                }

                _logger.LogDebug("Mapping role claim view model to role claim entity");
                RoleClaim? roleClaim = _mapper.Map<RoleClaim>(model);

                if (roleClaim == null)
                {
                    _logger.LogWarning("Failed to map role claim model to entity");
                    return Problem("Error mapping role claim data");
                }

                _logger.LogDebug("Adding role claim to database: ClaimType={ClaimType}, ClaimValue={ClaimValue} for RoleId={RoleId}",
                    roleClaim.ClaimType, roleClaim.ClaimValue, roleClaim.RoleId);
                _ = _dbContext.RoleClaims.Add(roleClaim);

                // log the permission addition
                _logger.LogDebug("Adding log entry for permission addition");
                _ = _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Permission {model.ClaimValue} added to role {model.RoleId}",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });

                _logger.LogDebug("Saving changes to database");
                _ = await _dbContext.SaveChangesAsync();

                _logger.LogDebug("Mapping role claim entity back to view model");
                model = _mapper.Map<RoleClaimViewModel>(roleClaim);

                _logger.LogInformation("Successfully added permission {ClaimValue} to role {RoleId}",
                    roleClaim.ClaimValue, roleClaim.RoleId);
                return Ok(model);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while adding permission for role {RoleId}: {Message}",
                    model?.RoleId ?? "unknown", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation while adding permission for role {RoleId}: {Message}",
                    model?.RoleId ?? "unknown", ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the permission. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding permission for role {RoleId}: {Message}",
                    model?.RoleId ?? "unknown", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpDelete]
        [Route("deletepermission/{Id}")]
        [Authorize(Policy = Permissions.ManageUsers)]
        public async Task<ActionResult<bool>> DeletePermissionFromRole([FromRoute] int Id)
        {
            _logger.LogInformation("Processing delete permission request for ID: {Id}", Id);

            try
            {
                if (Id == 0)
                {
                    _logger.LogWarning("Delete permission attempt with invalid ID: 0");
                    return BadRequest("Invalid permission ID");
                }

                _logger.LogDebug("Searching for role claim with ID: {Id}", Id);
                RoleClaim? roleClaim = await _dbContext.RoleClaims.FindAsync(Id);

                if (roleClaim == null)
                {
                    _logger.LogWarning("Role claim not found with ID: {Id}", Id);
                    return NotFound("Permission not found");
                }

                _logger.LogDebug("Removing role claim: ClaimType={ClaimType}, ClaimValue={ClaimValue} from RoleId={RoleId}",
                    roleClaim.ClaimType, roleClaim.ClaimValue, roleClaim.RoleId);
                _ = _dbContext.RoleClaims.Remove(roleClaim);

                // log the permission deletion
                _logger.LogDebug("Adding log entry for permission deletion");
                _ = _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Permission {roleClaim.ClaimValue} deleted from role {roleClaim.RoleId}",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });

                _logger.LogDebug("Saving changes to database");
                _ = await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted permission {ClaimValue} from role {RoleId}",
                    roleClaim.ClaimValue, roleClaim.RoleId);
                return Ok(true);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while deleting permission with ID {Id}: {Message}",
                    Id, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation while deleting permission with ID {Id}: {Message}",
                    Id, ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the permission. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting permission with ID {Id}: {Message}",
                    Id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            _logger.LogInformation("Processing email confirmation request for email: {Email}", email ?? "null");

            try
            {
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Invalid email confirmation attempt with missing token or email");
                    return BadRequest("Invalid email confirmation request");
                }

                _logger.LogDebug("Decoding confirmation token");
                byte[] codeDecodedBytes;
                try
                {
                    codeDecodedBytes = WebEncoders.Base64UrlDecode(token);
                }
                catch (FormatException fEx)
                {
                    _logger.LogWarning(fEx, "Invalid token format for email {Email}", email);
                    return BadRequest("Invalid token format");
                }

                var codeDecoded = Encoding.UTF8.GetString(codeDecodedBytes);
                _logger.LogDebug("Token decoded successfully");

                _logger.LogDebug("Looking up user with email: {Email}", email);
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Email confirmation failed: User not found with email: {Email}", email);
                    return NotFound("User not found");
                }

                _logger.LogDebug("Confirming email for user: {Email}", email);
                var result = await _userManager.ConfirmEmailAsync(user, codeDecoded);
                if (result.Succeeded)
                {
                    _logger.LogDebug("Email confirmed successfully, updating user roles");
                    var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, "UnconfirmedUser");
                    if (!removeRoleResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to remove user from UnconfirmedUser role: {Email}", email);
                    }

                    var addRoleResult = await _userManager.AddToRoleAsync(user, "User");
                    if (!addRoleResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to add user to User role: {Email}", email);
                    }

                    _logger.LogInformation("Email successfully confirmed for user: {Email}", email);
                    return LocalRedirect("/users/emailconfirmed");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Email confirmation failed for {Email}. Errors: {Errors}", email, errors);
                    return LocalRedirect("/users/emailnotconfirmed");
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while confirming email for {Email}: {Message}",
                    email ?? "unknown", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation while confirming email for {Email}: {Message}",
                    email ?? "unknown", ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while confirming your email. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while confirming email for {Email}: {Message}",
                    email ?? "unknown", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            _logger.LogInformation("Processing forgot password request for email: {Email}", model?.Email ?? "null");

            try
            {
                if (!ModelState.IsValid || model?.Email == null)
                {
                    _logger.LogWarning("Invalid forgot password attempt with invalid model state or missing email");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }

                _logger.LogDebug("Looking up user with email: {Email}", model.Email);
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist, but log it
                    _logger.LogDebug("User not found with email: {Email}", model.Email);
                    return Ok(new { Success = true, Message = "If your email exists in our system, you will receive a password reset link" });
                }

                _logger.LogDebug("Generating password reset token for user: {Email}", model.Email);
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                _logger.LogDebug("Encoding token for URL transmission");
                byte[] tokenGeneratedBytes = Encoding.UTF8.GetBytes(token);
                var encodedToken = WebEncoders.Base64UrlEncode(tokenGeneratedBytes);

                // Create the reset link
                var baseUrl = _configuration["ApiBaseAddress"] ?? string.Empty;
                _logger.LogDebug("Creating reset URL with base address: {BaseUrl}", baseUrl);
                var resetUrl = $"{baseUrl}/reset-password?email={WebUtility.UrlEncode(user.Email)}&token={encodedToken}";

                _logger.LogDebug("Sending password reset email to: {Email}", user.Email);
                bool emailSent = _emailService.SendPasswordResetEmail(user.Email, resetUrl);

                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent successfully to: {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to: {Email}", user.Email);
                    // We still return success to the user to prevent email enumeration attacks
                    // but log the failure for administrative awareness
                }

                // Log the event
                _logger.LogDebug("Adding log entry for password reset request");
                _ = _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Password reset requested for user {user.Email} at {DateTime.UtcNow}",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });
                _ = await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Password reset link successfully sent to: {Email}", user.Email);
                return Ok(new { Success = true, Message = "Password reset link has been sent to your email" });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while processing forgot password request for {Email}: {Message}",
                    model?.Email ?? "unknown", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation while processing forgot password request for {Email}: {Message}",
                    model?.Email ?? "unknown", ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = "An error occurred while processing your request. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing forgot password request for {Email}: {Message}",
                    model?.Email ?? "unknown", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            _logger.LogInformation("Processing password reset request for email: {Email}", model?.Email ?? "null");

            try
            {
                if (!ModelState.IsValid || model?.Email == null || model?.Token == null || model?.Password == null)
                {
                    _logger.LogWarning("Invalid password reset attempt with invalid model state or missing required fields");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }

                _logger.LogDebug("Looking up user with email: {Email}", model.Email);
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist, but log it
                    _logger.LogWarning("Password reset failed: User not found with email: {Email}", model.Email);
                    return BadRequest(new { Success = false, Message = "Failed to reset password" });
                }

                // Decode the token
                _logger.LogDebug("Decoding reset token");
                byte[] decodedToken;
                try
                {
                    decodedToken = WebEncoders.Base64UrlDecode(model.Token);
                }
                catch (FormatException fEx)
                {
                    _logger.LogWarning(fEx, "Invalid token format for password reset request: {Email}", model.Email);
                    return BadRequest(new { Success = false, Message = "Invalid token format" });
                }

                var token = Encoding.UTF8.GetString(decodedToken);

                // Reset the password
                _logger.LogDebug("Attempting to reset password for user: {Email}", model.Email);
                var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (result.Succeeded)
                {
                    // Log the successful password reset
                    _logger.LogDebug("Adding log entry for successful password reset");
                    _ = _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = $"Password reset successful for user {user.Email} at {DateTime.UtcNow}",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });
                    _ = await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Password successfully reset for user: {Email}", model.Email);
                    return Ok(new { Success = true, Message = "Password has been reset successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Password reset failed for {Email}. Errors: {Errors}", model.Email, errors);
                    return BadRequest(new { Success = false, Message = "Failed to reset password", Errors = result.Errors.Select(e => e.Description).ToList() });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while processing password reset for {Email}: {Message}",
                    model?.Email ?? "unknown", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation while processing password reset for {Email}: {Message}",
                    model?.Email ?? "unknown", ioEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = "An error occurred while processing your request. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing password reset for {Email}: {Message}",
                    model?.Email ?? "unknown", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("handlepasswordresetemailclick")]
        public IActionResult HandlePasswordResetEmailClick(string email, string token)
        {
            _logger.LogInformation("Processing password reset email click for email: {Email}", email ?? "null");

            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Invalid password reset email click with missing email or token");
                    return BadRequest("Invalid password reset request. Missing email or token.");
                }

                // Redirect to the client-side reset password page with the token and email as query parameters
                var baseUrl = _configuration["ApiBaseAddress"] ?? string.Empty;
                _logger.LogDebug("Creating client reset URL with base address: {BaseUrl}", baseUrl);
                var clientResetUrl = $"{baseUrl}/reset-password?email={WebUtility.UrlEncode(email)}&token={WebUtility.UrlEncode(token)}";

                _logger.LogInformation("Redirecting to password reset page for email: {Email}", email);
                return Redirect(clientResetUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while handling password reset email click for {Email}: {Message}",
                    email ?? "unknown", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        // Create model classes at the end of the AuthController class
        public class ForgotPasswordViewModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }
        }

        public class ResetPasswordViewModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }

            [Required]
            public string? Token { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
            public string? Password { get; set; }
        }
    }
}
