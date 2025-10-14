using Microsoft.AspNetCore.Authorization;
using JwtIdentity.Interfaces;

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
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ISurveyService _surveyService;
        private readonly IQuestionHandlerFactory _questionHandlerFactory;

        public SurveyController(ApplicationDbContext context, IMapper mapper, IApiAuthService authService, ILogger<SurveyController> logger, IOpenAi openAiService, IEmailService emailService, IConfiguration configuration, ISurveyService surveyService, IQuestionHandlerFactory questionHandlerFactory)
        {
            _context = context;
            _mapper = mapper;
            this.authService = authService;
            _logger = logger;
            _openAiService = openAiService;
            _emailService = emailService;
            _configuration = configuration;
            _surveyService = surveyService;
            _questionHandlerFactory = questionHandlerFactory;
        }

        // GET: api/Survey/5
        [HttpGet("{guid}")]
        public async Task<ActionResult<SurveyViewModel>> GetSurvey(string guid)
        {
            try
            {
                _logger.LogInformation("Retrieving survey with GUID: {Guid}", guid);

                var survey = await _context.Surveys
                    .Include(s => s.Questions.OrderBy(x => x.QuestionNumber))
                    .Include(s => s.QuestionGroups.OrderBy(x => x.GroupNumber))
                    .FirstOrDefaultAsync(s => s.Guid == guid);

                if (survey == null)
                {
                    _logger.LogWarning("Survey with GUID {Guid} not found", guid);
                    return NotFound();
                }

                // Use handlers to load related data for question types that need it
                var mcIds = survey.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Select(mc => mc.Id)
                    .ToList();

                if (mcIds.Any())
                {
                    var mcHandler = _questionHandlerFactory.GetHandler(QuestionType.MultipleChoice);
                    await mcHandler.LoadRelatedDataAsync(mcIds, _context);
                }

                var allIds = survey.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Select(mc => mc.Id)
                    .ToList();

                if (allIds.Any())
                {
                    var satHandler = _questionHandlerFactory.GetHandler(QuestionType.SelectAllThatApply);
                    await satHandler.LoadRelatedDataAsync(allIds, _context);
                }

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

                bool isAdmin = User.IsInRole("Admin");

                _logger.LogInformation(
                    isAdmin
                        ? "Retrieving surveys for admin user {UserId}"
                        : "Retrieving surveys created by user {UserId}",
                    createdById);

                var query = _context.Surveys
                    .Include(s => s.Questions.OrderBy(q => q.QuestionNumber))
                    .Include(s => s.CreatedBy)
                    .AsQueryable();

                if (!isAdmin)
                {
                    query = query.Where(s => s.CreatedById == createdById);
                }

                var surveys = await query.ToListAsync();

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

                _logger.LogInformation(
                    isAdmin
                        ? "Retrieved {Count} surveys for admin user {UserId}"
                        : "Retrieved {Count} surveys created by user {UserId}",
                    surveys.Count, createdById);
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
                if (!string.IsNullOrWhiteSpace(surveyViewModel.AiInstructions)
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
                var isNewSurvey = survey.Id == 0;

                if (survey == null)
                {
                    _logger.LogWarning("Invalid survey data submitted by user {UserId}", createdById);
                    return BadRequest();
                }

                if (isNewSurvey)
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

                    if (existingSurvey.Title != survey.Title || existingSurvey.Description != survey.Description ||
                        existingSurvey.AiInstructions != survey.AiInstructions ||
                        existingSurvey.AiRetryCount != survey.AiRetryCount ||
                        existingSurvey.AiQuestionsApproved != survey.AiQuestionsApproved)
                    {
                        existingSurvey.Title = survey.Title;
                        existingSurvey.Description = survey.Description;
                        existingSurvey.AiInstructions = survey.AiInstructions;
                        existingSurvey.AiRetryCount = survey.AiRetryCount;
                        existingSurvey.AiQuestionsApproved = survey.AiQuestionsApproved;
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
                                                existingMCQuestion.Options.Add(newOption);
                                            }
                                            else
                                            { // existing option
                                                var existingOption = existingMCQuestion.Options.FirstOrDefault(o => o.Id == newOption.Id);

                                                if (existingOption != null && (existingOption.OptionText != newOption.OptionText || existingOption.Order != newOption.Order))
                                                {
                                                    existingOption.OptionText = newOption.OptionText;
                                                    existingOption.Order = newOption.Order;
                                                }
                                            }
                                        }

                                        // remove any options that are no longer present
                                        var newOptionIds = (newMCQuestion.Options ?? new List<ChoiceOption>())
                                            .Where(o => o.Id != 0)
                                            .Select(o => o.Id)
                                            .ToHashSet();

                                        var removedOptions = existingMCQuestion.Options
                                            .Where(o => o.Id != 0 && !newOptionIds.Contains(o.Id))
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
                                                existingSAQuestion.Options.Add(newOption);
                                            }
                                            else
                                            { // existing option
                                                var existingOption = existingSAQuestion.Options.FirstOrDefault(o => o.Id == newOption.Id);

                                                if (existingOption != null && (existingOption.OptionText != newOption.OptionText || existingOption.Order != newOption.Order))
                                                {
                                                    existingOption.OptionText = newOption.OptionText;
                                                    existingOption.Order = newOption.Order;
                                                }
                                            }
                                        }

                                        // remove any options that are no longer present
                                        var newOptionIds = (newSAQuestion.Options ?? new List<ChoiceOption>())
                                            .Where(o => o.Id != 0)
                                            .Select(o => o.Id)
                                            .ToHashSet();

                                        var removedOptions = existingSAQuestion.Options
                                            .Where(o => o.Id != 0 && !newOptionIds.Contains(o.Id))
                                            .ToList();

                                        if (removedOptions.Any())
                                        {
                                            _context.ChoiceOptions.RemoveRange(removedOptions);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (isNewSurvey)
                {
                    var user = await _context.Users.FindAsync(createdById);

                    if (!(user?.Email?.StartsWith("DemoUser_", StringComparison.OrdinalIgnoreCase) ?? false) && !(user?.Email?.StartsWith("anonymous_", StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        var userName = user?.UserName ?? createdById.ToString();
                        var customerServiceEmail = _configuration["EmailSettings:CustomerServiceEmail"];
                        var adminBody = $"<p>User {userName} created a new survey.</p><p>Title: {survey.Title}</p><p>Description: {survey.Description}</p>";
                        if (!string.IsNullOrEmpty(customerServiceEmail))
                        {
                            await _emailService.SendEmailAsync(customerServiceEmail, $"Survey Created by {userName}", adminBody);
                        }

                        if (!string.IsNullOrEmpty(user?.Email))
                        {
                            var userBody = $"<p>Congratulations {userName}, you created a new survey!</p><p>Title: {survey.Title}</p><p>Description: {survey.Description}</p>";
                            await _emailService.SendEmailAsync(user.Email, $"Survey Created: {survey.Title}", userBody);
                        }
                    }
                }

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

                var userId = authService.GetUserId(User);
                var user = await _context.Users.FindAsync(userId);
                var userName = user?.UserName ?? userId.ToString();
                var wasPublished = survey.Published;

                // Update basic properties only
                survey.Title = surveyViewModel.Title;
                survey.Description = surveyViewModel.Description;
                survey.Published = surveyViewModel.Published;

                // We don't update the Complete property here as we now rely on Answer.Complete

                _ = await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated survey: {SurveyId}", survey.Id);

                if (!wasPublished && survey.Published)
                {
                    var customerServiceEmail = _configuration["EmailSettings:CustomerServiceEmail"];
                    var adminBody = $"<p>User {userName} published a survey.</p><p>Title: {survey.Title}</p><p>Description: {survey.Description}</p>";
                    if (!string.IsNullOrEmpty(customerServiceEmail))
                    {
                        await _emailService.SendEmailAsync(customerServiceEmail, $"Survey Published by {userName}", adminBody);
                    }

                    if (!string.IsNullOrEmpty(user?.Email) && !user.Email.StartsWith("DemoUser_", StringComparison.OrdinalIgnoreCase) && !user.Email.StartsWith("anonymous_", StringComparison.OrdinalIgnoreCase))
                    {
                        var userBody = $"<p>Congratulations {userName}, your survey has been published!</p><p>Title: {survey.Title}</p><p>Description: {survey.Description}</p>";
                        await _emailService.SendEmailAsync(user.Email, $"Survey Published: {survey.Title}", userBody);
                    }

                    if (user?.Email != null && user.Email.Contains("DemoUser", StringComparison.OrdinalIgnoreCase))
                    {
                        await _surveyService.GenerateDemoSurveyResponsesAsync(survey);
                    }
                }

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

        [HttpPost("regenerate")]
        public async Task<ActionResult<SurveyViewModel>> RegenerateQuestions(SurveyViewModel surveyViewModel)
        {
            try
            {
                int userId = authService.GetUserId(User);
                if (userId == 0)
                {
                    return Unauthorized();
                }

                var survey = await _context.Surveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == surveyViewModel.Id);

                if (survey == null)
                {
                    return NotFound();
                }

                if (survey.CreatedById != userId)
                {
                    return Forbid();
                }

                if (survey.AiRetryCount >= 2)
                {
                    return BadRequest("Retry limit reached");
                }

                var questionIds = survey.Questions.Select(q => q.Id).ToList();

                if (questionIds.Any())
                {
                    var choiceOptions = await _context.ChoiceOptions
                        .Where(co =>
                            (co.MultipleChoiceQuestionId.HasValue && questionIds.Contains(co.MultipleChoiceQuestionId.Value)) ||
                            (co.SelectAllThatApplyQuestionId.HasValue && questionIds.Contains(co.SelectAllThatApplyQuestionId.Value)))
                        .ToListAsync();

                    _context.ChoiceOptions.RemoveRange(choiceOptions);
                }

                _context.Questions.RemoveRange(survey.Questions);
                await _context.SaveChangesAsync();

                survey.AiInstructions = surveyViewModel.AiInstructions;
                survey.AiRetryCount += 1;
                survey.AiQuestionsApproved = false;
                survey.Questions = new List<Question>();

                if (!string.IsNullOrWhiteSpace(survey.Description) && !string.IsNullOrWhiteSpace(survey.AiInstructions))
                {
                    var generatedSurvey = await _openAiService.GenerateSurveyAsync(survey.Description, survey.AiInstructions);
                    if (generatedSurvey?.Questions != null)
                    {
                        foreach (var q in generatedSurvey.Questions)
                        {
                            var question = _mapper.Map<Question>(q);
                            question.CreatedById = userId;
                            question.SurveyId = survey.Id;
                            survey.Questions.Add(question);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(_mapper.Map<SurveyViewModel>(survey));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating survey questions for survey {SurveyId}", surveyViewModel?.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while regenerating questions");
            }
        }

        [HttpPost("accept")]
        public async Task<ActionResult<SurveyViewModel>> AcceptAiQuestions(SurveyViewModel surveyViewModel)
        {
            try
            {
                int userId = authService.GetUserId(User);
                if (userId == 0)
                {
                    return Unauthorized();
                }

                var survey = await _context.Surveys.FindAsync(surveyViewModel.Id);
                if (survey == null)
                {
                    return NotFound();
                }

                if (survey.CreatedById != userId)
                {
                    return Forbid();
                }

                survey.AiQuestionsApproved = true;
                await _context.SaveChangesAsync();

                return Ok(_mapper.Map<SurveyViewModel>(survey));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting AI questions for survey {SurveyId}", surveyViewModel?.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while accepting AI questions");
            }
        }

        private bool SurveyExists(int id)
        {
            return _context.Surveys.Any(e => e.Id == id);
        }
    }
}
