using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace ClearLend.Infrastructure.Identity;

public sealed class AspNetIdentityAuthenticator(UserManager<ClearLendIdentityUser> userManager) : IIdentityAuthenticator
{
    public async Task<DomainResult<IdentityAuthenticationResult>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return DomainResults.Failure<IdentityAuthenticationResult>(
                new("identity.credentials.invalid", "The email address or password is invalid."));
        }

        return DomainResults.Success(new IdentityAuthenticationResult(user.Id, user.MustChangePassword));
    }

    public async Task<DomainResult> ChangePasswordAsync(string subject, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(subject);
        if (user is null)
            return DomainResult.Failure(new("identity.account.not_found", "The identity account could not be found."));

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return DomainResult.Failure(new("identity.password.change_failed", result.Errors.FirstOrDefault()?.Description ?? "The password could not be changed."));

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        return DomainResult.Success();
    }

    public async Task<DomainResult> ResetPasswordAsync(string subject, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(subject);
        if (user is null)
            return DomainResult.Failure(new("identity.account.not_found", "The identity account could not be found."));
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return DomainResult.Failure(new("identity.password.reset_failed", result.Errors.FirstOrDefault()?.Description ?? "The password could not be reset."));
        user.MustChangePassword = true;
        await userManager.UpdateAsync(user);
        return DomainResult.Success();
    }
}
