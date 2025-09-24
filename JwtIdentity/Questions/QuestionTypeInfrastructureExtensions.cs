using Microsoft.Extensions.DependencyInjection;

namespace JwtIdentity.Questions
{
    public static class QuestionTypeInfrastructureExtensions
    {
        public static IServiceCollection AddQuestionTypeInfrastructure(this IServiceCollection services)
        {
            QuestionDomainRegistry.EnsureInitialized();

            foreach (var definition in QuestionDomainRegistry.All)
            {
                services.AddSingleton(typeof(IQuestionTypeHandler), serviceProvider =>
                {
                    return (IQuestionTypeHandler)ActivatorUtilities.CreateInstance(
                        serviceProvider,
                        definition.HandlerType,
                        definition.Definition);
                });
            }

            services.AddSingleton<IQuestionTypeHandlerResolver, QuestionTypeHandlerResolver>();

            return services;
        }
    }
}
