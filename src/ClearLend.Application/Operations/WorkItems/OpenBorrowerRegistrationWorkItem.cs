using ClearLend.Application.Abstractions;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;
using ClearLend.Application.Behaviors;

namespace ClearLend.Application.Operations.WorkItems;

public sealed record OpenBorrowerRegistrationWorkItemCommand(BorrowerProfileId BorrowerProfileId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult<WorkItemId>>, IAuthorizedRequest
{ public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems; }

public sealed class OpenBorrowerRegistrationWorkItemCommandValidator : AbstractValidator<OpenBorrowerRegistrationWorkItemCommand>
{
    public OpenBorrowerRegistrationWorkItemCommandValidator() { RuleFor(x => x.BorrowerProfileId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); }
}

public sealed class OpenBorrowerRegistrationWorkItemHandler(IWorkItemRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    : IRequestHandler<OpenBorrowerRegistrationWorkItemCommand, DomainResult<WorkItemId>>
{
    public async Task<DomainResult<WorkItemId>> Handle(OpenBorrowerRegistrationWorkItemCommand command, CancellationToken cancellationToken)
    {
        var result = WorkItem.Open(WorkItemType.Registration, command.BorrowerProfileId.ToString(), WorkItemPriority.Normal, (clock ?? TimeProvider.System).GetUtcNow());
        if (!result.TryGetValue(out var workItem)) return DomainResults.Failure<WorkItemId>(result.Error!);
        await repository.AddAsync(workItem, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResults.Success(workItem.Id);
    }
}
