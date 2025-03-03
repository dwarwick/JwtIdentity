using System.ComponentModel.DataAnnotations.Schema;

namespace JwtIdentity.Models
{
    public abstract class Question : BaseModel
    {
        public int Id { get; set; }

        [ForeignKey("Survey")]
        public int SurveyId { get; set; }
        public string Text { get; set; }
        public int QuestionNumber { get; set; }
        // Could store an enum or string describing the question type
        public QuestionType QuestionType { get; set; } // E.g. Text, TrueFalse, MultipleChoice
        public List<Answer> Answers { get; set; }
    }

    public class TextQuestion : Question
    {
        // e.g. maybe you allow setting a max character limit, or additional constraints
        public int MaxLength { get; set; }
    }

    public class TrueFalseQuestion : Question
    {
        // Possibly no extra fields, but you might store a “default” or “explanation”
    }

    public class MultipleChoiceQuestion : Question
    {
        // For multiple-choice, you might store a list of possible options
        public List<ChoiceOption> Options { get; set; }
    }
}
