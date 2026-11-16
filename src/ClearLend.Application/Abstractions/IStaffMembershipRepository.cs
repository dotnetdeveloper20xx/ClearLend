using ClearLend.Domain.Identity;
using ClearLend.Domain.Staff;

namespace ClearLend.Application.Abstractions;

public interface IStaffMembershipRepository
{
    Task<IReadOnlyList<StaffMembership>> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StaffMembership>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(StaffMembership membership, CancellationToken cancellationToken);
}
