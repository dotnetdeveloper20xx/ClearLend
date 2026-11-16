namespace ClearLend.Contracts.Borrowers;

public sealed record RegisterBorrowerRequest(
    string Email,
    string GivenName,
    string FamilyName,
    string ConsentStatus,
    string ConsentPolicyVersion);
