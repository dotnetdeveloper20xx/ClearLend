using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace ClearLend.Infrastructure.Identity;

public sealed class AspNetIdentityAuthenticator(UserManager<ClearLendIdentityUser> userManager) : IIdentityAuthenticator
{
    public async Task<DomainResult<string>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return DomainResults.Failure<string>(
                new("identity.credentials.invalid", "The email address or password is invalid."));
        }

        return DomainResults.Success(user.Id);
    }
}
