using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.QuestionHandlers
{
    /// <summary>
    /// Handler for text-based questions
    /// </summary>
    public class TextQuestionHandler : BaseQuestionHandler
    {
        public override QuestionType SupportedType => QuestionType.Text;

        public override bool IsValid(Question question)
        {
            if (!base.IsValid(question) || question is not TextQuestion textQuestion)
                return false;

            // Additional validation for text questions - check MaxLength
            return textQuestion.MaxLength > 0;
        }

        public override string GetDisplayInfo(Question question)
        {
            if (question is not TextQuestion textQuestion)
                return string.Empty;

            return $"Text input (max {textQuestion.MaxLength} characters)";
        }

        public override Answer CreateDemoAnswer(Question question, Random random, string userId)
        {
            return new TextAnswer
            {
                QuestionId = question.Id,
                Text = $"Sample answer {random.Next(1, 1000)}",
                Complete = true,
                CreatedById = int.Parse(userId),
                IpAddress = "127.0.0.1",
                AnswerType = AnswerType.Text
            };
        }
    }
}