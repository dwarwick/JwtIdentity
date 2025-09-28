using JwtIdentity.Common.Helpers;
using JwtIdentity.Interfaces;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Factory implementation for creating answer handlers
    /// </summary>
    public class AnswerHandlerFactory : IAnswerHandlerFactory
    {
        private readonly Dictionary<AnswerType, IAnswerHandler> _handlers;

        public AnswerHandlerFactory(IEnumerable<IAnswerHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.SupportedType, h => h);
        }

        public IAnswerHandler GetHandler(AnswerType answerType)
        {
            if (_handlers.TryGetValue(answerType, out var handler))
            {
                return handler;
            }

            throw new NotSupportedException($"No handler found for answer type: {answerType}");
        }

        public IEnumerable<IAnswerHandler> GetAllHandlers()
        {
            return _handlers.Values;
        }

        public bool HasHandler(AnswerType answerType)
        {
            return _handlers.ContainsKey(answerType);
        }
    }
}