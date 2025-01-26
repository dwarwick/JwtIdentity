using Blazored.LocalStorage;
using JwtIdentity.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// create an http client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped(typeof(IApiService<>), typeof(ApiService<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddMudServices();



await builder.Build().RunAsync();
