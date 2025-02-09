using Blazored.LocalStorage;
using JwtIdentity.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.Extensions.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IApiService, ApiService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new ApiService(httpClientFactory);
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>(); // if using a custom provider
builder.Services.AddScoped<CustomAuthStateProvider>(); // Add this line
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
builder.Services.AddMudServices();

builder.Services.AddTransient<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthorizedClient"));

await builder.Build().RunAsync();