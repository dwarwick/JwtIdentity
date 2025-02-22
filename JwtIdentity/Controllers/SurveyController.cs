using Microsoft.AspNetCore.Authorization;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IApiAuthService authService;

        public SurveyController(ApplicationDbContext context, IMapper mapper, IApiAuthService authService)
        {
            _context = context;
            _mapper = mapper;
            this.authService = authService;
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

            // Pull out the IDs of any multiple-choice questions in memory
            var mcIds = survey.Questions
                .OfType<MultipleChoiceQuestion>()
                .Select(mc => mc.Id)
                .ToList();

            // Now load each one’s Options
            await _context.Questions
                .OfType<MultipleChoiceQuestion>()
                .Where(mc => mcIds.Contains(mc.Id))
                .Include(mc => mc.Options)
                .LoadAsync();

            return Ok(_mapper.Map<SurveyViewModel>(survey));
        }

        // POST: api/Survey
        [HttpPost]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<SurveyViewModel>> PostSurvey(SurveyViewModel surveyViewModel)
        {
            var survey = _mapper.Map<Survey>(surveyViewModel);

            if (survey == null) return BadRequest();

            int createdById = authService.GetUserId(User);

            if (survey.Id == 0)
            { // new survey
                survey.CreatedById = createdById;

                _ = _context.Surveys.Add(survey);
            }
            else
            { // existing survey
                foreach (var question in survey.Questions)
                {
                    if (question.Id == 0)
                    { // new question
                        question.CreatedById = createdById;
                        question.SurveyId = survey.Id;

                        _ = _context.Questions.Add(question);
                    }
                    else
                    { // existing question

                        // check if question text has changed. If so, update the question
                        var existingQuestion = await _context.Questions.FindAsync(question.Id);
                        if (existingQuestion != null)
                        {
                            if (existingQuestion.Text != question.Text)
                            {
                                _context.Entry(question).State = EntityState.Modified;
                            }
                        }
                    }
                }
            }

            _ = await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(PostSurvey), new { id = survey.Id }, _mapper.Map<SurveyViewModel>(survey));
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
