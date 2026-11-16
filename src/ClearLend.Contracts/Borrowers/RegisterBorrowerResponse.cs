namespace ClearLend.Contracts.Borrowers;

public sealed record RegisterBorrowerResponse(
    string UserAccountId,
    string BorrowerProfileId,
    string ProfileState);
