namespace JwtIdentity.Common.ViewModels
{
    public class SurveyDataViewModel
    {
        public List<ChartData> SurveyData { get; set; } = new List<ChartData>();

        public QuestionType QuestionType { get; set; }

        public QuestionViewModel Question { get; set; }

        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class ChartData
    {
        public string X { get; set; }
        public double Y { get; set; }
    }
}
