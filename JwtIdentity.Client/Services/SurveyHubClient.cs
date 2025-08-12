using Microsoft.AspNetCore.SignalR.Client;

namespace JwtIdentity.Client.Services
{
    public class SurveyHubClient : IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly Task _startTask;

        public event Action<string>? SurveyUpdated;

        public SurveyHubClient(NavigationManager navigationManager)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(navigationManager.ToAbsoluteUri("/surveyHub"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>("SurveyUpdated", id => SurveyUpdated?.Invoke(id));
            _startTask = _hubConnection.StartAsync();
        }

        public async Task JoinSurveyGroup(string surveyId)
        {
            await _startTask;
            await _hubConnection.InvokeAsync("JoinSurveyGroup", surveyId);
        }

        public async ValueTask DisposeAsync()
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
