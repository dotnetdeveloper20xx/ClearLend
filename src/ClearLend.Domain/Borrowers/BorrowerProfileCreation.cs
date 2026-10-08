using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Borrowers;

public sealed record BorrowerProfileCreation(
    UserAccountId UserAccountId,
    DateTimeOffset CreatedAt);
