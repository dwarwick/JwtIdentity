using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Questions
{
    public sealed class QuestionDomainDefinition
    {
        public QuestionDomainDefinition(QuestionTypeDefinition definition, Type questionEntityType, Type answerEntityType, Type handlerType)
        {
            Definition = definition;
            QuestionEntityType = questionEntityType;
            AnswerEntityType = answerEntityType;
            HandlerType = handlerType;
        }

        public QuestionTypeDefinition Definition { get; }

        public QuestionType QuestionType => Definition.QuestionType;

        public AnswerType AnswerType => Definition.AnswerType;

        public Type QuestionEntityType { get; }

        public Type AnswerEntityType { get; }

        public Type HandlerType { get; }
    }
}
