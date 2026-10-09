using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using MediatR;

namespace ClearLend.Application.Behaviors;

public interface IAuthorizedRequest
{
    StaffMemberId ActorStaffMemberId { get; }
    PermissionCode RequiredPermission { get; }
}

public sealed class AuthorizationBehavior<TRequest, TResponse>(IAuthorizationService authorization)
    : IPipelineBehavior<TRequest, DomainResult<TResponse>>
    where TRequest : notnull, IRequest<DomainResult<TResponse>>, IAuthorizedRequest
{
    public async Task<DomainResult<TResponse>> Handle(TRequest request, RequestHandlerDelegate<DomainResult<TResponse>> next, CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(request.ActorStaffMemberId, request.RequiredPermission, cancellationToken))
            return DomainResults.Failure<TResponse>(new("authorization.forbidden", "The staff member does not have permission to perform this action."));
        return await next(cancellationToken);
    }
}
