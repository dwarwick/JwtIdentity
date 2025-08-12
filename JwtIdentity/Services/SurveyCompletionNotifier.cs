using Microsoft.AspNetCore.SignalR;
using JwtIdentity.Hubs;

namespace JwtIdentity.Services
{
    public class SurveyCompletionNotifier : ISurveyCompletionNotifier
    {
        private readonly IHubContext<SurveyHub> _hubContext;

        public SurveyCompletionNotifier(IHubContext<SurveyHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifySurveyCompleted(string surveyId)
        {
            return _hubContext.Clients.Group(surveyId).SendAsync("SurveyUpdated", surveyId);
        }
    }
}
