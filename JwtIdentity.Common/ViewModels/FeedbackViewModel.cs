namespace JwtIdentity.Common.ViewModels
{
    public enum FeedbackType
    {
        Problem,
        FeatureRequest,
        GeneralFeedback
    }

    public class FeedbackViewModel : BaseViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public FeedbackType Type { get; set; }
        public string Email { get; set; }
        public bool IsResolved { get; set; }
    }
}