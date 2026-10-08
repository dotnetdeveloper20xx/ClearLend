using System.Diagnostics.CodeAnalysis;

namespace ClearLend.Domain.Common;

public sealed record DomainError(string Code, string Message);

public sealed class DomainResult<T>
{
    private DomainResult(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private DomainResult(DomainError error) => Error = error;

    public bool IsSuccess { get; }

    public T? Value { get; }

    public DomainError? Error { get; }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = Value;
        return IsSuccess;
    }

    public bool TryGetError([NotNullWhen(true)] out DomainError? error)
    {
        error = Error;
        return !IsSuccess;
    }

    internal static DomainResult<T> CreateSuccess(T value) => new(value);

    internal static DomainResult<T> CreateFailure(DomainError error) => new(error);
}

public sealed class DomainResult
{
    private DomainResult() => IsSuccess = true;

    private DomainResult(DomainError error) => Error = error;

    public bool IsSuccess { get; }

    public DomainError? Error { get; }

    public bool TryGetError([NotNullWhen(true)] out DomainError? error)
    {
        error = Error;
        return !IsSuccess;
    }

    public static DomainResult Success() => new();

    public static DomainResult Failure(DomainError error) => new(error);
}

public static class DomainResults
{
    public static DomainResult<T> Success<T>(T value) => DomainResult<T>.CreateSuccess(value);

    public static DomainResult<T> Failure<T>(DomainError error) => DomainResult<T>.CreateFailure(error);
}
