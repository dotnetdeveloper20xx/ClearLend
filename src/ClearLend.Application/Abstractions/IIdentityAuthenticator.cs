using ClearLend.Domain.Common;

namespace ClearLend.Application.Abstractions;

public interface IIdentityAuthenticator
{
    Task<DomainResult<IdentityAuthenticationResult>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
    Task<DomainResult> ChangePasswordAsync(string subject, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<DomainResult> ResetPasswordAsync(string subject, string newPassword, CancellationToken cancellationToken);
}

public sealed record IdentityAuthenticationResult(string Subject, bool MustChangePassword);
