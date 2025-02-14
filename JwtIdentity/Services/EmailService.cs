using System.Net.Mail;
using System.Net.Mime;

namespace JwtIdentity.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration configuration;

        private readonly string domain;
        private readonly string fromEmail;

        private readonly string header;
        private readonly string footer;

        private readonly string tableHtml = "<table bgcolor=\"#f6f9fc\" border=\"1\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"margin:0;padding:2px;\">";

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;

            domain = configuration["EmailSettings:Domain"] ?? string.Empty;
            fromEmail = configuration["EmailSettings:CustomerServiceEmail"] ?? string.Empty;
            header = $"<html xmlns=\"http://www.w3.org/1999/xhtml\" lang=\"en\" xml:lang=\"en\"<head></head><body><div align=\"center\"></div><h3 style=\"text-align: center;\">{domain}</h3>";
            footer = $"<div style=\"text-align:center;margin-top:20px;\">&#169; {DateTime.Now.Year} {domain}</div></body></html>";
        }

        public void SendEmail(string FromEmail, string ToEmail, string Subject, string Body, AlternateView? alternate = null)
        {
            string userName = configuration["EmailSettings:CustomerServiceEmail"] ?? string.Empty;
            string password = configuration["EmailSettings:Password"] ?? string.Empty;

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            MailMessage m = new MailMessage();
            SmtpClient sc = new SmtpClient();
            m.From = new MailAddress(FromEmail);
            m.To.Add(ToEmail);
            m.Subject = Subject;
            m.Body = Body;

            if (alternate != null)
            {
                m.AlternateViews.Add(alternate);
            }

            sc.Host = configuration["EmailSettings:Server"] ?? string.Empty;
            try
            {
                sc.Port = 8889;
                sc.Credentials = new System.Net.NetworkCredential(userName, password);
                sc.UseDefaultCredentials = false;
                sc.EnableSsl = false;
                sc.Send(m);

            }
            catch (Exception)
            {

            }
        }

        #region Email Verification
        public void SendEmailVerificationMessage(string toEmail, string tokenUrl)
        {
            string body = $"Please click the link below to verify your email address to complete your registration for {domain}\n\n{tokenUrl}";

            // Construct the alternate body as HTML.                  
            string HtmlBody = $"<p>Please click the button below to verify your email address to complete your registration for {domain}.</p></br></br><div style=\"text-align:center;\"><a href=\"{tokenUrl}\"><button type=\"button\" style=\"background-color:blue;color:white;border-radius:20px;padding:5px 15px;\">Verify</button></a></div><p>If you cannot click the button, please navigate to the following URL to verify your email address.</p><p>{tokenUrl}</p>";

            string html = header + HtmlBody + footer;
            ContentType mimeType = new System.Net.Mime.ContentType("text/html");

            // Add the alternate body to the message.
            AlternateView alternate = AlternateView.CreateAlternateViewFromString(html, mimeType);

            SendEmail(fromEmail, toEmail ?? string.Empty, "Please Verify Your Email Address", body, alternate);
        }
        #endregion

        public bool SendPasswordResetMessage(string EmailAddress, string token)
        {
            string baseUrl = configuration["Url"] ?? string.Empty;
            string tokenUrl = $"{baseUrl}/api/auth/handlepasswordresetemailclick?email={EmailAddress}&token={System.Web.HttpUtility.UrlEncode(token)}";
            string body = $"Please click the link below to reset your password for {baseUrl}\n\n{tokenUrl}";

            // Construct the alternate body as HTML.                  
            string HtmlBody = $"<p>Please click the link below to reset your password for digitalcart.biz.</p></br></br><div style=\"text-align:center;\"><a href=\"{tokenUrl}\"><button type=\"button\" style=\"background-color:blue;color:white;border-radius:20px;padding:5px 15px;\">Verify</button></a></div><p>If you cannot click the button, please navigate to the following URL to reset your password.</p><p>{tokenUrl}</p>";

            string html = header + HtmlBody + footer;
            ContentType mimeType = new System.Net.Mime.ContentType("text/html");

            // Add the alternate body to the message.
            AlternateView alternate = AlternateView.CreateAlternateViewFromString(html, mimeType);

            SendEmail("customerservice@digitalcart.biz", EmailAddress ?? string.Empty, "digitalcart.biz Password Reset", body, alternate);

            return true;
        }
    }
}
