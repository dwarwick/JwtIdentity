namespace JwtIdentity.Models
{
    public class ChoiceOption
    {
        public int Id { get; set; }
        public int? MultipleChoiceQuestionId { get; set; }
        public int? SelectAllThatApplyQuestionId { get; set; }
        public string OptionText { get; set; }
        public int Order { get; set; }
        public int? BranchToGroupId { get; set; } // Group to branch to when this option is selected

        public MultipleChoiceQuestion MultipleChoiceQuestion { get; set; }
        public SelectAllThatApplyQuestion SelectAllThatApplyQuestion { get; set; }
    }
}
