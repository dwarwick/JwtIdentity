namespace JwtIdentity.Common.ViewModels
{
    public class SurveyDataViewModel
    {
        public List<ChartData> SurveyData { get; set; } = new List<ChartData>();

        public QuestionType QuestionType { get; set; }

        public QuestionViewModel Question { get; set; }

        public TextQuestionViewModel TextQuestion { get; set; }

        public TrueFalseQuestionViewModel TrueFalseQuestion { get; set; }

        public MultipleChoiceQuestionViewModel MultipleChoiceQuestion { get; set; }

        public Rating1To10QuestionViewModel Rating1To10Question { get; set; }

        public SelectAllThatApplyQuestionViewModel SelectAllThatApplyQuestion { get; set; }
    }

    public class ChartData
    {
        public string X { get; set; }
        public double Y { get; set; }
    }
}
