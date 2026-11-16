namespace ClearLend.Contracts.Borrowers;

public sealed record BorrowerProfileResponse(
    string UserAccountId, string BorrowerProfileId, string Email, string GivenName, string FamilyName,
    string AccountStatus, string ProfileState, string ConsentStatus, DateTimeOffset RegistrationDate);
