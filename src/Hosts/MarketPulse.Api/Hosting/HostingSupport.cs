using MarketPulse.Infrastructure.Common.Persistence;

namespace MarketPulse.Api.Hosting;

/// <summary>Security headers for API responses (spec §13). The Scalar UI (dev only) is excluded from the strict CSP.</summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            headers.CacheControl = "no-store";
        }

        return next(context);
    }
}

/// <summary>Applies every module's pending migrations (Development / tests only; production uses reviewed SQL scripts).</summary>
internal static partial class DatabaseMigrations
{
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigrations");
        foreach (var migrator in scope.ServiceProvider.GetServices<IModuleMigrator>())
        {
            LogMigrating(logger, migrator.ModuleName);
            await migrator.MigrateAsync(scope.ServiceProvider, ct).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying migrations for module {Module}")]
    private static partial void LogMigrating(ILogger logger, string module);
}
