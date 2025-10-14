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
        public bool IsRequired { get; set; } = true; // Indicates if the question is mandatory
        public QuestionType QuestionType { get; set; } // E.g. Text, TrueFalse, MultipleChoice
        public int GroupId { get; set; } = 0; // Question group, default is 0
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

    public class Rating1To10Question : Question
    {
        // No additional fields for Rating1To10Question
    }

    public class MultipleChoiceQuestion : Question
    {
        // For multiple-choice, you might store a list of possible options
        public List<ChoiceOption> Options { get; set; }
    }

    public class SelectAllThatApplyQuestion : Question
    {
        // For select-all-that-apply, store a list of possible options like multiple choice
        public List<ChoiceOption> Options { get; set; }
    }
}
