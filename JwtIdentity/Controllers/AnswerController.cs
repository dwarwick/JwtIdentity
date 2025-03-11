using System.Security.Claims;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnswerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IApiAuthService apiAuthService;

        public AnswerController(ApplicationDbContext context, IMapper mapper, IApiAuthService apiAuthService)
        {
            _context = context;
            _mapper = mapper;
            this.apiAuthService = apiAuthService;
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

        [HttpGet("getanswersforsurvey/{guid}")]
        public async Task<ActionResult<AnswerViewModel>> GetAnswersForSurvey(string guid)
        {
            // get the ip address of the user
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // generate code to get the usename of the user
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            int userId = apiAuthService.GetUserId(User);

            Survey survey = null;

            if ((username == "anonymous" && await _context.Answers.AnyAsync(a =>
              a.IpAddress == ipAddress &&
              a.CreatedById == userId &&
              _context.Surveys.Any(s => s.Id == a.Question.SurveyId && s.Guid == guid && s.Complete)))

              || await _context.Answers.AnyAsync(a =>
              a.CreatedById == userId &&
              _context.Surveys.Any(s => s.Id == a.Question.SurveyId && s.Guid == guid && s.Complete)))
            {
                return BadRequest("You have already taken this survey");
            }

            survey = await _context.Surveys
            .Where(s => s.Guid == guid)
            .Include(s => s.Questions).ThenInclude(q => q.Answers.Where(a => a.CreatedById == userId)).FirstOrDefaultAsync();



            if (survey == null) return BadRequest("Survey does not exist");

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

        // POST: api/Answer
        [HttpPost]
        public async Task<ActionResult<AnswerViewModel>> PostAnswer(AnswerViewModel answerViewModel)
        {
            if (answerViewModel == null) return BadRequest("Bad Request");

            var answer = _mapper.Map<Answer>(answerViewModel);

            answer.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (answer.Id == 0)
            {
                _ = _context.Answers.Add(answer);
            }
            else
            {
                var existingAnswer = await _context.Answers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == answerViewModel.Id);

                answer.CreatedById = answerViewModel.CreatedById;

                switch (answer.AnswerType)
                {
                    case AnswerType.Text:
                        if (((TextAnswer)answer).Text != ((TextAnswer)existingAnswer).Text) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.TrueFalse:
                        if (((TrueFalseAnswer)answer).Value != ((TrueFalseAnswer)existingAnswer).Value) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.SingleChoice:
                        if (((SingleChoiceAnswer)answer).SelectedOptionId != ((SingleChoiceAnswer)existingAnswer).SelectedOptionId) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.MultipleChoice:
                        if (((MultipleChoiceAnswer)answer).SelectedOptionId != ((MultipleChoiceAnswer)existingAnswer).SelectedOptionId) _ = _context.Answers.Update(answer);
                        break;
                    default:
                        break;
                }
            }

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
