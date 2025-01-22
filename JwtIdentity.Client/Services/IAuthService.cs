namespace JwtIdentity.Client.Services
{
    public interface IAuthService
    {
        Task<string> Login(LoginModel loginModel);
    }
}