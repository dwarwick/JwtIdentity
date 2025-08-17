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
        private readonly ILogger<SurveyController> _logger;
        private readonly IOpenAi _openAiService;

        public SurveyController(ApplicationDbContext context, IMapper mapper, IApiAuthService authService, ILogger<SurveyController> logger, IOpenAi openAiService)
        {
            _context = context;
            _mapper = mapper;
            this.authService = authService;
            _logger = logger;
            _openAiService = openAiService;
        }

        // GET: api/Survey/5
        [HttpGet("{guid}")]
        public async Task<ActionResult<SurveyViewModel>> GetSurvey(string guid)
        {
            try
            {
                _logger.LogInformation("Retrieving survey with GUID: {Guid}", guid);

                var survey = await _context.Surveys.Include(s => s.Questions.OrderBy(x => x.QuestionNumber)).FirstOrDefaultAsync(s => s.Guid == guid);

                if (survey == null)
                {
                    _logger.LogWarning("Survey with GUID {Guid} not found", guid);
                    return NotFound();
                }

                // Pull out the IDs of any multiple-choice questions in memory
                var mcIds = survey.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Select(mc => mc.Id)
                    .ToList();

                // Now load each one's Options
                await _context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(mc => mcIds.Contains(mc.Id))
                    .Include(mc => mc.Options.OrderBy(o => o.Order))
                    .LoadAsync();

                var allIds = survey.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Select(mc => mc.Id)
                    .ToList();

                // Now load each one's Options
                await _context.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Where(mc => allIds.Contains(mc.Id))
                    .Include(mc => mc.Options.OrderBy(o => o.Order))
                    .LoadAsync();

                _logger.LogInformation("Successfully retrieved survey with GUID {Guid}, title: {Title}", guid, survey.Title);
                return Ok(_mapper.Map<SurveyViewModel>(survey));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving survey with GUID {Guid}", guid);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the survey");
            }
        }

        // GET: api/Survey/MySurveys
        [HttpGet("surveysicreated")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<IEnumerable<SurveyViewModel>>> GetSurveysICreated()
        {
            try
            {
                var createdById = authService.GetUserId(User);
                if (createdById == 0)
                {
                    _logger.LogWarning("Unauthorized attempt to access created surveys");
                    return Unauthorized();
                }

                _logger.LogInformation("Retrieving surveys created by user {UserId}", createdById);

                var surveys = await _context.Surveys
                    .Include(s => s.Questions.OrderBy(q => q.QuestionNumber))
                    .Where(s => s.CreatedById == createdById)
                    .ToListAsync();

                // Map to view models
                var surveyViewModels = _mapper.Map<IEnumerable<SurveyViewModel>>(surveys).ToList();

                // For each survey, get the count of unique users who have completed the survey
                for (int i = 0; i < surveys.Count; i++)
                {
                    // Query to count distinct users who have completed answers for this survey
                    var responseCount = await _context.Answers
                        .Where(a => a.Question.SurveyId == surveys[i].Id && a.Complete)
                        .Select(a => a.CreatedById)
                        .Distinct()
                        .CountAsync();

                    // Assign the count to the corresponding view model
                    surveyViewModels[i].NumberOfResponses = responseCount;
                }

                _logger.LogInformation("Retrieved {Count} surveys created by user {UserId}", surveys.Count, createdById);
                return Ok(surveyViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving surveys created by user");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving your surveys");
            }
        }

        [HttpGet("surveysianswered")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<IEnumerable<SurveyViewModel>>> GetSurveysIAnswered()
        {
            try
            {
                var createdById = authService.GetUserId(User);
                if (createdById == 0)
                {
                    _logger.LogWarning("Unauthorized attempt to access answered surveys");
                    return Unauthorized();
                }

                _logger.LogInformation("Retrieving surveys answered by user {UserId}", createdById);

                var surveys = await _context.Surveys
                    .Include(s => s.Questions.OrderBy(q => q.QuestionNumber)).ThenInclude(q => q.Answers.Where(a => a.CreatedById == createdById))
                    .Where(s => s.Questions.Any(q => q.Answers.Any(a => a.CreatedById == createdById)))
                    .ToListAsync();

                // Map to view models
                var surveyViewModels = _mapper.Map<IEnumerable<SurveyViewModel>>(surveys).ToList();

                // For each survey, get the count of unique users who have completed the survey
                for (int i = 0; i < surveys.Count; i++)
                {
                    // Query to count distinct users who have completed answers for this survey
                    var responseCount = await _context.Answers
                        .Where(a => a.Question.SurveyId == surveys[i].Id && a.Complete)
                        .Select(a => a.CreatedById)
                        .Distinct()
                        .CountAsync();

                    // Assign the count to the corresponding view model
                    surveyViewModels[i].NumberOfResponses = responseCount;
                }

                _logger.LogInformation("Retrieved {Count} surveys answered by user {UserId}", surveys.Count, createdById);
                return Ok(surveyViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving surveys answered by user");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the surveys you've answered");
            }
        }

        // POST: api/Survey
        [HttpPost]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<SurveyViewModel>> PostSurvey(SurveyViewModel surveyViewModel)
        {
            try
            {
                int createdById = authService.GetUserId(User);
                if (createdById == 0)
                {
                    _logger.LogWarning("Unauthorized attempt to create or update survey");
                    return Unauthorized();
                }

                _logger.LogInformation("Processing survey creation/update request from user {UserId}", createdById);

                // If no questions, generate them using OpenAI
                if (surveyViewModel.UseAi
                    && (surveyViewModel.Questions == null || !surveyViewModel.Questions.Any())
                    && !string.IsNullOrWhiteSpace(surveyViewModel.Description))
                {
                    var generatedSurvey = await _openAiService.GenerateSurveyAsync(surveyViewModel.Description, surveyViewModel.AiInstructions);
                    if (generatedSurvey != null && generatedSurvey.Questions != null && generatedSurvey.Questions.Any())
                    {
                        surveyViewModel.Questions = generatedSurvey.Questions;
                    }
                }

                var survey = _mapper.Map<Survey>(surveyViewModel);

                if (survey == null)
                {
                    _logger.LogWarning("Invalid survey data submitted by user {UserId}", createdById);
                    return BadRequest();
                }

                if (survey.Id == 0)
                { // new survey
                    _logger.LogInformation("Creating new survey with title: {Title}", survey.Title);
                    survey.CreatedById = createdById;
                    survey.Guid = survey.Guid ?? Guid.NewGuid().ToString();
                    _ = _context.Surveys.Add(survey);
                }
                else
                { // existing survey
                    _logger.LogInformation("Updating existing survey with ID: {SurveyId}", survey.Id);

                    // check if survey title or description has changed. If so, update the survey
                    var existingSurvey = await _context.Surveys.FindAsync(survey.Id);

                    if (existingSurvey == null)
                    {
                        _logger.LogWarning("Survey with ID {SurveyId} not found for update", survey.Id);
                        return NotFound();
                    }

                    // Verify the user owns this survey
                    if (existingSurvey.CreatedById != createdById)
                    {
                        _logger.LogWarning("User {UserId} attempted to update survey {SurveyId} owned by user {OwnerId}",
                            createdById, survey.Id, existingSurvey.CreatedById);
                        return Forbid();
                    }

                    if (existingSurvey.Title != survey.Title || existingSurvey.Description != survey.Description)
                    {
                        existingSurvey.Title = survey.Title;
                        existingSurvey.Description = survey.Description;
                        _ = _context.Surveys.Update(existingSurvey);
                    }

                    foreach (var passedInQuestion in survey.Questions)
                    {
                        if (passedInQuestion.Id == 0)
                        { // new question
                            _logger.LogDebug("Adding new question to survey {SurveyId}", survey.Id);
                            passedInQuestion.CreatedById = createdById;
                            passedInQuestion.SurveyId = survey.Id;

                            _ = _context.Questions.Add(passedInQuestion);
                        }
                        else
                        { // existing question
                            _logger.LogDebug("Updating existing question {QuestionId} in survey {SurveyId}",
                                passedInQuestion.Id, survey.Id);

                            // check if question text has changed. If so, update the question
                            switch (passedInQuestion.QuestionType)
                            {
                                case QuestionType.Text:
                                    var existingTextQuestion = await _context.Questions.OfType<TextQuestion>().FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);

                                    if (existingTextQuestion != null && (existingTextQuestion.Text != passedInQuestion.Text
                                            || passedInQuestion.QuestionNumber != existingTextQuestion.QuestionNumber))
                                    {
                                        existingTextQuestion.Text = passedInQuestion.Text;
                                        existingTextQuestion.QuestionNumber = passedInQuestion.QuestionNumber;
                                        existingTextQuestion.IsRequired = passedInQuestion.IsRequired;

                                        _ = _context.Questions.Update(existingTextQuestion);
                                    }

                                    break;
                                case QuestionType.TrueFalse:
                                    var existingTrueFalseQuestion = await _context.Questions.OfType<TrueFalseQuestion>().FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);
                                    existingTrueFalseQuestion.Text = passedInQuestion.Text;
                                    existingTrueFalseQuestion.QuestionNumber = passedInQuestion.QuestionNumber;
                                    existingTrueFalseQuestion.IsRequired = passedInQuestion.IsRequired;

                                    _ = _context.Questions.Update(existingTrueFalseQuestion);
                                    break;
                                case QuestionType.Rating1To10:
                                    var existingRatingQuestion = await _context.Questions.OfType<Rating1To10Question>().FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);
                                    existingRatingQuestion.Text = passedInQuestion.Text;
                                    existingRatingQuestion.QuestionNumber = passedInQuestion.QuestionNumber;
                                    existingRatingQuestion.IsRequired = passedInQuestion.IsRequired;

                                    _ = _context.Questions.Update(existingRatingQuestion);
                                    break;
                                case QuestionType.MultipleChoice:
                                    var existingMCQuestion = await _context.Questions
                                        .OfType<MultipleChoiceQuestion>()
                                        .Include(x => x.Options)
                                        .FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);

                                    if (existingMCQuestion != null && (existingMCQuestion.Text != passedInQuestion.Text
                                            || passedInQuestion.QuestionNumber != existingMCQuestion.QuestionNumber || existingMCQuestion.IsRequired != passedInQuestion.IsRequired))
                                    {
                                        existingMCQuestion.Text = passedInQuestion.Text;
                                        existingMCQuestion.QuestionNumber = passedInQuestion.QuestionNumber;
                                        existingMCQuestion.IsRequired = passedInQuestion.IsRequired;

                                        _ = _context.Questions.Update(existingMCQuestion);
                                    }

                                    var newMCQuestion = passedInQuestion as MultipleChoiceQuestion;

                                    if (existingMCQuestion != null && newMCQuestion != null)
                                    {
                                        // check if any options have changed
                                        foreach (var newOption in newMCQuestion.Options ?? new List<ChoiceOption>())
                                        {
                                            if (newOption.Id == 0)
                                            { // new option

                                                newOption.MultipleChoiceQuestionId = passedInQuestion.Id;
                                                _ = _context.ChoiceOptions.Add(newOption);
                                            }
                                            else
                                            { // existing option
                                                var existingOption = existingMCQuestion.Options.FirstOrDefault(o => o.Id == newOption.Id);

                                                if (existingOption != null && (existingOption.OptionText != newOption.OptionText || existingOption.Order != newOption.Order))
                                                {
                                                    existingOption.OptionText = newOption.OptionText;
                                                    existingOption.Order = newOption.Order;
                                                    _ = _context.ChoiceOptions.Update(existingOption);
                                                }
                                            }
                                        }

                                        // remove any options that are no longer present
                                        var newOptionIds = (newMCQuestion.Options ?? new List<ChoiceOption>())
                                            .Where(o => o.Id != 0)
                                            .Select(o => o.Id)
                                            .ToHashSet();

                                        var removedOptions = existingMCQuestion.Options
                                            .Where(o => !newOptionIds.Contains(o.Id))
                                            .ToList();

                                        if (removedOptions.Any())
                                        {
                                            _context.ChoiceOptions.RemoveRange(removedOptions);
                                        }
                                    }
                                    break;

                                case QuestionType.SelectAllThatApply:
                                    var existingSAQuestion = await _context.Questions
                                        .OfType<SelectAllThatApplyQuestion>()
                                        .Include(x => x.Options)
                                        .FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);

                                    if (existingSAQuestion != null && (existingSAQuestion.Text != passedInQuestion.Text
                                            || passedInQuestion.QuestionNumber != existingSAQuestion.QuestionNumber || existingSAQuestion.IsRequired != passedInQuestion.IsRequired))
                                    {
                                        existingSAQuestion.Text = passedInQuestion.Text;
                                        existingSAQuestion.QuestionNumber = passedInQuestion.QuestionNumber;
                                        existingSAQuestion.IsRequired = passedInQuestion.IsRequired;

                                        _ = _context.Questions.Update(existingSAQuestion);
                                    }

                                    var newSAQuestion = passedInQuestion as SelectAllThatApplyQuestion;

                                    if (existingSAQuestion != null && newSAQuestion != null)
                                    {
                                        // check if any options have changed
                                        foreach (var newOption in newSAQuestion.Options ?? new List<ChoiceOption>())
                                        {
                                            if (newOption.Id == 0)
                                            { // new option
                                                newOption.SelectAllThatApplyQuestionId = passedInQuestion.Id;
                                                _ = _context.ChoiceOptions.Add(newOption);
                                            }
                                            else
                                            { // existing option
                                                var existingOption = existingSAQuestion.Options.FirstOrDefault(o => o.Id == newOption.Id);

                                                if (existingOption != null && (existingOption.OptionText != newOption.OptionText || existingOption.Order != newOption.Order))
                                                {
                                                    existingOption.OptionText = newOption.OptionText;
                                                    existingOption.Order = newOption.Order;
                                                    _ = _context.ChoiceOptions.Update(existingOption);
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved survey {SurveyId} with title: {Title}", survey.Id, survey.Title);
                return CreatedAtAction(nameof(PostSurvey), new { id = survey.Id }, _mapper.Map<SurveyViewModel>(survey));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while saving survey: {Message}", dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred while saving the survey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing survey submission");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your survey submission");
            }
        }

        // PUT: api/Survey
        [HttpPut]
        public async Task<IActionResult> PutSurvey(SurveyViewModel surveyViewModel)
        {
            try
            {
                if (surveyViewModel == null || surveyViewModel.Id == 0)
                {
                    _logger.LogWarning("Bad request: Invalid survey data for PUT operation");
                    return BadRequest("Bad Request");
                }

                if (!SurveyExists(surveyViewModel.Id))
                {
                    _logger.LogWarning("Survey not found for update: {SurveyId}", surveyViewModel.Id);
                    return NotFound("Survey not found");
                }

                _logger.LogInformation("Updating survey with ID: {SurveyId}", surveyViewModel.Id);

                var survey = await _context.Surveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == surveyViewModel.Id);

                if (survey == null)
                {
                    _logger.LogWarning("Survey not found after existence check: {SurveyId}", surveyViewModel.Id);
                    return NotFound("Survey not found");
                }

                // Update basic properties only
                survey.Title = surveyViewModel.Title;
                survey.Description = surveyViewModel.Description;
                survey.Published = surveyViewModel.Published;

                // We don't update the Complete property here as we now rely on Answer.Complete

                _ = await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated survey: {SurveyId}", survey.Id);
                return Ok(_mapper.Map<SurveyViewModel>(survey));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency exception while updating survey {SurveyId}", surveyViewModel?.Id);
                return BadRequest("Concurrency Exception");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while updating survey {SurveyId}: {Message}",
                    surveyViewModel?.Id, dbEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred while updating the survey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating survey {SurveyId}", surveyViewModel?.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the survey");
            }
        }

        private bool SurveyExists(int id)
        {
            return _context.Surveys.Any(e => e.Id == id);
        }
    }
}
