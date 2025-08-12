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

        public AnswerController(ApplicationDbContext context, IMapper mapper, IApiAuthService apiAuthService, ILogger<AnswerController> logger, ISurveyCompletionNotifier surveyNotifier)
        {
            _context = context;
            _mapper = mapper;
            this.apiAuthService = apiAuthService;
            _logger = logger;
            _surveyNotifier = surveyNotifier;
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

                // Pull out the IDs of any multiple-choice questions in memory
                var mcIds = survey.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Select(mc => mc.Id)
                    .ToList();
                _logger.LogDebug("Found {Count} multiple choice questions", mcIds.Count);

                // Pull out the IDs of any select-all-that-apply questions in memory
                var selectAllIds = survey.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Select(sa => sa.Id)
                    .ToList();
                _logger.LogDebug("Found {Count} select-all-that-apply questions", selectAllIds.Count);

                // Now load each one's Options
                await _context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(mc => mcIds.Contains(mc.Id))
                    .Include(mc => mc.Options)
                    .LoadAsync();
                    
                // Now load each select-all-that-apply question's Options
                await _context.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Where(sa => selectAllIds.Contains(sa.Id))
                    .Include(sa => sa.Options)
                    .LoadAsync();

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

                // Pull out the IDs of any multiple-choice questions in memory
                var mcIds = survey.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Select(mc => mc.Id)
                    .ToList();
                _logger.LogDebug("Found {Count} multiple choice questions", mcIds.Count);
                    
                // Pull out the IDs of any select-all-that-apply questions in memory
                var selectAllIds = survey.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Select(sa => sa.Id)
                    .ToList();
                _logger.LogDebug("Found {Count} select-all-that-apply questions", selectAllIds.Count);

                // Now load each one's Options
                await _context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(mc => mcIds.Contains(mc.Id))
                    .Include(mc => mc.Options)
                    .LoadAsync();
                    
                // Now load each select-all-that-apply question's Options
                await _context.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Where(sa => selectAllIds.Contains(sa.Id))
                    .Include(sa => sa.Options)
                    .LoadAsync();

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

                Survey survey = null;

                survey = await _context.Surveys
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

                // Pull out the IDs of any multiple-choice questions in memory
                var mcIds = survey.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Select(mc => mc.Id)
                    .ToList();
                _logger.LogDebug("Found {Count} multiple choice questions", mcIds.Count);
                    
                // Pull out the IDs of any select-all-that-apply questions in memory
                var selectAllIds = survey.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Select(sa => sa.Id)
                    .ToList();
                _logger.LogDebug("Found {Count} select-all-that-apply questions", selectAllIds.Count);

                // Now load each one's Options
                await _context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(mc => mcIds.Contains(mc.Id))
                    .Include(mc => mc.Options)
                    .LoadAsync();
                    
                // Now load each select-all-that-apply question's Options
                await _context.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Where(sa => selectAllIds.Contains(sa.Id))
                    .Include(sa => sa.Options)
                    .LoadAsync();

                List<SurveyDataViewModel> surveyData = new List<SurveyDataViewModel>();

                foreach (Question question in survey.Questions.OrderBy(x => x.QuestionNumber))
                {
                    _logger.LogDebug("Processing question {QuestionId} of type {QuestionType}", question.Id, question.QuestionType);
                    SurveyDataViewModel surveyDataViewModel = new SurveyDataViewModel() { QuestionType = question.QuestionType, Question = _mapper.Map<QuestionViewModel>(question) };

                    switch (question.QuestionType)
                    {
                        case QuestionType.Text:
                            surveyDataViewModel.SurveyData = new List<ChartData>() { new ChartData() { X = "Text", Y = question.Answers.Count } };
                            surveyDataViewModel.TextQuestion = _mapper.Map<TextQuestionViewModel>(question);
                            break;
                        case QuestionType.TrueFalse:
                            // add to the chart series the count of true answers and the number of false answers
                            // get the question.Answers as TrueFalseAnswer

                            var trueFalseAnswers = await _context.Answers.OfType<TrueFalseAnswer>().AsNoTracking().Where(a => a.QuestionId == question.Id).ToListAsync();
                            _logger.LogDebug("Found {Count} true/false answers for question {QuestionId}", trueFalseAnswers.Count, question.Id);

                            surveyDataViewModel.SurveyData = new List<ChartData>() { new ChartData() { X = "True", Y = trueFalseAnswers.Count(a => a.Value == true) }, new ChartData() { X = "False", Y = trueFalseAnswers.Count(a => a.Value == false) } };
                            surveyDataViewModel.TrueFalseQuestion = _mapper.Map<TrueFalseQuestionViewModel>(question);
                            break;
                        case QuestionType.Rating1To10:
                            // add to the chart series the count of each rating
                            // get the question.Answers as Rating1To10Answer
                            var ratingAnswers = await _context.Answers.OfType<Rating1To10Answer>().AsNoTracking().Where(a => a.QuestionId == question.Id).ToListAsync();
                            _logger.LogDebug("Found {Count} rating answers for question {QuestionId}", ratingAnswers.Count, question.Id);
                            
                            var ratingGroups = ratingAnswers.GroupBy(a => a.SelectedOptionId).ToDictionary(g => g.Key, g => g.Count());
                            surveyDataViewModel.SurveyData = Enumerable.Range(1, 10).Select(i => new ChartData { X = i.ToString(), Y = ratingGroups.ContainsKey(i) ? ratingGroups[i] : 0 }).ToList();
                            surveyDataViewModel.Rating1To10Question = _mapper.Map<Rating1To10QuestionViewModel>(question);
                            break;
                        case QuestionType.MultipleChoice:
                            // Retrieve the multiple-choice question with its options
                            var mcQuestion = await _context.Questions.OfType<MultipleChoiceQuestion>()
                                .AsNoTracking()
                                .Where(a => a.Id == question.Id)
                                .Include(x => x.Options)
                                .FirstOrDefaultAsync();

                            // Retrieve the answers for the multiple-choice question
                            var mcAnswers = await _context.Answers.OfType<MultipleChoiceAnswer>()
                                .AsNoTracking()
                                .Where(a => a.QuestionId == question.Id)
                                .ToListAsync();
                            _logger.LogDebug("Found {Count} multiple choice answers for question {QuestionId}", mcAnswers.Count, question.Id);

                            // Group the answers by the selected option ID
                            var answerGroups = mcAnswers.GroupBy(a => a.SelectedOptionId)
                                .ToDictionary(g => g.Key, g => g.Count());

                            // Create a list of ChartData that includes all options, ordered by the Order field
                            surveyDataViewModel.SurveyData = mcQuestion.Options
                                .OrderBy(o => o.Order)
                                .Select(o => new ChartData
                                {
                                    X = o.OptionText,
                                    Y = answerGroups.ContainsKey(o.Id) ? answerGroups[o.Id] : 0
                                })
                                .ToList();

                            surveyDataViewModel.MultipleChoiceQuestion = _mapper.Map<MultipleChoiceQuestionViewModel>(mcQuestion);
                            break;
                        case QuestionType.SelectAllThatApply:
                            // Retrieve the select-all-that-apply question with its options
                            var saQuestion = await _context.Questions.OfType<SelectAllThatApplyQuestion>()
                                .AsNoTracking()
                                .Where(a => a.Id == question.Id)
                                .Include(x => x.Options)
                                .FirstOrDefaultAsync();

                            // Retrieve all the answers for this question
                            var saAnswers = await _context.Answers.OfType<SelectAllThatApplyAnswer>()
                                .AsNoTracking()
                                .Where(a => a.QuestionId == question.Id)
                                .ToListAsync();
                            _logger.LogDebug("Found {Count} select-all-that-apply answers for question {QuestionId}", saAnswers.Count, question.Id);

                            // Initialize a dictionary to count selections for each option
                            var optionSelectionCounts = saQuestion.Options.ToDictionary(o => o.Id, _ => 0);

                            // Count selections for each option across all answers
                            foreach (var saAnswer in saAnswers)
                            {
                                if (!string.IsNullOrEmpty(saAnswer.SelectedOptionIds))
                                {
                                    var selectedIds = saAnswer.SelectedOptionIds.Split(',').Select(int.Parse);
                                    foreach (var id in selectedIds)
                                    {
                                        if (optionSelectionCounts.ContainsKey(id))
                                        {
                                            optionSelectionCounts[id]++;
                                        }
                                    }
                                }
                            }

                            // Create chart data for each option
                            surveyDataViewModel.SurveyData = saQuestion.Options
                                .OrderBy(o => o.Order)
                                .Select(o => new ChartData
                                {
                                    X = o.OptionText,
                                    Y = optionSelectionCounts[o.Id]
                                })
                                .ToList();

                            surveyDataViewModel.SelectAllThatApplyQuestion = _mapper.Map<SelectAllThatApplyQuestionViewModel>(saQuestion);
                            break;

                        default:
                            _logger.LogWarning("Unknown question type {QuestionType} for question {QuestionId}", question.QuestionType, question.Id);
                            break;
                    }

                    surveyData.Add(surveyDataViewModel);
                }

                _logger.LogInformation("Successfully generated chart data for survey {SurveyGuid} with {QuestionCount} questions for user {UserId}", 
                    guid, survey.Questions.Count, userId);
                    
                return Ok(surveyData);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while retrieving chart data for survey {SurveyGuid}: {Message}", guid, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Please try again later.");
            }
            catch (FormatException fEx)
            {
                _logger.LogError(fEx, "Format error while processing select-all-that-apply answers for survey {SurveyGuid}: {Message}", guid, fEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred processing the survey data. Please try again later.");
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

                    switch (answer.AnswerType)
                    {
                        case AnswerType.Text:
                            if (((TextAnswer)answer).Text != ((TextAnswer)existingAnswer).Text || 
                                ((TextAnswer)answer).Complete != ((TextAnswer)existingAnswer).Complete)
                            {
                                _logger.LogDebug("Updating text answer ID {AnswerId}", answer.Id);
                                _ = _context.Answers.Update(answer);
                            }
                            break;
                        case AnswerType.TrueFalse:
                            if (((TrueFalseAnswer)answer).Value != ((TrueFalseAnswer)existingAnswer).Value || 
                                ((TrueFalseAnswer)answer).Complete != ((TrueFalseAnswer)existingAnswer).Complete)
                            {
                                _logger.LogDebug("Updating true/false answer ID {AnswerId}", answer.Id);
                                _ = _context.Answers.Update(answer);
                            }
                            break;
                        case AnswerType.SingleChoice:
                            if (((SingleChoiceAnswer)answer).SelectedOptionId != ((SingleChoiceAnswer)existingAnswer).SelectedOptionId || 
                                ((SingleChoiceAnswer)answer).Complete != ((SingleChoiceAnswer)existingAnswer).Complete)
                            {
                                _logger.LogDebug("Updating single choice answer ID {AnswerId}", answer.Id);
                                _ = _context.Answers.Update(answer);
                            }
                            break;
                        case AnswerType.MultipleChoice:
                            if (((MultipleChoiceAnswer)answer).SelectedOptionId != ((MultipleChoiceAnswer)existingAnswer).SelectedOptionId || 
                                ((MultipleChoiceAnswer)answer).Complete != ((MultipleChoiceAnswer)existingAnswer).Complete)
                            {
                                _logger.LogDebug("Updating multiple choice answer ID {AnswerId}", answer.Id);
                                _ = _context.Answers.Update(answer);
                            }
                            break;
                        case AnswerType.Rating1To10:
                            if (((Rating1To10Answer)answer).SelectedOptionId != ((Rating1To10Answer)existingAnswer).SelectedOptionId || 
                                ((Rating1To10Answer)answer).Complete != ((Rating1To10Answer)existingAnswer).Complete)
                            {
                                _logger.LogDebug("Updating rating answer ID {AnswerId}", answer.Id);
                                _ = _context.Answers.Update(answer);
                            }
                            break;
                        case AnswerType.SelectAllThatApply:
                            if (((SelectAllThatApplyAnswer)answer).SelectedOptionIds != ((SelectAllThatApplyAnswer)existingAnswer).SelectedOptionIds || 
                                ((SelectAllThatApplyAnswer)answer).Complete != ((SelectAllThatApplyAnswer)existingAnswer).Complete)
                            {
                                _logger.LogDebug("Updating select-all answer ID {AnswerId}", answer.Id);
                                _ = _context.Answers.Update(answer);
                            }
                            break;
                        default:
                            _logger.LogWarning("Unknown answer type {AnswerType} for answer ID {AnswerId}", answer.AnswerType, answer.Id);
                            break;
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
