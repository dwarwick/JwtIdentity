namespace JwtIdentity.Client.Services
{
    public interface IAuthService
    {
        Task<Response<LoginModel>> Login(LoginModel model);

        Task Logout();
    }
}