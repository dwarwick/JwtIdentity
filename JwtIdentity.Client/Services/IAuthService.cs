namespace JwtIdentity.Client.Services
{
    public interface IAuthService
    {
        ApplicationUserViewModel? CurrentUser { get; set; }

        Task<Response<ApplicationUserViewModel>> Login(ApplicationUserViewModel model);

        Task Logout();
    }
}