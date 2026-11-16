using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Staff;
using MediatR;

namespace ClearLend.Application.Staff.List;

public sealed record ListStaffQuery : IRequest<DomainResult<IReadOnlyList<StaffSummary>>>;
public sealed record StaffSummary(string UserAccountId, string Email, IReadOnlyCollection<StaffRole> Roles, StaffMembershipState Status, DateTimeOffset CreatedAt);

public sealed class ListStaffHandler(ICurrentActor actor, IUserAccountRepository accounts, IStaffMembershipRepository memberships) : IRequestHandler<ListStaffQuery, DomainResult<IReadOnlyList<StaffSummary>>>
{
    public async Task<DomainResult<IReadOnlyList<StaffSummary>>> Handle(ListStaffQuery request, CancellationToken cancellationToken)
    {
        if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.IdentityProviderSubject))
            return DomainResults.Failure<IReadOnlyList<StaffSummary>>(new("staff.authorization.required", "An authenticated Application Owner is required."));
        var owner = await accounts.GetByIdentityProviderSubjectAsync(actor.IdentityProviderSubject, cancellationToken);
        if (owner is null || !(await memberships.GetByUserAccountIdAsync(owner.Id, cancellationToken)).Any(x => x.State == StaffMembershipState.Active && x.Role == StaffRole.ApplicationOwner))
            return DomainResults.Failure<IReadOnlyList<StaffSummary>>(new("staff.authorization.forbidden", "Only an Application Owner can manage staff."));

        var all = await memberships.GetAllAsync(cancellationToken);
        var result = new List<StaffSummary>();
        foreach (var group in all.GroupBy(x => x.UserAccountId))
        {
            var account = await accounts.GetByIdAsync(group.Key, cancellationToken);
            if (account is not null)
            {
                var status = group.Any(x => x.State == StaffMembershipState.Active) ? StaffMembershipState.Active : group.All(x => x.State == StaffMembershipState.Removed) ? StaffMembershipState.Removed : StaffMembershipState.Disabled;
                result.Add(new StaffSummary(account.Id.Value.ToString("D"), account.EmailAddress.Value, group.Select(x => x.Role).Distinct().ToArray(), status, group.Min(x => x.CreatedAt)));
            }
        }
        return DomainResults.Success<IReadOnlyList<StaffSummary>>(result);
    }
}
