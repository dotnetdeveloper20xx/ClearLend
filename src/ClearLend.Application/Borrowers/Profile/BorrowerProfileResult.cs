namespace ClearLend.Application.Borrowers.Profile;

public sealed record BorrowerProfileResult(
    string UserAccountId,
    string BorrowerProfileId,
    string Email,
    string GivenName,
    string FamilyName,
    string AccountStatus,
    string ProfileState,
    string ConsentStatus,
    DateTimeOffset RegistrationDate);
