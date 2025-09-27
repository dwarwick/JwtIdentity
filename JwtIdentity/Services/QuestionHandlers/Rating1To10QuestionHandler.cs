using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.QuestionHandlers
{
    /// <summary>
    /// Handler for rating 1-to-10 questions
    /// </summary>
    public class Rating1To10QuestionHandler : BaseQuestionHandler
    {
        public override QuestionType SupportedType => QuestionType.Rating1To10;

        public override string GetDisplayInfo(Question question)
        {
            return "Rating scale (1-10)";
        }

        public override Answer CreateDemoAnswer(Question question, Random random, string userId)
        {
            return new Rating1To10Answer
            {
                QuestionId = question.Id,
                SelectedOptionId = random.Next(1, 11),
                Complete = true,
                CreatedById = int.Parse(userId),
                IpAddress = "127.0.0.1",
                AnswerType = AnswerType.Rating1To10
            };
        }
    }
}