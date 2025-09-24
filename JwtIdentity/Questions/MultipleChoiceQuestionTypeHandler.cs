using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Questions
{
    public sealed class MultipleChoiceQuestionTypeHandler : QuestionTypeHandler<MultipleChoiceQuestion, MultipleChoiceAnswer>
    {
        public MultipleChoiceQuestionTypeHandler(QuestionTypeDefinition definition, ILogger<MultipleChoiceQuestionTypeHandler> logger)
            : base(definition, logger)
        {
        }

        public override async Task EnsureDependenciesLoadedAsync(ApplicationDbContext context, IReadOnlyCollection<Question> questions, CancellationToken cancellationToken = default)
        {
            var ids = questions.OfType<MultipleChoiceQuestion>().Select(q => q.Id).ToList();
            if (ids.Count == 0)
            {
                return;
            }

            await context.Questions
                .OfType<MultipleChoiceQuestion>()
                .Where(q => ids.Contains(q.Id))
                .Include(q => q.Options)
                .LoadAsync(cancellationToken);
        }

        protected override Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, MultipleChoiceQuestion question, CancellationToken cancellationToken)
        {
            var options = question.Options?.OrderBy(o => o.Order).ToList() ?? new List<ChoiceOption>();
            var answerGroups = question.Answers?.OfType<MultipleChoiceAnswer>()
                .GroupBy(a => a.SelectedOptionId)
                .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<int, int>();

            viewModel.SurveyData = options
                .Select(o => new ChartData
                {
                    X = o.OptionText,
                    Y = answerGroups.TryGetValue(o.Id, out var count) ? count : 0
                })
                .ToList();

            return Task.CompletedTask;
        }

        protected override Answer CreateDemoAnswerInternal(MultipleChoiceQuestion question, ApplicationUser user, Random random)
        {
            if (question.Options == null || question.Options.Count == 0)
            {
                return null;
            }

            var option = question.Options[random.Next(question.Options.Count)];
            return new MultipleChoiceAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = option.Id,
                Complete = true,
                CreatedById = user.Id,
                IpAddress = "127.0.0.1"
            };
        }

        protected override Task UpdateNewQuestionAsync(ApplicationDbContext context, MultipleChoiceQuestion question)
        {
            if (question.Options != null)
            {
                int order = 1;
                foreach (var option in question.Options)
                {
                    if (option.Order == 0)
                    {
                        option.Order = order;
                    }
                    order++;
                }
            }

            return Task.CompletedTask;
        }

        public override async Task UpdateQuestionAsync(ApplicationDbContext context, Question updatedQuestion, CancellationToken cancellationToken = default)
        {
            var updated = CastQuestion(updatedQuestion);

            if (updated.Id == 0)
            {
                await UpdateNewQuestionAsync(context, updated);
                return;
            }

            var existing = await context.Questions
                .OfType<MultipleChoiceQuestion>()
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == updated.Id, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"Multiple choice question with ID {updated.Id} could not be found.");
            }

            await ApplyUpdatesAsync(existing, updated, context, cancellationToken);
        }

        protected override async Task ApplyUpdatesAsync(MultipleChoiceQuestion existing, MultipleChoiceQuestion updated, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            await base.ApplyUpdatesAsync(existing, updated, context, cancellationToken);

            existing.Options ??= new List<ChoiceOption>();
            var updatedOptions = updated.Options ?? new List<ChoiceOption>();

            foreach (var option in updatedOptions)
            {
                if (option.Id == 0)
                {
                    option.MultipleChoiceQuestionId = existing.Id;
                    existing.Options.Add(option);
                }
                else
                {
                    var existingOption = existing.Options.FirstOrDefault(o => o.Id == option.Id);
                    if (existingOption != null)
                    {
                        existingOption.OptionText = option.OptionText;
                        existingOption.Order = option.Order;
                    }
                }
            }

            var updatedIds = updatedOptions.Where(o => o.Id != 0).Select(o => o.Id).ToHashSet();
            var removed = existing.Options.Where(o => o.Id != 0 && !updatedIds.Contains(o.Id)).ToList();
            if (removed.Count > 0)
            {
                context.ChoiceOptions.RemoveRange(removed);
            }

            return;
        }

        public override bool ShouldUpdateAnswer(Answer newAnswer, Answer existingAnswer)
        {
            var newTyped = CastAnswer(newAnswer);
            var oldTyped = CastAnswer(existingAnswer);
            return newTyped.SelectedOptionId != oldTyped.SelectedOptionId || base.ShouldUpdateAnswer(newAnswer, existingAnswer);
        }
    }
}
