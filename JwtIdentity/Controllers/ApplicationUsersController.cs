namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IApiAuthService _apiAuthService;
        private readonly ILogger<ApplicationUserController> _logger;

        public ApplicationUserController(ApplicationDbContext context, IMapper mapper, IApiAuthService apiAuthService, ILogger<ApplicationUserController> logger)
        {
            _context = context;
            _mapper = mapper;
            _apiAuthService = apiAuthService;
            _logger = logger;
        }

        // GET: api/ApplicationUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApplicationUserViewModel>> GetApplicationUser(int id)
        {
            _logger.LogInformation("Getting application user with ID {UserId}", id);
            
            try
            {
                var applicationUser = await _context.ApplicationUsers.FindAsync(id);

                if (applicationUser == null)
                {
                    _logger.LogWarning("Application user with ID {UserId} not found", id);
                    return NotFound();
                }

                _logger.LogDebug("Found user: {UserId}, {UserName}", applicationUser.Id, applicationUser.UserName);

                // Map the ApplicationUser to ApplicationUserViewModel
                ApplicationUserViewModel applicationUserViewModel = _mapper.Map<ApplicationUserViewModel>(applicationUser);
                applicationUserViewModel.Roles = await _apiAuthService.GetUserRoles(User);
                applicationUserViewModel.Permissions = _apiAuthService.GetUserPermissions(User);

                _logger.LogInformation("Successfully retrieved user with ID {UserId}", id);
                return Ok(applicationUserViewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while retrieving user {UserId}: {Message}", id, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        // PUT: api/ApplicationUsers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutApplicationUser(int id, ApplicationUserViewModel applicationUserViewModel)
        {
            _logger.LogInformation("Updating application user with ID {UserId}", id);
            
            try
            {
                if (applicationUserViewModel == null)
                {
                    _logger.LogWarning("Received null application user view model for ID {UserId}", id);
                    return BadRequest("User data is required");
                }
                
                if (id != applicationUserViewModel.Id)
                {
                    _logger.LogWarning("ID mismatch: URL ID {UrlId} does not match model ID {ModelId}", id, applicationUserViewModel.Id);
                    return BadRequest("ID in URL must match ID in the request body");
                }

                var applicationUser = await _context.ApplicationUsers.FindAsync(id);
                if (applicationUser == null)
                {
                    _logger.LogWarning("Application user with ID {UserId} not found", id);
                    return NotFound();
                }

                _logger.LogDebug("Found user to update: {UserId}, {UserName}", applicationUser.Id, applicationUser.UserName);

                // Map updated fields from the view model to the existing entity
                _mapper.Map(applicationUserViewModel, applicationUser);
                applicationUser.UpdatedDate = DateTime.UtcNow;
                _logger.LogDebug("Updated user properties and set UpdatedDate to {UpdatedDate}", applicationUser.UpdatedDate);

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated user with ID {UserId}", id);
                    return NoContent();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ApplicationUserExists(id))
                    {
                        _logger.LogWarning("Application user with ID {UserId} no longer exists", id);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error occurred while updating user {UserId}: {Message}", id, ex.Message);
                        throw;
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while updating user {UserId}: {Message}", id, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        private bool ApplicationUserExists(int id)
        {
            return _context.ApplicationUsers.Any(e => e.Id == id);
        }
    }
}
