using ClearLend.Application.Abstractions;
using ClearLend.Domain.Identity;

namespace ClearLend.Infrastructure.Identity;

public sealed class InMemoryUserAccountRepository : IUserAccountRepository
{
    private readonly List<UserAccount> accounts = [];

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(accounts.Any(account =>
            string.Equals(account.EmailAddress.Value, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) =>
        Task.FromResult(accounts.Any(account =>
            string.Equals(account.IdentityProviderSubject.Value, subject, StringComparison.Ordinal)));

    public Task<UserAccount?> GetByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) =>
        Task.FromResult(accounts.SingleOrDefault(account =>
            string.Equals(account.IdentityProviderSubject.Value, subject, StringComparison.Ordinal)));

    public Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        accounts.Add(account);
        return Task.CompletedTask;
    }
}
