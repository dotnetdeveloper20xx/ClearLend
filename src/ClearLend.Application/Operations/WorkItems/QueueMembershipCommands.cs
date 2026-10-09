using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;
using ClearLend.Application.Behaviors;

namespace ClearLend.Application.Operations.WorkItems;

public sealed record AddQueueMemberCommand(WorkQueueId QueueId, StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest
{ public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }
public sealed record RemoveQueueMemberCommand(WorkQueueId QueueId, StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest
{ public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }

public sealed class AddQueueMemberCommandValidator : AbstractValidator<AddQueueMemberCommand>
{ public AddQueueMemberCommandValidator() { RuleFor(x => x.QueueId.Value).NotEmpty(); RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class RemoveQueueMemberCommandValidator : AbstractValidator<RemoveQueueMemberCommand>
{ public RemoveQueueMemberCommandValidator() { RuleFor(x => x.QueueId.Value).NotEmpty(); RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }

public sealed class AddQueueMemberHandler(IWorkQueueRepository repository, IStaffMemberRepository staffRepository, IUnitOfWork unitOfWork) : IRequestHandler<AddQueueMemberCommand, DomainResult>
{
    public async Task<DomainResult> Handle(AddQueueMemberCommand command, CancellationToken cancellationToken)
    {
        var queue = await repository.GetAsync(command.QueueId, cancellationToken); if (queue is null) return DomainResult.Failure(new("work_queue.not_found", "The work queue was not found."));
        var staffMember = await staffRepository.GetAsync(command.StaffMemberId, cancellationToken);
        if (staffMember is null || staffMember.Status != StaffStatus.Active) return DomainResult.Failure(new("work_queue.member.inactive", "Only an active staff member can join a work queue."));
        var result = queue.AddMember(command.StaffMemberId); if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken); return DomainResult.Success();
    }
}

public sealed class RemoveQueueMemberHandler(IWorkQueueRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<RemoveQueueMemberCommand, DomainResult>
{
    public async Task<DomainResult> Handle(RemoveQueueMemberCommand command, CancellationToken cancellationToken)
    {
        var queue = await repository.GetAsync(command.QueueId, cancellationToken); if (queue is null) return DomainResult.Failure(new("work_queue.not_found", "The work queue was not found."));
        var result = queue.RemoveMember(command.StaffMemberId); if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken); return DomainResult.Success();
    }
}
