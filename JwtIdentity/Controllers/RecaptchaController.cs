using System.Text.Json;

namespace JwtIdentity.Controllers
{
    [ApiController]
    [Route("api/recaptcha")]
    public class RecaptchaController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly HttpClient _client;

        public RecaptchaController(IConfiguration configuration, HttpClient client = null)
        {
            this.configuration = configuration;
            _client = client ?? new HttpClient();
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] RecaptchaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { Success = false, ErrorCodes = new[] { "Missing token" } });

            bool valid = await VerifyRecaptchaAsync(request.Token, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new { Success = valid });
        }

        private async Task<bool> VerifyRecaptchaAsync(string token, string remoteIp)
        {
            string secretKey = configuration["Recaptcha:SecretKey"];
            var parameters = new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", token },
                { "remoteip", remoteIp }
            };

            var response = await _client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(parameters));
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var recaptchaResult = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString);
                return recaptchaResult != null && recaptchaResult.Success;
            }
            return false;
        }
    }
}
