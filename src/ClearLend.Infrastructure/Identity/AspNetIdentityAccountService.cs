using ClearLend.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace ClearLend.Infrastructure.Identity;

public sealed class AspNetIdentityAccountService(UserManager<ClearLendIdentityUser> userManager) : IIdentityAccountService
{
    public async Task<IdentityAccountCreationResult> CreateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = new ClearLendIdentityUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            return IdentityAccountCreationResult.Success(user.Id);

        var error = result.Errors.FirstOrDefault();
        return IdentityAccountCreationResult.Failure(
            error?.Code == "DuplicateUserName" ? "identity.email.already_registered" : "identity.registration.failed",
            error?.Description ?? "The identity account could not be created.");
    }
}
