using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public sealed class EvidenceItem
{
    private EvidenceItem(
        EvidenceItemId id,
        VettingCaseId vettingCaseId,
        EvidenceType type,
        DateTimeOffset requestedAt)
    {
        Id = id;
        VettingCaseId = vettingCaseId;
        Type = type;
        RequestedAt = requestedAt;
        StatusChangedAt = requestedAt;
        Status = EvidenceStatus.Requested;
    }

    public EvidenceItemId Id { get; }

    public VettingCaseId VettingCaseId { get; }

    public EvidenceType Type { get; }

    public EvidenceStatus Status { get; private set; }

    public StorageReference? StorageReference { get; private set; }

    public DateTimeOffset RequestedAt { get; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public DateTimeOffset StatusChangedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public static DomainResult<EvidenceItem> Request(
        VettingCaseId vettingCaseId,
        EvidenceType type,
        DateTimeOffset requestedAt,
        EvidenceItemId? id = null)
    {
        if (requestedAt.Offset != TimeSpan.Zero)
        {
            return DomainResults.Failure<EvidenceItem>(
                new("evidence.requested_at.not_utc", "Request time must be expressed in UTC."));
        }

        return DomainResults.Success<EvidenceItem>(
            new(id ?? EvidenceItemId.New(), vettingCaseId, type, requestedAt));
    }

    public DomainResult Submit(StorageReference? storageReference, DateTimeOffset submittedAt)
    {
        var validTime = EnsureUtc(submittedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status != EvidenceStatus.Requested)
        {
            return DomainResult.Failure(
                new("evidence.submit.invalid_status", "Only requested evidence can be submitted. Create a new item for replacement evidence."));
        }

        if (storageReference is null)
        {
            return DomainResult.Failure(
                new("evidence.storage_reference.required", "A storage reference is required."));
        }

        StorageReference = storageReference;
        SubmittedAt = submittedAt;
        ReviewedAt = null;
        RejectionReason = null;
        Status = EvidenceStatus.Submitted;
        StatusChangedAt = submittedAt;
        return DomainResult.Success();
    }

    public DomainResult Accept(DateTimeOffset reviewedAt)
    {
        return Review(EvidenceStatus.Accepted, null, reviewedAt);
    }

    public DomainResult Reject(string? reason, DateTimeOffset reviewedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return DomainResult.Failure(
                new("evidence.rejection_reason.required", "A rejection reason is required."));
        }

        var normalisedReason = reason.Trim();
        if (normalisedReason.Length > 1000)
        {
            return DomainResult.Failure(
                new("evidence.rejection_reason.too_long", "A rejection reason cannot exceed 1000 characters."));
        }

        return Review(EvidenceStatus.Rejected, normalisedReason, reviewedAt);
    }

    public DomainResult Expire(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status is not (EvidenceStatus.Submitted or EvidenceStatus.Accepted))
        {
            return DomainResult.Failure(
                new("evidence.expire.invalid_status", "Only submitted or accepted evidence can expire."));
        }

        Status = EvidenceStatus.Expired;
        StatusChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult Supersede(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status is not (EvidenceStatus.Accepted or EvidenceStatus.Expired))
        {
            return DomainResult.Failure(
                new("evidence.supersede.invalid_status", "Only accepted or expired evidence can be superseded."));
        }

        Status = EvidenceStatus.Superseded;
        StatusChangedAt = changedAt;
        return DomainResult.Success();
    }

    private DomainResult Review(EvidenceStatus status, string? rejectionReason, DateTimeOffset reviewedAt)
    {
        var validTime = EnsureUtc(reviewedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status != EvidenceStatus.Submitted)
        {
            return DomainResult.Failure(
                new("evidence.review.invalid_status", "Only submitted evidence can be reviewed."));
        }

        Status = status;
        RejectionReason = rejectionReason;
        ReviewedAt = reviewedAt;
        StatusChangedAt = reviewedAt;
        return DomainResult.Success();
    }

    private static DomainResult EnsureUtc(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero
            ? DomainResult.Success()
            : DomainResult.Failure(new("evidence.timestamp.not_utc", "Time must be expressed in UTC."));
}
