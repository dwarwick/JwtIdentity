namespace JwtIdentity.Client.Pages.Survey
{
    public class CreateSurveyModel : BlazorBase
    {


        protected SurveyViewModel Survey = new SurveyViewModel();

        protected async Task CreateSurvey()
        {
            Survey.Guid = Guid.NewGuid().ToString();

            var response = await ApiService.PostAsync(ApiEndpoints.Survey, Survey);
            if (response != null && response.Id > 0)
            {
                _ = Snackbar.Add("Survey Created", MudBlazor.Severity.Success);
                Navigation.NavigateTo($"/survey/createquestions/{response.Guid}");
            }
            else
            {
                _ = Snackbar.Add("Survey Not Created", MudBlazor.Severity.Error);
            }
        }
    }
}
