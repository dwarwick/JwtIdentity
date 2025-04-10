namespace JwtIdentity.Services
{
    public interface IEmailService
    {
        void SendEmailVerificationMessage(string Email, string tokenUrl);
        void SendPasswordResetEmail(string Email, string tokenUrl);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}