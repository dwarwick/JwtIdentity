using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$answerType", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(TextAnswerViewModel), (int)AnswerType.Text)]
    [JsonDerivedType(typeof(TrueFalseAnswerViewModel), (int)AnswerType.TrueFalse)]
    [JsonDerivedType(typeof(SingleChoiceAnswerViewModel), (int)AnswerType.SingleChoice)]
    [JsonDerivedType(typeof(MultipleChoiceAnswerViewModel), (int)AnswerType.MultipleChoice)]
    [JsonDerivedType(typeof(Rating1To10AnswerViewModel), (int)AnswerType.Rating1To10)]
    [JsonDerivedType(typeof(SelectAllThatApplyAnswerViewModel), (int)AnswerType.SelectAllThatApply)]
    public abstract class AnswerViewModel : BaseViewModel
    {
        protected AnswerViewModel()
        {
        }

        protected AnswerViewModel(AnswerType answerType)
        {
            AnswerType = answerType;
        }

        public int Id { get; set; }

        public int QuestionId { get; set; }

        public string IpAddress { get; set; }

        public bool Complete { get; set; }

        [JsonPropertyName("answerType")]
        public AnswerType AnswerType { get; set; }

        [JsonIgnore]
        public QuestionTypeDefinition Definition => QuestionTypeRegistry.GetDefinitionForAnswer(AnswerType);

        // For TrueFalse answers, cast
        public bool? TrueFalseValue
            => this is TrueFalseAnswerViewModel tf ? tf.Value : null;

        // For Text answers, cast
        public string TextValue
            => this is TextAnswerViewModel ta ? ta.Text : null;

        // For multiple/single choice, etc.
        public int? SelectedOptionValue
            => this switch
            {
                MultipleChoiceAnswerViewModel m => m.SelectedOptionId,
                SingleChoiceAnswerViewModel s => s.SelectedOptionId,
                Rating1To10AnswerViewModel r => r.SelectedOptionId,
                SelectAllThatApplyAnswerViewModel => null, // Multiple selections, no single value
                _ => null
            };
    }

    public class TextAnswerViewModel : AnswerViewModel
    {
        public TextAnswerViewModel() : base(AnswerType.Text)
        {
        }

        public string Text { get; set; }
    }

    public class TrueFalseAnswerViewModel : AnswerViewModel
    {
        public TrueFalseAnswerViewModel() : base(AnswerType.TrueFalse)
        {
        }

        public bool? Value { get; set; }
    }

    public class MultipleChoiceAnswerViewModel : AnswerViewModel
    {
        public MultipleChoiceAnswerViewModel() : base(AnswerType.MultipleChoice)
        {
        }

        public int SelectedOptionId { get; set; }

        public List<ChoiceOptionViewModel> Options { get; set; }
    }

    public class Rating1To10AnswerViewModel : AnswerViewModel
    {
        public Rating1To10AnswerViewModel() : base(AnswerType.Rating1To10)
        {
        }

        public int SelectedOptionId { get; set; }
    }

    public class SingleChoiceAnswerViewModel : AnswerViewModel
    {
        public SingleChoiceAnswerViewModel() : base(AnswerType.SingleChoice)
        {
        }

        public int SelectedOptionId { get; set; }
    }

    public class SelectAllThatApplyAnswerViewModel : AnswerViewModel
    {
        public SelectAllThatApplyAnswerViewModel() : base(AnswerType.SelectAllThatApply)
        {
        }

        public string SelectedOptionIds { get; set; }
        public List<bool> SelectedOptions { get; set; } = new List<bool>();
        public List<ChoiceOptionViewModel> Options { get; set; }
    }

    public class SurveyResponseViewModel
    {
        public int ResponseId { get; set; }
        public string IpAddress { get; set; }

        // Key = QuestionId, Value = an actual AnswerViewModel (TextAnswer, TrueFalseAnswer, etc.)
        public Dictionary<int, AnswerViewModel> Answers { get; } = new();
    }
}
