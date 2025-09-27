using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Handler for rating 1-to-10 answers
    /// </summary>
    public class Rating1To10AnswerHandler : BaseAnswerHandler
    {
        public override AnswerType SupportedType => AnswerType.Rating1To10;

        public override bool HasChanged(Answer newAnswer, Answer existingAnswer)
        {
            var newRating = (Rating1To10Answer)newAnswer;
            var existingRating = (Rating1To10Answer)existingAnswer;

            return newRating.SelectedOptionId != existingRating.SelectedOptionId || BasePropertiesChanged(newAnswer, existingAnswer);
        }

        public override bool IsValid(Answer answer)
        {
            if (!base.IsValid(answer) || answer is not Rating1To10Answer ratingAnswer)
                return false;

            // Rating answers should have a selected option if marked as complete
            // Option ID should be between 1-10 for valid ratings
            return !ratingAnswer.Complete || (ratingAnswer.SelectedOptionId >= 1 && ratingAnswer.SelectedOptionId <= 10);
        }

        public override string GetDisplayValue(Answer answer)
        {
            if (answer is not Rating1To10Answer ratingAnswer)
                return string.Empty;

            return ratingAnswer.SelectedOptionId > 0 ? $"Rating: {ratingAnswer.SelectedOptionId}/10" : "[No rating]";
        }
    }
}