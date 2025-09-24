using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Questions
{
    public static class QuestionDomainRegistry
    {
        private static readonly Dictionary<QuestionType, QuestionDomainDefinition> _definitions = new();
        private static readonly Dictionary<AnswerType, QuestionDomainDefinition> _definitionsByAnswer = new();

        public static IReadOnlyCollection<QuestionDomainDefinition> All => _definitions.Values;

        public static QuestionDomainDefinition Get(QuestionType questionType)
        {
            if (_definitions.TryGetValue(questionType, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"No handler registered for question type '{questionType}'.");
        }

        public static QuestionDomainDefinition GetByAnswer(AnswerType answerType)
        {
            if (_definitionsByAnswer.TryGetValue(answerType, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"No handler registered for answer type '{answerType}'.");
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
