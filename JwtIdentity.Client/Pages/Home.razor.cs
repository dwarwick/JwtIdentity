namespace JwtIdentity.Client.Pages
{
    public class HomeModel : BlazorBase
    {
        protected bool DemoLoading { get; set; }

        protected async Task StartDemo()
        {
            if (DemoLoading) return;
            DemoLoading = true;

            Response<ApplicationUserViewModel> loginResponse = await AuthService.Login(new ApplicationUserViewModel()
            {
                UserName = "logmeindemouser",
                Password = "123"
            });

            if (loginResponse.Success)
            {
                Navigation.NavigateTo("/survey/create");
            }

            DemoLoading = false;
        }
    }
}
