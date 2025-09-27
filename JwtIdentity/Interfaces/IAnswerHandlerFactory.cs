using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Interfaces
{
    /// <summary>
    /// Factory for creating answer handlers based on answer type
    /// </summary>
    public interface IAnswerHandlerFactory
    {
        /// <summary>
        /// Gets the appropriate handler for the given answer type
        /// </summary>
        /// <param name="answerType">The answer type to get handler for</param>
        /// <returns>The handler for the specified answer type</returns>
        /// <exception cref="NotSupportedException">Thrown when no handler is found for the answer type</exception>
        IAnswerHandler GetHandler(AnswerType answerType);

        /// <summary>
        /// Gets all registered handlers
        /// </summary>
        /// <returns>Collection of all registered answer handlers</returns>
        IEnumerable<IAnswerHandler> GetAllHandlers();

        /// <summary>
        /// Checks if a handler exists for the given answer type
        /// </summary>
        /// <param name="answerType">The answer type to check</param>
        /// <returns>True if a handler exists, false otherwise</returns>
        bool HasHandler(AnswerType answerType);
    }
}