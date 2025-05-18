namespace JwtIdentity.Common.ViewModels
{
    // ...existing code...
    public class AppSettings
    {
        public LoggingOptions Logging { get; set; }
        //  public ConnectionStringsOptions ConnectionStrings { get; set; }
        //  public JwtOptions Jwt { get; set; }
        public bool DetailedErrors { get; set; }
        public string ApiBaseAddress { get; set; }
        public EmailSettings EmailSettings { get; set; }
        public WordPress WordPress { get; set; }
        public Youtube Youtube { get; set; }
    }

    public class LoggingOptions
    {
        public LogLevelOptions LogLevel { get; set; }
    }

    public class LogLevelOptions
    {
        public string Default { get; set; }
        public string MicrosoftAspNetCore { get; set; }
        public string MicrosoftEntityFrameworkCore { get; set; }
    }

    public class ConnectionStringsOptions
    {
        public string DefaultConnection { get; set; }
    }

    public class JwtOptions
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationMinutes { get; set; }
    }

    public class EmailSettings
    {
        public string CustomerServiceEmail { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public string Domain { get; set; }
    }

    public class WordPress
    {
        public string SiteDomain { get; set; }
        public string GetUrl { get; set; }
        public string SinglePostUrl { get; set; }
    }

    public class Youtube
    {
        public string HomePageCode { get; set; }
        public string ApiKey { get; set; }
    }
}