using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Staff;
using ClearLend.Domain.Audit;
using MediatR;

namespace ClearLend.Application.Staff.Lifecycle;

public sealed record RemoveStaffCommand(string UserAccountId) : IRequest<DomainResult>;

public sealed class RemoveStaffHandler(ICurrentActor actor, IUserAccountRepository accounts, IStaffMembershipRepository memberships, IUnitOfWork unitOfWork, IAuditRecordRepository audit, TimeProvider? time = null) : IRequestHandler<RemoveStaffCommand, DomainResult>
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;
    public async Task<DomainResult> Handle(RemoveStaffCommand request, CancellationToken cancellationToken)
    {
        var owner = await accounts.GetByIdentityProviderSubjectAsync(actor.IdentityProviderSubject ?? string.Empty, cancellationToken);
        if (!actor.IsAuthenticated || owner is null || !(await memberships.GetByUserAccountIdAsync(owner.Id, cancellationToken)).Any(x => x.State == StaffMembershipState.Active && x.Role == StaffRole.ApplicationOwner))
            return DomainResult.Failure(new("staff.authorization.forbidden", "Only an Application Owner can manage staff."));
        if (!Guid.TryParse(request.UserAccountId, out var id))
            return DomainResult.Failure(new("staff.account.invalid", "A valid staff account identifier is required."));
        var target = UserAccountId.From(id).Value!;
        var current = await memberships.GetByUserAccountIdAsync(target, cancellationToken);
        if (current.Count == 0)
            return DomainResult.Failure(new("staff.account.not_found", "The staff account was not found."));
        foreach (var membership in current.Where(x => x.State != StaffMembershipState.Removed))
            membership.Remove(clock.GetUtcNow());
        await audit.AddAsync(AuditRecord.Create(actor.IdentityProviderSubject!, "StaffRemoved", request.UserAccountId, clock.GetUtcNow()), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResult.Success();
    }
}
