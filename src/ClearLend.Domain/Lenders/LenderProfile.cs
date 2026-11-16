using ClearLend.Domain.Common;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Lenders;

public sealed class LenderProfile
{
    private LenderProfile() { }
    private LenderProfile(LenderProfileId id, UserAccountId accountId, string legalName, string? tradingName,
        string? registrationNumber, PersonalName contact, DateTimeOffset createdAt, ConsentRecord consent)
    {
        Id = id; UserAccountId = accountId; LegalBusinessName = legalName; TradingName = tradingName;
        BusinessRegistrationNumber = registrationNumber; PrimaryContact = contact; CreatedAt = createdAt;
        State = ProfileState.Completed; LatestConsent = consent;
    }

    public LenderProfileId Id { get; }
    public UserAccountId UserAccountId { get; }
    public string LegalBusinessName { get; } = null!;
    public string? TradingName { get; }
    public string? BusinessRegistrationNumber { get; }
    public PersonalName PrimaryContact { get; } = null!;
    public ProfileState State { get; }
    // EF Core needs a setter to include this value when inserting the aggregate.
    // The private setter keeps the timestamp immutable to application callers.
    public DateTimeOffset CreatedAt { get; private set; }
    public ConsentRecord LatestConsent { get; } = null!;

    public static DomainResult<LenderProfile> Create(UserAccountId accountId, string? legalName, string? tradingName,
        string? registrationNumber, PersonalName? contact, DateTimeOffset createdAt, ConsentRecord? consent,
        LenderProfileId? id = null)
    {
        if (accountId.Value == Guid.Empty) return DomainResults.Failure<LenderProfile>(new("lender.account.required", "A user account is required."));
        if (string.IsNullOrWhiteSpace(legalName)) return DomainResults.Failure<LenderProfile>(new("lender.business_name.required", "A legal business name is required."));
        if (contact is null) return DomainResults.Failure<LenderProfile>(new("lender.contact.required", "A primary contact is required."));
        if (consent is null || consent.Status != ConsentStatus.Granted) return DomainResults.Failure<LenderProfile>(new("lender.consent.required", "Lender terms must be accepted."));
        if (createdAt.Offset != TimeSpan.Zero) return DomainResults.Failure<LenderProfile>(new("lender.created_at.not_utc", "Creation time must be UTC."));
        return DomainResults.Success(new LenderProfile(id ?? LenderProfileId.New(), accountId, legalName.Trim(), tradingName?.Trim(), registrationNumber?.Trim(), contact, createdAt, consent));
    }
}
