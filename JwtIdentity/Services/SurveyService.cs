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
    }
}
