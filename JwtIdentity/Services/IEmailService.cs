namespace JwtIdentity.Services
{
    public interface IEmailService
    {
        bool SendEmailVerificationMessage(string Email, string tokenUrl);
        bool SendPasswordResetEmail(string Email, string tokenUrl);
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
    }
}