using ClearLend.Domain.Identity;

namespace ClearLend.Application.Identity.Register;

public sealed record RegisterUserResult(UserAccountId UserAccountId, string Email, AccountStatus Status);
