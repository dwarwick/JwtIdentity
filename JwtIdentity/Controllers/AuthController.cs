
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

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            if (string.IsNullOrEmpty(_configuration["Jwt:Key"]) || string.IsNullOrEmpty(_configuration["Jwt:Issuer"]) || string.IsNullOrEmpty(_configuration["Jwt:Audience"]))
            {
                return string.Empty;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(q => new Claim("role", q)).ToList();

            var userClaims = await _userManager.GetClaimsAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }
            .Union(userClaims)
            .Union(roleClaims);

            //var claims = new[]
            //{
            //    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            //};

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
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
