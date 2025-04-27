namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChoiceOptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChoiceOptionController> _logger;

        public ChoiceOptionController(ApplicationDbContext context, ILogger<ChoiceOptionController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting choice option with ID {ChoiceOptionId}", id);
            
            try
            {
                var choice = await _context.ChoiceOptions.FindAsync(id);
                if (choice == null)
                {
                    _logger.LogWarning("Choice option with ID {ChoiceOptionId} not found", id);
                    return NotFound();
                }

                _logger.LogDebug("Removing choice option with ID {ChoiceOptionId}", id);
                _ = _context.ChoiceOptions.Remove(choice);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted choice option with ID {ChoiceOptionId}", id);
                return Ok("Choice Deleted");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while deleting choice option with ID {ChoiceOptionId}: {Message}", 
                    id, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting choice option with ID {ChoiceOptionId}: {Message}", 
                    id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }
    }
}
