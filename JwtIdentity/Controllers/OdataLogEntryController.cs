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
    // Keep the controller exactly like OdataQuestionController
    [Route("[controller]")]    
    [Authorize]
    public class OdataLogEntryController : ODataController
    {
        private readonly ApplicationDbContext _context;

        public OdataLogEntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Keep method signature identical to working controller
        [EnableQuery]
        [HttpGet]
        public IQueryable<LogEntryViewModel> Get()
        {
            // Keep the basic query structure
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
                    StackTrace = l.StackTrace,
                    // Include new fields
                    Controller = l.Controller,
                    Action = l.Action,
                    IpAddress = l.IpAddress,
                    UserName = l.UserName,
                    Status = l.Status
                });
        }

        // Remove additional methods for now to simplify
    }
}