namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChoiceOptionController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;

        public ChoiceOptionController(IMapper mapper, ApplicationDbContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateChoiceOption([FromBody] ChoiceOptionViewModel choiceOptionViewModel)
        {
            var choiceOption = _mapper.Map<ChoiceOption>(choiceOptionViewModel);
            _ = _context.ChoiceOptions.Add(choiceOption);
            _ = await _context.SaveChangesAsync();
            var createdChoiceOptionViewModel = _mapper.Map<ChoiceOptionViewModel>(choiceOption);
            return CreatedAtAction(nameof(GetChoiceOptionById), new { id = createdChoiceOptionViewModel.Id }, createdChoiceOptionViewModel);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChoiceOptionById(int id)
        {
            var choiceOption = await _context.ChoiceOptions.FindAsync(id);
            if (choiceOption == null)
            {
                return NotFound();
            }
            var choiceOptionViewModel = _mapper.Map<ChoiceOptionViewModel>(choiceOption);
            return Ok(choiceOptionViewModel);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChoiceOption(int id, [FromBody] ChoiceOptionViewModel choiceOptionViewModel)
        {
            if (id != choiceOptionViewModel.Id)
            {
                return BadRequest();
            }
            var choiceOption = _mapper.Map<ChoiceOption>(choiceOptionViewModel);
            _context.Entry(choiceOption).State = EntityState.Modified;
            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChoiceOptionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            var updatedChoiceOptionViewModel = _mapper.Map<ChoiceOptionViewModel>(choiceOption);
            return Ok(updatedChoiceOptionViewModel);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChoiceOption(int id)
        {
            var choiceOption = await _context.ChoiceOptions.FindAsync(id);
            if (choiceOption == null)
            {
                return NotFound();
            }
            _ = _context.ChoiceOptions.Remove(choiceOption);
            _ = await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool ChoiceOptionExists(int id)
        {
            return _context.ChoiceOptions.Any(e => e.Id == id);
        }
    }
}
