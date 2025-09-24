using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Questions
{
    public sealed class TextQuestionTypeHandler : QuestionTypeHandler<TextQuestion, TextAnswer>
    {
        public TextQuestionTypeHandler(QuestionTypeDefinition definition, ILogger<TextQuestionTypeHandler> logger)
            : base(definition, logger)
        {
        }

        protected override Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, TextQuestion question, CancellationToken cancellationToken)
        {
            int count = question.Answers?.Count ?? 0;
            viewModel.SurveyData = new List<ChartData>
            {
                new ChartData { X = "Text", Y = count }
            };
            return Task.CompletedTask;
        }

        protected override Answer CreateDemoAnswerInternal(TextQuestion question, ApplicationUser user, Random random)
        {
            return new TextAnswer
            {
                QuestionId = question.Id,
                Text = $"Sample answer {random.Next(1, 1000)}",
                Complete = true,
                CreatedById = user.Id,
                IpAddress = "127.0.0.1"
            };
        }

        protected override Task ApplyUpdatesAsync(TextQuestion existing, TextQuestion updated, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            existing.MaxLength = updated.MaxLength;
            return base.ApplyUpdatesAsync(existing, updated, context, cancellationToken);
        }

        public override bool ShouldUpdateAnswer(Answer newAnswer, Answer existingAnswer)
        {
            var newText = CastAnswer(newAnswer);
            var oldText = CastAnswer(existingAnswer);
            return !string.Equals(newText.Text, oldText.Text, StringComparison.Ordinal) || base.ShouldUpdateAnswer(newAnswer, existingAnswer);
        }
    }
}
