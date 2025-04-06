namespace JwtIdentity.Models
{
    public enum FeedbackType
    {
        Problem,
        FeatureRequest,
        GeneralFeedback
    }

    public class Feedback : BaseModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public FeedbackType Type { get; set; }
        public string Email { get; set; }
        public bool IsResolved { get; set; }
    }
}