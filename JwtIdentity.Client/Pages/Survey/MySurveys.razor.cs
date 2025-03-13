

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

        protected static string GetTitleText(bool published) => published ? "Copy Survey Link" : "Survey not published";

        protected static string ShareButtonDisabled(bool published, bool disabledCondition) => published == disabledCondition ? "" : "disabled";

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
            if (!UserSurveys.FirstOrDefault(x => x.Guid == guid).Published)
            {
                _ = Snackbar.Add("Survey not published", Severity.Error);
                return;
            }

            string url = $"{NavigationManager.BaseUri}survey/{guid}";
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", url);

            _ = Snackbar.Add("Survey link copied to clipboard", Severity.Success);
        }

        protected void CheckDisabledButton(string guid)
        {
            if (!UserSurveys.FirstOrDefault(x => x.Guid == guid).Published)
            {
                _ = Snackbar.Add("Survey not published", Severity.Error);
            }
        }

        protected async Task HandleShareClick(string guid, bool published)
        {
            if (!published)
            {
                _ = Snackbar.Add("Survey not published", Severity.Error);
                return;
            }

            // Construct the URL
            string url = $"https://www.facebook.com/sharer/sharer.php?u=https%3A%2F%2Fwww.davidtest.xyz%2Fsurvey%2F{guid}&amp;src=sdkpreparse";

            // Open the URL in a new tab
            await JSRuntime.InvokeVoidAsync("open", url, "_blank");
        }

        protected void AddEditQuestions(string guid, bool published)
        {
            if (published)
            {
                _ = Snackbar.Add("Survey published. You cannot edit survey questions once it has been published", Severity.Error);
                return;
            }

            NavigationManager.NavigateTo($"/survey/createquestions/{guid}");
        }
    }
}
