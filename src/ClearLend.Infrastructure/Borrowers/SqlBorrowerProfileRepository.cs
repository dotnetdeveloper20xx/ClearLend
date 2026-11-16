using ClearLend.Application.Abstractions;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;
using ClearLend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClearLend.Infrastructure.Borrowers;

public sealed class SqlBorrowerProfileRepository(ClearLendDbContext db) : IBorrowerProfileRepository
{
    public Task<BorrowerProfile?> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken) => db.BorrowerProfiles.SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);
    public Task AddAsync(BorrowerProfile profile, CancellationToken cancellationToken) { db.BorrowerProfiles.Add(profile); return Task.CompletedTask; }
}
