using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;
using ClearLend.Application.Behaviors;

namespace ClearLend.Application.Operations.WorkItems;

public sealed record OpenWorkItemCommand(WorkItemType Type, string SubjectReference, WorkItemPriority Priority, WorkQueueId QueueId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult<WorkItemId>>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record AssignWorkItemCommand(WorkItemId WorkItemId, StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record CompleteWorkItemCommand(WorkItemId WorkItemId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record EscalateWorkItemCommand(WorkItemId WorkItemId, string Reason, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record UnassignWorkItemCommand(WorkItemId WorkItemId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record TransferWorkItemCommand(WorkItemId WorkItemId, WorkQueueId TargetQueueId, string Reason, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record CancelWorkItemCommand(WorkItemId WorkItemId, string Reason, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record ChangeWorkItemPriorityCommand(WorkItemId WorkItemId, WorkItemPriority Priority, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record ResolveWorkItemEscalationCommand(WorkItemId WorkItemId, string Resolution, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }

public sealed class OpenWorkItemCommandValidator : AbstractValidator<OpenWorkItemCommand>
{ public OpenWorkItemCommandValidator() { RuleFor(x => x.SubjectReference).NotEmpty().MaximumLength(200); RuleFor(x => x.Type).IsInEnum(); RuleFor(x => x.Priority).IsInEnum(); RuleFor(x => x.QueueId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class AssignWorkItemCommandValidator : AbstractValidator<AssignWorkItemCommand>
{ public AssignWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class CompleteWorkItemCommandValidator : AbstractValidator<CompleteWorkItemCommand>
{ public CompleteWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class EscalateWorkItemCommandValidator : AbstractValidator<EscalateWorkItemCommand>
{ public EscalateWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.Reason).NotEmpty().MaximumLength(500); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class UnassignWorkItemCommandValidator : AbstractValidator<UnassignWorkItemCommand>
{ public UnassignWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class TransferWorkItemCommandValidator : AbstractValidator<TransferWorkItemCommand>
{ public TransferWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.TargetQueueId.Value).NotEmpty(); RuleFor(x => x.Reason).NotEmpty().MaximumLength(500); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class CancelWorkItemCommandValidator : AbstractValidator<CancelWorkItemCommand>
{ public CancelWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.Reason).NotEmpty().MaximumLength(500); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class ChangeWorkItemPriorityCommandValidator : AbstractValidator<ChangeWorkItemPriorityCommand>
{ public ChangeWorkItemPriorityCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.Priority).IsInEnum(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class ResolveWorkItemEscalationCommandValidator : AbstractValidator<ResolveWorkItemEscalationCommand>
{ public ResolveWorkItemEscalationCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.Resolution).NotEmpty().MaximumLength(500); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }

public sealed class OpenWorkItemHandler(IWorkItemRepository repository, IWorkQueueRepository queues, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<OpenWorkItemCommand, DomainResult<WorkItemId>>
{
    public async Task<DomainResult<WorkItemId>> Handle(OpenWorkItemCommand command, CancellationToken cancellationToken)
    {
        var queue = await queues.GetAsync(command.QueueId, cancellationToken);
        if (queue is null || !QueueRolePolicy.CanReceive(command.Type, queue.Type)) return DomainResults.Failure<WorkItemId>(new("work_item.queue.ineligible", "The selected queue cannot receive this type of work."));
        if (await repository.FindActiveAsync(command.Type, command.SubjectReference, cancellationToken) is not null) return DomainResults.Failure<WorkItemId>(new("work_item.duplicate_active", "An active work item already exists for this subject and type."));
        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var result = WorkItem.Open(command.Type, command.SubjectReference, command.Priority, at, queueId: command.QueueId);
        if (!result.TryGetValue(out var item)) return DomainResults.Failure<WorkItemId>(result.Error!);
        await repository.AddAsync(item, cancellationToken); await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.opened", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, command.Type.ToString()); return DomainResults.Success(item.Id);
    }
}

public sealed class AssignWorkItemHandler(IWorkItemRepository repository, IWorkQueueRepository queueRepository, IStaffMemberRepository staffRepository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<AssignWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(AssignWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        if (item.QueueId is null) return DomainResult.Failure(new("work_item.queue.required", "The work item must belong to a queue before it can be assigned."));
        var staffMember = await staffRepository.GetAsync(command.StaffMemberId, cancellationToken);
        if (staffMember is null || staffMember.Status != StaffStatus.Active) return DomainResult.Failure(new("work_item.assignee.inactive", "Only an active staff member can be assigned work."));
        var queue = await queueRepository.GetAsync(item.QueueId.Value, cancellationToken);
        if (queue is null || !queue.Members.Contains(command.StaffMemberId)) return DomainResult.Failure(new("work_item.assignee.not_queue_member", "The assignee must belong to the work item queue."));
        if (!QueueRolePolicy.CanJoin(queue.Type, staffMember.Roles)) return DomainResult.Failure(new("work_item.assignee.role_ineligible", "The assignee no longer has a role eligible for this queue."));
        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var result = item.AssignTo(command.StaffMemberId, at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.assigned", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, command.StaffMemberId.Value.ToString("D")); return DomainResult.Success();
    }
}

public sealed class CompleteWorkItemHandler(IWorkItemRepository repository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<CompleteWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(CompleteWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var result = item.Complete(at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.completed", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken); return DomainResult.Success();
    }
}

public sealed class EscalateWorkItemHandler(IWorkItemRepository repository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<EscalateWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(EscalateWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var result = item.Escalate(command.Reason, at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.escalated", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, command.Reason); return DomainResult.Success();
    }
}

public sealed class UnassignWorkItemHandler(IWorkItemRepository repository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<UnassignWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(UnassignWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var at = (clock ?? TimeProvider.System).GetUtcNow(); var result = item.Unassign(at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.unassigned", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken); return DomainResult.Success();
    }
}

public sealed class TransferWorkItemHandler(IWorkItemRepository repository, IWorkQueueRepository queues, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<TransferWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(TransferWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var destination = await queues.GetAsync(command.TargetQueueId, cancellationToken);
        if (destination is null || !QueueRolePolicy.CanReceive(item.Type, destination.Type)) return DomainResult.Failure(new("work_item.queue.ineligible", "The destination queue cannot receive this type of work."));
        var at = (clock ?? TimeProvider.System).GetUtcNow(); var result = item.TransferTo(command.TargetQueueId, command.Reason, at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.transferred", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, command.Reason); return DomainResult.Success();
    }
}

public sealed class CancelWorkItemHandler(IWorkItemRepository repository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<CancelWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(CancelWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var at = (clock ?? TimeProvider.System).GetUtcNow(); var result = item.Cancel(command.Reason, at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.cancelled", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, command.Reason); return DomainResult.Success();
    }
}

public sealed class ChangeWorkItemPriorityHandler(IWorkItemRepository repository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<ChangeWorkItemPriorityCommand, DomainResult>
{
    public async Task<DomainResult> Handle(ChangeWorkItemPriorityCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var at = (clock ?? TimeProvider.System).GetUtcNow(); var result = item.ChangePriority(command.Priority, at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.priority_changed", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, command.Priority.ToString()); return DomainResult.Success();
    }
}

public sealed class ResolveWorkItemEscalationHandler(IWorkItemRepository repository, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<ResolveWorkItemEscalationCommand, DomainResult>
{
    public async Task<DomainResult> Handle(ResolveWorkItemEscalationCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var at = (clock ?? TimeProvider.System).GetUtcNow(); var result = item.ResolveEscalation(command.ActorStaffMemberId, command.Resolution, at); if (!result.IsSuccess) return result;
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.escalation_resolved", "work_item", item.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, command.Resolution); return DomainResult.Success();
    }
}
