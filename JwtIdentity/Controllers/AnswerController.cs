namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnswerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AnswerController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Answer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AnswerViewModel>>> GetAnswers()
        {
            var answers = await _context.Answers.Include(a => a.Question).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<AnswerViewModel>>(answers));
        }

        // GET: api/Answer/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AnswerViewModel>> GetAnswer(int id)
        {
            var answer = await _context.Answers.Include(a => a.Question).FirstOrDefaultAsync(a => a.Id == id);

            if (answer == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<AnswerViewModel>(answer));
        }

        // POST: api/Answer
        [HttpPost]
        public async Task<ActionResult<AnswerViewModel>> PostAnswer(AnswerViewModel answerViewModel)
        {
            var answer = _mapper.Map<Answer>(answerViewModel);
            _ = _context.Answers.Add(answer);
            _ = await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAnswer), new { id = answer.Id }, _mapper.Map<AnswerViewModel>(answer));
        }

        // PUT: api/Answer/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAnswer(int id, AnswerViewModel answerViewModel)
        {
            if (id != answerViewModel.Id)
            {
                return BadRequest();
            }

            var answer = _mapper.Map<Answer>(answerViewModel);
            _context.Entry(answer).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AnswerExists(id))
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

        // DELETE: api/Answer/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var answer = await _context.Answers.FindAsync(id);
            if (answer == null)
            {
                return NotFound();
            }

            _ = _context.Answers.Remove(answer);
            _ = await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AnswerExists(int id)
        {
            return _context.Answers.Any(e => e.Id == id);
        }
    }
}
