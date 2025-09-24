using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "questionType", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(TextQuestionViewModel), (int)QuestionType.Text)]
    [JsonDerivedType(typeof(TrueFalseQuestionViewModel), (int)QuestionType.TrueFalse)]
    [JsonDerivedType(typeof(MultipleChoiceQuestionViewModel), (int)QuestionType.MultipleChoice)]
    [JsonDerivedType(typeof(Rating1To10QuestionViewModel), (int)QuestionType.Rating1To10)]
    [JsonDerivedType(typeof(SelectAllThatApplyQuestionViewModel), (int)QuestionType.SelectAllThatApply)]
    public abstract class QuestionViewModel : BaseViewModel
    {
        protected QuestionViewModel()
        {
        }

        protected QuestionViewModel(QuestionType questionType)
        {
            QuestionType = questionType;
        }

        public int Id { get; set; }

        public int SurveyId { get; set; }

        public string Text { get; set; }

        public int QuestionNumber { get; set; }

        public bool IsRequired { get; set; } = true; // Indicates if the question is mandatory

        [JsonPropertyName("questionType")]
        public QuestionType QuestionType { get; set; }

        public List<AnswerViewModel> Answers { get; set; }

        [JsonIgnore]
        public QuestionTypeDefinition Definition => QuestionTypeRegistry.GetDefinition(QuestionType);
    }

    public class TextQuestionViewModel : QuestionViewModel
    {
        public TextQuestionViewModel() : base(QuestionType.Text)
        {
        }

        public int MaxLength { get; set; }
    }

    public class TrueFalseQuestionViewModel : QuestionViewModel
    {
        public TrueFalseQuestionViewModel() : base(QuestionType.TrueFalse)
        {
        }
        // No additional fields for TrueFalseQuestion
    }

    public class Rating1To10QuestionViewModel : QuestionViewModel
    {
        public Rating1To10QuestionViewModel() : base(QuestionType.Rating1To10)
        {
        }
        // No additional fields for Rating1To10Question
    }

    public class MultipleChoiceQuestionViewModel : QuestionViewModel
    {
        public MultipleChoiceQuestionViewModel() : base(QuestionType.MultipleChoice)
        {
        }

        public List<ChoiceOptionViewModel> Options { get; set; } = new List<ChoiceOptionViewModel>();
    }

    public class SelectAllThatApplyQuestionViewModel : QuestionViewModel
    {
        public SelectAllThatApplyQuestionViewModel() : base(QuestionType.SelectAllThatApply)
        {
        }

        public List<ChoiceOptionViewModel> Options { get; set; } = new List<ChoiceOptionViewModel>();
    }

    public class BaseQuestionDto
    {
        public int Id { get; set; }

        public int SurveyId { get; set; }

        public string Text { get; set; }

        public int QuestionNumber { get; set; }

        public QuestionType QuestionType { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }
        // etc. – but *no* Answers or derived class fields
    }
}
