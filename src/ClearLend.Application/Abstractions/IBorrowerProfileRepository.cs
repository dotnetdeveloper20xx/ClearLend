using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;

namespace ClearLend.Application.Abstractions;

public interface IBorrowerProfileRepository
{
    Task<BorrowerProfile?> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken);
    Task AddAsync(BorrowerProfile profile, CancellationToken cancellationToken);
}
