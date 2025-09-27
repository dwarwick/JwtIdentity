using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Handler for true/false answers
    /// </summary>
    public class TrueFalseAnswerHandler : BaseAnswerHandler
    {
        public override AnswerType SupportedType => AnswerType.TrueFalse;

        public override bool HasChanged(Answer newAnswer, Answer existingAnswer)
        {
            var newTrueFalse = (TrueFalseAnswer)newAnswer;
            var existingTrueFalse = (TrueFalseAnswer)existingAnswer;

            return newTrueFalse.Value != existingTrueFalse.Value || BasePropertiesChanged(newAnswer, existingAnswer);
        }

        public override bool IsValid(Answer answer)
        {
            if (!base.IsValid(answer) || answer is not TrueFalseAnswer trueFalseAnswer)
                return false;

            // True/false answers should have a value if marked as complete
            return !trueFalseAnswer.Complete || trueFalseAnswer.Value.HasValue;
        }

        public override string GetDisplayValue(Answer answer)
        {
            if (answer is not TrueFalseAnswer trueFalseAnswer)
                return string.Empty;

            return trueFalseAnswer.Value?.ToString() ?? "[No selection]";
        }
    }
}