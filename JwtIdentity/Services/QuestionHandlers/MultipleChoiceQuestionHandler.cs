using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtIdentity.Services.QuestionHandlers
{
    /// <summary>
    /// Handler for multiple choice questions
    /// </summary>
    public class MultipleChoiceQuestionHandler : BaseQuestionHandler
    {
        public override QuestionType SupportedType => QuestionType.MultipleChoice;

        public override bool IsValid(Question question)
        {
            if (!base.IsValid(question) || question is not MultipleChoiceQuestion mcQuestion)
                return false;

            // Multiple choice questions should have at least one option
            return mcQuestion.Options != null && mcQuestion.Options.Count > 0;
        }

        public override string GetDisplayInfo(Question question)
        {
            if (question is not MultipleChoiceQuestion mcQuestion)
                return string.Empty;

            var optionCount = mcQuestion.Options?.Count ?? 0;
            return $"Multiple choice ({optionCount} option{(optionCount == 1 ? "" : "s")})";
        }

        public override async Task HandleDeletionAsync(int questionId, ApplicationDbContext context)
        {
            // Delete associated choice options for multiple choice questions
            var mcQuestion = await context.Questions.OfType<MultipleChoiceQuestion>()
                .Include(x => x.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
                
            if (mcQuestion?.Options != null)
            {
                context.ChoiceOptions.RemoveRange(mcQuestion.Options);
            }
        }

        public override async Task LoadRelatedDataAsync(List<int> questionIds, ApplicationDbContext context)
        {
            // Load options for multiple choice questions
            if (questionIds.Any())
            {
                await context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(q => questionIds.Contains(q.Id))
                    .Include(q => q.Options.OrderBy(o => o.Order))
                    .LoadAsync();
            }
        }

        public override Answer CreateDemoAnswer(Question question, Random random, string userId)
        {
            if (question is not MultipleChoiceQuestion mcQuestion || !mcQuestion.Options.Any())
            {
                return null;
            }

            return new MultipleChoiceAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = mcQuestion.Options[random.Next(mcQuestion.Options.Count)].Id,
                Complete = true,
                CreatedById = int.Parse(userId),
                IpAddress = "127.0.0.1",
                AnswerType = AnswerType.MultipleChoice
            };
        }
    }
}