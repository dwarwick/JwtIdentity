using Microsoft.Extensions.Options;

namespace JwtIdentity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppSettingsController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<AppSettingsController> _logger;

        public AppSettingsController(IOptions<AppSettings> appSettings, ILogger<AppSettingsController> logger)
        {
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<AppSettings> Get()
        {
            try
            {
                _logger.LogInformation("Retrieving application settings");
                _logger.LogDebug("Loading app settings configuration");
                
                return Ok(_appSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application settings: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving application settings. Please try again later.");
            }
        }
    }
}
