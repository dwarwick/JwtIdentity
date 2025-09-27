using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.QuestionHandlers
{
    /// <summary>
    /// Handler for true/false questions
    /// </summary>
    public class TrueFalseQuestionHandler : BaseQuestionHandler
    {
        public override QuestionType SupportedType => QuestionType.TrueFalse;

        public override string GetDisplayInfo(Question question)
        {
            return "True/False selection";
        }

        public override Answer CreateDemoAnswer(Question question, Random random, string userId)
        {
            return new TrueFalseAnswer
            {
                QuestionId = question.Id,
                Value = random.Next(0, 2) == 0,
                Complete = true,
                CreatedById = int.Parse(userId),
                IpAddress = "127.0.0.1",
                AnswerType = AnswerType.TrueFalse
            };
        }
    }
}