using JwtIdentity.Common.Helpers;
using JwtIdentity.Interfaces;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Base abstract implementation of IAnswerHandler with common functionality
    /// </summary>
    public abstract class BaseAnswerHandler : IAnswerHandler
    {
        public abstract AnswerType SupportedType { get; }

        public abstract bool HasChanged(Answer newAnswer, Answer existingAnswer);

        public virtual bool IsValid(Answer answer)
        {
            // Basic validation - all answers should have a valid answer type and complete status
            return answer != null && answer.AnswerType == SupportedType;
        }

        public abstract string GetDisplayValue(Answer answer);

        /// <summary>
        /// Helper method to check if base properties have changed
        /// </summary>
        /// <param name="newAnswer">New answer</param>
        /// <param name="existingAnswer">Existing answer</param>
        /// <returns>True if base properties changed</returns>
        protected bool BasePropertiesChanged(Answer newAnswer, Answer existingAnswer)
        {
            return newAnswer.Complete != existingAnswer.Complete;
        }
    }
}