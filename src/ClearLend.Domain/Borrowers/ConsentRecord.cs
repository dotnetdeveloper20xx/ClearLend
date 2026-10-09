using ClearLend.Domain.Common;

namespace ClearLend.Domain.Borrowers;

public enum ConsentStatus { Granted = 1, Declined = 2, Withdrawn = 3 }

public sealed record ConsentRecord(
    Guid Id,
    ConsentStatus Status,
    string PolicyVersion,
    DateTimeOffset RecordedAt)
{
    public static DomainResult<ConsentRecord> Record(ConsentStatus status, string? policyVersion, DateTimeOffset recordedAt, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(policyVersion))
            return DomainResults.Failure<ConsentRecord>(new("consent.policy_version.required", "A consent-policy version is required."));
        if (recordedAt.Offset != TimeSpan.Zero)
            return DomainResults.Failure<ConsentRecord>(new("consent.recorded_at.not_utc", "Consent time must be expressed in UTC."));
        if (policyVersion.Trim().Length > 100)
            return DomainResults.Failure<ConsentRecord>(new("consent.policy_version.too_long", "A consent-policy version cannot exceed 100 characters."));
        if (id is Guid suppliedId && suppliedId == Guid.Empty)
            return DomainResults.Failure<ConsentRecord>(new("consent.id.empty", "A consent identifier cannot be empty."));
        return DomainResults.Success(new ConsentRecord(id ?? Guid.NewGuid(), status, policyVersion.Trim(), recordedAt));
    }
}
