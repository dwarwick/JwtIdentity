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

        public ApiAuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            if (string.IsNullOrEmpty(_configuration["Jwt:Key"]) || string.IsNullOrEmpty(_configuration["Jwt:Issuer"]) || string.IsNullOrEmpty(_configuration["Jwt:Audience"]))
            {
                return string.Empty;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(q => new Claim(ClaimTypes.Role, q)).ToList();

            var userClaims = await _userManager.GetClaimsAsync(user);

            List<Claim> permissions;
            if (!roles.Any(x => x == "Admin"))
            {
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

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(int.TryParse(_configuration["Jwt:ExpirationMinutes"], out int minutes) ? minutes : 60),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ...existing code...

        public async Task<string> GenerateEmailVerificationLink(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            byte[] tokenGeneratedBytes = Encoding.UTF8.GetBytes(token);
            var codeEncoded = WebEncoders.Base64UrlEncode(tokenGeneratedBytes);



            var baseUrl = _configuration["ApiBaseAddress"] ?? string.Empty;
            var verificationUrl = $"{baseUrl}/api/auth/confirmemail?token={codeEncoded}&email={user.Email}";
            return verificationUrl;
        }

        // get the id of the logged in user 
        public int GetUserId(ClaimsPrincipal user)
        {
            var nameIdentifierClaim = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier && int.TryParse(x.Value, out _));
            if (nameIdentifierClaim != null && int.TryParse(nameIdentifierClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }

        // generate a list of the logged in users roles and a list of the logged in users permissions
        public async Task<List<string>> GetUserRoles(ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var roles = await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(userId.ToString()));
            return roles.ToList();
        }


        public List<string> GetUserPermissions(ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var rolePermissions = from ur in _dbContext.UserRoles
                                      where ur.UserId == userId
                                      join r in _dbContext.Roles on ur.RoleId equals r.Id
                                      join rc in _dbContext.RoleClaims on r.Id equals rc.RoleId
                                      select rc.ClaimValue;

            return rolePermissions.Distinct().ToList();
        }
    }
}
