namespace ClearLend.Domain.Identity;

public sealed record UserAccountRegistration(
    IdentityProviderSubject IdentityProviderSubject,
    EmailAddress EmailAddress,
    DateTimeOffset RegisteredAt);
