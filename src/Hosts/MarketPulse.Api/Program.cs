using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using MarketPulse.Api.Hosting;
using MarketPulse.Application.Abstractions.Messaging;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Messaging;
using MarketPulse.Infrastructure.Common.Security;
using MarketPulse.Infrastructure.Common.Web;
using MarketPulse.Modules.Administration;
using MarketPulse.Modules.Identity;
using MarketPulse.Modules.Identity.Infrastructure;
using MarketPulse.Modules.Instruments;
using MarketPulse.Modules.Workspace;
using MarketPulse.SharedKernel.Time;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Cross-cutting services ----------
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddExceptionHandler<EnvelopeExceptionHandler>();
builder.Services.AddProblemDetails(); // required by UseExceptionHandler(); EnvelopeExceptionHandler handles every exception first

builder.Services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
builder.Services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.SectionName));
if (string.Equals(config["Email:Mode"], "File", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IEmailSender, FileEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}

// ---------- Modules ----------
builder.Services
    .AddIdentityModule(config)
    .AddInstrumentsModule(config)
    .AddWorkspaceModule(config)
    .AddAdministrationModule(config);
builder.Services.AddAuthorization();

// ---------- Rate limiting (spec §10.1) ----------
var authPermits = config.GetValue("RateLimiting:AuthPermitPerMinute", 10);
var userTokens = config.GetValue("RateLimiting:UserTokensPerMinute", 120);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = (ctx, _) =>
    {
        var (code, message) = ApiErrors.ForStatus(StatusCodes.Status429TooManyRequests);
        return new ValueTask(new ErrorResult(StatusCodes.Status429TooManyRequests, code, message).ExecuteAsync(ctx.HttpContext));
    };
    o.AddPolicy(RateLimitPolicies.Auth, http => RateLimitPartition.GetFixedWindowLimiter(
        $"auth:{http.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy(RateLimitPolicies.PerUser, http =>
    {
        var key = http.User.FindFirst(MarketPulseClaims.Subject)?.Value is { } sub ? $"user:{sub}" : $"ip:{http.Connection.RemoteIpAddress}";
        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = userTokens,
            TokensPerPeriod = userTokens,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

// ---------- CORS (credentials needed for the refresh cookie) ----------
var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowCredentials()
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Content-Type", "Authorization", "X-Requested-With", CorrelationIdExtensions.HeaderName)
    .WithExposedHeaders(CorrelationIdExtensions.HeaderName)));

// ---------- OpenAPI, health, telemetry ----------
builder.Services.AddOpenApi("v1");
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityModuleDbContext>("database", tags: ["ready"]);

var otlpEndpoint = config["OpenTelemetry:OtlpEndpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MarketPulse.Api"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            t.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            m.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
        }
    });

var app = builder.Build();

// ---------- Pipeline ----------
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(StatusCodeEnvelope.WriteAsync);
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

var api = app.MapGroup("/api/v1").RequireRateLimiting(RateLimitPolicies.PerUser);
api.MapIdentityEndpoints()
    .MapInstrumentsEndpoints()
    .MapWorkspaceEndpoints()
    .MapAdministrationEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

if (config.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await DatabaseMigrations.ApplyAsync(app.Services, app.Lifetime.ApplicationStopping).ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
