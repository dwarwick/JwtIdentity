namespace JwtIdentity.Client.Services
{
    public interface IAuthService
    {
        Task<Response<ApplicationUserViewModel>> Login(ApplicationUserViewModel model);

        Task<Response<ApplicationUserViewModel>> StartDemo();

        Task Logout();

        Task<int> GetUserId();
    }
}