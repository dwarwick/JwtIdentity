using JwtIdentity.Interfaces;

namespace JwtIdentity.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SurveyService> _logger;
        private readonly IQuestionHandlerFactory _questionHandlerFactory;

        public SurveyService(ApplicationDbContext dbContext, ILogger<SurveyService> logger, IQuestionHandlerFactory questionHandlerFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _questionHandlerFactory = questionHandlerFactory;
        }

        public Survey GetSurvey(string guid)
        {
            try
            {
                _logger.LogInformation("Retrieving survey with GUID: {Guid}", guid);
                
                if (string.IsNullOrEmpty(guid))
                {
                    _logger.LogWarning("Attempted to retrieve survey with null or empty GUID");
                    return null;
                }

                var survey = _dbContext.Surveys.Where(x => x.Guid == guid).FirstOrDefault();
                
                if (survey == null)
                {
                    _logger.LogWarning("Survey with GUID {Guid} not found", guid);
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved survey with ID: {Id}", survey.Id);
                }
                
                return survey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving survey with GUID: {Guid}", guid);
                throw;
            }
        }

        public async Task GenerateDemoSurveyResponsesAsync(Survey survey, int numberOfUsers = 20)
        {
            try
            {
                if (survey == null)
                {
                    _logger.LogWarning("Survey is null when attempting to generate demo responses");
                    return;
                }

                // Reload survey with questions and options
                survey = await _dbContext.Surveys
                    .Where(s => s.Id == survey.Id)
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync();

                if (survey == null)
                {
                    _logger.LogWarning("Survey with ID {SurveyId} not found when generating demo responses", survey?.Id);
                    return;
                }

                // Load options for multiple choice and select-all questions using handlers
                var mcIds = survey.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Select(q => q.Id)
                    .ToList();

                if (mcIds.Any())
                {
                    var mcHandler = _questionHandlerFactory.GetHandler(QuestionType.MultipleChoice);
                    await mcHandler.LoadRelatedDataAsync(mcIds, _dbContext);
                }

                var satIds = survey.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Select(q => q.Id)
                    .ToList();

                if (satIds.Any())
                {
                    var satHandler = _questionHandlerFactory.GetHandler(QuestionType.SelectAllThatApply);
                    await satHandler.LoadRelatedDataAsync(satIds, _dbContext);
                }

                var random = new Random();

                for (int i = 0; i < numberOfUsers; i++)
                {
                    var userGuid = Guid.NewGuid().ToString();
                    var email = $"anonymous_{userGuid}@surveyshark.site";

                    var anonUser = new ApplicationUser
                    {
                        UserName = email,
                        NormalizedUserName = email.ToUpperInvariant(),
                        Email = email,
                        NormalizedEmail = email.ToUpperInvariant(),
                        EmailConfirmed = true,
                        SecurityStamp = string.Empty,
                        Theme = "light",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };

                    _dbContext.Users.Add(anonUser);
                    await _dbContext.SaveChangesAsync();

                    foreach (var question in survey.Questions)
                    {
                        // Use the appropriate handler to create a demo answer
                        var handler = _questionHandlerFactory.GetHandler(question.QuestionType);
                        var answer = handler.CreateDemoAnswer(question, random, anonUser.Id.ToString());

                        if (answer != null)
                        {
                            _dbContext.Answers.Add(answer);
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Generated {Count} demo responses for survey {SurveyId}", numberOfUsers, survey.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating demo survey responses for survey {SurveyId}", survey?.Id);
                throw;
            }
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateSurveyForPublishingAsync(int surveyId)
        {
            try
            {
                _logger.LogInformation("Validating survey {SurveyId} for publishing", surveyId);

                // Load survey with all related data needed for validation
                var survey = await _dbContext.Surveys
                    .Include(s => s.Questions)
                    .Include(s => s.QuestionGroups)
                    .FirstOrDefaultAsync(s => s.Id == surveyId);

                if (survey == null)
                {
                    return (false, "Survey not found");
                }

                // If there are no question groups, the survey is valid (backward compatibility)
                if (survey.QuestionGroups == null || !survey.QuestionGroups.Any())
                {
                    _logger.LogInformation("Survey {SurveyId} has no question groups, skipping group validation", surveyId);
                    return (true, string.Empty);
                }

                // Load choice options for multiple choice and select-all questions
                var mcQuestions = survey.Questions.OfType<MultipleChoiceQuestion>().ToList();
                var satQuestions = survey.Questions.OfType<SelectAllThatApplyQuestion>().ToList();
                
                var mcIds = mcQuestions.Select(q => q.Id).ToList();
                var satIds = satQuestions.Select(q => q.Id).ToList();

                List<ChoiceOption> allOptions = new List<ChoiceOption>();
                
                if (mcIds.Any() || satIds.Any())
                {
                    allOptions = await _dbContext.ChoiceOptions
                        .Where(co => 
                            (co.MultipleChoiceQuestionId.HasValue && mcIds.Contains(co.MultipleChoiceQuestionId.Value)) ||
                            (co.SelectAllThatApplyQuestionId.HasValue && satIds.Contains(co.SelectAllThatApplyQuestionId.Value)))
                        .ToListAsync();
                }

                // Assign loaded options back to questions
                foreach (var mcQ in mcQuestions)
                {
                    mcQ.Options = allOptions.Where(o => o.MultipleChoiceQuestionId == mcQ.Id).ToList();
                }
                
                foreach (var satQ in satQuestions)
                {
                    satQ.Options = allOptions.Where(o => o.SelectAllThatApplyQuestionId == satQ.Id).ToList();
                }

                // Check for empty groups
                var emptyGroups = survey.QuestionGroups
                    .Where(g => !survey.Questions.Any(q => q.GroupId == g.GroupNumber))
                    .ToList();

                if (emptyGroups.Any())
                {
                    var groupNames = string.Join(", ", emptyGroups.Select(g => 
                        string.IsNullOrWhiteSpace(g.GroupName) ? $"Group {g.GroupNumber}" : g.GroupName));
                    _logger.LogWarning("Survey {SurveyId} has empty groups: {Groups}", surveyId, groupNames);
                    return (false, $"Cannot publish survey with empty groups: {groupNames}. Please add questions to these groups or delete them.");
                }

                // Find all reachable groups starting from group 0
                var reachableGroupNumbers = new HashSet<int>();
                var groupNumbersToCheck = new Queue<int>();

                // Start with group 0 (default group) - it's always the entry point
                reachableGroupNumbers.Add(0);
                groupNumbersToCheck.Enqueue(0);

                // Traverse all possible paths
                while (groupNumbersToCheck.Count > 0)
                {
                    var currentGroupNumber = groupNumbersToCheck.Dequeue();
                    var currentGroup = survey.QuestionGroups.FirstOrDefault(g => g.GroupNumber == currentGroupNumber);

                    if (currentGroup == null)
                        continue;

                    // Check NextGroupId (if it stores GroupNumber)
                    if (currentGroup.NextGroupId.HasValue && !reachableGroupNumbers.Contains(currentGroup.NextGroupId.Value))
                    {
                        reachableGroupNumbers.Add(currentGroup.NextGroupId.Value);
                        groupNumbersToCheck.Enqueue(currentGroup.NextGroupId.Value);
                    }

                    // Check branching from questions in this group
                    var questionsInGroup = survey.Questions.Where(q => q.GroupId == currentGroupNumber).ToList();

                    foreach (var question in questionsInGroup)
                    {
                        // Check True/False branching
                        if (question is TrueFalseQuestion tfQuestion)
                        {
                            if (tfQuestion.BranchToGroupIdOnTrue.HasValue && !reachableGroupNumbers.Contains(tfQuestion.BranchToGroupIdOnTrue.Value))
                            {
                                reachableGroupNumbers.Add(tfQuestion.BranchToGroupIdOnTrue.Value);
                                groupNumbersToCheck.Enqueue(tfQuestion.BranchToGroupIdOnTrue.Value);
                            }

                            if (tfQuestion.BranchToGroupIdOnFalse.HasValue && !reachableGroupNumbers.Contains(tfQuestion.BranchToGroupIdOnFalse.Value))
                            {
                                reachableGroupNumbers.Add(tfQuestion.BranchToGroupIdOnFalse.Value);
                                groupNumbersToCheck.Enqueue(tfQuestion.BranchToGroupIdOnFalse.Value);
                            }
                        }
                        // Check Multiple Choice branching
                        else if (question is MultipleChoiceQuestion mcQuestion && mcQuestion.Options != null)
                        {
                            foreach (var option in mcQuestion.Options)
                            {
                                if (option.BranchToGroupId.HasValue && !reachableGroupNumbers.Contains(option.BranchToGroupId.Value))
                                {
                                    reachableGroupNumbers.Add(option.BranchToGroupId.Value);
                                    groupNumbersToCheck.Enqueue(option.BranchToGroupId.Value);
                                }
                            }
                        }
                        // Check Select All That Apply branching
                        else if (question is SelectAllThatApplyQuestion satQuestion && satQuestion.Options != null)
                        {
                            foreach (var option in satQuestion.Options)
                            {
                                if (option.BranchToGroupId.HasValue && !reachableGroupNumbers.Contains(option.BranchToGroupId.Value))
                                {
                                    reachableGroupNumbers.Add(option.BranchToGroupId.Value);
                                    groupNumbersToCheck.Enqueue(option.BranchToGroupId.Value);
                                }
                            }
                        }
                    }
                }

                // Find orphaned groups (groups that are not reachable)
                var orphanedGroups = survey.QuestionGroups
                    .Where(g => g.GroupNumber != 0 && !reachableGroupNumbers.Contains(g.GroupNumber))
                    .ToList();

                if (orphanedGroups.Any())
                {
                    var groupNames = string.Join(", ", orphanedGroups.Select(g => 
                        string.IsNullOrWhiteSpace(g.GroupName) ? $"Group {g.GroupNumber}" : g.GroupName));
                    _logger.LogWarning("Survey {SurveyId} has orphaned groups: {Groups}", surveyId, groupNames);
                    return (false, $"Cannot publish survey with unreachable groups: {groupNames}. Please add branching rules to connect these groups or delete them.");
                }

                _logger.LogInformation("Survey {SurveyId} validation passed", surveyId);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating survey {SurveyId} for publishing", surveyId);
                return (false, "An error occurred while validating the survey");
            }
        }
    }
}
