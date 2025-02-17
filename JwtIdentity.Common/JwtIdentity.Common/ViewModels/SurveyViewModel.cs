namespace JwtIdentity.Common.ViewModels
{
    public class SurveyViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<QuestionViewModel> Questions { get; set; }
    }
}
