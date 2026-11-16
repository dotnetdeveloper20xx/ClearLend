using ClearLend.Application.Abstractions;
using ClearLend.Domain.Identity;
using ClearLend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClearLend.Infrastructure.Identity;

public sealed class SqlUserAccountRepository(ClearLendDbContext db) : IUserAccountRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) => db.UserAccounts.AnyAsync(x => x.EmailAddress.Value == email, cancellationToken);
    public Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) => db.UserAccounts.AnyAsync(x => x.IdentityProviderSubject.Value == subject, cancellationToken);
    public Task<UserAccount?> GetByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) => db.UserAccounts.SingleOrDefaultAsync(x => x.IdentityProviderSubject.Value == subject, cancellationToken);
    public Task AddAsync(UserAccount account, CancellationToken cancellationToken) { db.UserAccounts.Add(account); return Task.CompletedTask; }
}
