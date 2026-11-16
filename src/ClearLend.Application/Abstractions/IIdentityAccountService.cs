namespace ClearLend.Application.Abstractions;

public interface IIdentityAccountService
{
    Task<IdentityAccountCreationResult> CreateAsync(string email, string password, CancellationToken cancellationToken);
}

public sealed record IdentityAccountCreationResult(bool Succeeded, string? Subject, string? ErrorCode, string? ErrorMessage)
{
    public static IdentityAccountCreationResult Success(string subject) => new(true, subject, null, null);
    public static IdentityAccountCreationResult Failure(string code, string message) => new(false, null, code, message);
}
