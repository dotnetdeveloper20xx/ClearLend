using ClearLend.Application.Abstractions;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Lenders;
using ClearLend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClearLend.Infrastructure.Lenders;

public sealed class SqlLenderProfileRepository(ClearLendDbContext db) : ILenderProfileRepository
{
    public Task<LenderProfile?> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken) => db.LenderProfiles.SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);
    public Task AddAsync(LenderProfile profile, CancellationToken cancellationToken) { db.LenderProfiles.Add(profile); return Task.CompletedTask; }
}
