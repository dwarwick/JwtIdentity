using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Interfaces
{
    /// <summary>
    /// Interface for handling type-specific question operations
    /// </summary>
    public interface IQuestionHandler
    {
        /// <summary>
        /// The question type this handler supports
        /// </summary>
        QuestionType SupportedType { get; }

        /// <summary>
        /// Validates the question data
        /// </summary>
        /// <param name="question">The question to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValid(Question question);

        /// <summary>
        /// Gets a display-friendly string representation of the question configuration
        /// </summary>
        /// <param name="question">The question to get display info for</param>
        /// <returns>String representation of the question configuration</returns>
        string GetDisplayInfo(Question question);

        /// <summary>
        /// Handles question-specific deletion logic (e.g., removing options)
        /// </summary>
        /// <param name="questionId">The ID of the question being deleted</param>
        /// <param name="context">The database context</param>
        /// <returns>Task representing the async operation</returns>
        Task HandleDeletionAsync(int questionId, ApplicationDbContext context);

        /// <summary>
        /// Loads any related data for the question (e.g., options for choice questions)
        /// </summary>
        /// <param name="questionIds">List of question IDs to load data for</param>
        /// <param name="context">The database context</param>
        /// <returns>Task representing the async operation</returns>
        Task LoadRelatedDataAsync(List<int> questionIds, ApplicationDbContext context);

        /// <summary>
        /// Creates a demo answer for this question type
        /// </summary>
        /// <param name="question">The question to create an answer for</param>
        /// <param name="random">Random number generator</param>
        /// <param name="userId">User ID for the answer</param>
        /// <returns>A demo answer instance</returns>
        Answer CreateDemoAnswer(Question question, Random random, string userId);
    }
}