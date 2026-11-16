namespace ClearLend.Contracts.Lenders;

public sealed record RegisterLenderRequest(string Email, string Password, string LegalBusinessName, string? TradingName,
    string? BusinessRegistrationNumber, string ContactGivenName, string ContactFamilyName, string ConsentPolicyVersion);

public sealed record RegisterLenderResponse(string UserAccountId, string LenderProfileId, string ProfileState);
public sealed record LenderProfileResponse(string UserAccountId, string LenderProfileId, string Email, string LegalBusinessName,
    string? TradingName, string? BusinessRegistrationNumber, string ContactGivenName, string ContactFamilyName,
    string AccountStatus, string ProfileState, string ConsentStatus, DateTimeOffset RegistrationDate);
