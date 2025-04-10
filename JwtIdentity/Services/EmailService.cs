using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

#nullable enable

namespace JwtIdentity.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        private readonly string domain;
        private readonly string fromEmail;

        private readonly string header;
        private readonly string footer;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            domain = configuration["EmailSettings:Domain"] ?? string.Empty;
            fromEmail = configuration["EmailSettings:CustomerServiceEmail"] ?? string.Empty;
            header = $"<html xmlns=\"http://www.w3.org/1999/xhtml\" lang=\"en\" xml:lang=\"en\"<head></head><body><div align=\"center\"></div><h3 style=\"text-align: center;\">{domain}</h3>";
            footer = $"<div style=\"text-align:center;margin-top:20px;\">&#169; {DateTime.Now.Year} {domain}</div></body></html>";
        }

        public void SendEmailVerificationMessage(string email, string tokenUrl)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var fromEmail = emailSettings["CustomerServiceEmail"] ?? string.Empty;
                var password = emailSettings["Password"] ?? string.Empty;
                var server = emailSettings["Server"] ?? string.Empty;
                var domain = emailSettings["Domain"] ?? string.Empty;

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(server))
                {
                    _logger.LogError("Email configuration is missing required values");
                    return;
                }

                var subject = "Email Verification";
                var body = $@"
                <h2>Verify Your Email</h2>
                <p>Thank you for registering. Please click the link below to verify your email address:</p>
                <p><a href='{tokenUrl}'>Verify Email</a></p>
                <p>If you didn't request this verification, please ignore this email.</p>
                ";

                SendEmail(fromEmail, password, server, email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification message");
            }
        }

        public void SendPasswordResetEmail(string email, string tokenUrl)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var fromEmail = emailSettings["CustomerServiceEmail"] ?? string.Empty;
                var password = emailSettings["Password"] ?? string.Empty;
                var server = emailSettings["Server"] ?? string.Empty;
                var domain = emailSettings["Domain"] ?? string.Empty;

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(server))
                {
                    _logger.LogError("Email configuration is missing required values");
                    return;
                }

                var subject = "Password Reset Request";
                var body = $@"
                <h2>Reset Your Password</h2>
                <p>You requested a password reset. Please click the link below to reset your password:</p>
                <p><a href='{tokenUrl}'>Reset Password</a></p>
                <p>If you didn't request a password reset, please ignore this email.</p>
                ";

                SendEmail(fromEmail, password, server, email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email");
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var fromEmail = emailSettings["CustomerServiceEmail"] ?? string.Empty;
                var password = emailSettings["Password"] ?? string.Empty;
                var server = emailSettings["Server"] ?? string.Empty;
                var domain = emailSettings["Domain"] ?? string.Empty;

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(server))
                {
                    _logger.LogError("Email configuration is missing required values");
                    return;
                }

                await Task.Run(() => SendEmail(fromEmail, password, server, toEmail, subject, body));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email asynchronously");
            }
        }

        private void SendEmail(string fromEmail, string password, string server, string toEmail, string subject, string body)
        {
            using (var message = new MailMessage())
            {
                message.From = new MailAddress(fromEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;
                message.To.Add(new MailAddress(toEmail));

                using (var client = new SmtpClient(server))
                {
                    client.Port = 587;
                    client.Credentials = new NetworkCredential(fromEmail, password);
                    client.EnableSsl = true;

                    client.Send(message);
                }
            }
        }
    }
}
