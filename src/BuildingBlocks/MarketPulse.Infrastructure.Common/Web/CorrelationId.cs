using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MarketPulse.Infrastructure.Common.Web;

public static partial class CorrelationIdExtensions
{
    public const string HeaderName = "X-Correlation-Id";
    internal const string ItemKey = "MarketPulse.CorrelationId";

    public static string GetCorrelationId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.Items.TryGetValue(ItemKey, out var value) && value is string id
            ? id
            : httpContext.TraceIdentifier;
    }

    /// <summary>Accept a caller-supplied id only if it is short and safe to log; otherwise generate one.</summary>
    internal static bool IsAcceptable(string? value) => value is { Length: > 0 and <= 64 } && SafeId().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex SafeId();
}

/// <summary>Assigns a correlation id to every request, echoes it in the response and adds it to the logging scope.</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var supplied = context.Request.Headers[CorrelationIdExtensions.HeaderName].ToString();
        var correlationId = CorrelationIdExtensions.IsAcceptable(supplied)
            ? supplied
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.Items[CorrelationIdExtensions.ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdExtensions.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
