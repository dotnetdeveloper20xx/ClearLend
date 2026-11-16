using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Staff;
using ClearLend.Domain.Audit;
using MediatR;

namespace ClearLend.Application.Staff.Lifecycle;

public sealed record ChangeStaffRolesCommand(string UserAccountId, IReadOnlyCollection<StaffRole> Roles) : IRequest<DomainResult>;

public sealed class ChangeStaffRolesHandler(ICurrentActor actor, IUserAccountRepository accounts, IStaffMembershipRepository memberships, IUnitOfWork unitOfWork, IAuditRecordRepository audit, TimeProvider? time = null) : IRequestHandler<ChangeStaffRolesCommand, DomainResult>
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;

    public async Task<DomainResult> Handle(ChangeStaffRolesCommand request, CancellationToken cancellationToken)
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
        var roles = request.Roles.Distinct().ToArray();
        if (roles.Length == 0 || roles.Any(role => !Enum.IsDefined(role)))
            return DomainResult.Failure(new("staff.role.invalid", "At least one valid staff role is required."));
        var changedAt = clock.GetUtcNow();
        foreach (var membership in current.Where(x => x.State == StaffMembershipState.Active))
            membership.Disable(changedAt);
        foreach (var role in roles)
        {
            var membership = StaffMembership.Create(target, role, changedAt);
            if (!membership.TryGetValue(out var created)) return DomainResult.Failure(membership.Error!);
            await memberships.AddAsync(created, cancellationToken);
        }
        await audit.AddAsync(AuditRecord.Create(actor.IdentityProviderSubject!, "StaffRolesChanged", request.UserAccountId, changedAt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResult.Success();
    }
}
