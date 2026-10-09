using ClearLend.Application.Abstractions;
using ClearLend.Application.Behaviors;
using ClearLend.Application.Operations;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;

namespace ClearLend.Application.Operations.WorkItems;

public sealed record CreateWorkQueueCommand(WorkQueueType Type, string Name, StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult<WorkQueueId>>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }

public sealed class CreateWorkQueueCommandValidator : AbstractValidator<CreateWorkQueueCommand>
{
    public CreateWorkQueueCommandValidator()
    { RuleFor(x => x.Type).IsInEnum(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); }
}

public sealed class CreateWorkQueueHandler(IWorkQueueRepository queues, IAuditEventWriter audit, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    : IRequestHandler<CreateWorkQueueCommand, DomainResult<WorkQueueId>>
{
    public async Task<DomainResult<WorkQueueId>> Handle(CreateWorkQueueCommand command, CancellationToken cancellationToken)
    {
        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var created = WorkQueueDefinition.Create(command.Type, command.Name, at);
        if (!created.TryGetValue(out var queue)) return DomainResults.Failure<WorkQueueId>(created.Error!);
        await queues.AddAsync(queue, cancellationToken);
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "work_queue.created", "work_queue", queue.Id.Value.ToString("D"), command.ActorStaffMemberId, at, cancellationToken);
        return DomainResults.Success(queue.Id);
    }
}
