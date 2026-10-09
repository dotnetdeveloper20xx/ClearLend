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

public sealed class AuthorizationBehavior<TRequest, TResponse>(IServiceProvider services)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IAuthorizedRequest authorized || !ValidationBehavior<TRequest, TResponse>.IsDomainResult(typeof(TResponse)))
            return await next(cancellationToken);

        var currentActor = (services.GetService(typeof(ICurrentStaffActor)) as ICurrentStaffActor)?.StaffMemberId;
        if (authorized.ActorStaffMemberId.Value == Guid.Empty || currentActor is null || currentActor.Value != authorized.ActorStaffMemberId)
            return ValidationBehavior<TRequest, TResponse>.CreateFailure<TResponse>(
                new DomainError("authorization.forbidden", "The staff member does not have permission to perform this action."));

        var staffRepository = services.GetService(typeof(IStaffMemberRepository)) as IStaffMemberRepository;
        var staff = staffRepository is null ? null : await staffRepository.GetAsync(authorized.ActorStaffMemberId, cancellationToken);
        var authorization = services.GetService(typeof(IAuthorizationService)) as IAuthorizationService;
        if (staff?.Status != StaffStatus.Active || !StaffPermissionPolicy.Allows(staff.Roles, authorized.RequiredPermission) ||
            authorization is null || !await authorization.HasPermissionAsync(authorized.ActorStaffMemberId, authorized.RequiredPermission, cancellationToken))
            return ValidationBehavior<TRequest, TResponse>.CreateFailure<TResponse>(
                new DomainError("authorization.forbidden", "The staff member does not have permission to perform this action."));

        return await next(cancellationToken);
    }
}

public static class StaffPermissionPolicy
{
    public static bool Allows(IEnumerable<StaffRole> roles, PermissionCode permission) => roles.Any(role => role switch
    {
        StaffRole.ApplicationOwner => true,
        StaffRole.OperationsStaff => permission is PermissionCode.ManageWorkItems or PermissionCode.ViewAudit,
        StaffRole.ComplianceReviewer => permission is PermissionCode.ReviewCompliance or PermissionCode.ManageWorkItems or PermissionCode.ViewAudit,
        StaffRole.CreditReviewer => permission is PermissionCode.ReviewCredit or PermissionCode.ManageWorkItems or PermissionCode.ViewAudit,
        StaffRole.FinanceOperator => permission is PermissionCode.OperateFinance or PermissionCode.ManageWorkItems or PermissionCode.ViewAudit,
        StaffRole.ServicingOperator => permission is PermissionCode.OperateServicing or PermissionCode.ManageWorkItems or PermissionCode.ViewAudit,
        StaffRole.SupportAgent => permission is PermissionCode.ManageSupport or PermissionCode.ManageWorkItems or PermissionCode.ViewAudit,
        _ => false
    });
}
