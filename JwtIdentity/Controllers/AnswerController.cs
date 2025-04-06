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

        [HttpGet("getanswersforsurveyforloggedinuser/{guid}")]
        public async Task<ActionResult<AnswerViewModel>> GetAnswersForSurveyForLoggedInUser(string guid, [FromQuery] bool Preview)
        {
            // get the ip address of the user
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // generate code to get the usename of the user
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            int userId = apiAuthService.GetUserId(User);

            Survey survey = null;

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
                return BadRequest("You have already taken this survey");
            }

            survey = await _context.Surveys
            .Where(s => s.Guid == guid)
            .Include(s => s.Questions).ThenInclude(q => q.Answers.Where(a => a.CreatedById == userId)).FirstOrDefaultAsync();

            if (survey == null) return BadRequest("Survey does not exist");

            if (!Preview && !survey.Published) return BadRequest("This survey has not been published");

            // Pull out the IDs of any multiple-choice questions in memory
            var mcIds = survey.Questions
                .OfType<MultipleChoiceQuestion>()
                .Select(mc => mc.Id)
                .ToList();

            // Pull out the IDs of any select-all-that-apply questions in memory
            var selectAllIds = survey.Questions
                .OfType<SelectAllThatApplyQuestion>()
                .Select(sa => sa.Id)
                .ToList();

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

            return Ok(_mapper.Map<SurveyViewModel>(survey));
        }

        [HttpGet("getsurveyresults/{guid}")]
        public async Task<ActionResult<SurveyViewModel>> GetSurveyResults(string guid)
        {
            var survey = await _context.Surveys
                .Where(s => s.Guid == guid)
                .Include(s => s.Questions).ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync();
            if (survey == null) return BadRequest("Survey does not exist");

            // Pull out the IDs of any multiple-choice questions in memory
            var mcIds = survey.Questions
                .OfType<MultipleChoiceQuestion>()
                .Select(mc => mc.Id)
                .ToList();
                
            // Pull out the IDs of any select-all-that-apply questions in memory
            var selectAllIds = survey.Questions
                .OfType<SelectAllThatApplyQuestion>()
                .Select(sa => sa.Id)
                .ToList();

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

            return Ok(_mapper.Map<SurveyViewModel>(survey));
        }

        [HttpGet("getanswersforsurveyforCharts/{guid}")]
        public async Task<ActionResult<SurveyDataViewModel>> GetAnswersForSurveyForCharts(string guid)
        {
            int userId = apiAuthService.GetUserId(User);

            Survey survey = null;

            survey = await _context.Surveys
            .Where(s => s.Guid == guid && s.CreatedById == userId)
            .Include(s => s.Questions).ThenInclude(q => q.Answers).FirstOrDefaultAsync();

            if (survey == null) return BadRequest("Survey does not exist");

            if (!survey.Published) return BadRequest("This survey has not been published");

            // Pull out the IDs of any multiple-choice questions in memory
            var mcIds = survey.Questions
                .OfType<MultipleChoiceQuestion>()
                .Select(mc => mc.Id)
                .ToList();
                
            // Pull out the IDs of any select-all-that-apply questions in memory
            var selectAllIds = survey.Questions
                .OfType<SelectAllThatApplyQuestion>()
                .Select(sa => sa.Id)
                .ToList();

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

                        surveyDataViewModel.SurveyData = new List<ChartData>() { new ChartData() { X = "True", Y = trueFalseAnswers.Count(a => a.Value == true) }, new ChartData() { X = "False", Y = trueFalseAnswers.Count(a => a.Value == false) } };
                        surveyDataViewModel.TrueFalseQuestion = _mapper.Map<TrueFalseQuestionViewModel>(question);
                        break;
                    case QuestionType.Rating1To10:
                        // add to the chart series the count of each rating
                        // get the question.Answers as Rating1To10Answer
                        var ratingAnswers = await _context.Answers.OfType<Rating1To10Answer>().AsNoTracking().Where(a => a.QuestionId == question.Id).ToListAsync();
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
                        break;
                }

                surveyData.Add(surveyDataViewModel);
            }

            return Ok(surveyData);
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
                        if (((TextAnswer)answer).Text != ((TextAnswer)existingAnswer).Text || ((TextAnswer)answer).Complete != ((TextAnswer)existingAnswer).Complete) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.TrueFalse:
                        if (((TrueFalseAnswer)answer).Value != ((TrueFalseAnswer)existingAnswer).Value || ((TrueFalseAnswer)answer).Complete != ((TrueFalseAnswer)existingAnswer).Complete) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.SingleChoice:
                        if (((SingleChoiceAnswer)answer).SelectedOptionId != ((SingleChoiceAnswer)existingAnswer).SelectedOptionId || ((SingleChoiceAnswer)answer).Complete != ((SingleChoiceAnswer)existingAnswer).Complete) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.MultipleChoice:
                        if (((MultipleChoiceAnswer)answer).SelectedOptionId != ((MultipleChoiceAnswer)existingAnswer).SelectedOptionId || ((MultipleChoiceAnswer)answer).Complete != ((MultipleChoiceAnswer)existingAnswer).Complete) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.Rating1To10:
                        if (((Rating1To10Answer)answer).SelectedOptionId != ((Rating1To10Answer)existingAnswer).SelectedOptionId || ((Rating1To10Answer)answer).Complete != ((Rating1To10Answer)existingAnswer).Complete) _ = _context.Answers.Update(answer);
                        break;
                    case AnswerType.SelectAllThatApply:
                        if (((SelectAllThatApplyAnswer)answer).SelectedOptionIds != ((SelectAllThatApplyAnswer)existingAnswer).SelectedOptionIds || ((SelectAllThatApplyAnswer)answer).Complete != ((SelectAllThatApplyAnswer)existingAnswer).Complete) _ = _context.Answers.Update(answer);
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
