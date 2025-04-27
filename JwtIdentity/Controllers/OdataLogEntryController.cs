using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Controllers
{
    [Route("[controller]")]    
    [Authorize] // Add this attribute to require authentication
   
    public class OdataLogEntryController : ODataController
    {
        private readonly ApplicationDbContext _context;

        public OdataLogEntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: odata/logentries
        [EnableQuery]
        [HttpGet]
        public IQueryable<LogEntryViewModel> Get()
        {
            return _context.LogEntries
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
                });
        }

       /*  // GET: odata/logentry(5)
        [EnableQuery]
        [HttpGet("{id}")]
        public async Task<ActionResult<LogEntryViewModel>> Get(int id)
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

        // DELETE: odata/logentry(5)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
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

        // DELETE: odata/logentry/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllLogs()
        {
            _context.LogEntries.RemoveRange(_context.LogEntries);
            await _context.SaveChangesAsync();

            return NoContent();
        } */
    }
}