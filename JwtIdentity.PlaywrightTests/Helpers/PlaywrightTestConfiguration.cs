using Microsoft.Extensions.Configuration;

namespace JwtIdentity.PlaywrightTests.Helpers;

public sealed class PlaywrightTestSettings
{
    public bool Headless { get; set; } = true;
}

public static class PlaywrightTestConfiguration
{
    private static readonly Lazy<PlaywrightTestSettings> SettingsLazy = new(LoadSettings);

    public static PlaywrightTestSettings Settings => SettingsLazy.Value;

    private static PlaywrightTestSettings LoadSettings()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                          ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .Build();

        var settings = configuration.GetSection("Playwright").Get<PlaywrightTestSettings>();
        return settings ?? new PlaywrightTestSettings();
    }
}
