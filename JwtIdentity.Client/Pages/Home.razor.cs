namespace JwtIdentity.Client.Pages
{
    public class HomeModel : BlazorBase
    {
        protected bool DemoLoading { get; set; }

        protected void StartDemo()
        {
            if (DemoLoading) return;
            DemoLoading = true;

            Navigation.NavigateTo("/demo");
        }
    }
}
