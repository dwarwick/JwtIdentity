namespace JwtIdentity.Model
{
    public class ChoiceOption
    {
        public int Id { get; set; }
        public int MultipleChoiceQuestionId { get; set; }

        public string OptionText { get; set; }
    }
}
