using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Interfaces
{
    /// <summary>
    /// Interface for handling type-specific answer operations
    /// </summary>
    public interface IAnswerHandler
    {
        /// <summary>
        /// The answer type this handler supports
        /// </summary>
        AnswerType SupportedType { get; }

        /// <summary>
        /// Determines if an existing answer has changed and needs updating
        /// </summary>
        /// <param name="newAnswer">The new answer data</param>
        /// <param name="existingAnswer">The existing answer from database</param>
        /// <returns>True if the answer has changed and needs updating</returns>
        bool HasChanged(Answer newAnswer, Answer existingAnswer);

        /// <summary>
        /// Validates the answer data
        /// </summary>
        /// <param name="answer">The answer to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValid(Answer answer);

        /// <summary>
        /// Gets a display-friendly string representation of the answer value
        /// </summary>
        /// <param name="answer">The answer to get display value for</param>
        /// <returns>String representation of the answer</returns>
        string GetDisplayValue(Answer answer);
    }
}