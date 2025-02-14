namespace JwtIdentity.Services
{
    public interface IApiAuthService
    {
        Task<string> GenerateJwtToken(ApplicationUser user);
        // ...existing code...

        Task<string> GenerateEmailVerificationLink(ApplicationUser user);
    }
}
