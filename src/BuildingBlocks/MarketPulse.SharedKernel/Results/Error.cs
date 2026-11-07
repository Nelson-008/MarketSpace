namespace MarketPulse.SharedKernel.Results;

/// <summary>Category of an error; the API layer maps it to an HTTP status and the error envelope.</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    /// <summary>The requested data or feature is not permitted by the provider licence or user entitlement.</summary>
    NotEntitled = 6,
    /// <summary>The upstream data provider is unavailable or the data is not usable.</summary>
    DataUnavailable = 7,
}

/// <summary>
/// An expected, domain-level error. Codes are stable, dot-separated identifiers
/// (e.g. "instrument.not_found") that clients may rely on; messages are human readable.
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public string Code { get; }

    public string Message { get; }

    public ErrorType Type { get; }

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error NotEntitled(string code, string message) => new(code, message, ErrorType.NotEntitled);

    public static Error DataUnavailable(string code, string message) => new(code, message, ErrorType.DataUnavailable);
}
