using Backend.Swagger;
using Backend.Cors;
using Backend.Entities.CloudinaryUpload;
using Backend.Entities.Daily;
using Backend.Entities.Users;
using Backend.Health;
using Backend.Jobs;
using Backend.Middlewares;
using Backend.Persistence;
using Backend.Repositories;
using Backend.Services;
using Backend.Services.Daily;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Backend.RateLimits;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultDevConnection")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultDevConnection")
                       ?? throw new Exception("No connection string defined");

// Controllers
builder.Services.AddControllers();

// Swagger con JWT
builder.Services.AddSwaggerConfiguration();

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Rate Limit
builder.Services.AddFullRateLimit();

// Data source
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));

// Cloudinary
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary")
);

// User avatar presets
builder.Services.Configure<UserAvatarPresetsSettings>(
    builder.Configuration.GetSection("UserAvatarPresets")
);

// HttpClient
builder.Services.AddHttpClient();

// Daily.co
builder.Services.Configure<DailySettings>(
    builder.Configuration.GetSection("Daily")
);

builder.Services.AddHttpClient<IDailyApiService, DailyApiService>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<DailySettings>>().Value;

    var apiKey = !string.IsNullOrWhiteSpace(settings.ApiKey)
        ? settings.ApiKey
        : Environment.GetEnvironmentVariable("Daily__ApiKey")
          ?? throw new InvalidOperationException("Daily API Key no configurado");

    var baseUrl = settings.BaseUrl.EndsWith('/') ? settings.BaseUrl : settings.BaseUrl + "/";

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Repositories & Services
builder.Services.AddRepositories();
builder.Services.AddServices();

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("Jwt__Issuer"),
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("Jwt__Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("Jwt__Key")!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCorsService(builder.Environment);

// Postgres Health Check
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("PostgresSQL", tags: ["ready"]);

// HttpAccessor
builder.Services.AddHttpContextAccessor();

// Read IPs for limit requests
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
});

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(connectionString);
    }));

// Background jobs
builder.Services.AddHangfireServer();
builder.Services.AddScoped<AppointmentReminderJob>();

var app = builder.Build();

// Middlewares
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseForwardedHeaders();

// Swagger
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwaggerConfiguration();
}

app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<AppointmentReminderJob>(
    "appointment-reminders",
    job => job.SendPendingRemindersAsync(),
    "*/10 * * * *");

app.UseCors("Confirm_Med_Rule");

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
});

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapControllers();

app.Run();