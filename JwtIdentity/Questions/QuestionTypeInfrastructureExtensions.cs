using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JwtIdentity.Questions
{
    public static class QuestionTypeInfrastructureExtensions
    {
        public static IServiceCollection AddQuestionTypeInfrastructure(this IServiceCollection services)
        {
            Register<TextQuestionTypeHandler, TextQuestion, TextAnswer>(QuestionType.Text);
            Register<TrueFalseQuestionTypeHandler, TrueFalseQuestion, TrueFalseAnswer>(QuestionType.TrueFalse);
            Register<RatingQuestionTypeHandler, Rating1To10Question, Rating1To10Answer>(QuestionType.Rating1To10);
            Register<MultipleChoiceQuestionTypeHandler, MultipleChoiceQuestion, MultipleChoiceAnswer>(QuestionType.MultipleChoice);
            Register<SelectAllThatApplyQuestionTypeHandler, SelectAllThatApplyQuestion, SelectAllThatApplyAnswer>(QuestionType.SelectAllThatApply);

            services.AddSingleton<IQuestionTypeHandler, TextQuestionTypeHandler>();
            services.AddSingleton<IQuestionTypeHandler, TrueFalseQuestionTypeHandler>();
            services.AddSingleton<IQuestionTypeHandler, RatingQuestionTypeHandler>();
            services.AddSingleton<IQuestionTypeHandler, MultipleChoiceQuestionTypeHandler>();
            services.AddSingleton<IQuestionTypeHandler, SelectAllThatApplyQuestionTypeHandler>();
            services.AddSingleton<IQuestionTypeHandlerResolver, QuestionTypeHandlerResolver>();

            return services;
        }

        private static void Register<THandler, TQuestion, TAnswer>(QuestionType questionType)
            where THandler : class, IQuestionTypeHandler
            where TQuestion : Question
            where TAnswer : Answer
        {
            var definition = QuestionTypeRegistry.GetDefinition(questionType);
            QuestionDomainRegistry.Register(new QuestionDomainDefinition(definition, typeof(TQuestion), typeof(TAnswer), typeof(THandler)));
        }
    }
}
