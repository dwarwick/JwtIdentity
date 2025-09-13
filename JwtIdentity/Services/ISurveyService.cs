namespace JwtIdentity.Services
{
    public interface ISurveyService
    {
        Survey GetSurvey(string guid);
        Task GenerateDemoSurveyResponsesAsync(Survey survey, int numberOfUsers = 20);
    }
}