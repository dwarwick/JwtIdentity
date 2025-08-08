using JwtIdentity.Common.ViewModels;

namespace JwtIdentity.Services
{
    public interface IOpenAi
    {
        Task<SurveyViewModel> GenerateSurveyAsync(string description);
    }
}
