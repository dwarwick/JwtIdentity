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
    }
}
