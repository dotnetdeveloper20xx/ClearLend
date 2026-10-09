using ClearLend.Domain.Identity;

namespace ClearLend.Application.Abstractions;

public interface IUserAccountRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken);
    Task AddAsync(UserAccount account, CancellationToken cancellationToken);
}
