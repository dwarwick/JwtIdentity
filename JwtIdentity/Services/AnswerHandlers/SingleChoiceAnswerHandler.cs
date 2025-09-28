using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Handler for single choice answers
    /// </summary>
    public class SingleChoiceAnswerHandler : BaseAnswerHandler
    {
        public override AnswerType SupportedType => AnswerType.SingleChoice;

        public override bool HasChanged(Answer newAnswer, Answer existingAnswer)
        {
            var newSingle = (SingleChoiceAnswer)newAnswer;
            var existingSingle = (SingleChoiceAnswer)existingAnswer;

            return newSingle.SelectedOptionId != existingSingle.SelectedOptionId || BasePropertiesChanged(newAnswer, existingAnswer);
        }

        public override bool IsValid(Answer answer)
        {
            if (!base.IsValid(answer) || answer is not SingleChoiceAnswer singleChoiceAnswer)
                return false;

            // Single choice answers should have a selected option if marked as complete
            return !singleChoiceAnswer.Complete || singleChoiceAnswer.SelectedOptionId > 0;
        }

        public override string GetDisplayValue(Answer answer)
        {
            if (answer is not SingleChoiceAnswer singleChoiceAnswer)
                return string.Empty;

            return singleChoiceAnswer.SelectedOptionId > 0 ? $"Option {singleChoiceAnswer.SelectedOptionId}" : "[No selection]";
        }
    }
}