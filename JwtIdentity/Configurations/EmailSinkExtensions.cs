using JwtIdentity.Services;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace JwtIdentity.Configurations
{
    public static class EmailSinkExtensions
    {
        public static LoggerConfiguration EmailSink(
            this LoggerSinkConfiguration loggerConfiguration,
            IEmailService emailService,
            string customerServiceEmail,
            string applicationName,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Error)
        {
            return loggerConfiguration.Sink(
                new EmailLogEventSink(emailService, customerServiceEmail, applicationName),
                restrictedToMinimumLevel);
        }
    }
}