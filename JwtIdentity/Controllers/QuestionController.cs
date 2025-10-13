using JwtIdentity.Interfaces;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionController> _logger;
        private readonly IQuestionHandlerFactory _questionHandlerFactory;

        public QuestionController(ApplicationDbContext context, IMapper mapper, ILogger<QuestionController> logger, IQuestionHandlerFactory questionHandlerFactory)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _questionHandlerFactory = questionHandlerFactory;
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
                
                // Use handler to load any related data for this question type
                var handler = _questionHandlerFactory.GetHandler(question.QuestionType);
                await handler.LoadRelatedDataAsync(new List<int> { id }, _context);
                
                // Re-fetch the question to get any related data that was loaded
                question = await _context.Questions.FirstOrDefaultAsync(x => x.Id == id);
                
                _logger.LogDebug("Returning question with handler-loaded related data");
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

                // Use handler to perform question-type-specific deletion logic
                var handler = _questionHandlerFactory.GetHandler(question.QuestionType);
                await handler.HandleDeletionAsync(id, _context);

                _logger.LogDebug("Handler completed deletion logic for question {QuestionId}", id);

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

        // PUT: api/Question/UpdateGroup
        [HttpPut("UpdateGroup")]
        public async Task<IActionResult> UpdateQuestionGroup([FromBody] UpdateQuestionGroupRequest request)
        {
            try
            {
                _logger.LogInformation("Updating group for question {QuestionId} to group {GroupId}", 
                    request.QuestionId, request.GroupId);

                var question = await _context.Questions.FindAsync(request.QuestionId);
                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found", request.QuestionId);
                    return NotFound($"Question with ID {request.QuestionId} not found");
                }

                question.GroupId = request.GroupId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated question {QuestionId} to group {GroupId}", 
                    request.QuestionId, request.GroupId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question group for question {QuestionId}", 
                    request?.QuestionId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while updating the question group");
            }
        }
    }

    public class UpdateQuestionGroupRequest
    {
        public int QuestionId { get; set; }
        public int GroupId { get; set; }
    }
}
