using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Staff;
using ClearLend.Domain.Audit;
using MediatR;

namespace ClearLend.Application.Staff.Lifecycle;

public sealed record ChangeStaffStatusCommand(string UserAccountId, bool Restore) : IRequest<DomainResult>;

public sealed class ChangeStaffStatusHandler(ICurrentActor actor, IUserAccountRepository accounts, IStaffMembershipRepository memberships, IUnitOfWork unitOfWork, IAuditRecordRepository audit, TimeProvider? time = null) : IRequestHandler<ChangeStaffStatusCommand, DomainResult>
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;

    public async Task<DomainResult> Handle(ChangeStaffStatusCommand request, CancellationToken cancellationToken)
    {
        var owner = await accounts.GetByIdentityProviderSubjectAsync(actor.IdentityProviderSubject ?? string.Empty, cancellationToken);
        if (!actor.IsAuthenticated || owner is null || !(await memberships.GetByUserAccountIdAsync(owner.Id, cancellationToken)).Any(x => x.State == StaffMembershipState.Active && x.Role == StaffRole.ApplicationOwner))
            return DomainResult.Failure(new("staff.authorization.forbidden", "Only an Application Owner can manage staff."));
        if (!Guid.TryParse(request.UserAccountId, out var id))
            return DomainResult.Failure(new("staff.account.invalid", "A valid staff account identifier is required."));
        var target = ClearLend.Domain.Identity.UserAccountId.From(id).Value!;
        var targetMemberships = await memberships.GetByUserAccountIdAsync(target, cancellationToken);
        if (targetMemberships.Count == 0)
            return DomainResult.Failure(new("staff.account.not_found", "The staff account was not found."));
        foreach (var membership in targetMemberships)
        {
            var result = request.Restore ? membership.Restore() : membership.Disable(clock.GetUtcNow());
            if (!result.IsSuccess) return result;
        }
        await audit.AddAsync(AuditRecord.Create(actor.IdentityProviderSubject!, request.Restore ? "StaffRestored" : "StaffSuspended", request.UserAccountId, clock.GetUtcNow()), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResult.Success();
    }
}
