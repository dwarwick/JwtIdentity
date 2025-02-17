namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SurveyController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Survey
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SurveyViewModel>>> GetSurveys()
        {
            var surveys = await _context.Surveys.Include(s => s.Questions).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<SurveyViewModel>>(surveys));
        }

        // GET: api/Survey/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SurveyViewModel>> GetSurvey(int id)
        {
            var survey = await _context.Surveys.Include(s => s.Questions).FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<SurveyViewModel>(survey));
        }

        // POST: api/Survey
        [HttpPost]
        public async Task<ActionResult<SurveyViewModel>> PostSurvey(SurveyViewModel surveyViewModel)
        {
            var survey = _mapper.Map<Survey>(surveyViewModel);
            _ = _context.Surveys.Add(survey);
            _ = await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, _mapper.Map<SurveyViewModel>(survey));
        }

        // PUT: api/Survey/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSurvey(int id, SurveyViewModel surveyViewModel)
        {
            if (id != surveyViewModel.Id)
            {
                return BadRequest();
            }

            var survey = _mapper.Map<Survey>(surveyViewModel);
            _context.Entry(survey).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SurveyExists(id))
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

        // DELETE: api/Survey/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSurvey(int id)
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey == null)
            {
                return NotFound();
            }

            _ = _context.Surveys.Remove(survey);
            _ = await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SurveyExists(int id)
        {
            return _context.Surveys.Any(e => e.Id == id);
        }
    }
}
