using Blazored.LocalStorage;
using Hangfire;
using Hangfire.SqlServer;
using JwtIdentity.Client.Helpers;
using JwtIdentity.Client.Services;
using JwtIdentity.Hubs;
using JwtIdentity.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.RollingFileAlternate;
using Syncfusion.Blazor;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var columnOptions = new ColumnOptions
{
    AdditionalColumns = new System.Collections.ObjectModel.Collection<SqlColumn>
    {
        new SqlColumn
        {
            ColumnName = "UserName",
            PropertyName = "UserName",
            DataType = SqlDbType.NVarChar,
            DataLength = 100
        }
    }
};
// Remove unneeded columns
columnOptions.Store.Remove(StandardColumn.Properties);
columnOptions.Store.Remove(StandardColumn.MessageTemplate);

// Configure the main Serilog logger
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Debug(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.RollingFileAlternate(
        "logs/log-{Date}.txt",
        fileSizeLimitBytes: null,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} User: {UserName}{NewLine}{Properties:j}{NewLine}{Exception}")
    // Add MSSqlServer sink to log warnings and errors to the LogEntries table
    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "LogEntries",
            AutoCreateSqlTable = false, // Table already exists with migrations
        },
        columnOptions: columnOptions,
        restrictedToMinimumLevel: LogEventLevel.Warning) // Only log Warning and above
    .CreateLogger();

builder.Host.UseSerilog();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddHttpContextAccessor(); // Register HttpContextAccessor

// Add environment-based appsettings.json files
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Bind "AppSettings" section to the AppSettings class
builder.Services.Configure<AppSettings>(builder.Configuration);

// Register Settings service
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSignalR();
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.EnableRetryOnFailure(
    maxRetryCount: 3,
    maxRetryDelay: TimeSpan.FromSeconds(10),
    errorNumbersToAdd: null
)).ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

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
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<ISurveyCompletionNotifier, SurveyCompletionNotifier>();
builder.Services.AddScoped<JwtIdentity.Services.BackgroundJobs.BackgroundJobService>();
builder.Services.AddHttpClient<IOpenAi, OpenAiService>();
// Services required for prerendering shared client components
builder.Services.AddSyncfusionBlazor();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUtility, JwtIdentity.Client.Helpers.Utility>();
builder.Services.AddScoped<IWordPressBlogService, WordPressBlogService>();
builder.Services.AddScoped<SurveyHubClient>();
builder.Services.AddTransient<CustomAuthorizationMessageHandler>();
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
builder.Services.AddHttpClient("AuthorizedClient", (sp, client) =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = accessor.HttpContext?.Request;
    client.BaseAddress = new Uri($"{request?.Scheme}://{request?.Host}");
}).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();
builder.Services.AddHttpClient("PublicClient", (sp, client) =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = accessor.HttpContext?.Request;
    client.BaseAddress = new Uri($"{request?.Scheme}://{request?.Host}");
});
builder.Services.AddScoped<IApiService, ApiService>();
// Replace the existing line with the following line
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MapperConfig>());
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
}).AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new AnswerViewModelConverter());
    opts.JsonSerializerOptions.Converters.Add(new QuestionViewModelConverter());
    opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
}).AddOData(options =>
{
    _ = options.Select().Filter().OrderBy().Count().Expand().SetMaxTop(null);
    _ = options.AddRouteComponents("odata", EdmModelBuilder.GetEdmModel());
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

// Add Hangfire services with SQL Server storage
try
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
    // Add the Hangfire background job processing server as a service
    builder.Services.AddHangfireServer();
}
catch (Exception ex)
{
    // If SQL Server is unreachable, log a error and continue without Hangfire
    Log.Logger.Error(ex, "Hangfire database unavailable; skipping background job setup");
}

// Add Data Protection services with a persistent key ring
var keyRingPath = Path.Combine(AppContext.BaseDirectory, "KeyRing");
// Ensure the directory exists
Directory.CreateDirectory(keyRingPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetApplicationName("SurveyShark")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Ensure Antiforgery is configured
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Uncommenting the cyclical reference handling
//builder.Services.AddCyclicalReferenceHandling();

var app = builder.Build();

// Configure Serilog to use email sink for errors after services are built
using (var scope = app.Services.CreateScope())
{
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var customerServiceEmail = builder.Configuration["EmailSettings:CustomerServiceEmail"] ?? "admin@example.com";
    var applicationName = builder.Configuration["AppSettings:ApplicationName"] ?? "JwtIdentity";

    // Add email sink to the existing logger configuration
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Logger(Log.Logger)
        .WriteTo.EmailSink(
            emailService,
            customerServiceEmail,
            applicationName,
            restrictedToMinimumLevel: LogEventLevel.Error)
        .CreateLogger();
}

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

// Add the UserNameEnricher middleware after authentication/authorization
app.UseUserNameEnricher();

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

app.MapHub<SurveyHub>("/surveyHub");
// Configure Hangfire Dashboard with custom authorization
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "Survey Shark Job Dashboard",
    DisplayStorageConnectionString = false
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JwtIdentity.Client.Layout.MainLayout).Assembly);

// Apply database migrations at startup
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    try
    {
        // Run database migrations
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        // Initialize recurring Hangfire jobs
        var backgroundJobService = serviceProvider.GetRequiredService<JwtIdentity.Services.BackgroundJobs.BackgroundJobService>();
        backgroundJobService.InitializeRecurringJobs();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Application startup completed successfully");
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during startup");
    }
}

app.Run();
