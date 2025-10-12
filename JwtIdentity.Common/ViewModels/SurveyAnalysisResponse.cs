namespace JwtIdentity.Common.ViewModels
{
    public class SurveyAnalysisResponse
    {
        public string OverallAnalysis { get; set; }
        public List<QuestionAnalysis> QuestionAnalyses { get; set; }
    }

    public class QuestionAnalysis
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; }
        public int QuestionType { get; set; }
        public string Analysis { get; set; }
    }
}
