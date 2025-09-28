using JwtIdentity.Common.Helpers;
using JwtIdentity.Interfaces;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Services.QuestionHandlers
{
    /// <summary>
    /// Base abstract implementation of IQuestionHandler with common functionality
    /// </summary>
    public abstract class BaseQuestionHandler : IQuestionHandler
    {
        public abstract QuestionType SupportedType { get; }

        public virtual bool IsValid(Question question)
        {
            // Basic validation - all questions should have text and correct type
            return question != null && 
                   question.QuestionType == SupportedType && 
                   !string.IsNullOrWhiteSpace(question.Text);
        }

        public abstract string GetDisplayInfo(Question question);

        public virtual async Task HandleDeletionAsync(int questionId, ApplicationDbContext context)
        {
            // Base implementation - no special cleanup needed for simple question types
            await Task.CompletedTask;
        }

        public virtual async Task LoadRelatedDataAsync(List<int> questionIds, ApplicationDbContext context)
        {
            // Base implementation - no related data to load for simple question types
            await Task.CompletedTask;
        }

        public abstract Answer CreateDemoAnswer(Question question, Random random, string userId);
    }
}