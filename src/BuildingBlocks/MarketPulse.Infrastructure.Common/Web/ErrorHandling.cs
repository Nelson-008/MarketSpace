using MarketPulse.Infrastructure.Common.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MarketPulse.Infrastructure.Common.Web;

/// <summary>Turns unhandled exceptions into the standard error envelope. Never leaks exception details.</summary>
public sealed partial class EnvelopeExceptionHandler(ILogger<EnvelopeExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is BadHttpRequestException badRequest)
        {
            await new ErrorResult(badRequest.StatusCode, "request.invalid", "The request body or parameters could not be read.")
                .ExecuteAsync(httpContext).ConfigureAwait(false);
            return true;
        }

        if (exception is CrossTenantWriteException)
        {
            LogCrossTenant(logger, exception);
        }
        else
        {
            LogUnhandled(logger, exception);
        }

        var (code, message) = ApiErrors.ForStatus(StatusCodes.Status500InternalServerError);
        await new ErrorResult(StatusCodes.Status500InternalServerError, code, message).ExecuteAsync(httpContext).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "SECURITY: blocked cross-tenant write")]
    private static partial void LogCrossTenant(ILogger logger, Exception exception);
}

public static class StatusCodeEnvelope
{
    /// <summary>For framework-generated empty responses (401 challenge, 403, 404 route, 405, 429) write the envelope.</summary>
    public static Task WriteAsync(StatusCodeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var response = context.HttpContext.Response;
        var (code, message) = ApiErrors.ForStatus(response.StatusCode);
        return new ErrorResult(response.StatusCode, code, message).ExecuteAsync(context.HttpContext);
    }
}
