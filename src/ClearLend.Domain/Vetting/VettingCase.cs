using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed class VettingCase
{
    private VettingCase(
        VettingCaseId id,
        UserAccountId subjectAccountId,
        VettingSubjectType subjectType,
        DateTimeOffset openedAt)
    {
        Id = id;
        SubjectAccountId = subjectAccountId;
        SubjectType = subjectType;
        OpenedAt = openedAt;
        LastChangedAt = openedAt;
        Status = VettingCaseStatus.Open;
    }

    public VettingCaseId Id { get; }

    public UserAccountId SubjectAccountId { get; }

    public VettingSubjectType SubjectType { get; }

    public VettingCaseStatus Status { get; private set; }

    public UserAccountId? ReviewerAccountId { get; private set; }

    public DateTimeOffset OpenedAt { get; }

    public DateTimeOffset LastChangedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public static DomainResult<VettingCase> Open(
        UserAccountId subjectAccountId,
        VettingSubjectType subjectType,
        DateTimeOffset openedAt,
        VettingCaseId? id = null)
    {
        if (openedAt.Offset != TimeSpan.Zero)
        {
            return DomainResults.Failure<VettingCase>(
                new("vetting.opened_at.not_utc", "Opening time must be expressed in UTC."));
        }

        return DomainResults.Success<VettingCase>(
            new(id ?? VettingCaseId.New(), subjectAccountId, subjectType, openedAt));
    }

    public DomainResult AssignReviewer(UserAccountId reviewerAccountId, DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status is VettingCaseStatus.Approved or VettingCaseStatus.Rejected or VettingCaseStatus.Closed)
        {
            return DomainResult.Failure(
                new("vetting.reviewer_assignment.invalid_status", "A completed or closed case cannot be assigned."));
        }

        ReviewerAccountId = reviewerAccountId;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult StartReview(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status != VettingCaseStatus.Open)
        {
            return DomainResult.Failure(
                new("vetting.review.start.invalid_status", "Only an open case can enter review."));
        }

        Status = VettingCaseStatus.InReview;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult RequestInformation(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status != VettingCaseStatus.InReview)
        {
            return DomainResult.Failure(
                new("vetting.information_request.invalid_status", "Information can only be requested during review."));
        }

        Status = VettingCaseStatus.AwaitingInformation;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult ResumeReview(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status != VettingCaseStatus.AwaitingInformation)
        {
            return DomainResult.Failure(
                new("vetting.review.resume.invalid_status", "Only a case awaiting information can resume review."));
        }

        Status = VettingCaseStatus.InReview;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult ApplyDecision(VettingDecision decision, DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (decision.VettingCaseId != Id)
        {
            return DomainResult.Failure(
                new("vetting.decision.case_mismatch", "The decision does not belong to this vetting case."));
        }

        if (Status != VettingCaseStatus.InReview)
        {
            return DomainResult.Failure(
                new("vetting.decision.invalid_status", "Only a case in review can receive a decision."));
        }

        Status = decision.Outcome switch
        {
            DecisionOutcome.Approved => VettingCaseStatus.Approved,
            DecisionOutcome.Rejected => VettingCaseStatus.Rejected,
            DecisionOutcome.MoreInformationRequired => VettingCaseStatus.AwaitingInformation,
            DecisionOutcome.Restricted => VettingCaseStatus.Suspended,
            _ => VettingCaseStatus.Suspended
        };
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult Suspend(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status is VettingCaseStatus.Approved or VettingCaseStatus.Rejected or VettingCaseStatus.Closed)
        {
            return DomainResult.Failure(
                new("vetting.suspend.invalid_status", "A completed or closed case cannot be suspended."));
        }

        Status = VettingCaseStatus.Suspended;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult Close(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status == VettingCaseStatus.Closed)
        {
            return DomainResult.Failure(
                new("vetting.case.already_closed", "The vetting case is already closed."));
        }

        Status = VettingCaseStatus.Closed;
        ClosedAt = changedAt;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    private static DomainResult EnsureUtc(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero
            ? DomainResult.Success()
            : DomainResult.Failure(new("vetting.timestamp.not_utc", "Time must be expressed in UTC."));
}
