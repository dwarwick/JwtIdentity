
namespace JwtIdentity.Client.Pages.Demo
{
    public class DemoBorderModel : BlazorBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Class { get; set; }

        [Parameter]
        public int CurrentDemoStep { get; set; }

        [Parameter]
        public List<int> StepsToShow { get; set; }

        [Parameter]
        public bool IsButton { get; set; } = false;

        protected bool ShowDemoStep() => IsDemoUser && StepsToShow.Contains(CurrentDemoStep);

        protected bool IsDemoUser { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsDemoUser = authState.User.Identity?.Name == "DemoUser@surveyshark.site";
        }
    }
}
