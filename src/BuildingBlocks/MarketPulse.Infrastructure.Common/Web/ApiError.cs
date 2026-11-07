using MarketPulse.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace MarketPulse.Infrastructure.Common.Web;

/// <summary>
/// The single error envelope returned by every endpoint (spec §10.1):
/// <c>{ "code", "message", "correlationId", "errors": { "field": ["..."] } }</c>.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    string CorrelationId,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public static class ApiErrors
{
    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotEntitled => StatusCodes.Status403Forbidden,
        ErrorType.DataUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status400BadRequest,
    };

    /// <summary>Default code/message for responses produced by the framework (401 challenge, 404 route, 429...).</summary>
    public static (string Code, string Message) ForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ("request.invalid", "The request is invalid."),
        StatusCodes.Status401Unauthorized => ("auth.unauthenticated", "Authentication is required."),
        StatusCodes.Status403Forbidden => ("auth.forbidden", "You do not have access to this resource."),
        StatusCodes.Status404NotFound => ("resource.not_found", "The resource was not found."),
        StatusCodes.Status405MethodNotAllowed => ("request.method_not_allowed", "The HTTP method is not allowed."),
        StatusCodes.Status415UnsupportedMediaType => ("request.unsupported_media_type", "Unsupported content type."),
        StatusCodes.Status429TooManyRequests => ("rate_limit.exceeded", "Too many requests. Please retry later."),
        StatusCodes.Status503ServiceUnavailable => ("service.unavailable", "The service is temporarily unavailable."),
        >= 500 => ("server.error", "An unexpected error occurred."),
        _ => ("request.failed", "The request failed."),
    };
}

/// <summary>An <see cref="IResult"/> that writes the error envelope with the request's correlation id.</summary>
public sealed class ErrorResult(int statusCode, string code, string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : IResult, IStatusCodeHttpResult
{
    public int? StatusCode => statusCode;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.StatusCode = statusCode;
        var body = new ApiError(code, message, httpContext.GetCorrelationId(), errors);
        return httpContext.Response.WriteAsJsonAsync(body);
    }
}

public static class ApiResults
{
    public static IResult Error(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ErrorResult(ApiErrors.StatusCodeFor(error.Type), error.Code, error.Message);
    }

    public static IResult Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new ErrorResult(StatusCodes.Status400BadRequest, "validation.failed", "One or more fields are invalid.", errors);

    /// <summary>Maps a result to 200 OK (or the supplied success result) / the error envelope.</summary>
    public static IResult From<T>(Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
        {
            return Error(result.Error);
        }

        return onSuccess is null ? Results.Ok(result.Value) : onSuccess(result.Value);
    }

    public static IResult From(Result result, Func<IResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
        {
            return Error(result.Error);
        }

        return onSuccess is null ? Results.NoContent() : onSuccess();
    }
}
