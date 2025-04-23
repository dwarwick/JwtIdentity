using Microsoft.AspNetCore.Mvc;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CookieController : ControllerBase
    {
        [HttpGet("consent")]
        public IActionResult SetCookieConsent(string consent)
        {
            // Validate the consent parameter
            if (string.IsNullOrEmpty(consent))
            {
                return BadRequest("Consent parameter is required");
            }

            // Store the consent value in a cookie
            Response.Cookies.Append("ThirdPartyCookieConsent", consent, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMonths(12),
                HttpOnly = false,  // Must be false to read from JavaScript
                Secure = true,     // Recommended for HTTPS
                SameSite = SameSiteMode.Lax
            });

            return Ok(new { success = true, consent });
        }
        
        [HttpGet("clear")]
        public IActionResult ClearThirdPartyCookies()
        {
            // Get all cookies from the request
            var cookies = Request.Cookies;
            
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
                    continue;
                }
                
                // Check if it's a third-party cookie
                bool isThirdPartyCookie = thirdPartyCookiePrefixes.Any(prefix => 
                    name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                
                // Delete the cookie by setting expiration to past
                if (isThirdPartyCookie || name != "ThirdPartyCookieConsent")
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
            }
            
            // Also explicitly delete common Google cookies that might not be in the request
            foreach (var prefix in thirdPartyCookiePrefixes)
            {
                Response.Cookies.Delete(prefix);
                Response.Cookies.Delete(prefix, new CookieOptions { Path = "/" });
            }
            
            return Ok(new { success = true, message = $"Deleted {deletedCookies.Count} cookies", cookies = deletedCookies });
        }
    }
}