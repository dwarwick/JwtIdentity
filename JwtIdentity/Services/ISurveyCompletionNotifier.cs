namespace JwtIdentity.Services
{
    public interface ISurveyCompletionNotifier
    {
        Task NotifySurveyCompleted(string surveyId);
    }
}
