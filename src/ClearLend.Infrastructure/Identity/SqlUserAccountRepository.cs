using ClearLend.Application.Abstractions;
using ClearLend.Domain.Identity;
using ClearLend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClearLend.Infrastructure.Identity;

public sealed class SqlUserAccountRepository(ClearLendDbContext db) : IUserAccountRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.Create(email).Value!;
        return db.UserAccounts.AnyAsync(x => x.EmailAddress == emailAddress, cancellationToken);
    }

    public Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken)
    {
        var identitySubject = IdentityProviderSubject.Create(subject).Value!;
        return db.UserAccounts.AnyAsync(x => x.IdentityProviderSubject == identitySubject, cancellationToken);
    }

    public Task<UserAccount?> GetByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken)
    {
        var identitySubject = IdentityProviderSubject.Create(subject).Value!;
        return db.UserAccounts.SingleOrDefaultAsync(x => x.IdentityProviderSubject == identitySubject, cancellationToken);
    }
    public Task AddAsync(UserAccount account, CancellationToken cancellationToken) { db.UserAccounts.Add(account); return Task.CompletedTask; }
}
