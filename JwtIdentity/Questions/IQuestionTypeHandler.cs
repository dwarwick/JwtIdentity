using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Data;
using JwtIdentity.Models;

namespace JwtIdentity.Questions
{
    public interface IQuestionTypeHandler
    {
        QuestionTypeDefinition Definition { get; }

        QuestionType QuestionType { get; }

        AnswerType AnswerType { get; }

        Type QuestionEntityType { get; }

        Type AnswerEntityType { get; }

        Task EnsureDependenciesLoadedAsync(ApplicationDbContext context, IReadOnlyCollection<Question> questions, CancellationToken cancellationToken = default);

        Task PopulateSurveyDataAsync(ApplicationDbContext context, SurveyDataViewModel viewModel, Question question, CancellationToken cancellationToken = default);

        Answer CreateDemoAnswer(Question question, ApplicationUser user, Random random);

        Task UpdateQuestionAsync(ApplicationDbContext context, Question updatedQuestion, CancellationToken cancellationToken = default);

        bool ShouldUpdateAnswer(Answer newAnswer, Answer existingAnswer);
    }
}
