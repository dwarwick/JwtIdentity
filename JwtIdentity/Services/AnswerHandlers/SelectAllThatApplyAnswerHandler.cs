using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    /// <summary>
    /// Handler for select-all-that-apply answers
    /// </summary>
    public class SelectAllThatApplyAnswerHandler : BaseAnswerHandler
    {
        public override AnswerType SupportedType => AnswerType.SelectAllThatApply;

        public override bool HasChanged(Answer newAnswer, Answer existingAnswer)
        {
            var newSelectAll = (SelectAllThatApplyAnswer)newAnswer;
            var existingSelectAll = (SelectAllThatApplyAnswer)existingAnswer;

            return newSelectAll.SelectedOptionIds != existingSelectAll.SelectedOptionIds || BasePropertiesChanged(newAnswer, existingAnswer);
        }

        public override bool IsValid(Answer answer)
        {
            if (!base.IsValid(answer) || answer is not SelectAllThatApplyAnswer selectAllAnswer)
                return false;

            // Select all that apply answers should have at least one selection if marked as complete
            return !selectAllAnswer.Complete || !string.IsNullOrWhiteSpace(selectAllAnswer.SelectedOptionIds);
        }

        public override string GetDisplayValue(Answer answer)
        {
            if (answer is not SelectAllThatApplyAnswer selectAllAnswer)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(selectAllAnswer.SelectedOptionIds))
                return "[No selections]";

            var selections = selectAllAnswer.SelectedOptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return selections.Length == 1 ? $"{selections.Length} selection" : $"{selections.Length} selections";
        }
    }
}