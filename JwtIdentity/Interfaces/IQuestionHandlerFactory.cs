using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Interfaces
{
    /// <summary>
    /// Factory for creating question handlers based on question type
    /// </summary>
    public interface IQuestionHandlerFactory
    {
        /// <summary>
        /// Gets the appropriate handler for the given question type
        /// </summary>
        /// <param name="questionType">The question type to get handler for</param>
        /// <returns>The handler for the specified question type</returns>
        /// <exception cref="NotSupportedException">Thrown when no handler is found for the question type</exception>
        IQuestionHandler GetHandler(QuestionType questionType);

        /// <summary>
        /// Gets all registered handlers
        /// </summary>
        /// <returns>Collection of all registered question handlers</returns>
        IEnumerable<IQuestionHandler> GetAllHandlers();

        /// <summary>
        /// Checks if a handler exists for the given question type
        /// </summary>
        /// <param name="questionType">The question type to check</param>
        /// <returns>True if a handler exists, false otherwise</returns>
        bool HasHandler(QuestionType questionType);
    }
}