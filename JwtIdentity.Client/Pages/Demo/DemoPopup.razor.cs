namespace JwtIdentity.Client.Pages.Demo
{
    public class DemoPopupModel : BlazorBase
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string Class { get; set; }

        [Parameter]
        public int CurrentDemoStep { get; set; }

        [Parameter]
        public List<int> StepsToShow { get; set; }

        [Parameter]
        public bool IsButton { get; set; } = false;

        [Parameter]
        public Origin AnchorOrigin { get; set; } = Origin.BottomRight;

        [Parameter]
        public Origin TransformOrigin { get; set; } = Origin.TopLeft;

        protected bool ShowDemoStep() => IsDemoUser && StepsToShow.Contains(CurrentDemoStep);

        protected bool IsDemoUser { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name ?? string.Empty;
            IsDemoUser = userName.StartsWith("DemoUser") && userName.EndsWith("@surveyshark.site");
        }
    }
}
