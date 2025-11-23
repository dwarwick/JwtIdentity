namespace JwtIdentity.Middleware
{
    /// <summary>
    /// Middleware to handle favicon.ico requests by rewriting them to favicon.png
    /// </summary>
    public class FaviconMiddleware
    {
        private readonly RequestDelegate _next;
        private const string FaviconIcoPath = "/favicon.ico";
        private const string FaviconPngPath = "/favicon.png";

        public FaviconMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Rewrite favicon.ico requests to favicon.png
            if (context.Request.Path.Equals(FaviconIcoPath, StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Path = FaviconPngPath;
            }

            await _next(context);
        }

        /// <summary>
        /// Checks if the request path is for the favicon
        /// </summary>
        public static bool IsFaviconRequest(string path)
        {
            return path?.Equals(FaviconIcoPath, StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }

    /// <summary>
    /// Extension method to make it easier to add the favicon middleware to the pipeline
    /// </summary>
    public static class FaviconMiddlewareExtensions
    {
        public static IApplicationBuilder UseFaviconRewrite(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FaviconMiddleware>();
        }
    }
}
