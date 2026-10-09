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
        EvidenceRequest? request,
        EvidenceItemId? id = null)
    {
        if (request is null)
        {
            return DomainResults.Failure<EvidenceItem>(
                new("evidence.request.required", "Evidence-request details are required."));
        }

        if (request.RequestedAt.Offset != TimeSpan.Zero)
        {
            return DomainResults.Failure<EvidenceItem>(
                new("evidence.requested_at.not_utc", "Request time must be expressed in UTC."));
        }
        if (request.VettingCaseId.Value == Guid.Empty || !Enum.IsDefined(request.Type))
            return DomainResults.Failure<EvidenceItem>(new("evidence.request.invalid", "A valid case and supported evidence type are required."));
        if (id is { } suppliedId && suppliedId.Value == Guid.Empty)
            return DomainResults.Failure<EvidenceItem>(new("evidence.item_id.empty", "An evidence-item identifier cannot be empty."));

        return DomainResults.Success<EvidenceItem>(
            new(id ?? EvidenceItemId.New(), request.VettingCaseId, request.Type, request.RequestedAt));
    }

    public DomainResult Submit(EvidenceSubmission? submission)
    {
        if (submission is null)
        {
            return DomainResult.Failure(new("evidence.submission.required", "Evidence-submission details are required."));
        }

        var validTime = EnsureUtc(submission.SubmittedAt);
        if (!validTime.IsSuccess) return validTime;
        if (submission.StorageReference is null || submission.SubmittedAt < RequestedAt)
            return DomainResult.Failure(new("evidence.submission.invalid", "A storage reference and submission time no earlier than the request are required."));

        if (Status != EvidenceStatus.Requested)
        {
            return DomainResult.Failure(
                new("evidence.submit.invalid_status", "Only requested evidence can be submitted. Create a new item for replacement evidence."));
        }

        StorageReference = submission.StorageReference;
        SubmittedAt = submission.SubmittedAt;
        ReviewedAt = null;
        RejectionReason = null;
        Status = EvidenceStatus.Submitted;
        StatusChangedAt = submission.SubmittedAt;
        return DomainResult.Success();
    }

    public DomainResult Accept(DateTimeOffset reviewedAt)
    {
        return Review(EvidenceStatus.Accepted, null, reviewedAt);
    }

    public DomainResult Reject(EvidenceRejection? rejection)
    {
        if (rejection is null)
        {
            return DomainResult.Failure(new("evidence.rejection.required", "Evidence-rejection details are required."));
        }

        if (string.IsNullOrWhiteSpace(rejection.Reason))
        {
            return DomainResult.Failure(
                new("evidence.rejection_reason.required", "A rejection reason is required."));
        }

        var normalisedReason = rejection.Reason.Trim();
        if (normalisedReason.Length > 1000)
        {
            return DomainResult.Failure(
                new("evidence.rejection_reason.too_long", "A rejection reason cannot exceed 1000 characters."));
        }

        return Review(EvidenceStatus.Rejected, normalisedReason, rejection.ReviewedAt);
    }

    public DomainResult Expire(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;
        if (changedAt < StatusChangedAt) return DomainResult.Failure(new("evidence.timestamp.out_of_order", "Evidence history cannot move backwards."));

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
        if (changedAt < StatusChangedAt) return DomainResult.Failure(new("evidence.timestamp.out_of_order", "Evidence history cannot move backwards."));

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
        if (reviewedAt < StatusChangedAt) return DomainResult.Failure(new("evidence.timestamp.out_of_order", "Evidence history cannot move backwards."));

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
