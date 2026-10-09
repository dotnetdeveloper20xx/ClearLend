using ClearLend.Application.Abstractions;
using ClearLend.Application.Behaviors;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;
using ClearLend.Application.Operations;

namespace ClearLend.Application.Operations.Staff;

public sealed record AddStaffRoleCommand(StaffMemberId StaffMemberId, StaffRole Role, StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageStaff; }
public sealed record RemoveStaffRoleCommand(StaffMemberId StaffMemberId, StaffRole Role, StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageStaff; }
public sealed record ReinstateStaffMemberCommand(StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageStaff; }

public sealed class AddStaffRoleCommandValidator : AbstractValidator<AddStaffRoleCommand>
{ public AddStaffRoleCommandValidator() { RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.Role).IsInEnum(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class RemoveStaffRoleCommandValidator : AbstractValidator<RemoveStaffRoleCommand>
{ public RemoveStaffRoleCommandValidator() { RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.Role).IsInEnum(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class ReinstateStaffMemberCommandValidator : AbstractValidator<ReinstateStaffMemberCommand>
{ public ReinstateStaffMemberCommandValidator() { RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }

public sealed class AddStaffRoleHandler(IStaffMemberRepository staff, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    : IRequestHandler<AddStaffRoleCommand, DomainResult>
{
    public async Task<DomainResult> Handle(AddStaffRoleCommand command, CancellationToken cancellationToken)
    {
        var member = await staff.GetAsync(command.StaffMemberId, cancellationToken);
        if (member is null) return DomainResult.Failure(new("staff.not_found", "The staff member was not found."));
        var result = member.AddRole(command.Role); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "staff.role_added", "staff_member", member.Id.Value.ToString("D"), command.ActorStaffMemberId, (clock ?? TimeProvider.System).GetUtcNow(), cancellationToken, command.Role.ToString());
        return DomainResult.Success();
    }
}

public sealed class RemoveStaffRoleHandler(IStaffMemberRepository staff, IWorkItemRepository workItems, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    : IRequestHandler<RemoveStaffRoleCommand, DomainResult>
{
    public async Task<DomainResult> Handle(RemoveStaffRoleCommand command, CancellationToken cancellationToken)
    {
        var member = await staff.GetAsync(command.StaffMemberId, cancellationToken);
        if (member is null) return DomainResult.Failure(new("staff.not_found", "The staff member was not found."));
        if (command.Role == StaffRole.ApplicationOwner && member.Status == StaffStatus.Active && member.Roles.Contains(command.Role) &&
            await staff.CountActiveApplicationOwnersAsync(member.Id, cancellationToken) == 0)
            return DomainResult.Failure(new("staff.owner.last_active", "The last active application owner cannot lose this role."));
        if (member.Roles.Contains(command.Role) && await workItems.HasAnyActiveAssignmentAsync(member.Id, cancellationToken))
            return DomainResult.Failure(new("staff.has_assigned_work", "Return or transfer this staff member’s active work before changing their roles."));
        var result = member.RemoveRole(command.Role); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "staff.role_removed", "staff_member", member.Id.Value.ToString("D"), command.ActorStaffMemberId, (clock ?? TimeProvider.System).GetUtcNow(), cancellationToken, command.Role.ToString());
        return DomainResult.Success();
    }
}

public sealed class ReinstateStaffMemberHandler(IStaffMemberRepository staff, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    : IRequestHandler<ReinstateStaffMemberCommand, DomainResult>
{
    public async Task<DomainResult> Handle(ReinstateStaffMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await staff.GetAsync(command.StaffMemberId, cancellationToken);
        if (member is null) return DomainResult.Failure(new("staff.not_found", "The staff member was not found."));
        var result = member.Reinstate((clock ?? TimeProvider.System).GetUtcNow()); if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken); return DomainResult.Success();
    }
}
