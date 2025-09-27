using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Handler for multiple choice answers
    /// </summary>
    public class MultipleChoiceAnswerHandler : BaseAnswerHandler
    {
        public override AnswerType SupportedType => AnswerType.MultipleChoice;

        public override bool HasChanged(Answer newAnswer, Answer existingAnswer)
        {
            var newMultiple = (MultipleChoiceAnswer)newAnswer;
            var existingMultiple = (MultipleChoiceAnswer)existingAnswer;

            return newMultiple.SelectedOptionId != existingMultiple.SelectedOptionId || BasePropertiesChanged(newAnswer, existingAnswer);
        }

        public override bool IsValid(Answer answer)
        {
            if (!base.IsValid(answer) || answer is not MultipleChoiceAnswer multipleChoiceAnswer)
                return false;

            // Multiple choice answers should have a selected option if marked as complete
            return !multipleChoiceAnswer.Complete || multipleChoiceAnswer.SelectedOptionId > 0;
        }

        public override string GetDisplayValue(Answer answer)
        {
            if (answer is not MultipleChoiceAnswer multipleChoiceAnswer)
                return string.Empty;

            return multipleChoiceAnswer.SelectedOptionId > 0 ? $"Option {multipleChoiceAnswer.SelectedOptionId}" : "[No selection]";
        }
    }
}