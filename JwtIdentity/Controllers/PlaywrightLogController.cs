using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaywrightLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PlaywrightLogController> _logger;

        public PlaywrightLogController(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<PlaywrightLogController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PlaywrightLogViewModel>>> GetPlaywrightLogs()
        {
            try
            {
                var logs = await _context.PlaywrightLogs
                    .OrderByDescending(log => log.ExecutedAt)
                    .ToListAsync();

                return Ok(_mapper.Map<IEnumerable<PlaywrightLogViewModel>>(logs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Playwright logs");
                return StatusCode(500, "An error occurred while retrieving Playwright logs.");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PlaywrightLogViewModel>> GetPlaywrightLog(int id)
        {
            try
            {
                var log = await _context.PlaywrightLogs.FindAsync(id);

                if (log == null)
                {
                    return NotFound();
                }

                return Ok(_mapper.Map<PlaywrightLogViewModel>(log));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Playwright log {LogId}", id);
                return StatusCode(500, "An error occurred while retrieving the Playwright log.");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<PlaywrightLogViewModel>> CreatePlaywrightLog(PlaywrightLogViewModel logViewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logViewModel.TestName))
                {
                    return BadRequest("Test name is required.");
                }

                if (string.IsNullOrWhiteSpace(logViewModel.Status))
                {
                    logViewModel.Status = "Unknown";
                }

                if (logViewModel.ExecutedAt == default)
                {
                    logViewModel.ExecutedAt = DateTime.UtcNow;
                }

                var log = _mapper.Map<PlaywrightLog>(logViewModel);

                _context.PlaywrightLogs.Add(log);
                await _context.SaveChangesAsync();

                var createdLog = _mapper.Map<PlaywrightLogViewModel>(log);
                return CreatedAtAction(nameof(GetPlaywrightLog), new { id = createdLog.Id }, createdLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Playwright log entry for test {TestName}", logViewModel.TestName);
                return StatusCode(500, "An error occurred while recording the Playwright log.");
            }
        }
    }
}
