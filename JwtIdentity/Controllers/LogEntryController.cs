using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogEntryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LogEntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LogEntry
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LogEntryViewModel>>> GetLogEntries()
        {
            var logs = await _context.LogEntries
                .OrderByDescending(l => l.Id)
                .Select(l => new LogEntryViewModel
                {
                    Id = l.Id,
                    Message = l.Message,
                    Level = l.Level,
                    LoggedAt = l.LoggedAt,
                    RequestPath = l.RequestPath,
                    RequestMethod = l.RequestMethod,
                    Parameters = l.Parameters,
                    StatusCode = l.StatusCode,
                    ExceptionType = l.ExceptionType,
                    ExceptionMessage = l.ExceptionMessage,
                    StackTrace = l.StackTrace
                })
                .ToListAsync();

            return logs;
        }

        // GET: api/LogEntry/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LogEntryViewModel>> GetLogEntry(int id)
        {
            var logEntry = await _context.LogEntries.FindAsync(id);

            if (logEntry == null)
            {
                return NotFound();
            }

            var logViewModel = new LogEntryViewModel
            {
                Id = logEntry.Id,
                Message = logEntry.Message,
                Level = logEntry.Level,
                LoggedAt = logEntry.LoggedAt,
                RequestPath = logEntry.RequestPath,
                RequestMethod = logEntry.RequestMethod,
                Parameters = logEntry.Parameters,
                StatusCode = logEntry.StatusCode,
                ExceptionType = logEntry.ExceptionType,
                ExceptionMessage = logEntry.ExceptionMessage,
                StackTrace = logEntry.StackTrace
            };

            return logViewModel;
        }

        // DELETE: api/LogEntry/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLogEntry(int id)
        {
            var logEntry = await _context.LogEntries.FindAsync(id);
            if (logEntry == null)
            {
                return NotFound();
            }

            _context.LogEntries.Remove(logEntry);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/LogEntry/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllLogs()
        {
            _context.LogEntries.RemoveRange(_context.LogEntries);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}