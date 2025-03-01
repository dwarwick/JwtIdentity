

namespace JwtIdentity.Client.Pages.Survey
{
    public class MySurveysModel : BlazorBase
    {
        public List<SurveyViewModel> UserSurveys { get; set; } = new();

        protected HashSet<SurveyViewModel> _filterItems = new();
        // Added HashSet for the CreatedDate filter operators
        protected HashSet<string> FilterOperators { get; set; } = new()
        {
            FilterOperator.DateTime.OnOrAfter,
            FilterOperator.DateTime.OnOrBefore
        };

        protected FilterDefinition<SurveyViewModel> _filterDefinition { get; set; }

        protected override async Task OnInitializedAsync()
        {
            UserSurveys = (await ApiService.GetAllAsync<SurveyViewModel>("api/Survey/MySurveys")).ToList();

            _filterItems = UserSurveys.ToHashSet();

            _filterDefinition = new FilterDefinition<SurveyViewModel>
            {
                FilterFunction = x => _filterItems.Contains(x)
            };
        }

        protected async Task CopySurveyLinkAsync(string guid)
        {
            string url = $"{NavigationManager.BaseUri}survey/{guid}";
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", url);

            _ = Snackbar.Add("Survey link copied to clipboard", Severity.Success);
        }
    }
}
