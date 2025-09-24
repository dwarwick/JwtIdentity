using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;


namespace JwtIdentity.Questions
{
    public static class QuestionDomainRegistry
    {
        private static readonly Dictionary<QuestionType, QuestionDomainDefinition> _definitions = new();
        private static readonly Dictionary<AnswerType, QuestionDomainDefinition> _definitionsByAnswer = new();
        private static readonly object _initializationLock = new();
        private static bool _initialized;

        public static IReadOnlyCollection<QuestionDomainDefinition> All
        {
            get
            {
                EnsureInitialized();
                return _definitions.Values;
            }
        }

        public static QuestionDomainDefinition Get(QuestionType questionType)
        {
            EnsureInitialized();

            if (_definitions.TryGetValue(questionType, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"No handler registered for question type '{questionType}'.");
        }

        public static QuestionDomainDefinition GetByAnswer(AnswerType answerType)
        {
            EnsureInitialized();


            if (_definitionsByAnswer.TryGetValue(answerType, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"No handler registered for answer type '{answerType}'.");
        }

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (_initializationLock)
            {
                if (_initialized)
                {
                    return;
                }

                Register(new QuestionDomainDefinition(
                    QuestionTypeRegistry.GetDefinition(QuestionType.Text),
                    typeof(TextQuestion),
                    typeof(TextAnswer),
                    typeof(TextQuestionTypeHandler)));

                Register(new QuestionDomainDefinition(
                    QuestionTypeRegistry.GetDefinition(QuestionType.TrueFalse),
                    typeof(TrueFalseQuestion),
                    typeof(TrueFalseAnswer),
                    typeof(TrueFalseQuestionTypeHandler)));

                Register(new QuestionDomainDefinition(
                    QuestionTypeRegistry.GetDefinition(QuestionType.Rating1To10),
                    typeof(Rating1To10Question),
                    typeof(Rating1To10Answer),
                    typeof(RatingQuestionTypeHandler)));

                Register(new QuestionDomainDefinition(
                    QuestionTypeRegistry.GetDefinition(QuestionType.MultipleChoice),
                    typeof(MultipleChoiceQuestion),
                    typeof(MultipleChoiceAnswer),
                    typeof(MultipleChoiceQuestionTypeHandler)));

                Register(new QuestionDomainDefinition(
                    QuestionTypeRegistry.GetDefinition(QuestionType.SelectAllThatApply),
                    typeof(SelectAllThatApplyQuestion),
                    typeof(SelectAllThatApplyAnswer),
                    typeof(SelectAllThatApplyQuestionTypeHandler)));

                _initialized = true;
            }
        }


        internal static void Register(QuestionDomainDefinition definition)
        {
            if (_definitions.ContainsKey(definition.QuestionType))
            {
                throw new InvalidOperationException($"Question type '{definition.QuestionType}' is already registered.");
            }

            _definitions.Add(definition.QuestionType, definition);
            _definitionsByAnswer.Add(definition.AnswerType, definition);

            QuestionTypeRegistry.AttachDomainTypes(definition.QuestionType, definition.QuestionEntityType, definition.AnswerEntityType);
        }
    }
}
