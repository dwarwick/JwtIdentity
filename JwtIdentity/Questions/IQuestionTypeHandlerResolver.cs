using JwtIdentity.Data;
using JwtIdentity.Models;

namespace JwtIdentity.Questions
{
    public interface IQuestionTypeHandlerResolver
    {
        IReadOnlyCollection<IQuestionTypeHandler> Handlers { get; }

        IQuestionTypeHandler GetHandler(QuestionType questionType);

        IQuestionTypeHandler GetHandler(AnswerType answerType);

        Task EnsureDependenciesLoadedAsync(ApplicationDbContext context, IEnumerable<Question> questions, CancellationToken cancellationToken = default);
    }
}
