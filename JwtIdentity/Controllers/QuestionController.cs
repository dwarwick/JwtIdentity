namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public QuestionController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Question
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuestionViewModel>>> GetQuestions()
        {
            var questions = await _context.Questions.Include(q => q.Answers).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<QuestionViewModel>>(questions));
        }

        // GET: api/Question
        [HttpGet("QuestionAndOptions/{id}")]
        public async Task<ActionResult<QuestionViewModel>> GetQuestionsContainingQuestionText(int id)
        {
            Question question = await _context.Questions.FirstOrDefaultAsync(x => x.Id == id);

            if (question.QuestionType == QuestionType.MultipleChoice)
            {
                var mcQuestion = await _context.Questions.OfType<MultipleChoiceQuestion>().Include(x => x.Options).FirstOrDefaultAsync(x => x.Id == id);
                return Ok(_mapper.Map<QuestionViewModel>(mcQuestion));
            }

            return Ok(_mapper.Map<QuestionViewModel>(question));
        }

        // GET: api/Question/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QuestionViewModel>> GetQuestion(int id)
        {
            var question = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<QuestionViewModel>(question));
        }

        // POST: api/Question
        [HttpPost]
        public async Task<ActionResult<QuestionViewModel>> PostQuestion(QuestionViewModel questionViewModel)
        {
            var question = _mapper.Map<Question>(questionViewModel);
            _ = _context.Questions.Add(question);
            _ = await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, _mapper.Map<QuestionViewModel>(question));
        }

        // PUT: api/Question/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQuestion(int id, QuestionViewModel questionViewModel)
        {
            if (id != questionViewModel.Id)
            {
                return BadRequest();
            }

            var question = _mapper.Map<Question>(questionViewModel);
            _context.Entry(question).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuestionExists(id))
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

        [HttpPut("UpdateQuestionNumbers")]

        public async Task<IActionResult> UpdateQuestionNumbers(List<QuestionViewModel> questions)
        {
            int questionNumber = 1;

            foreach (var question in questions.OrderBy(x => x.QuestionNumber))
            {
                var questionToUpdate = await _context.Questions.FindAsync(question.Id);
                questionToUpdate.QuestionNumber = questionNumber;
                _context.Entry(questionToUpdate).State = EntityState.Modified;
                questionNumber++;
            }
            _ = await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Question/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // delete all choiceoptions for multiple choice questions
            if (question.QuestionType == QuestionType.MultipleChoice)
            {
                var existingMCQuestion = await _context.Questions.OfType<MultipleChoiceQuestion>().AsNoTracking().Include(x => x.Options).FirstOrDefaultAsync(q => q.Id == id);
                _context.ChoiceOptions.RemoveRange(existingMCQuestion.Options);
            }

            _ = _context.Questions.Remove(question);

            _ = await _context.SaveChangesAsync();

            List<Question> questions = await _context.Questions.Where(q => q.SurveyId == question.SurveyId).OrderBy(q => q.QuestionNumber).ToListAsync();

            questions.ForEach(q => q.QuestionNumber = questions.IndexOf(q) + 1);

            _ = await _context.SaveChangesAsync();

            return Ok("Questions Deleted");
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.Id == id);
        }
    }
}
