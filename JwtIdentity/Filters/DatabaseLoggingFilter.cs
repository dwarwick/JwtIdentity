using Microsoft.AspNetCore.Mvc.Filters;

namespace JwtIdentity.Filters
{
    public class DatabaseLoggingFilter : IActionFilter, IExceptionFilter
    {
        private readonly ApplicationDbContext _context;

        public DatabaseLoggingFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _context.LogEntries.Add(new LogEntry
            {
                Message = $"Controller: {context.ActionDescriptor.DisplayName} started",
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
            _context.LogEntries.Add(new LogEntry
            {
                Message = $"Exception: {context.Exception.Message}",
                Level = "Error",
                LoggedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }
    }
}
