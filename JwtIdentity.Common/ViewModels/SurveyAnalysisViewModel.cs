namespace JwtIdentity.Common.ViewModels
{
    public class SurveyAnalysisViewModel : BaseViewModel
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Analysis { get; set; }
    }
}
