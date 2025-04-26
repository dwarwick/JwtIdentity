using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace JwtIdentity.Filters
{
    public class DatabaseLoggingFilter : IActionFilter, IExceptionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DatabaseLoggingFilter(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Extract the clean controller and action name without the assembly info
            string actionDescriptor = context.ActionDescriptor.DisplayName ?? string.Empty;
            string cleanDisplayName = FormatActionName(actionDescriptor);
            
            // Get the current username
            string userName = GetCurrentUsername();

            _context.LogEntries.Add(new LogEntry
            {
                Message = $"Controller: {cleanDisplayName} started [User: {userName}]",
                Level = "Info",
                LoggedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnException(ExceptionContext context)
        {
            // Get the current username
            string userName = GetCurrentUsername();

            _context.LogEntries.Add(new LogEntry
            {
                Message = $"Exception: {context.Exception.Message} [User: {userName}]",
                Level = "Error",
                LoggedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        private string FormatActionName(string actionDescriptor)
        {
            // Example input: "JwtIdentity.Controllers.ApplicationUserController.GetApplicationUser (JwtIdentity)"
            // Remove the assembly name in parentheses
            int parenthesisIndex = actionDescriptor.IndexOf(" (");
            if (parenthesisIndex > 0)
            {
                actionDescriptor = actionDescriptor.Substring(0, parenthesisIndex);
            }

            // Get just the controller and action name without the namespace
            string[] parts = actionDescriptor.Split('.');
            if (parts.Length >= 2)
            {
                return $"{parts[parts.Length - 2]}.{parts[parts.Length - 1]}";
            }

            return actionDescriptor;
        }

        private string GetCurrentUsername()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true
                ? user.FindFirst(ClaimTypes.Name)?.Value 
                  ?? user.FindFirst(ClaimTypes.Email)?.Value 
                  ?? "anonymous"
                : "anonymous";
        }
    }
}
