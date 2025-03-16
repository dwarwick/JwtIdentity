namespace JwtIdentity.Common.ViewModels
{
    public class ChoiceOptionViewModel
    {
        public int Id { get; set; }
        public int MultipleChoiceQuestionId { get; set; }
        public string OptionText { get; set; }
        public int Order { get; set; }
    }
}
