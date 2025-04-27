namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CookieController : ControllerBase
    {
        private readonly ILogger<CookieController> _logger;

        public CookieController(ILogger<CookieController> logger)
        {
            _logger = logger;
        }

        [HttpGet("consent")]
        public IActionResult SetCookieConsent(string consent)
        {
            _logger.LogInformation("Processing cookie consent request with value: {Consent}", consent);
            
            try
            {
                // Validate the consent parameter
                if (string.IsNullOrEmpty(consent))
                {
                    _logger.LogWarning("Invalid cookie consent attempt with empty consent value");
                    return BadRequest("Consent parameter is required");
                }

                _logger.LogDebug("Setting cookie consent value to: {Consent}", consent);
                
                // Store the consent value in a cookie
                Response.Cookies.Append("ThirdPartyCookieConsent", consent, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMonths(12),
                    HttpOnly = false,  // Must be false to read from JavaScript
                    Secure = true,     // Recommended for HTTPS
                    SameSite = SameSiteMode.Lax
                });

                _logger.LogInformation("Cookie consent successfully set to: {Consent}", consent);
                return Ok(new { success = true, consent });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cookie consent: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An unexpected error occurred while setting cookie consent. Please try again later.");
            }
        }
        
        [HttpGet("clear")]
        public IActionResult ClearThirdPartyCookies()
        {
            _logger.LogInformation("Processing request to clear third-party cookies");
            
            try
            {
                // Get all cookies from the request
                var cookies = Request.Cookies;
                _logger.LogDebug("Found {Count} cookies in the request", cookies.Count);
                
                // List of essential cookies that should be preserved
                var essentialCookies = new[] { "authToken" };
                
                // Known third-party cookie prefixes
                var thirdPartyCookiePrefixes = new[] 
                { 
                    "_ga", "_gid", "_gat", "AMP_TOKEN", "_gac", 
                    "IDE", "DSID", "NID", "ANID", "CONSENT",
                    "_fbp", "fr", 
                    "_hj", "__hstc", "hubspotutk"
                };
                
                // Track deleted cookies for logging
                var deletedCookies = new List<string>();
                
                // Delete each cookie that is not in the essential list
                foreach (var cookie in cookies)
                {
                    var name = cookie.Key;
                    
                    // Skip essential cookies
                    if (essentialCookies.Contains(name))
                    {
                        _logger.LogDebug("Skipping essential cookie: {CookieName}", name);
                        continue;
                    }
                    
                    // Check if it's a third-party cookie
                    bool isThirdPartyCookie = thirdPartyCookiePrefixes.Any(prefix => 
                        name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    
                    // Delete the cookie by setting expiration to past
                    if (isThirdPartyCookie || name != "ThirdPartyCookieConsent")
                    {
                        _logger.LogDebug("Deleting cookie: {CookieName}", name);
                        
                        try
                        {
                            // Try to delete with different path combinations
                            Response.Cookies.Delete(name);
                            Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
                            Response.Cookies.Delete(name, new CookieOptions { Path = "", SameSite = SameSiteMode.None, Secure = true });
                            
                            // Also try setting the cookie value to empty with an expired date
                            Response.Cookies.Append(name, "", new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddYears(-1),
                                Path = "/"
                            });
                            
                            deletedCookies.Add(name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error deleting cookie {CookieName}: {Message}", name, ex.Message);
                            // Continue with other cookies even if one fails
                        }
                    }
                }
                
                // Also explicitly delete common Google cookies that might not be in the request
                foreach (var prefix in thirdPartyCookiePrefixes)
                {
                    try
                    {
                        _logger.LogDebug("Explicitly deleting potential cookie with prefix: {Prefix}", prefix);
                        Response.Cookies.Delete(prefix);
                        Response.Cookies.Delete(prefix, new CookieOptions { Path = "/" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting cookie with prefix {Prefix}: {Message}", prefix, ex.Message);
                        // Continue with other prefixes even if one fails
                    }
                }
                
                _logger.LogInformation("Successfully deleted {Count} cookies", deletedCookies.Count);
                return Ok(new { success = true, message = $"Deleted {deletedCookies.Count} cookies", cookies = deletedCookies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing third-party cookies: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An unexpected error occurred while clearing cookies. Please try again later.");
            }
        }
    }
}