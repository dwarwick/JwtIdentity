namespace JwtIdentity.Client.Pages.Survey
{
    public class CreateSurveyModel : BlazorBase
    {
        protected SurveyViewModel Survey = new SurveyViewModel();
        protected bool IsBusy = false;

        protected async Task CreateSurvey()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Survey.Guid = Guid.NewGuid().ToString();
                var response = await ApiService.PostAsync(ApiEndpoints.Survey, Survey);
                if (response != null && response.Id > 0)
                {
                    _ = Snackbar.Add("Survey Created", MudBlazor.Severity.Success);
                    Navigation.NavigateTo($"/survey/edit/{response.Guid}");
                }
                else
                {
                    _ = Snackbar.Add("Survey Not Created", MudBlazor.Severity.Error);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
