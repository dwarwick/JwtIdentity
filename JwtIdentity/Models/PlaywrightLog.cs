namespace JwtIdentity.Models
{
    public class PlaywrightLog
    {
        public int Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ErrorMessage { get; set; }
        public string FailedElement { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string Browser { get; set; }
    }
}
