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

            // Now load each one�s Options
            await _context.Questions
                .OfType<SelectAllThatApplyQuestion>()
                .Where(mc => allIds.Contains(mc.Id))
                .Include(mc => mc.Options.OrderBy(o => o.Order))
                .LoadAsync();

            return Ok(_mapper.Map<SurveyViewModel>(survey));
        }

        // GET: api/Survey/MySurveys
        [HttpGet("surveysicreated")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<IEnumerable<SurveyViewModel>>> GetSurveysICreated()
        {
            var createdById = authService.GetUserId(User);
            if (createdById == 0)
                return Unauthorized();
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

            return Ok(surveyViewModels);
        }

        [HttpGet("surveysianswered")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<IEnumerable<SurveyViewModel>>> GetSurveysIAnswered()
        {
            var createdById = authService.GetUserId(User);
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

            return Ok(surveyViewModels);
        }

        // POST: api/Survey
        [HttpPost]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<SurveyViewModel>> PostSurvey(SurveyViewModel surveyViewModel)
        {
            int createdById = authService.GetUserId(User);
            if (createdById == 0)
                return Unauthorized();
            var survey = _mapper.Map<Survey>(surveyViewModel);

            if (survey == null) return BadRequest();

            if (survey.Id == 0)
            { // new survey
                survey.CreatedById = createdById;

                _ = _context.Surveys.Add(survey);
            }
            else
            { // existing survey

                // check if survey title or description has changed. If so, update the survey
                var existingSurvey = await _context.Surveys.FindAsync(survey.Id);

                if (existingSurvey != null &&
                    (existingSurvey.Title != survey.Title || existingSurvey.Description != survey.Description))
                {
                    existingSurvey.Title = survey.Title;
                    existingSurvey.Description = survey.Description;
                    _ = _context.Surveys.Update(existingSurvey);
                }

                foreach (var passedInQuestion in survey.Questions)
                {
                    if (passedInQuestion.Id == 0)
                    { // new question
                        passedInQuestion.CreatedById = createdById;
                        passedInQuestion.SurveyId = survey.Id;

                        _ = _context.Questions.Add(passedInQuestion);
                    }
                    else
                    { // existing question

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

                                    _ = _context.Questions.Update(existingTextQuestion);
                                }

                                break;
                            case QuestionType.TrueFalse:
                                var existingTrueFalseQuestion = await _context.Questions.OfType<TrueFalseQuestion>().FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);
                                existingTrueFalseQuestion.Text = passedInQuestion.Text;
                                existingTrueFalseQuestion.QuestionNumber = passedInQuestion.QuestionNumber;

                                _ = _context.Questions.Update(existingTrueFalseQuestion);
                                break;
                            case QuestionType.Rating1To10:
                                var existingRatingQuestion = await _context.Questions.OfType<Rating1To10Question>().FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);
                                existingRatingQuestion.Text = passedInQuestion.Text;
                                existingRatingQuestion.QuestionNumber = passedInQuestion.QuestionNumber;
                                _ = _context.Questions.Update(existingRatingQuestion);
                                break;
                            case QuestionType.MultipleChoice:
                                var existingMCQuestion = await _context.Questions.OfType<MultipleChoiceQuestion>().AsNoTracking().Include(x => x.Options).FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);

                                if (existingMCQuestion != null && (existingMCQuestion.Text != passedInQuestion.Text
                                        || passedInQuestion.QuestionNumber != existingMCQuestion.QuestionNumber))
                                {
                                    existingMCQuestion.Text = passedInQuestion.Text;
                                    existingMCQuestion.QuestionNumber = passedInQuestion.QuestionNumber;

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
                                }
                                break;

                            case QuestionType.SelectAllThatApply:
                                var existingSAQuestion = await _context.Questions.OfType<SelectAllThatApplyQuestion>().AsNoTracking().Include(x => x.Options).FirstOrDefaultAsync(q => q.Id == passedInQuestion.Id);

                                if (existingSAQuestion != null && (existingSAQuestion.Text != passedInQuestion.Text
                                        || passedInQuestion.QuestionNumber != existingSAQuestion.QuestionNumber))
                                {
                                    existingSAQuestion.Text = passedInQuestion.Text;
                                    existingSAQuestion.QuestionNumber = passedInQuestion.QuestionNumber;

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

            _ = await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(PostSurvey), new { id = survey.Id }, _mapper.Map<SurveyViewModel>(survey));
        }

        // PUT: api/Survey
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

            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == surveyViewModel.Id);

            if (survey == null)
            {
                return NotFound("Survey not found");
            }

            // Update basic properties only
            survey.Title = surveyViewModel.Title;
            survey.Description = surveyViewModel.Description;
            survey.Published = surveyViewModel.Published;

            // We don't update the Complete property here as we now rely on Answer.Complete

            try
            {
                _ = await _context.SaveChangesAsync();
                return Ok(_mapper.Map<SurveyViewModel>(survey));
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest("Concurrency Exception");
            }
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
