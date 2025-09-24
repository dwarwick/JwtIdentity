using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Questions
{
    public sealed class RatingQuestionTypeHandler : QuestionTypeHandler<Rating1To10Question, Rating1To10Answer>
    {
        public RatingQuestionTypeHandler(QuestionTypeDefinition definition, ILogger<RatingQuestionTypeHandler> logger)
            : base(definition, logger)
        {
        }

        protected override Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, Rating1To10Question question, CancellationToken cancellationToken)
        {
            var answers = question.Answers?.OfType<Rating1To10Answer>().ToList() ?? new List<Rating1To10Answer>();

            var groups = answers
                .GroupBy(a => a.SelectedOptionId)
                .ToDictionary(g => g.Key, g => g.Count());

            viewModel.SurveyData = Enumerable.Range(1, 10)
                .Select(i => new ChartData
                {
                    X = i.ToString(),
                    Y = groups.TryGetValue(i, out var count) ? count : 0
                })
                .ToList();

            return Task.CompletedTask;
        }

        protected override Answer CreateDemoAnswerInternal(Rating1To10Question question, ApplicationUser user, Random random)
        {
            return new Rating1To10Answer
            {
                QuestionId = question.Id,
                SelectedOptionId = random.Next(1, 11),
                Complete = true,
                CreatedById = user.Id,
                IpAddress = "127.0.0.1"
            };
        }

        public override bool ShouldUpdateAnswer(Answer newAnswer, Answer existingAnswer)
        {
            var newTyped = CastAnswer(newAnswer);
            var oldTyped = CastAnswer(existingAnswer);
            return newTyped.SelectedOptionId != oldTyped.SelectedOptionId || base.ShouldUpdateAnswer(newAnswer, existingAnswer);
        }
    }
}
