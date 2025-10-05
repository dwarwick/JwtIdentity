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
                // Preserve auth and consent, and antiforgery cookies to avoid CSRF validation failures
                var essentialCookies = new[] 
                { 
                    "authToken",
                    "ThirdPartyCookieConsent",
                    ".AspNetCore.Cookies", // cookie-auth ticket
                    "RequestVerificationToken", // SPA antiforgery cookie name in .NET 8/9
                    "__RequestVerificationToken" // legacy name
                };
                
                bool IsEssential(string name) =>
                    essentialCookies.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                    name.StartsWith(".AspNetCore.Antiforgery", StringComparison.OrdinalIgnoreCase);
                
                // Known third-party cookie prefixes
                var thirdPartyCookiePrefixes = new[] 
                { 
                    // Google Analytics
                    "_ga", "_gid", "_gat", "AMP_TOKEN", "_gac",
                    // Google Ads and DoubleClick
                    "IDE", "DSID", "NID", "ANID", "CONSENT", "DV", "1P_JAR",
                    // Facebook
                    "_fbp", "fr",
                    // Hotjar/Hubspot/etc
                    "_hj", "__hstc", "hubspotutk"
                };
                
                // Track deleted cookies for logging
                var deletedCookies = new List<string>();
                
                // Delete only third-party cookies; preserve essentials and site/session cookies
                foreach (var cookie in cookies)
                {
                    var name = cookie.Key;
                    
                    if (IsEssential(name))
                    {
                        _logger.LogDebug("Preserving essential cookie: {CookieName}", name);
                        continue;
                    }
                    
                    bool isThirdPartyCookie = thirdPartyCookiePrefixes.Any(prefix => 
                        name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    
                    if (!isThirdPartyCookie)
                    {
                        // Not a known third-party cookie: skip deletion to avoid breaking app/session
                        _logger.LogDebug("Skipping non-third-party cookie: {CookieName}", name);
                        continue;
                    }

                    _logger.LogDebug("Deleting cookie: {CookieName}", name);
                    try
                    {
                        // Try to delete with different path combinations
                        Response.Cookies.Delete(name);
                        Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
                        Response.Cookies.Delete(name, new CookieOptions { Path = "", SameSite = SameSiteMode.None, Secure = true });
                        
                        // Also try setting the cookie value to empty with an expired date
                        Response.Cookies.Append(name, string.Empty, new CookieOptions
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
                
                _logger.LogInformation("Successfully deleted {Count} third-party cookies", deletedCookies.Count);
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