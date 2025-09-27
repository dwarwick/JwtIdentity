using JwtIdentity.Common.Helpers;
using JwtIdentity.Interfaces;

namespace JwtIdentity.Services.QuestionHandlers
{
    /// <summary>
    /// Factory implementation for creating question handlers
    /// </summary>
    public class QuestionHandlerFactory : IQuestionHandlerFactory
    {
        private readonly Dictionary<QuestionType, IQuestionHandler> _handlers;

        public QuestionHandlerFactory(IEnumerable<IQuestionHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.SupportedType, h => h);
        }

        public IQuestionHandler GetHandler(QuestionType questionType)
        {
            if (_handlers.TryGetValue(questionType, out var handler))
            {
                return handler;
            }

            throw new NotSupportedException($"No handler found for question type: {questionType}");
        }

        public IEnumerable<IQuestionHandler> GetAllHandlers()
        {
            return _handlers.Values;
        }

        public bool HasHandler(QuestionType questionType)
        {
            return _handlers.ContainsKey(questionType);
        }
    }
}