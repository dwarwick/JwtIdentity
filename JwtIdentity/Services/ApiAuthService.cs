using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JwtIdentity.Services
{
    public class ApiAuthService : IApiAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ApiAuthService> _logger;

        public ApiAuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, ApplicationDbContext dbContext, ILogger<ApiAuthService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            try
            {
                _logger.LogInformation("Generating JWT token for user: {UserId} ({Email})", user.Id, user.Email);
                
                if (string.IsNullOrEmpty(_configuration["Jwt:Key"]) || 
                    string.IsNullOrEmpty(_configuration["Jwt:Issuer"]) || 
                    string.IsNullOrEmpty(_configuration["Jwt:Audience"]))
                {
                    _logger.LogError("JWT configuration is missing or incomplete. Check appsettings.json for Jwt:Key, Jwt:Issuer, and Jwt:Audience");
                    return string.Empty;
                }

                _logger.LogDebug("Retrieving roles for user {UserId}", user.Id);
                var roles = await _userManager.GetRolesAsync(user);
                var roleClaims = roles.Select(q => new Claim(ClaimTypes.Role, q)).ToList();

                var userClaims = await _userManager.GetClaimsAsync(user);

                List<Claim> permissions;
                if (!roles.Any(x => x == "Admin"))
                {
                    _logger.LogDebug("Getting role-specific permissions for non-admin user {UserId}", user.Id);
                    var rolePermissions = from ur in _dbContext.UserRoles
                                        where ur.UserId == user.Id
                                        join r in _dbContext.Roles on ur.RoleId equals r.Id
                                        join rc in _dbContext.RoleClaims on r.Id equals rc.RoleId
                                        select rc.ClaimValue;

                    rolePermissions = rolePermissions.Distinct();

                    permissions = await rolePermissions.Select(q => new Claim(CustomClaimTypes.Permission, q)).ToListAsync();
                }
                else
                {
                    _logger.LogDebug("Setting all permissions for admin user {UserId}", user.Id);
                    var type = typeof(Permissions);

                    permissions = type.GetFields().Select(q => new Claim(CustomClaimTypes.Permission, q.Name)).ToList();
                }

                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                }
                .Union(userClaims)
                .Union(roleClaims)
                .Union(permissions);

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                int expirationMinutes = 60;
                if (!int.TryParse(_configuration["Jwt:ExpirationMinutes"], out expirationMinutes))
                {
                    _logger.LogWarning("JWT expiration minutes not configured or invalid, using default: 60 minutes");
                }

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(expirationMinutes),
                    signingCredentials: creds);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogInformation("JWT token generated successfully for user {UserId}", user.Id);
                
                return tokenString;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument when generating JWT token for user {UserId}: {Message}", user.Id, ex.Message);
                throw;
            }
            catch (SecurityTokenEncryptionFailedException ex)
            {
                _logger.LogError(ex, "Token encryption failed for user {UserId}: {Message}", user.Id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating JWT token for user {UserId}: {Message}", user.Id, ex.Message);
                throw;
            }
        }

        public async Task<string> GenerateEmailVerificationLink(ApplicationUser user)
        {
            try
            {
                _logger.LogInformation("Generating email verification link for user: {UserId} ({Email})", user.Id, user.Email);
                
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                _logger.LogDebug("Email confirmation token generated for user {UserId}", user.Id);

                byte[] tokenGeneratedBytes = Encoding.UTF8.GetBytes(token);
                var codeEncoded = WebEncoders.Base64UrlEncode(tokenGeneratedBytes);

                var baseUrl = _configuration["ApiBaseAddress"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    _logger.LogWarning("ApiBaseAddress configuration is missing, using empty base URL");
                    baseUrl = string.Empty;
                }

                var verificationUrl = $"{baseUrl}/api/auth/confirmemail?token={codeEncoded}&email={user.Email}";
                _logger.LogInformation("Verification link generated successfully for user {UserId}", user.Id);
                
                return verificationUrl;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Null argument provided when generating email verification link for user {UserId}: {Message}", 
                    user?.Id ?? 0, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating email verification link for user {UserId}: {Message}", 
                    user?.Id ?? 0, ex.Message);
                throw;
            }
        }

        // get the id of the logged in user 
        public int GetUserId(ClaimsPrincipal user)
        {
            try
            {
                _logger.LogDebug("Getting user ID from claims principal");
                
                if (user == null)
                {
                    _logger.LogWarning("Attempted to get user ID from null ClaimsPrincipal");
                    return 0;
                }
                
                var nameIdentifierClaim = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier && int.TryParse(x.Value, out _));
                if (nameIdentifierClaim != null && int.TryParse(nameIdentifierClaim.Value, out int userId))
                {
                    _logger.LogDebug("Retrieved user ID: {UserId} from claims", userId);
                    return userId;
                }
                
                _logger.LogWarning("No valid user ID found in claims principal");
                return 0;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Null argument error while getting user ID from claims: {Message}", ex.Message);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting user ID from claims: {Message}", ex.Message);
                return 0;
            }
        }

        // generate a list of the logged in users roles and a list of the logged in users permissions
        public async Task<List<string>> GetUserRoles(ClaimsPrincipal user)
        {
            try
            {
                _logger.LogDebug("Getting user roles from claims principal");
                
                var userId = GetUserId(user);
                if (userId == 0)
                {
                    _logger.LogWarning("Cannot get roles for user with ID 0");
                    return new List<string>();
                }
                
                _logger.LogDebug("Looking up user {UserId} in the database", userId);
                var dbUser = await _userManager.FindByIdAsync(userId.ToString());
                
                if (dbUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found in database", userId);
                    return new List<string>();
                }
                
                _logger.LogDebug("Retrieving roles for user {UserId}", userId);
                var roles = await _userManager.GetRolesAsync(dbUser);
                
                _logger.LogInformation("Retrieved {RoleCount} roles for user {UserId}", roles.Count, userId);
                return roles.ToList();
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Null argument error while getting user roles: {Message}", ex.Message);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting roles for user: {Message}", ex.Message);
                return new List<string>();
            }
        }


        public List<string> GetUserPermissions(ClaimsPrincipal user)
        {
            try
            {
                _logger.LogDebug("Getting user permissions from claims principal");
                
                var userId = GetUserId(user);
                if (userId == 0)
                {
                    _logger.LogWarning("Cannot get permissions for user with ID 0");
                    return new List<string>();
                }
                
                _logger.LogDebug("Querying permissions for user {UserId}", userId);
                var rolePermissions = from ur in _dbContext.UserRoles
                                      where ur.UserId == userId
                                      join r in _dbContext.Roles on ur.RoleId equals r.Id
                                      join rc in _dbContext.RoleClaims on r.Id equals rc.RoleId
                                      select rc.ClaimValue;

                var permissions = rolePermissions.Distinct().ToList();
                _logger.LogInformation("Retrieved {PermissionCount} permissions for user {UserId}", permissions.Count, userId);
                
                return permissions;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database operation error while getting user permissions: {Message}", ex.Message);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting permissions for user: {Message}", ex.Message);
                return new List<string>();
            }
        }
    }
}
