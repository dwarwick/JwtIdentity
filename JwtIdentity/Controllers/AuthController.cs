﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

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

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, IMapper mapper, ApplicationDbContext applicationDbContext, IEmailService emailService, IApiAuthService apiAuthService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _mapper = mapper;
            _dbContext = applicationDbContext;
            _emailService = emailService;
            _apiAuthService = apiAuthService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginModel>> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid client request");
            }

            if (model.Username == "logmein")
            {
                model.Username = "anonymous";
                model.Password = _configuration["AnonymousPassword"];
            }

            ApplicationUser user = await _userManager.FindByNameAsync(model.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                string Token = await _apiAuthService.GenerateJwtToken(user);

                ApplicationUserViewModel applicationUserViewModel = _mapper.Map<ApplicationUserViewModel>(user);

                applicationUserViewModel.Token = Token;

                // 'Response' is the HttpResponse for the current request
                Response.Cookies.Append(
                    "authToken",
                    Token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.Now.AddMinutes(int.TryParse(_configuration["Jwt:ExpirationMinutes"], out int minutes) ? minutes : 60)
                    }
                );

                _ = _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"User {model.Username} logged in at {DateTime.UtcNow}",
                    Level = "Info",
                    LoggedAt = DateTime.UtcNow
                });
                _ = await _dbContext.SaveChangesAsync();

                return Ok(applicationUserViewModel);
            }
            return Unauthorized();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(".AspNetCore.Identity.Application");
            Response.Cookies.Delete("authToken"); // Delete the authToken cookie
            return Ok(new { message = "Logged out" });
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterViewModel>> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password) || model.Password != model.ConfirmPassword)
            {
                model.Response = "Invalid client request";
                return Ok(model);
            }

            ApplicationUser? existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                model.Response = "Email already exists";
                return Ok(model);
            }

            DateTime now = DateTime.UtcNow;

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
                return Problem("Error creating user");
            }

            model.Response = "User created successfully";
            _ = await _userManager.AddToRoleAsync(newUser, "UnconfirmedUser");
            string link = await _apiAuthService.GenerateEmailVerificationLink(newUser);
            _emailService.SendEmailVerificationMessage(newUser.Email, link);

            return Ok(model);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.ManageUsers)]
        [Route("GetRolesAndPermissions")]
        public async Task<ActionResult<List<ApplicationRoleViewModel>>> GetRolesAndPermissions()
        {
            List<ApplicationRole> applicationRoles = await _dbContext.ApplicationRoles
                .Include(x => x.Claims)
                .ToListAsync();
            return Ok(_mapper.Map<List<ApplicationRoleViewModel>>(applicationRoles));
        }

        [HttpPost]
        [Route("addpermission")]
        [Authorize(Policy = Permissions.ManageUsers)]
        public async Task<ActionResult<RoleClaimViewModel>> AddPermissionFromRole([FromBody] RoleClaimViewModel model)
        {
            if (model == null)
            {
                return BadRequest(false);
            }

            try
            {
                RoleClaim? roleClaim = _mapper.Map<RoleClaim>(model);

                if (roleClaim != null)
                {
                    _ = _dbContext.RoleClaims.Add(roleClaim);

                    // log the permission addition
                    _ = _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = $"Permission {model.ClaimValue} added to role {model.RoleId}",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });

                    _ = await _dbContext.SaveChangesAsync();

                    model = _mapper.Map<RoleClaimViewModel>(roleClaim);

                    return this.Ok(model);
                }
            }
            catch (Exception ex)
            {
                _ = _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Error adding permission: {ex.Message}",
                    Level = "Error",
                    LoggedAt = DateTime.UtcNow
                });
                _ = _dbContext.SaveChanges();
            }

            return Problem("Error adding permission");
        }

        [HttpDelete]
        [Route("deletepermission/{Id}")]
        [Authorize(Policy = Permissions.ManageUsers)]
        public async Task<ActionResult<bool>> DeletePermissionFromRole([FromRoute] int Id)
        {
            if (Id == 0)
            {
                return BadRequest(false);
            }

            try
            {
                RoleClaim? roleClaim = await _dbContext.RoleClaims.FindAsync(Id);

                if (roleClaim != null)
                {
                    _ = _dbContext.RoleClaims.Remove(roleClaim);

                    // log the permission deletion
                    _ = _dbContext.LogEntries.Add(new LogEntry
                    {
                        Message = $"Permission {roleClaim.ClaimValue} deleted from role {roleClaim.RoleId}",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow
                    });

                    _ = await _dbContext.SaveChangesAsync();

                    return this.Ok(true);
                }
            }
            catch (Exception ex)
            {
                _ = _dbContext.LogEntries.Add(new LogEntry
                {
                    Message = $"Error deleting permission: {ex.Message}",
                    Level = "Error",
                    LoggedAt = DateTime.UtcNow
                });
                _ = _dbContext.SaveChanges();

            }

            return Problem("Error deleting permission");
        }

        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest("Invalid email confirmation request");
            }

            Console.WriteLine($"Received Token: {token}");

            var codeDecodedBytes = WebEncoders.Base64UrlDecode(token);
            var codeDecoded = Encoding.UTF8.GetString(codeDecodedBytes);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.ConfirmEmailAsync(user, codeDecoded);
            if (result.Succeeded)
            {
                _ = await _userManager.RemoveFromRoleAsync(user, "UnconfirmedUser");
                _ = await _userManager.AddToRoleAsync(user, "User");
                return LocalRedirect("/users/emailconfirmed");
            }

            return LocalRedirect("/users/emailnotconfirmed");
        }
    }
}
