namespace JwtIdentity.Services
{
    public interface IOpenAi
    {
        Task<SurveyViewModel> GenerateSurveyAsync(string description, string aiInstructions = "");
        Task<SurveyAnalysisResponse> AnalyzeSurveyAsync(int surveyId);
    }
}
