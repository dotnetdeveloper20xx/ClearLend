using FluentValidation;
using MediatR;
using ClearLend.Domain.Common;

namespace ClearLend.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!IsDomainResult(typeof(TResponse)) || !validators.Any()) return await next(cancellationToken);
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken))))
            .SelectMany(x => x.Errors).Where(x => x is not null)
            .Select(x => new DomainValidationError(x.PropertyName, x.ErrorCode, x.ErrorMessage)).ToArray();
        if (failures.Length == 0) return await next(cancellationToken);
        return CreateFailure<TResponse>(new DomainError("validation.failed", "One or more validation errors occurred.", failures));
    }

    internal static bool IsDomainResult(Type type) => type == typeof(DomainResult) ||
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DomainResult<>);

    internal static T CreateFailure<T>(DomainError error)
    {
        if (typeof(T) == typeof(DomainResult)) return (T)(object)DomainResult.Failure(error);
        var valueType = typeof(T).GetGenericArguments()[0];
        var method = typeof(DomainResults).GetMethod(nameof(DomainResults.Failure))!.MakeGenericMethod(valueType);
        return (T)method.Invoke(null, [error])!;
    }
}
