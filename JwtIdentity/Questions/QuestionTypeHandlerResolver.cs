using JwtIdentity.Data;
using JwtIdentity.Models;

namespace JwtIdentity.Questions
{
    public sealed class QuestionTypeHandlerResolver : IQuestionTypeHandlerResolver
    {
        private readonly IReadOnlyDictionary<QuestionType, IQuestionTypeHandler> _handlers;
        private readonly IReadOnlyDictionary<AnswerType, IQuestionTypeHandler> _handlersByAnswer;

        public QuestionTypeHandlerResolver(IEnumerable<IQuestionTypeHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.QuestionType);
            _handlersByAnswer = handlers.ToDictionary(h => h.AnswerType);
            Handlers = _handlers.Values.ToList().AsReadOnly();
        }

        public IReadOnlyCollection<IQuestionTypeHandler> Handlers { get; }

        public IQuestionTypeHandler GetHandler(QuestionType questionType)
        {
            if (_handlers.TryGetValue(questionType, out var handler))
            {
                return handler;
            }

            throw new KeyNotFoundException($"No question handler registered for type '{questionType}'.");
        }

        public IQuestionTypeHandler GetHandler(AnswerType answerType)
        {
            if (_handlersByAnswer.TryGetValue(answerType, out var handler))
            {
                return handler;
            }

            throw new KeyNotFoundException($"No answer handler registered for type '{answerType}'.");
        }

        public async Task EnsureDependenciesLoadedAsync(ApplicationDbContext context, IEnumerable<Question> questions, CancellationToken cancellationToken = default)
        {
            if (questions == null)
            {
                throw new ArgumentNullException(nameof(questions));
            }

            var grouped = questions
                .Where(q => q != null)
                .GroupBy(q => q.QuestionType);

            foreach (var group in grouped)
            {
                if (_handlers.TryGetValue(group.Key, out var handler))
                {
                    await handler.EnsureDependenciesLoadedAsync(context, group.ToList(), cancellationToken);
                }
            }
        }
    }
}
