using JwtIdentity.Common.ViewModels;

namespace JwtIdentity.Common.Helpers
{
    public abstract class QuestionTypeDefinition
    {
        private readonly Func<QuestionViewModel> _questionFactory;
        private readonly Func<AnswerViewModel> _answerFactory;
        private readonly Action<QuestionViewModel, AnswerViewModel> _answerInitializer;

        protected QuestionTypeDefinition(
            QuestionType questionType,
            AnswerType answerType,
            string displayName,
            bool requiresOptions,
            bool allowsMultipleSelections,
            Type questionViewModelType,
            Type answerViewModelType,
            Func<QuestionViewModel> questionFactory,
            Func<AnswerViewModel> answerFactory,
            Action<QuestionViewModel, AnswerViewModel> answerInitializer)
        {
            QuestionType = questionType;
            AnswerType = answerType;
            DisplayName = displayName;
            RequiresOptions = requiresOptions;
            AllowsMultipleSelections = allowsMultipleSelections;
            QuestionViewModelType = questionViewModelType;
            AnswerViewModelType = answerViewModelType;
            _questionFactory = questionFactory;
            _answerFactory = answerFactory;
            _answerInitializer = answerInitializer ?? ((_, _) => { });
        }

        public QuestionType QuestionType { get; }

        public AnswerType AnswerType { get; }

        public string DisplayName { get; }

        public bool RequiresOptions { get; }

        public bool AllowsMultipleSelections { get; }

        public Type QuestionViewModelType { get; }

        public Type AnswerViewModelType { get; }

        internal Type QuestionEntityType { get; private set; }

        internal Type AnswerEntityType { get; private set; }

        public QuestionViewModel CreateQuestionViewModel()
        {
            QuestionViewModel model = _questionFactory();
            model.QuestionType = QuestionType;
            return model;
        }

        public AnswerViewModel CreateAnswerViewModel()
        {
            AnswerViewModel answer = _answerFactory();
            answer.AnswerType = AnswerType;
            return answer;
        }

        public AnswerViewModel EnsureAnswerInitialized(QuestionViewModel question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            question.Answers ??= new List<AnswerViewModel>();

            AnswerViewModel answer = question.Answers
                .FirstOrDefault(a => a != null && a.AnswerType == AnswerType && a.GetType() == AnswerViewModelType);

            if (answer == null)
            {
                answer = CreateAnswerViewModel();

                // Replace any existing answer for this question/answer type
                question.Answers.RemoveAll(a => a?.AnswerType == AnswerType);
                question.Answers.Add(answer);
            }

            // Always ensure the answer is associated with the current question.
            // Without this, newly initialized answers may be posted with a QuestionId of 0,
            // which the API rightfully rejects as invalid.
            answer.QuestionId = question.Id;

            _answerInitializer(question, answer);

            return answer;
        }

        internal void AttachDomainTypes(Type questionEntityType, Type answerEntityType)
        {
            QuestionEntityType = questionEntityType ?? throw new ArgumentNullException(nameof(questionEntityType));
            AnswerEntityType = answerEntityType ?? throw new ArgumentNullException(nameof(answerEntityType));
        }
    }

    public sealed class QuestionTypeDefinition<TQuestionViewModel, TAnswerViewModel> : QuestionTypeDefinition
        where TQuestionViewModel : QuestionViewModel, new()
        where TAnswerViewModel : AnswerViewModel, new()
    {
        public QuestionTypeDefinition(
            QuestionType questionType,
            AnswerType answerType,
            string displayName,
            bool requiresOptions = false,
            bool allowsMultipleSelections = false,
            Action<TQuestionViewModel, TAnswerViewModel> answerInitializer = null)
            : base(
                questionType,
                answerType,
                displayName,
                requiresOptions,
                allowsMultipleSelections,
                typeof(TQuestionViewModel),
                typeof(TAnswerViewModel),
                () => new TQuestionViewModel(),
                () => new TAnswerViewModel(),
                answerInitializer != null
                    ? new Action<QuestionViewModel, AnswerViewModel>((q, a) => answerInitializer((TQuestionViewModel)q, (TAnswerViewModel)a))
                    : ((_, _) => { }))
        {
        }
    }
}
