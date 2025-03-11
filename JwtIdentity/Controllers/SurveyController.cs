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
        [HttpGet("{guid}")]
        public async Task<ActionResult<SurveyViewModel>> GetSurvey(string guid)
        {
            var survey = await _context.Surveys.Include(s => s.Questions.OrderBy(x => x.QuestionNumber)).FirstOrDefaultAsync(s => s.Guid == guid);

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

        // GET: api/Survey/MySurveys
        [HttpGet("MySurveys")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<IEnumerable<SurveyViewModel>>> GetMySurveys()
        {
            var createdById = authService.GetUserId(User);
            var surveys = await _context.Surveys
                .Include(s => s.Questions)
                .Where(s => s.CreatedById == createdById)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<SurveyViewModel>>(surveys));
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
                _ = _context.Surveys.Update(survey);
            }

            _ = await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(PostSurvey), new { id = survey.Id }, _mapper.Map<SurveyViewModel>(survey));
        }

        // PUT: api/Survey/5
        [HttpPut]
        public async Task<IActionResult> PutSurvey(SurveyViewModel surveyViewModel)
        {
            if (surveyViewModel == null || surveyViewModel.Id == 0)
            {
                return BadRequest("Bad Request");
            }

            if (!SurveyExists(surveyViewModel.Id))
            {
                return NotFound("Survey not found");
            }

            var survey = _mapper.Map<Survey>(surveyViewModel);
            _context.Entry(survey).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();

                surveyViewModel = _mapper.Map<SurveyViewModel>(survey);
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest("Concurrency Exception");
            }

            return Ok(surveyViewModel);
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
