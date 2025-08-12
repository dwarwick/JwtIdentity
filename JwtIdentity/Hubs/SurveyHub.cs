using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace JwtIdentity.Hubs
{
    public class SurveyHub : Hub
    {
        public Task JoinSurveyGroup(string surveyId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, surveyId);
        }
    }
}
