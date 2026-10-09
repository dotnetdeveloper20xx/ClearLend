using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IStaffMemberRepository
{
    Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken);
    Task<StaffMember?> GetAsync(StaffMemberId id, CancellationToken cancellationToken);
    Task<int> CountActiveApplicationOwnersAsync(StaffMemberId? excluding, CancellationToken cancellationToken);
    Task<int> CountApplicationOwnersAsync(CancellationToken cancellationToken);
    Task<bool> ExistsForAccountAsync(ClearLend.Domain.Identity.UserAccountId accountId, CancellationToken cancellationToken);
}
