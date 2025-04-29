namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionController> _logger;

        public QuestionController(ApplicationDbContext context, IMapper mapper, ILogger<QuestionController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Question
        [HttpGet("QuestionAndOptions/{id}")]
        public async Task<ActionResult<QuestionViewModel>> GetQuestionsContainingQuestionText(int id)
        {
            try
            {
                _logger.LogInformation("Fetching question with ID {QuestionId}", id);
                
                Question question = await _context.Questions.FirstOrDefaultAsync(x => x.Id == id);
                
                if (question == null)
                {
                    _logger.LogWarning("Question with ID {QuestionId} not found", id);
                    return NotFound($"Question with ID {id} not found");
                }

                _logger.LogDebug("Found question of type {QuestionType}", question.QuestionType);
                
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    var mcQuestion = await _context.Questions.OfType<MultipleChoiceQuestion>()
                        .Include(x => x.Options)
                        .FirstOrDefaultAsync(x => x.Id == id);
                        
                    _logger.LogDebug("Returning multiple choice question with {OptionCount} options", 
                        mcQuestion?.Options?.Count ?? 0);
                        
                    return Ok(_mapper.Map<QuestionViewModel>(mcQuestion));
                }
                else if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    var selectAllQuestion = await _context.Questions.OfType<SelectAllThatApplyQuestion>()
                        .Include(x => x.Options)
                        .FirstOrDefaultAsync(x => x.Id == id);
                        
                    _logger.LogDebug("Returning select-all question with {OptionCount} options", 
                        selectAllQuestion?.Options?.Count ?? 0);
                        
                    return Ok(_mapper.Map<QuestionViewModel>(selectAllQuestion));
                }

                _logger.LogDebug("Returning basic question");
                return Ok(_mapper.Map<QuestionViewModel>(question));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving question with ID {QuestionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while retrieving the question. Please try again later.");
            }
        }

        // DELETE: api/Question/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete question with ID {QuestionId}", id);
                
                // Load the question first to check if it exists
                var question = await _context.Questions.FindAsync(id);
                if (question == null)
                {
                    _logger.LogWarning("Question with ID {QuestionId} not found for deletion", id);
                    return NotFound($"Question with ID {id} not found");
                }

                int surveyId = question.SurveyId;
                _logger.LogDebug("Question belongs to survey {SurveyId}", surveyId);

                // Clear tracking of the initially loaded entity to avoid conflicts
                _context.Entry(question).State = EntityState.Detached;

                // delete all choiceoptions for multiple choice questions
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    _logger.LogDebug("Deleting options for multiple choice question {QuestionId}", id);
                    
                    // Load the question with its options in a single query
                    var mcQuestion = await _context.Questions.OfType<MultipleChoiceQuestion>()
                        .Include(x => x.Options)
                        .FirstOrDefaultAsync(q => q.Id == id);
                        
                    if (mcQuestion?.Options != null)
                    {
                        _context.ChoiceOptions.RemoveRange(mcQuestion.Options);
                        _logger.LogDebug("Removed {OptionCount} options for multiple choice question", 
                            mcQuestion.Options.Count);
                    }
                }
                else if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    _logger.LogDebug("Deleting options for select-all question {QuestionId}", id);
                    
                    // Load the question with its options in a single query
                    var selectAllQuestion = await _context.Questions.OfType<SelectAllThatApplyQuestion>()
                        .Include(x => x.Options)
                        .FirstOrDefaultAsync(q => q.Id == id);
                        
                    if (selectAllQuestion?.Options != null)
                    {
                        _context.ChoiceOptions.RemoveRange(selectAllQuestion.Options);
                        _logger.LogDebug("Removed {OptionCount} options for select-all question", 
                            selectAllQuestion.Options.Count);
                    }
                }

                // Reload the question to delete it
                question = await _context.Questions.FindAsync(id);
                _logger.LogDebug("Removing question {QuestionId}", id);
                _context.Questions.Remove(question);

                await _context.SaveChangesAsync();
                _logger.LogDebug("Question {QuestionId} deleted successfully", id);

                // Re-number the remaining questions in the survey
                _logger.LogDebug("Re-numbering remaining questions in survey {SurveyId}", surveyId);
                List<Question> questions = await _context.Questions
                    .Where(q => q.SurveyId == surveyId)
                    .OrderBy(q => q.QuestionNumber)
                    .ToListAsync();

                for (int i = 0; i < questions.Count; i++)
                {
                    questions[i].QuestionNumber = i + 1;
                }
                _logger.LogDebug("Updated question numbers for {QuestionCount} questions", questions.Count);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Question {QuestionId} successfully deleted and remaining questions re-numbered", id);

                return Ok("Question Deleted");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict when deleting question {QuestionId}", id);
                return StatusCode(StatusCodes.Status409Conflict, 
                    "The question was modified by another user. Please refresh and try again.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error when deleting question {QuestionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "A database error occurred while deleting the question. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when deleting question {QuestionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An unexpected error occurred while deleting the question. Please try again later.");
            }
        }
    }
}
