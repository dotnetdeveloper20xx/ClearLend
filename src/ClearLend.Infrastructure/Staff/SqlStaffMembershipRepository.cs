using ClearLend.Application.Abstractions;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Staff;
using ClearLend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClearLend.Infrastructure.Staff;

public sealed class SqlStaffMembershipRepository(ClearLendDbContext db) : IStaffMembershipRepository
{
    public async Task<IReadOnlyList<StaffMembership>> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken) =>
        await db.StaffMemberships.Where(x => x.UserAccountId == userAccountId).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<StaffMembership>> GetAllAsync(CancellationToken cancellationToken) => await db.StaffMemberships.OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);

    public Task AddAsync(StaffMembership membership, CancellationToken cancellationToken)
    {
        db.StaffMemberships.Add(membership);
        return Task.CompletedTask;
    }
}
