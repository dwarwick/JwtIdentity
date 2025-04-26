using Serilog.Core;
using Serilog.Events;
using System.Text;

namespace JwtIdentity.Services
{
    public class EmailLogEventSink : ILogEventSink
    {
        private readonly IEmailService _emailService;
        private readonly string _customerServiceEmail;
        private readonly string _applicationName;

        public EmailLogEventSink(IEmailService emailService, string customerServiceEmail, string applicationName)
        {
            _emailService = emailService;
            _customerServiceEmail = customerServiceEmail;
            _applicationName = applicationName;
        }

        public void Emit(LogEvent logEvent)
        {
            // We only want to send emails for errors and fatal errors
            if (logEvent.Level < LogEventLevel.Error)
                return;

            var subject = $"{_applicationName} Error: {logEvent.Level} at {DateTime.Now}";
            
            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine("<h2>Error Details</h2>");
            bodyBuilder.AppendLine($"<p><strong>Timestamp:</strong> {logEvent.Timestamp}</p>");
            bodyBuilder.AppendLine($"<p><strong>Level:</strong> {logEvent.Level}</p>");
            
            if (logEvent.Exception != null)
            {
                bodyBuilder.AppendLine("<h3>Exception Details</h3>");
                bodyBuilder.AppendLine($"<p><strong>Exception Type:</strong> {logEvent.Exception.GetType().Name}</p>");
                bodyBuilder.AppendLine($"<p><strong>Message:</strong> {logEvent.Exception.Message}</p>");
                bodyBuilder.AppendLine($"<p><strong>Stack Trace:</strong></p>");
                bodyBuilder.AppendLine($"<pre>{logEvent.Exception.StackTrace}</pre>");
                
                if (logEvent.Exception.InnerException != null)
                {
                    bodyBuilder.AppendLine("<h4>Inner Exception</h4>");
                    bodyBuilder.AppendLine($"<p><strong>Type:</strong> {logEvent.Exception.InnerException.GetType().Name}</p>");
                    bodyBuilder.AppendLine($"<p><strong>Message:</strong> {logEvent.Exception.InnerException.Message}</p>");
                    bodyBuilder.AppendLine($"<pre>{logEvent.Exception.InnerException.StackTrace}</pre>");
                }
            }
            
            // Include log message
            bodyBuilder.AppendLine("<h3>Log Message</h3>");
            bodyBuilder.AppendLine($"<p>{logEvent.RenderMessage()}</p>");
            
            // Include log properties
            bodyBuilder.AppendLine("<h3>Additional Properties</h3>");
            bodyBuilder.AppendLine("<table border='1' cellpadding='5'>");
            bodyBuilder.AppendLine("<tr><th>Property</th><th>Value</th></tr>");
            
            foreach (var property in logEvent.Properties)
            {
                var value = property.Value.ToString();
                bodyBuilder.AppendLine($"<tr><td>{property.Key}</td><td>{value}</td></tr>");
            }
            
            bodyBuilder.AppendLine("</table>");
            
            // Send email asynchronously to avoid blocking
            _ = _emailService.SendEmailAsync(_customerServiceEmail, subject, bodyBuilder.ToString());
        }
    }
}