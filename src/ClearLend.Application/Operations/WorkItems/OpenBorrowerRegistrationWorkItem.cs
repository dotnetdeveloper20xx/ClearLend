using ClearLend.Application.Abstractions;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;
using ClearLend.Application.Behaviors;

namespace ClearLend.Application.Operations.WorkItems;

public sealed record OpenBorrowerRegistrationWorkItemCommand(BorrowerProfileId BorrowerProfileId, WorkQueueId QueueId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult<WorkItemId>>, IAuthorizedRequest
{ public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }

public sealed class OpenBorrowerRegistrationWorkItemCommandValidator : AbstractValidator<OpenBorrowerRegistrationWorkItemCommand>
{
    public OpenBorrowerRegistrationWorkItemCommandValidator() { RuleFor(x => x.BorrowerProfileId.Value).NotEmpty(); RuleFor(x => x.QueueId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); }
}

public sealed class OpenBorrowerRegistrationWorkItemHandler(IWorkItemRepository repository, IWorkQueueRepository queues, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    : IRequestHandler<OpenBorrowerRegistrationWorkItemCommand, DomainResult<WorkItemId>>
{
    public async Task<DomainResult<WorkItemId>> Handle(OpenBorrowerRegistrationWorkItemCommand command, CancellationToken cancellationToken)
    {
        var queue = await queues.GetAsync(command.QueueId, cancellationToken);
        if (queue is null || queue.Type != WorkQueueType.Operations) return DomainResults.Failure<WorkItemId>(new("work_item.registration_queue.invalid", "Registration intake must enter an Operations queue."));
        var subject = command.BorrowerProfileId.ToString();
        if (await repository.FindActiveAsync(WorkItemType.Registration, subject, cancellationToken) is not null)
            return DomainResults.Failure<WorkItemId>(new("work_item.duplicate_active", "An active registration work item already exists for this subject."));
        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var result = WorkItem.Open(WorkItemType.Registration, subject, WorkItemPriority.Normal, at, queueId: queue.Id);
        if (!result.TryGetValue(out var workItem)) return DomainResults.Failure<WorkItemId>(result.Error!);
        await repository.AddAsync(workItem, cancellationToken);
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_item.opened", "work_item", workItem.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken, "registration_intake");
        return DomainResults.Success(workItem.Id);
    }
}
