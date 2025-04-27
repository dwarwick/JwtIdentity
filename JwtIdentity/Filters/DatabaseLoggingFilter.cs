using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Filters
{
    public class DatabaseLoggingFilter : IActionFilter, IExceptionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DatabaseLoggingFilter> _logger;

        public DatabaseLoggingFilter(
            ApplicationDbContext context, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<DatabaseLoggingFilter> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                // Extract the clean controller and action name without the assembly info
                string actionDescriptor = context.ActionDescriptor.DisplayName ?? string.Empty;
                string cleanDisplayName = FormatActionName(actionDescriptor);
                
                // Get the current username
                string userName = GetCurrentUsername();
                
                // Get request details
                string ipAddress = GetClientIpAddress();
                string requestMethod = _httpContextAccessor.HttpContext?.Request.Method ?? "Unknown";
                string requestPath = _httpContextAccessor.HttpContext?.Request.Path ?? "Unknown";
                
                // Log parameters (safely - avoiding sensitive data)
                string parameters = GetSafeParameterString(context.ActionArguments);

                // Log to console/file via ILogger
                _logger.LogInformation(
                    "Action starting: {Action}, User: {User}, IP: {IP}, Method: {Method}, Path: {Path}, Parameters: {Params}",
                    cleanDisplayName, userName, ipAddress, requestMethod, requestPath, parameters);

                // Log to database
                try
                {
                    _context.LogEntries.Add(new LogEntry
                    {
                        Message = $"Controller: {cleanDisplayName} started [User: {userName}, IP: {ipAddress}]",
                        Level = "Info",
                        LoggedAt = DateTime.UtcNow,
                        RequestPath = requestPath,
                        RequestMethod = requestMethod,
                        Parameters = parameters
                    });
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    // If database logging fails, still log the failure via ILogger
                    _logger.LogError(ex, "Failed to log action execution to database");
                }
            }
            catch (Exception ex)
            {
                // Catch any exception in the logging process itself
                _logger.LogError(ex, "Exception in DatabaseLoggingFilter.OnActionExecuting");
                // Continue execution even if logging fails
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                // Extract the clean controller and action name without the assembly info
                string actionDescriptor = context.ActionDescriptor.DisplayName ?? string.Empty;
                string cleanDisplayName = FormatActionName(actionDescriptor);
                
                // Get the current username
                string userName = GetCurrentUsername();
                
                // Get response status code
                int? statusCode = null;
                if (context.HttpContext?.Response != null)
                {
                    statusCode = context.HttpContext.Response.StatusCode;
                }

                string statusMessage = context.Canceled 
                    ? "Canceled" 
                    : context.Exception != null 
                        ? "Failed" 
                        : "Completed";

                // Log to console/file via ILogger
                _logger.LogInformation(
                    "Action {Status}: {Action}, User: {User}, StatusCode: {StatusCode}",
                    statusMessage, cleanDisplayName, userName, statusCode);

                // Skip database logging if there's an exception - that will be handled in OnException
                if (context.Exception == null)
                {
                    try
                    {
                        _context.LogEntries.Add(new LogEntry
                        {
                            Message = $"Controller: {cleanDisplayName} {statusMessage} [User: {userName}]",
                            Level = context.Canceled ? "Warning" : "Info",
                            LoggedAt = DateTime.UtcNow,
                            StatusCode = statusCode
                        });
                        _context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        // If database logging fails, still log the failure via ILogger
                        _logger.LogError(ex, "Failed to log action completion to database");
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any exception in the logging process itself
                _logger.LogError(ex, "Exception in DatabaseLoggingFilter.OnActionExecuted");
                // Continue execution even if logging fails
            }
        }

        public void OnException(ExceptionContext context)
        {
            try
            {
                // Extract the clean controller and action name without the assembly info
                string actionDescriptor = context.ActionDescriptor.DisplayName ?? string.Empty;
                string cleanDisplayName = FormatActionName(actionDescriptor);
                
                // Get the current username
                string userName = GetCurrentUsername();
                
                // Extract exception details
                string exceptionType = context.Exception.GetType().Name;
                string exceptionMessage = context.Exception.Message;
                string stackTrace = context.Exception.StackTrace ?? "No stack trace available";
                
                // Get request details
                string ipAddress = GetClientIpAddress();
                string requestMethod = _httpContextAccessor.HttpContext?.Request.Method ?? "Unknown";
                string requestPath = _httpContextAccessor.HttpContext?.Request.Path ?? "Unknown";

                // Log to console/file via ILogger first (in case database fails)
                _logger.LogError(
                    context.Exception,
                    "Exception in {Action}: Type: {ExType}, Message: {ExMessage}, User: {User}, IP: {IP}",
                    cleanDisplayName, exceptionType, exceptionMessage, userName, ipAddress);

                try
                {
                    _context.LogEntries.Add(new LogEntry
                    {
                        Message = $"Exception in {cleanDisplayName}: {exceptionMessage} [User: {userName}, IP: {ipAddress}]",
                        Level = "Error",
                        LoggedAt = DateTime.UtcNow,
                        ExceptionType = exceptionType,
                        ExceptionMessage = exceptionMessage,
                        StackTrace = stackTrace,
                        RequestPath = requestPath,
                        RequestMethod = requestMethod
                    });
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    // If database logging fails, still log the failure via ILogger
                    _logger.LogError(ex, "Failed to log exception to database");
                }
            }
            catch (Exception ex)
            {
                // Catch any exception in the logging process itself
                _logger.LogError(ex, "Exception in DatabaseLoggingFilter.OnException: {Message}", ex.Message);
                // Continue execution even if logging fails
            }
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

        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "Unknown";

            // Try to get IP from X-Forwarded-For header first
            string ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            
            // If not available, use RemoteIpAddress
            if (string.IsNullOrEmpty(ip) && httpContext.Connection?.RemoteIpAddress != null)
            {
                ip = httpContext.Connection.RemoteIpAddress.ToString();
            }
            
            return string.IsNullOrEmpty(ip) ? "Unknown" : ip;
        }
        
        private string GetSafeParameterString(IDictionary<string, object> parameters)
        {
            try
            {
                // Filter out or mask potential sensitive data
                var safeParams = new Dictionary<string, object>();
                
                foreach (var param in parameters)
                {
                    // Skip parameters with sensitive names
                    if (IsSensitiveParameterName(param.Key))
                    {
                        safeParams[param.Key] = "[REDACTED]";
                    }
                    else
                    {
                        safeParams[param.Key] = param.Value?.ToString() ?? "null";
                    }
                }
                
                return JsonSerializer.Serialize(safeParams);
            }
            catch
            {
                return "Error serializing parameters";
            }
        }
        
        private bool IsSensitiveParameterName(string paramName)
        {
            // List of common sensitive parameter names to redact
            string[] sensitiveNames = new[] {
                "password", "pwd", "secret", "token", "apikey", "api_key", 
                "credential", "ssn", "creditcard", "credit_card", "cc",
                "auth", "authentication", "key", "private"
            };
            
            return sensitiveNames.Any(s => 
                paramName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
    }
}
