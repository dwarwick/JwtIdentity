namespace JwtIdentity.Client.Pages.Survey
{
    public class MySurveysModel : BlazorBase, IBrowserViewportObserver, IAsyncDisposable
    {
        [Inject]
        protected IBrowserViewportService BrowserViewportService { get; set; }

        public List<SurveyViewModel> UserSurveys { get; set; } = new();

        protected HashSet<SurveyViewModel> _filterItems = new();
        // Added HashSet for the CreatedDate filter operators
        protected HashSet<string> FilterOperators { get; set; } = new()
        {
            FilterOperator.DateTime.OnOrAfter,
            FilterOperator.DateTime.OnOrBefore
        };

        protected FilterDefinition<SurveyViewModel> _filterDefinition { get; set; }

        protected int FrozenColumns { get; set; }

        protected static string GetTitleText(bool published) => published ? "Copy Survey Link" : "Survey not published";

        protected static string ShareButtonDisabled(bool published, bool disabledCondition) => published == disabledCondition ? "" : "disabled";

        Guid IBrowserViewportObserver.Id => Guid.NewGuid();

        ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
        {
            ReportRate = 250,
            NotifyOnBreakpointOnly = true
        };

        protected override async Task OnInitializedAsync()
        {
            UserSurveys = (await ApiService.GetAllAsync<SurveyViewModel>("api/Survey/MySurveys")).ToList();

            _filterItems = UserSurveys.ToHashSet();

            _filterDefinition = new FilterDefinition<SurveyViewModel>
            {
                FilterFunction = x => _filterItems.Contains(x)
            };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
            }

            await base.OnAfterRenderAsync(firstRender);
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

        Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
        {
            Breakpoint breakpoint = browserViewportEventArgs.Breakpoint;

            if (breakpoint == Breakpoint.Sm || breakpoint == Breakpoint.Xs)
            {
                FrozenColumns = 0;
            }
            else
            {
                FrozenColumns = 1;
            }

            return InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync() => await BrowserViewportService.UnsubscribeAsync(this);
    }
}
