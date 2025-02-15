using Microsoft.Extensions.Options;

namespace JwtIdentity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppSettingsController : ControllerBase
    {
        private readonly AppSettings _appSettings;

        // ...existing code...
        public AppSettingsController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        public ActionResult<AppSettings> Get()
        {
            return Ok(_appSettings);
        }
    }
}
