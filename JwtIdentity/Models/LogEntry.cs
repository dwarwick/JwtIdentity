namespace JwtIdentity.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime LoggedAt { get; set; }
        
        // Request information
        public string RequestPath { get; set; }
        public string RequestMethod { get; set; }
        public string Parameters { get; set; }
        public int? StatusCode { get; set; }
        
        // Exception details
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
    }
}