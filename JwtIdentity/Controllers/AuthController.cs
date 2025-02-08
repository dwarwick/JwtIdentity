using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, IMapper mapper, ApplicationDbContext applicationDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _mapper = mapper;
            _dbContext = applicationDbContext;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginModel>> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid client request");
            }

            ApplicationUser? user = await _userManager.FindByNameAsync(model.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                string Token = await GenerateJwtToken(user);

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
                        Expires = DateTimeOffset.Now.AddMinutes(30)
                    }
                );

                return Ok(applicationUserViewModel);
            }
            return Unauthorized();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(".AspNetCore.Identity.Application");
            return Ok(new { message = "Logged out" });
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
                    _ = await _dbContext.SaveChangesAsync();

                    model = _mapper.Map<RoleClaimViewModel>(roleClaim);

                    return this.Ok(model);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error adding permission\r\n\r\n{ex}");
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
                    _ = await _dbContext.SaveChangesAsync();
                    return this.Ok(true);
                }
            }
            catch (Exception ex)
            {


            }

            return Problem("Error deleting permission");
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
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

            //var claims = new[]
            //{
            //    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            //};

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
