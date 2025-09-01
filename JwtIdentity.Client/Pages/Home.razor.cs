namespace JwtIdentity.Client.Pages
{
    public class HomeModel : BlazorBase
    {
        protected bool DemoLoading { get; set; }

        protected async Task StartDemo()
        {
            if (DemoLoading) return;
            DemoLoading = true;

            Response<ApplicationUserViewModel> loginResponse = await AuthService.StartDemo();

            if (loginResponse.Success)
            {
                Navigation.NavigateTo("/survey/create");
            }

            DemoLoading = false;
        }
    }
}
