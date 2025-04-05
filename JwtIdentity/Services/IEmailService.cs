namespace JwtIdentity.Services
{
    public interface IEmailService
    {
        void SendEmailVerificationMessage(string Email, string tokenUrl);
        void SendPasswordResetEmail(string Email, string tokenUrl);
    }
}