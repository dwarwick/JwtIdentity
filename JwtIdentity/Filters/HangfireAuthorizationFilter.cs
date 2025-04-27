using Hangfire.Dashboard;
using System.Security.Claims;

namespace JwtIdentity.Filters
{
    /// <summary>
    /// Authorization filter for Hangfire dashboard that checks if the user has the UseHangfire permission
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // User must be authenticated
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }
            
            // Check if the user has the required permission
            return httpContext.User.HasClaim(c => 
                c.Type == CustomClaimTypes.Permission && 
                c.Value == Common.Auth.Permissions.UseHangfire);
        }
    }
}