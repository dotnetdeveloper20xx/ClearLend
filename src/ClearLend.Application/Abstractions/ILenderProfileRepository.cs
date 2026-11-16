using ClearLend.Domain.Identity;
using ClearLend.Domain.Lenders;

namespace ClearLend.Application.Abstractions;

public interface ILenderProfileRepository
{
    Task<LenderProfile?> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken);
    Task AddAsync(LenderProfile profile, CancellationToken cancellationToken);
}
