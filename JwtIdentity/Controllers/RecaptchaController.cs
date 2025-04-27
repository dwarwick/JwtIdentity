using System.Text.Json;

namespace JwtIdentity.Controllers
{
    [ApiController]
    [Route("api/recaptcha")]
    public class RecaptchaController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly HttpClient _client;
        private readonly ILogger<RecaptchaController> _logger;

        public RecaptchaController(IConfiguration configuration, ILogger<RecaptchaController> logger, HttpClient client = null)
        {
            this.configuration = configuration;
            _logger = logger;
            _client = client ?? new HttpClient();
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] RecaptchaRequest request)
        {
            _logger.LogInformation("Processing reCAPTCHA validation request");
            
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    _logger.LogWarning("reCAPTCHA validation failed: Missing token");
                    return BadRequest(new { Success = false, ErrorCodes = new[] { "Missing token" } });
                }

                string remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogDebug("Verifying reCAPTCHA token for IP: {IpAddress}", remoteIp);
                
                bool valid = await VerifyRecaptchaAsync(request.Token, remoteIp);
                
                _logger.LogInformation("reCAPTCHA validation result: {Result}", valid ? "Success" : "Failed");
                return Ok(new { Success = valid });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error occurred while verifying reCAPTCHA token: {Message}", httpEx.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Success = false, ErrorCodes = new[] { "reCAPTCHA service unavailable" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during reCAPTCHA validation: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, ErrorCodes = new[] { "Internal server error" } });
            }
        }

        private async Task<bool> VerifyRecaptchaAsync(string token, string remoteIp)
        {
            try
            {
                string secretKey = configuration["Recaptcha:SecretKey"];
                
                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogError("reCAPTCHA secret key is not configured");
                    throw new InvalidOperationException("reCAPTCHA is not properly configured");
                }
                
                var parameters = new Dictionary<string, string>
                {
                    { "secret", secretKey },
                    { "response", token },
                    { "remoteip", remoteIp }
                };

                _logger.LogDebug("Sending request to reCAPTCHA API");
                var response = await _client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(parameters));
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("reCAPTCHA API response: {Response}", jsonString);
                    
                    var recaptchaResult = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString);
                    
                    if (recaptchaResult != null && !recaptchaResult.Success)
                    {
                        _logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}", 
                            recaptchaResult.ErrorCodes != null ? string.Join(", ", recaptchaResult.ErrorCodes) : "none");
                    }
                    
                    return recaptchaResult != null && recaptchaResult.Success;
                }
                
                _logger.LogWarning("reCAPTCHA API returned non-success status code: {StatusCode}", (int)response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyRecaptchaAsync: {Message}", ex.Message);
                throw; // Re-throw to be handled by the calling method
            }
        }
    }
}
