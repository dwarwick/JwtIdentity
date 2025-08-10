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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ApplicationUserController(ApplicationDbContext context, IMapper mapper, IApiAuthService apiAuthService, ILogger<ApplicationUserController> logger, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _mapper = mapper;
            _apiAuthService = apiAuthService;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/ApplicationUser
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ApplicationUserViewModel>>> GetApplicationUsers()
        {
            _logger.LogInformation("Getting all application users");

            var users = await _context.ApplicationUsers.ToListAsync();
            var result = new List<ApplicationUserViewModel>();
            foreach (var user in users)
            {
                var vm = _mapper.Map<ApplicationUserViewModel>(user);
                vm.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                result.Add(vm);
            }

            return Ok(result);
        }

        // GET: api/ApplicationUser/5
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

                ApplicationUserViewModel applicationUserViewModel = _mapper.Map<ApplicationUserViewModel>(applicationUser);
                applicationUserViewModel.Roles = (await _userManager.GetRolesAsync(applicationUser)).ToList();
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

        // PUT: api/ApplicationUser/5
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

                if (string.IsNullOrWhiteSpace(applicationUserViewModel.UserName))
                {
                    return BadRequest("Username is required");
                }

                if (applicationUserViewModel.Roles == null || !applicationUserViewModel.Roles.Any())
                {
                    return BadRequest("At least one role is required");
                }

                if (await _context.ApplicationUsers.AnyAsync(u => u.UserName == applicationUserViewModel.UserName && u.Id != id))
                {
                    return BadRequest("Username already exists");
                }

                var applicationUser = await _context.ApplicationUsers.FindAsync(id);
                if (applicationUser == null)
                {
                    _logger.LogWarning("Application user with ID {UserId} not found", id);
                    return NotFound();
                }

                applicationUser.UserName = applicationUserViewModel.UserName;
                applicationUser.NormalizedUserName = _userManager.NormalizeName(applicationUserViewModel.UserName);
                applicationUser.Email = applicationUserViewModel.Email;
                applicationUser.NormalizedEmail = _userManager.NormalizeEmail(applicationUserViewModel.Email);
                applicationUser.PhoneNumber = applicationUserViewModel.PhoneNumber;
                applicationUser.EmailConfirmed = applicationUserViewModel.EmailConfirmed;
                applicationUser.PhoneNumberConfirmed = applicationUserViewModel.PhoneNumberConfirmed;
                applicationUser.TwoFactorEnabled = applicationUserViewModel.TwoFactorEnabled;
                applicationUser.LockoutEnd = applicationUserViewModel.LockoutEnd;
                applicationUser.LockoutEnabled = applicationUserViewModel.LockoutEnabled;
                applicationUser.AccessFailedCount = applicationUserViewModel.AccessFailedCount;
                applicationUser.Theme = applicationUserViewModel.Theme;
                applicationUser.UpdatedDate = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();

                    var currentRoles = await _userManager.GetRolesAsync(applicationUser);
                    var rolesToAdd = applicationUserViewModel.Roles.Except(currentRoles);
                    var rolesToRemove = currentRoles.Except(applicationUserViewModel.Roles);
                    if (rolesToAdd.Any())
                    {
                        await _userManager.AddToRolesAsync(applicationUser, rolesToAdd);
                    }
                    if (rolesToRemove.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(applicationUser, rolesToRemove);
                    }

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

        // POST: api/ApplicationUser
        [HttpPost]
        public async Task<ActionResult<ApplicationUserViewModel>> PostApplicationUser(ApplicationUserViewModel model)
        {
            if (model == null)
            {
                return BadRequest("User data is required");
            }

            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                return BadRequest("Username is required");
            }

            if (model.Roles == null || !model.Roles.Any())
            {
                return BadRequest("At least one role is required");
            }

            if (await _context.ApplicationUsers.AnyAsync(u => u.UserName == model.UserName))
            {
                return BadRequest("Username already exists");
            }

            var user = _mapper.Map<ApplicationUser>(model);
            user.CreatedDate = DateTime.UtcNow;
            user.UpdatedDate = DateTime.UtcNow;

            var result = await _userManager.CreateAsync(user, "mypassword");
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRolesAsync(user, model.Roles);

            var vm = _mapper.Map<ApplicationUserViewModel>(user);
            vm.Roles = model.Roles;
            return CreatedAtAction(nameof(GetApplicationUser), new { id = user.Id }, vm);
        }

        private bool ApplicationUserExists(int id)
        {
            return _context.ApplicationUsers.Any(e => e.Id == id);
        }
    }
}
