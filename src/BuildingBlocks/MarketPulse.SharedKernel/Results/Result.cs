using System.Diagnostics.CodeAnalysis;

namespace MarketPulse.SharedKernel.Results;

/// <summary>Outcome of an operation that can fail with an expected <see cref="Error"/>. Exceptions are reserved for bugs.</summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("A failed result must carry an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>The value of a successful result. Throws if the result is a failure.</summary>
    [NotNull]
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Result.Success<T> is the named alternate.")]
    public static implicit operator Result<T>(T value) => Success(value);

    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Result.Failure<T> is the named alternate.")]
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
