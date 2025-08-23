namespace JwtIdentity.Client.Pages.Survey
{
    public class CreateSurveyModel : BlazorBase
    {
        protected SurveyViewModel Survey = new SurveyViewModel();
        protected bool IsBusy = false;

        protected bool IsDemoUser { get; set; }
        protected int DemoStep { get; set; }

        protected Origin AnchorOrigin { get; set; } = Origin.BottomRight;
        protected Origin TransformOrigin { get; set; } = Origin.TopLeft;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsDemoUser = authState.User.Identity?.Name == "DemoUser@surveyshark.site";
        }

        protected bool ShowDemoStep(int step) => IsDemoUser && DemoStep == step;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var isMobile = await JSRuntime.InvokeAsync<bool>("isMobile");
                if (isMobile)
                {
                    AnchorOrigin = Origin.BottomCenter;
                    TransformOrigin = Origin.TopCenter;
                }
                StateHasChanged();
            }
        }

        protected void NextDemoStep()
        {
            if (!IsDemoUser) return;

            switch (DemoStep)
            {
                case 0:
                    Survey.Title = "Customer Satisfaction Survey";
                    DemoStep = 1;
                    break;
                case 1:
                    Survey.Description = "Please take our customer satisfaction survey so we can get valuable feedback from you regarding our service. The description helps guide the AI engine on what type of questions to create.";
                    DemoStep = 2;
                    break;
                case 2:
                    Survey.AiInstructions = "Create 10 questions. The last question should be a free text question that will give the customer a chance to leave additional feedback.";
                    DemoStep = 3;
                    break;
                case 3:
                    DemoStep = 4;
                    break;
                case 4:
                    DemoStep = 5;
                    break;
            }
        }

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
