using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JwtIdentity.Questions
{
    public abstract class QuestionTypeHandler<TQuestion, TAnswer> : IQuestionTypeHandler
        where TQuestion : Question
        where TAnswer : Answer, new()
    {
        protected QuestionTypeHandler(QuestionTypeDefinition definition, ILogger logger)
        {
            Definition = definition;
            Logger = logger;
        }

        protected ILogger Logger { get; }

        public QuestionTypeDefinition Definition { get; }

        public QuestionType QuestionType => Definition.QuestionType;

        public AnswerType AnswerType => Definition.AnswerType;

        public Type QuestionEntityType => typeof(TQuestion);

        public Type AnswerEntityType => typeof(TAnswer);

        public virtual Task EnsureDependenciesLoadedAsync(ApplicationDbContext context, IReadOnlyCollection<Question> questions, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, Question question, CancellationToken cancellationToken = default)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            TQuestion typedQuestion = CastQuestion(question);
            await PopulateSurveyDataAsync(context, viewModel, typedQuestion, cancellationToken);
        }

        public Answer CreateDemoAnswer(Question question, ApplicationUser user, Random random)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            TQuestion typedQuestion = CastQuestion(question);
            return CreateDemoAnswerInternal(typedQuestion, user, random);
        }

        public virtual async Task UpdateQuestionAsync(ApplicationDbContext context, Question updatedQuestion, CancellationToken cancellationToken = default)
        {
            if (updatedQuestion == null)
            {
                throw new ArgumentNullException(nameof(updatedQuestion));
            }

            if (updatedQuestion.Id == 0)
            {
                await UpdateNewQuestionAsync(context, CastQuestion(updatedQuestion));
                return;
            }

            var existing = await context.Set<TQuestion>()
                .AsTracking()
                .FirstOrDefaultAsync(q => q.Id == updatedQuestion.Id, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"Question with ID {updatedQuestion.Id} could not be found for update.");
            }

            await ApplyUpdatesAsync(existing, CastQuestion(updatedQuestion), context, cancellationToken);
        }

        public virtual bool ShouldUpdateAnswer(Answer newAnswer, Answer existingAnswer)
        {
            if (newAnswer == null)
            {
                throw new ArgumentNullException(nameof(newAnswer));
            }

            if (existingAnswer == null)
            {
                throw new ArgumentNullException(nameof(existingAnswer));
            }

            return newAnswer.Complete != existingAnswer.Complete;
        }

        protected virtual Task UpdateNewQuestionAsync(ApplicationDbContext context, TQuestion question)
        {
            return Task.CompletedTask;
        }

        protected abstract Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, TQuestion question, CancellationToken cancellationToken);

        protected abstract Answer CreateDemoAnswerInternal(TQuestion question, ApplicationUser user, Random random);

        protected virtual Task ApplyUpdatesAsync(TQuestion existing, TQuestion updated, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            existing.Text = updated.Text;
            existing.QuestionNumber = updated.QuestionNumber;
            existing.IsRequired = updated.IsRequired;
            return Task.CompletedTask;
        }

        protected TQuestion CastQuestion(Question question)
        {
            if (question is TQuestion typed)
            {
                return typed;
            }

            throw new InvalidOperationException($"Question with ID {question.Id} is not of expected type {typeof(TQuestion).Name}.");
        }

        protected TAnswer CastAnswer(Answer answer)
        {
            if (answer is TAnswer typed)
            {
                return typed;
            }

            throw new InvalidOperationException($"Answer with ID {answer.Id} is not of expected type {typeof(TAnswer).Name}.");
        }
    }
}
