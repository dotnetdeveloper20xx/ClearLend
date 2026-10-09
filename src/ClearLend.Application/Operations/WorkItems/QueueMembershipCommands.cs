using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;
using ClearLend.Application.Behaviors;
using ClearLend.Application.Operations;

namespace ClearLend.Application.Operations.WorkItems;

public sealed record AddQueueMemberCommand(WorkQueueId QueueId, StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest
{ public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record RemoveQueueMemberCommand(WorkQueueId QueueId, StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest
{ public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }

public sealed class AddQueueMemberCommandValidator : AbstractValidator<AddQueueMemberCommand>
{ public AddQueueMemberCommandValidator() { RuleFor(x => x.QueueId.Value).NotEmpty(); RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class RemoveQueueMemberCommandValidator : AbstractValidator<RemoveQueueMemberCommand>
{ public RemoveQueueMemberCommandValidator() { RuleFor(x => x.QueueId.Value).NotEmpty(); RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }

public sealed class AddQueueMemberHandler(IWorkQueueRepository repository, IStaffMemberRepository staffRepository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<AddQueueMemberCommand, DomainResult>
{
    public async Task<DomainResult> Handle(AddQueueMemberCommand command, CancellationToken cancellationToken)
    {
        var queue = await repository.GetAsync(command.QueueId, cancellationToken); if (queue is null) return DomainResult.Failure(new("work_queue.not_found", "The work queue was not found."));
        var staffMember = await staffRepository.GetAsync(command.StaffMemberId, cancellationToken);
        if (staffMember is null || staffMember.Status != StaffStatus.Active) return DomainResult.Failure(new("work_queue.member.inactive", "Only an active staff member can join a work queue."));
        if (!QueueRolePolicy.CanJoin(queue.Type, staffMember.Roles)) return DomainResult.Failure(new("work_queue.member.role_ineligible", "The staff member does not have a role eligible for this queue."));
        var result = queue.AddMember(command.StaffMemberId); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_queue.member_added", "work_queue", queue.Id.Value.ToString("D"), command.ActorStaffMemberId, (clock ?? TimeProvider.System).GetUtcNow(), cancellationToken, command.StaffMemberId.Value.ToString("D")); return DomainResult.Success();
    }
}

public static class QueueRolePolicy
{
    public static bool CanJoin(WorkQueueType type, IEnumerable<StaffRole> roles) => roles.Any(role =>
        role == StaffRole.ApplicationOwner || type switch
        {
            WorkQueueType.Operations => role == StaffRole.OperationsStaff,
            WorkQueueType.Compliance => role == StaffRole.ComplianceReviewer,
            WorkQueueType.CreditReview => role == StaffRole.CreditReviewer,
            WorkQueueType.Finance => role == StaffRole.FinanceOperator,
            WorkQueueType.Servicing => role == StaffRole.ServicingOperator,
            WorkQueueType.Support => role == StaffRole.SupportAgent,
            _ => false
    });

    public static bool CanReceive(WorkItemType itemType, WorkQueueType queueType) => itemType switch
    {
        WorkItemType.Registration => queueType == WorkQueueType.Operations,
        WorkItemType.Vetting => queueType is WorkQueueType.Operations or WorkQueueType.Compliance,
        WorkItemType.CreditReview => queueType == WorkQueueType.CreditReview,
        WorkItemType.Support => queueType == WorkQueueType.Support,
        WorkItemType.Finance => queueType == WorkQueueType.Finance,
        WorkItemType.Servicing => queueType == WorkQueueType.Servicing,
        _ => false
    };
}

public sealed class RemoveQueueMemberHandler(IWorkQueueRepository repository, IWorkItemRepository workItems, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<RemoveQueueMemberCommand, DomainResult>
{
    public async Task<DomainResult> Handle(RemoveQueueMemberCommand command, CancellationToken cancellationToken)
    {
        var queue = await repository.GetAsync(command.QueueId, cancellationToken); if (queue is null) return DomainResult.Failure(new("work_queue.not_found", "The work queue was not found."));
        if (await workItems.HasActiveAssignmentAsync(command.QueueId, command.StaffMemberId, cancellationToken)) return DomainResult.Failure(new("work_queue.member.assigned_work", "Return or transfer this member’s active work before removing them from the queue."));
        var result = queue.RemoveMember(command.StaffMemberId); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_queue.member_removed", "work_queue", queue.Id.Value.ToString("D"), command.ActorStaffMemberId, (clock ?? TimeProvider.System).GetUtcNow(), cancellationToken, command.StaffMemberId.Value.ToString("D")); return DomainResult.Success();
    }
}
