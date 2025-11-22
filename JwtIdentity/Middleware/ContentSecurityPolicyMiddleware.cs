using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace JwtIdentity.Middleware
{
    public class ContentSecurityPolicyMiddleware
    {
        private readonly RequestDelegate _next;

        public ContentSecurityPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Define CSP policy that allows reCAPTCHA and other application resources to function
            var cspPolicy = "default-src 'self'; " +
                           "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com https://www.gstatic.com https://www.googletagmanager.com https://pagead2.googlesyndication.com https://connect.facebook.net https://cdn.jsdelivr.net; " +
                           "frame-src 'self' https://www.google.com https://www.facebook.com; " +
                           "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
                           "font-src 'self' data: https://fonts.gstatic.com; " +
                           "img-src 'self' data: https: blob:; " +
                           "connect-src 'self' ws://localhost:* wss://localhost:* http://localhost:* https://localhost:* https://www.google.com https://www.gstatic.com https://cdn.jsdelivr.net https://raw.githubusercontent.com; " +
                           "worker-src 'self' blob: https://www.google.com https://www.gstatic.com;";

            // Add CSP header
            context.Response.Headers.Append("Content-Security-Policy", cspPolicy);

            await _next(context);
        }
    }

    // Extension method to easily add the middleware to the pipeline
    public static class ContentSecurityPolicyMiddlewareExtensions
    {
        public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ContentSecurityPolicyMiddleware>();
        }
    }
}
