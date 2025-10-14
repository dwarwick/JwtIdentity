namespace JwtIdentity.Common.ViewModels
{
    public class QuestionGroupViewModel : BaseViewModel
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public int GroupNumber { get; set; }
        public string GroupName { get; set; }
        public int? NextGroupId { get; set; }
        public bool SubmitAfterGroup { get; set; }
    }
}
