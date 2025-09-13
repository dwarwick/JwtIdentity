namespace JwtIdentity.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SurveyService> _logger;

        public SurveyService(ApplicationDbContext dbContext, ILogger<SurveyService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
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

                // Load options for multiple choice and select-all questions
                var mcIds = survey.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Select(q => q.Id)
                    .ToList();

                if (mcIds.Any())
                {
                    await _dbContext.Questions
                        .OfType<MultipleChoiceQuestion>()
                        .Where(q => mcIds.Contains(q.Id))
                        .Include(q => q.Options)
                        .LoadAsync();
                }

                var satIds = survey.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Select(q => q.Id)
                    .ToList();

                if (satIds.Any())
                {
                    await _dbContext.Questions
                        .OfType<SelectAllThatApplyQuestion>()
                        .Where(q => satIds.Contains(q.Id))
                        .Include(q => q.Options)
                        .LoadAsync();
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
                        Answer answer = question.QuestionType switch
                        {
                            QuestionType.Text => new TextAnswer
                            {
                                QuestionId = question.Id,
                                Text = $"Sample answer {random.Next(1, 1000)}",
                                Complete = true,
                                CreatedById = anonUser.Id,
                                IpAddress = "127.0.0.1"
                            },
                            QuestionType.TrueFalse => new TrueFalseAnswer
                            {
                                QuestionId = question.Id,
                                Value = random.Next(0, 2) == 0,
                                Complete = true,
                                CreatedById = anonUser.Id,
                                IpAddress = "127.0.0.1"
                            },
                            QuestionType.MultipleChoice => new MultipleChoiceAnswer
                            {
                                QuestionId = question.Id,
                                SelectedOptionId = ((MultipleChoiceQuestion)question).Options
                                    [random.Next(((MultipleChoiceQuestion)question).Options.Count)].Id,
                                Complete = true,
                                CreatedById = anonUser.Id,
                                IpAddress = "127.0.0.1"
                            },
                            QuestionType.Rating1To10 => new Rating1To10Answer
                            {
                                QuestionId = question.Id,
                                SelectedOptionId = random.Next(1, 11),
                                Complete = true,
                                CreatedById = anonUser.Id,
                                IpAddress = "127.0.0.1"
                            },
                            QuestionType.SelectAllThatApply => new SelectAllThatApplyAnswer
                            {
                                QuestionId = question.Id,
                                SelectedOptionIds = string.Join(",", ((SelectAllThatApplyQuestion)question).Options
                                    .Where(_ => random.Next(0, 2) == 1)
                                    .Select(o => o.Id)
                                    .DefaultIfEmpty(((SelectAllThatApplyQuestion)question).Options
                                        [random.Next(((SelectAllThatApplyQuestion)question).Options.Count)].Id)),
                                Complete = true,
                                CreatedById = anonUser.Id,
                                IpAddress = "127.0.0.1"
                            },
                            _ => null
                        };

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
