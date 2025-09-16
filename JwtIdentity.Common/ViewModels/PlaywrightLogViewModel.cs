namespace JwtIdentity.Common.ViewModels
{
    public class PlaywrightLogViewModel
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FailedElement { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string? Browser { get; set; }
    }
}
