using ClearLend.Application.Abstractions;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;

namespace ClearLend.Infrastructure.Borrowers;

public sealed class InMemoryBorrowerProfileRepository : IBorrowerProfileRepository
{
    public List<BorrowerProfile> Items { get; } = [];

    public Task<BorrowerProfile?> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(profile => profile.UserAccountId == userAccountId));

    public Task AddAsync(BorrowerProfile profile, CancellationToken cancellationToken)
    {
        Items.Add(profile);
        return Task.CompletedTask;
    }
}
