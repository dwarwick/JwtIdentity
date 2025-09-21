using Serilog.Context;
using System.Security.Claims;

namespace JwtIdentity.Middleware
{
    public class UserNameEnricherMiddleware
    {
        private readonly RequestDelegate _next;

        public UserNameEnricherMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract the username from the ClaimsPrincipal
            string userName = context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.FindFirst(ClaimTypes.Email)?.Value ?? "anonymous"
                : "anonymous";

            // Add the username to the LogContext - store just the username without extra formatting
            using (LogContext.PushProperty("UserName", userName))
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
        }
    }

    // Extension method to make it easier to add the middleware to the pipeline
    public static class UserNameEnricherMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserNameEnricher(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserNameEnricherMiddleware>();
        }
    }
}