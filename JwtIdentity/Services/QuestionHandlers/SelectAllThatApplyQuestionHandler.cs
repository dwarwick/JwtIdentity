using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Services.QuestionHandlers
{
    /// <summary>
    /// Handler for select-all-that-apply questions
    /// </summary>
    public class SelectAllThatApplyQuestionHandler : BaseQuestionHandler
    {
        public override QuestionType SupportedType => QuestionType.SelectAllThatApply;

        public override bool IsValid(Question question)
        {
            if (!base.IsValid(question) || question is not SelectAllThatApplyQuestion selectAllQuestion)
                return false;

            // Select-all questions should have at least one option
            return selectAllQuestion.Options != null && selectAllQuestion.Options.Count > 0;
        }

        public override string GetDisplayInfo(Question question)
        {
            if (question is not SelectAllThatApplyQuestion selectAllQuestion)
                return string.Empty;

            var optionCount = selectAllQuestion.Options?.Count ?? 0;
            return $"Select all that apply ({optionCount} option{(optionCount == 1 ? "" : "s")})";
        }

        public override async Task HandleDeletionAsync(int questionId, ApplicationDbContext context)
        {
            // Delete associated choice options for select-all questions
            var selectAllQuestion = await context.Questions.OfType<SelectAllThatApplyQuestion>()
                .Include(x => x.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
                
            if (selectAllQuestion?.Options != null)
            {
                context.ChoiceOptions.RemoveRange(selectAllQuestion.Options);
            }
        }

        public override async Task LoadRelatedDataAsync(List<int> questionIds, ApplicationDbContext context)
        {
            // Load options for select-all questions
            if (questionIds.Any())
            {
                await context.Questions
                    .OfType<SelectAllThatApplyQuestion>()
                    .Where(q => questionIds.Contains(q.Id))
                    .Include(q => q.Options.OrderBy(o => o.Order))
                    .LoadAsync();
            }
        }

        public override Answer CreateDemoAnswer(Question question, Random random, string userId)
        {
            if (question is not SelectAllThatApplyQuestion selectAllQuestion || !selectAllQuestion.Options.Any())
            {
                return null;
            }

            // Randomly select some options
            var selectedOptions = selectAllQuestion.Options
                .Where(_ => random.Next(0, 2) == 1)
                .Select(o => o.Id)
                .ToList();

            // Ensure at least one option is selected
            if (!selectedOptions.Any())
            {
                selectedOptions.Add(selectAllQuestion.Options[random.Next(selectAllQuestion.Options.Count)].Id);
            }

            return new SelectAllThatApplyAnswer
            {
                QuestionId = question.Id,
                SelectedOptionIds = string.Join(",", selectedOptions),
                Complete = true,
                CreatedById = int.Parse(userId),
                IpAddress = "127.0.0.1",
                AnswerType = AnswerType.SelectAllThatApply
            };
        }
    }
}