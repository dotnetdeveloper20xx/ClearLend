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

public sealed class OpenWorkItemCommandValidator : AbstractValidator<OpenWorkItemCommand>
{ public OpenWorkItemCommandValidator() { RuleFor(x => x.SubjectReference).NotEmpty().MaximumLength(200); RuleFor(x => x.Type).IsInEnum(); RuleFor(x => x.Priority).IsInEnum(); RuleFor(x => x.QueueId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class AssignWorkItemCommandValidator : AbstractValidator<AssignWorkItemCommand>
{ public AssignWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class CompleteWorkItemCommandValidator : AbstractValidator<CompleteWorkItemCommand>
{ public CompleteWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class EscalateWorkItemCommandValidator : AbstractValidator<EscalateWorkItemCommand>
{ public EscalateWorkItemCommandValidator() { RuleFor(x => x.WorkItemId.Value).NotEmpty(); RuleFor(x => x.Reason).NotEmpty().MaximumLength(500); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }

public sealed class OpenWorkItemHandler(IWorkItemRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<OpenWorkItemCommand, DomainResult<WorkItemId>>
{
    public async Task<DomainResult<WorkItemId>> Handle(OpenWorkItemCommand command, CancellationToken cancellationToken)
    {
        var result = WorkItem.Open(command.Type, command.SubjectReference, command.Priority, (clock ?? TimeProvider.System).GetUtcNow(), queueId: command.QueueId);
        if (!result.TryGetValue(out var item)) return DomainResults.Failure<WorkItemId>(result.Error!);
        await repository.AddAsync(item, cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken); return DomainResults.Success(item.Id);
    }
}

public sealed class AssignWorkItemHandler(IWorkItemRepository repository, IWorkQueueRepository queueRepository, IStaffMemberRepository staffRepository, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<AssignWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(AssignWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        if (item.QueueId is null) return DomainResult.Failure(new("work_item.queue.required", "The work item must belong to a queue before it can be assigned."));
        var staffMember = await staffRepository.GetAsync(command.StaffMemberId, cancellationToken);
        if (staffMember is null || staffMember.Status != StaffStatus.Active) return DomainResult.Failure(new("work_item.assignee.inactive", "Only an active staff member can be assigned work."));
        var queue = await queueRepository.GetAsync(item.QueueId.Value, cancellationToken);
        if (queue is null || !queue.Members.Contains(command.StaffMemberId)) return DomainResult.Failure(new("work_item.assignee.not_queue_member", "The assignee must belong to the work item queue."));
        var result = item.AssignTo(command.StaffMemberId, (clock ?? TimeProvider.System).GetUtcNow()); if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken); return DomainResult.Success();
    }
}

public sealed class CompleteWorkItemHandler(IWorkItemRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<CompleteWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(CompleteWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var result = item.Complete((clock ?? TimeProvider.System).GetUtcNow()); if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken); return DomainResult.Success();
    }
}

public sealed class EscalateWorkItemHandler(IWorkItemRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null) : IRequestHandler<EscalateWorkItemCommand, DomainResult>
{
    public async Task<DomainResult> Handle(EscalateWorkItemCommand command, CancellationToken cancellationToken)
    {
        var item = await repository.GetAsync(command.WorkItemId, cancellationToken); if (item is null) return DomainResult.Failure(new("work_item.not_found", "The work item was not found."));
        var result = item.Escalate(command.Reason, (clock ?? TimeProvider.System).GetUtcNow()); if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken); return DomainResult.Success();
    }
}
