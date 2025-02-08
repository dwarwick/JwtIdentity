using System.Text.Json.Serialization;

public static class JsonConfigurationExtensions
{
    public static IMvcBuilder AddCyclicalReferenceHandling(this IServiceCollection services)
    {
        return services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
    }
}
