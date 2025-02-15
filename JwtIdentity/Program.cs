using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Add this using statement
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add environment-based appsettings.json files
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Bind "AppSettings" section to the AppSettings class
builder.Services.Configure<AppSettings>(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add Swagger services
if (builder.Environment.IsDevelopment())
{
    _ = builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    });
}

// Add DbContext and Identity services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<EmailTokenAuthorizationProvider<ApplicationUser>>("Email");

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IApiAuthService, ApiAuthService>();

builder.Services.AddAutoMapper(typeof(MapperConfig));
builder.Services.AddAuthentication(options =>
{
    // Let cookies handle the challenge (so it can do 302 to /not-authorized):
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // If you still want JWT to validate tokens for API calls:
    //  - set this as the default **authenticate** scheme
    //  - or use [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] on your API endpoints
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

    // The key line:
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // If the header is empty, try to read the token from a cookie
            var cookie = context.Request.Cookies["authToken"];
            if (!string.IsNullOrEmpty(cookie))
            {
                context.Token = cookie;
            }
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
    };
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login"; // Blazor page for login
    options.LogoutPath = "/api/auth/logout"; // Controller endpoint for logout
    options.AccessDeniedPath = "/not-authorized"; // Blazor page for access denied
});

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

// Add MVC services
builder.Services.AddControllers(options =>
{
    _ = options.Filters.Add<DatabaseLoggingFilter>();
});

// add an AllowAll Cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            _ = builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddCyclicalReferenceHandling();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
}
else
{
    _ = app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePages(context =>
{
    var response = context.HttpContext.Response;

    // If the server sets 401 or 403, redirect to /not-authorized
    if (response.StatusCode == StatusCodes.Status401Unauthorized ||
        response.StatusCode == StatusCodes.Status403Forbidden)
    {
        response.Redirect("/not-authorized");
    }

    if (response.StatusCode == StatusCodes.Status404NotFound && !(context.HttpContext.Request.Path.Value?.StartsWith("/api/") ?? false))
    {
        response.Redirect("/not-found");
    }

    return Task.CompletedTask;
});

// Use CORS policymap
app.UseCors("AllowAll");

// Map controllers
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JwtIdentity.Client._Imports).Assembly);

app.Run();
