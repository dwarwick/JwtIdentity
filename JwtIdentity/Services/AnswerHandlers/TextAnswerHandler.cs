using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Handler for text-based answers
    /// </summary>
    public class TextAnswerHandler : BaseAnswerHandler
    {
        public override AnswerType SupportedType => AnswerType.Text;

        public override bool HasChanged(Answer newAnswer, Answer existingAnswer)
        {
            var newText = (TextAnswer)newAnswer;
            var existingText = (TextAnswer)existingAnswer;

            return newText.Text != existingText.Text || BasePropertiesChanged(newAnswer, existingAnswer);
        }

        public override bool IsValid(Answer answer)
        {
            if (!base.IsValid(answer) || answer is not TextAnswer textAnswer)
                return false;

            // Text answers should have text content if marked as complete
            return !textAnswer.Complete || !string.IsNullOrWhiteSpace(textAnswer.Text);
        }

        public override string GetDisplayValue(Answer answer)
        {
            if (answer is not TextAnswer textAnswer)
                return string.Empty;

            return string.IsNullOrWhiteSpace(textAnswer.Text) ? "[No text provided]" : textAnswer.Text;
        }
    }
}