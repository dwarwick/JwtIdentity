using JwtIdentity.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// create an http client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// add the authentication service
builder.Services.AddScoped<IAuthService, AuthService>();


await builder.Build().RunAsync();
