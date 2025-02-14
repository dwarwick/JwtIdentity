namespace JwtIdentity.Services
{
    public interface IEmailService
    {
        void SendEmailVerificationMessage(string Email, string tokenUrl);
    }
}