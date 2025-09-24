using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Questions
{
    public sealed class SelectAllThatApplyQuestionTypeHandler : QuestionTypeHandler<SelectAllThatApplyQuestion, SelectAllThatApplyAnswer>
    {
        public SelectAllThatApplyQuestionTypeHandler(QuestionTypeDefinition definition, ILogger<SelectAllThatApplyQuestionTypeHandler> logger)
            : base(definition, logger)
        {
        }

        public override async Task EnsureDependenciesLoadedAsync(ApplicationDbContext context, IReadOnlyCollection<Question> questions, CancellationToken cancellationToken = default)
        {
            var ids = questions.OfType<SelectAllThatApplyQuestion>().Select(q => q.Id).ToList();
            if (ids.Count == 0)
            {
                return;
            }

            await context.Questions
                .OfType<SelectAllThatApplyQuestion>()
                .Where(q => ids.Contains(q.Id))
                .Include(q => q.Options)
                .LoadAsync(cancellationToken);
        }

        protected override Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, SelectAllThatApplyQuestion question, CancellationToken cancellationToken)
        {
            var options = question.Options?.OrderBy(o => o.Order).ToList() ?? new List<ChoiceOption>();
            var counts = new Dictionary<int, int>();

            var answers = question.Answers?.OfType<SelectAllThatApplyAnswer>() ?? Enumerable.Empty<SelectAllThatApplyAnswer>();
            foreach (var answer in answers)
            {
                if (string.IsNullOrWhiteSpace(answer.SelectedOptionIds))
                {
                    continue;
                }

                var ids = answer.SelectedOptionIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.Parse(id));

                foreach (var id in ids)
                {
                    counts[id] = counts.TryGetValue(id, out var existing) ? existing + 1 : 1;
                }
            }

            viewModel.SurveyData = options
                .Select(o => new ChartData
                {
                    X = o.OptionText,
                    Y = counts.TryGetValue(o.Id, out var count) ? count : 0
                })
                .ToList();

            return Task.CompletedTask;
        }

        protected override Answer CreateDemoAnswerInternal(SelectAllThatApplyQuestion question, ApplicationUser user, Random random)
        {
            if (question.Options == null || question.Options.Count == 0)
            {
                return null;
            }

            var selected = question.Options
                .Where(_ => random.Next(0, 2) == 1)
                .Select(o => o.Id)
                .ToList();

            if (selected.Count == 0)
            {
                selected.Add(question.Options[random.Next(question.Options.Count)].Id);
            }

            return new SelectAllThatApplyAnswer
            {
                QuestionId = question.Id,
                SelectedOptionIds = string.Join(',', selected),
                Complete = true,
                CreatedById = user.Id,
                IpAddress = "127.0.0.1"
            };
        }

        protected override Task UpdateNewQuestionAsync(ApplicationDbContext context, SelectAllThatApplyQuestion question)
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
                .OfType<SelectAllThatApplyQuestion>()
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == updated.Id, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"Select-all question with ID {updated.Id} could not be found.");
            }

            await ApplyUpdatesAsync(existing, updated, context, cancellationToken);
        }

        protected override async Task ApplyUpdatesAsync(SelectAllThatApplyQuestion existing, SelectAllThatApplyQuestion updated, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            await base.ApplyUpdatesAsync(existing, updated, context, cancellationToken);

            existing.Options ??= new List<ChoiceOption>();
            var updatedOptions = updated.Options ?? new List<ChoiceOption>();

            foreach (var option in updatedOptions)
            {
                if (option.Id == 0)
                {
                    option.SelectAllThatApplyQuestionId = existing.Id;
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
        }

        public override bool ShouldUpdateAnswer(Answer newAnswer, Answer existingAnswer)
        {
            var newTyped = CastAnswer(newAnswer);
            var oldTyped = CastAnswer(existingAnswer);
            return !string.Equals(newTyped.SelectedOptionIds, oldTyped.SelectedOptionIds, StringComparison.Ordinal) || base.ShouldUpdateAnswer(newAnswer, existingAnswer);
        }
    }
}
