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
        private readonly ILogger<AnswerController> _logger;
        private readonly ISurveyCompletionNotifier _surveyNotifier;
        private readonly IQuestionTypeHandlerResolver _handlerResolver;

        public AnswerController(ApplicationDbContext context, IMapper mapper, IApiAuthService apiAuthService, ILogger<AnswerController> logger, ISurveyCompletionNotifier surveyNotifier, IQuestionTypeHandlerResolver handlerResolver)
        {
            _context = context;
            _mapper = mapper;
            this.apiAuthService = apiAuthService;
            _logger = logger;
            _surveyNotifier = surveyNotifier;
            _handlerResolver = handlerResolver;
        }

        [HttpGet("getanswersforsurveyforloggedinuser/{guid}")]
        public async Task<ActionResult<AnswerViewModel>> GetAnswersForSurveyForLoggedInUser(string guid, [FromQuery] bool Preview)
        {
            _logger.LogInformation("Getting answers for survey {SurveyGuid} for logged-in user. Preview mode: {IsPreview}", guid, Preview);
            
            try
            {
                // get the ip address of the user
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogDebug("User IP address: {IpAddress}", ipAddress);

                // generate code to get the usename of the user
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogDebug("Username: {Username}", username ?? "unknown");

                int userId = apiAuthService.GetUserId(User);
                _logger.LogDebug("User ID: {UserId}", userId);

                Survey survey = null;

                // Check if user has already taken this survey
                if (!Preview && ((username == "anonymous" &&
                    await _context.Answers
                    .Where(a =>
                        a.IpAddress == ipAddress &&
                        a.CreatedById == userId &&
                        _context.Surveys.Any(s => s.Id == a.Question.SurveyId && s.Guid == guid))
                    .GroupBy(a => a.Question.SurveyId)
                    .AnyAsync(g => g.All(a => a.Complete)))

                || await _context.Answers
                    .Where(a =>
                        a.CreatedById == userId &&
                        _context.Surveys.Any(s => s.Id == a.Question.SurveyId && s.Guid == guid))
                    .GroupBy(a => a.Question.SurveyId)
                    .AnyAsync(g => g.All(a => a.Complete))))
                {
                    _logger.LogWarning("User {UserId} has already completed survey {SurveyGuid}", userId, guid);
                    return BadRequest("You have already taken this survey");
                }

                // Get the survey with questions and user's answers
                survey = await _context.Surveys
                    .Where(s => s.Guid == guid)
                    .Include(s => s.Questions).ThenInclude(q => q.Answers.Where(a => a.CreatedById == userId))
                    .FirstOrDefaultAsync();

                if (survey == null)
                {
                    _logger.LogWarning("Survey with GUID {SurveyGuid} not found", guid);
                    return BadRequest("Survey does not exist");
                }

                _logger.LogDebug("Found survey: {SurveyId}, {SurveyTitle}", survey.Id, survey.Title);

                if (!Preview && !survey.Published)
                {
                    _logger.LogWarning("User {UserId} attempted to access unpublished survey {SurveyGuid}", userId, guid);
                    return BadRequest("This survey has not been published");
                }

                await _handlerResolver.EnsureDependenciesLoadedAsync(_context, survey.Questions);

                _logger.LogInformation("Successfully retrieved survey {SurveyGuid} with {QuestionCount} questions for user {UserId}", 
                    guid, survey.Questions.Count, userId);
                    
                return Ok(_mapper.Map<SurveyViewModel>(survey));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while retrieving survey {SurveyGuid}: {Message}", guid, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving survey {SurveyGuid}: {Message}", guid, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpGet("getsurveyresults/{guid}")]
        public async Task<ActionResult<SurveyViewModel>> GetSurveyResults(string guid)
        {
            _logger.LogInformation("Getting survey results for survey {SurveyGuid}", guid);
            
            try
            {
                var survey = await _context.Surveys
                    .Where(s => s.Guid == guid)
                    .Include(s => s.Questions).ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync();
                    
                if (survey == null)
                {
                    _logger.LogWarning("Survey with GUID {SurveyGuid} not found", guid);
                    return BadRequest("Survey does not exist");
                }
                
                _logger.LogDebug("Found survey: {SurveyId}, {SurveyTitle}", survey.Id, survey.Title);

                await _handlerResolver.EnsureDependenciesLoadedAsync(_context, survey.Questions);

                _logger.LogInformation("Successfully retrieved survey results for {SurveyGuid} with {QuestionCount} questions", 
                    guid, survey.Questions.Count);
                    
                return Ok(_mapper.Map<SurveyViewModel>(survey));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while retrieving survey results for {SurveyGuid}: {Message}", guid, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving survey results for {SurveyGuid}: {Message}", guid, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpGet("getanswersforsurveyforCharts/{guid}")]
        public async Task<ActionResult<SurveyDataViewModel>> GetAnswersForSurveyForCharts(string guid)
        {
            _logger.LogInformation("Getting chart data for survey {SurveyGuid}", guid);

            try
            {
                int userId = apiAuthService.GetUserId(User);
                _logger.LogDebug("User ID: {UserId}", userId);

                var survey = await _context.Surveys
                    .Where(s => s.Guid == guid && s.CreatedById == userId)
                    .Include(s => s.Questions).ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync();

                if (survey == null)
                {
                    _logger.LogWarning("Survey with GUID {SurveyGuid} not found for user {UserId}", guid, userId);
                    return BadRequest("Survey does not exist");
                }

                _logger.LogDebug("Found survey: {SurveyId}, {SurveyTitle}", survey.Id, survey.Title);

                if (!survey.Published)
                {
                    _logger.LogWarning("User {UserId} attempted to access unpublished survey {SurveyGuid} for charts", userId, guid);
                    return BadRequest("This survey has not been published");
                }

                await _handlerResolver.EnsureDependenciesLoadedAsync(_context, survey.Questions);

                List<SurveyDataViewModel> surveyData = new List<SurveyDataViewModel>();

                foreach (Question question in survey.Questions.OrderBy(x => x.QuestionNumber))
                {
                    _logger.LogDebug("Processing question {QuestionId} of type {QuestionType}", question.Id, question.QuestionType);

                    var handler = _handlerResolver.GetHandler(question.QuestionType);
                    SurveyDataViewModel surveyDataViewModel = new SurveyDataViewModel
                    {
                        QuestionType = question.QuestionType,
                        Question = _mapper.Map<QuestionViewModel>(question)
                    };

                    await handler.PopulateSurveyDataAsync(_context, surveyDataViewModel, question);

                    surveyData.Add(surveyDataViewModel);
                }

                _logger.LogInformation("Successfully generated chart data for survey {SurveyGuid}", guid);

                return Ok(surveyData);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while retrieving chart data for survey {SurveyGuid}: {Message}", guid, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chart data for survey {SurveyGuid}: {Message}", guid, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

// POST: api/Answer
        [HttpPost]
        public async Task<ActionResult<AnswerViewModel>> PostAnswer(AnswerViewModel answerViewModel)
        {
            _logger.LogInformation("Saving answer for question ID {QuestionId}", answerViewModel?.QuestionId);
            
            try
            {
                if (answerViewModel == null)
                {
                    _logger.LogWarning("Received null answer view model");
                    return BadRequest("Bad Request: Answer data is required");
                }

                if (answerViewModel.QuestionId <= 0)
                {
                    _logger.LogWarning("Invalid question ID {QuestionId} provided while saving answer", answerViewModel.QuestionId);
                    return BadRequest("A valid question identifier is required");
                }

                if (!await _context.Questions.AsNoTracking().AnyAsync(q => q.Id == answerViewModel.QuestionId))
                {
                    _logger.LogWarning("Question ID {QuestionId} not found while saving answer", answerViewModel.QuestionId);
                    return BadRequest("Question does not exist");
                }

                _logger.LogDebug("Mapping answer view model to domain model. Answer type: {AnswerType}", answerViewModel.AnswerType);
                var answer = _mapper.Map<Answer>(answerViewModel);

                // Record the IP address
                answer.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogDebug("Setting IP address: {IpAddress}", answer.IpAddress);

                if (answer.Id == 0)
                {
                    _logger.LogDebug("Creating new answer for question ID {QuestionId}", answer.QuestionId);
                    _ = _context.Answers.Add(answer);
                }
                else
                {
                    _logger.LogDebug("Updating existing answer ID {AnswerId}", answer.Id);
                    
                    var existingAnswer = await _context.Answers.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == answerViewModel.Id);
                        
                    if (existingAnswer == null)
                    {
                        _logger.LogWarning("Attempted to update answer ID {AnswerId} that doesn't exist", answer.Id);
                        return NotFound($"Answer with ID {answer.Id} not found");
                    }

                    answer.CreatedById = answerViewModel.CreatedById;

                    var handler = _handlerResolver.GetHandler(answer.AnswerType);
                    if (handler.ShouldUpdateAnswer(answer, existingAnswer))
                    {
                        _logger.LogDebug("Updating answer ID {AnswerId} of type {AnswerType}", answer.Id, answer.AnswerType);
                        _context.Answers.Update(answer);
                    }

                }

                await _context.SaveChangesAsync();

                if (answer.Complete)
                {
                    var surveyInfo = await (from q in _context.Questions
                                             join s in _context.Surveys on q.SurveyId equals s.Id
                                             where q.Id == answer.QuestionId
                                             select new { SurveyId = s.Id, s.Guid })
                                            .FirstOrDefaultAsync();

                    if (surveyInfo != null)
                    {
                        var completed = await _context.Answers
                            .Where(a => a.Question.SurveyId == surveyInfo.SurveyId && a.CreatedById == answer.CreatedById)
                            .GroupBy(a => a.Question.SurveyId)
                            .AnyAsync(g => g.All(a => a.Complete));

                        if (completed)
                        {
                            await _surveyNotifier.NotifySurveyCompleted(surveyInfo.Guid);
                        }
                    }
                }

                _logger.LogInformation("Successfully saved answer ID {AnswerId} for question ID {QuestionId}", answer.Id, answer.QuestionId);
                return CreatedAtAction(nameof(PostAnswer), new { id = answer.Id }, _mapper.Map<AnswerViewModel>(answer));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while saving answer for question {QuestionId}: {Message}", 
                    answerViewModel?.QuestionId, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (InvalidCastException icEx)
            {
                _logger.LogError(icEx, "Type casting error while processing answer of type {AnswerType}: {Message}", 
                    answerViewModel?.AnswerType, icEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred processing the answer data. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving answer for question {QuestionId}: {Message}", 
                    answerViewModel?.QuestionId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }
    }
}
