using ClearLend.Domain.Common;

namespace ClearLend.Application.Abstractions;

public interface IIdentityAuthenticator
{
    Task<DomainResult<string>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
}
