namespace JwtIdentity.Services
{
    public class SurveyService : ISurveyService
    {
        public SurveyService(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public ApplicationDbContext DbContext { get; }

        public Survey GetSurvey(string guid)
        {
            return DbContext.Surveys.Where(x => x.Guid == guid).FirstOrDefault();
        }
    }
}
