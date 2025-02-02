namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ApplicationUserController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/ApplicationUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ApplicationUser>>> GetApplicationUsers()
        {
            return await _context.ApplicationUsers.ToListAsync();
        }

        // GET: api/ApplicationUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApplicationUserViewModel>> GetApplicationUser(int id)
        {
            var applicationUser = await _context.ApplicationUsers.FindAsync(id);

            if (applicationUser == null)
            {
                return NotFound();
            }

            return _mapper.Map<ApplicationUserViewModel>(applicationUser);
        }

        // PUT: api/ApplicationUsers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutApplicationUser(int id, ApplicationUserViewModel applicationUserViewModel)
        {
            if (applicationUserViewModel == null || id != applicationUserViewModel.Id)
            {
                return BadRequest();
            }

            ApplicationUser applicationUser = _mapper.Map<ApplicationUser>(applicationUserViewModel);

            _context.Entry(applicationUser).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApplicationUserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ApplicationUsers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ApplicationUser>> PostApplicationUser(ApplicationUser applicationUser)
        {
            _ = _context.ApplicationUsers.Add(applicationUser);
            _ = await _context.SaveChangesAsync();

            return CreatedAtAction("GetApplicationUser", new { id = applicationUser.Id }, applicationUser);
        }

        // DELETE: api/ApplicationUsers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApplicationUser(int id)
        {
            var applicationUser = await _context.ApplicationUsers.FindAsync(id);
            if (applicationUser == null)
            {
                return NotFound();
            }

            _ = _context.ApplicationUsers.Remove(applicationUser);
            _ = await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ApplicationUserExists(int id)
        {
            return _context.ApplicationUsers.Any(e => e.Id == id);
        }
    }
}
