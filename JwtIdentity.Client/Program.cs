using Blazored.LocalStorage;
using JwtIdentity.Client.Helpers;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true, reloadOnChange: true);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IApiService, ApiService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var snackbar = sp.GetRequiredService<ISnackbar>();
    return new ApiService(httpClientFactory, navigationManager, snackbar);
});

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXtecnZUQ2NdUkZzWENWYUA=");

builder.Services.AddSyncfusionBlazor();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>(); // if using a custom provider
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<IUtility, Utility>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthorizationCore(options =>
{
    var type = typeof(Permissions);

    var permissionNames = type.GetFields().Select(permission => permission.Name);
    foreach (var name in permissionNames)
    {
        options.AddPolicy(
            name,
            policyBuilder => policyBuilder.RequireAssertion(
                context => context.User.HasClaim(claim => claim.Type == CustomClaimTypes.Permission && claim.Value == name)));
    }
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddTransient<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthorizedClient"));

await builder.Build().RunAsync();