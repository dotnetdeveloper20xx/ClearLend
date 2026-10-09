using FluentValidation;
using MediatR;
using ClearLend.Domain.Common;
using ClearLend.Application.Borrowers.Register;

namespace ClearLend.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, DomainResult<TResponse>>
    where TRequest : notnull, IRequest<DomainResult<TResponse>>
{
    public async Task<DomainResult<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<DomainResult<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken))))
            .SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .Select(x => new DomainValidationError(x.PropertyName, x.ErrorCode, x.ErrorMessage))
            .ToArray();

        if (failures.Length == 0)
        {
            return await next(cancellationToken);
        }

        var error = new DomainError(
            "validation.failed",
            "One or more validation errors occurred.",
            failures);
        return DomainResults.Failure<TResponse>(error);
    }
}
