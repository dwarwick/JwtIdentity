using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Questions
{
    public sealed class TrueFalseQuestionTypeHandler : QuestionTypeHandler<TrueFalseQuestion, TrueFalseAnswer>
    {
        public TrueFalseQuestionTypeHandler(QuestionTypeDefinition definition, ILogger<TrueFalseQuestionTypeHandler> logger)
            : base(definition, logger)
        {
        }

        protected override Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, TrueFalseQuestion question, CancellationToken cancellationToken)
        {
            var answers = question.Answers?.OfType<TrueFalseAnswer>().ToList() ?? new List<TrueFalseAnswer>();

            int trueCount = answers.Count(a => a.Value == true);
            int falseCount = answers.Count(a => a.Value == false);

            viewModel.SurveyData = new List<ChartData>
            {
                new ChartData { X = "True", Y = trueCount },
                new ChartData { X = "False", Y = falseCount }
            };

            return Task.CompletedTask;
        }

        protected override Answer CreateDemoAnswerInternal(TrueFalseQuestion question, ApplicationUser user, Random random)
        {
            return new TrueFalseAnswer
            {
                QuestionId = question.Id,
                Value = random.Next(0, 2) == 0,
                Complete = true,
                CreatedById = user.Id,
                IpAddress = "127.0.0.1"
            };
        }

        public override bool ShouldUpdateAnswer(Answer newAnswer, Answer existingAnswer)
        {
            var newTyped = CastAnswer(newAnswer);
            var oldTyped = CastAnswer(existingAnswer);
            return newTyped.Value != oldTyped.Value || base.ShouldUpdateAnswer(newAnswer, existingAnswer);
        }
    }
}
