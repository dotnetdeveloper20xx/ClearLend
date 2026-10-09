using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed class VettingCase
{
    private readonly List<VettingDecision> decisionHistory = [];
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
    public IReadOnlyList<VettingDecision> DecisionHistory => decisionHistory.AsReadOnly();

    public static DomainResult<VettingCase> Open(
        VettingCaseOpening? opening,
        VettingCaseId? id = null)
    {
        if (opening is null)
        {
            return DomainResults.Failure<VettingCase>(
                new("vetting.opening.required", "Vetting-case opening details are required."));
        }

        if (opening.OpenedAt.Offset != TimeSpan.Zero)
        {
            return DomainResults.Failure<VettingCase>(
                new("vetting.opened_at.not_utc", "Opening time must be expressed in UTC."));
        }

        if (opening.SubjectAccountId.Value == Guid.Empty || !Enum.IsDefined(opening.SubjectType))
            return DomainResults.Failure<VettingCase>(new("vetting.opening.invalid", "A valid subject and supported subject type are required."));
        if (id is { } suppliedId && suppliedId.Value == Guid.Empty)
            return DomainResults.Failure<VettingCase>(new("vetting.case_id.empty", "A vetting-case identifier cannot be empty."));

        return DomainResults.Success<VettingCase>(
            new(id ?? VettingCaseId.New(), opening.SubjectAccountId, opening.SubjectType, opening.OpenedAt));
    }

    public DomainResult AssignReviewer(ReviewerAssignment? assignment)
    {
        if (assignment is null)
        {
            return DomainResult.Failure(
                new("vetting.reviewer_assignment.required", "Reviewer-assignment details are required."));
        }

        var validTime = EnsureUtcAfterLastChange(assignment.AssignedAt);
        if (!validTime.IsSuccess) return validTime;

        if (assignment.ReviewerAccountId.Value == Guid.Empty || assignment.ReviewerAccountId == SubjectAccountId)
            return DomainResult.Failure(new("vetting.reviewer_assignment.invalid", "The reviewer must be a valid account distinct from the subject."));

        if (Status is VettingCaseStatus.Approved or VettingCaseStatus.Rejected or VettingCaseStatus.Closed)
        {
            return DomainResult.Failure(
                new("vetting.reviewer_assignment.invalid_status", "A completed or closed case cannot be assigned."));
        }

        ReviewerAccountId = assignment.ReviewerAccountId;
        LastChangedAt = assignment.AssignedAt;
        return DomainResult.Success();
    }

    public DomainResult StartReview(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtcAfterLastChange(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status != VettingCaseStatus.Open)
        {
            return DomainResult.Failure(
                new("vetting.review.start.invalid_status", "Only an open case can enter review."));
        }

        if (ReviewerAccountId is null)
            return DomainResult.Failure(new("vetting.review.reviewer.required", "A reviewer must be assigned before review can start."));

        Status = VettingCaseStatus.InReview;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult RequestInformation(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtcAfterLastChange(changedAt);
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
        var validTime = EnsureUtcAfterLastChange(changedAt);
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

    public DomainResult ResumeFromSuspension(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtcAfterLastChange(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (Status != VettingCaseStatus.Suspended)
        {
            return DomainResult.Failure(
                new("vetting.suspension.resume.invalid_status", "Only a suspended case can resume review."));
        }

        Status = VettingCaseStatus.InReview;
        LastChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult ApplyDecision(VettingDecision? decision, DateTimeOffset changedAt)
    {
        var validTime = EnsureUtcAfterLastChange(changedAt);
        if (!validTime.IsSuccess) return validTime;
        if (decision is null) return DomainResult.Failure(new("vetting.decision.required", "A vetting decision is required."));

        if (decision.VettingCaseId != Id)
        {
            return DomainResult.Failure(
                new("vetting.decision.case_mismatch", "The decision does not belong to this vetting case."));
        }

        if (decision.SubjectAccountId != SubjectAccountId || decision.SubjectType != SubjectType)
            return DomainResult.Failure(new("vetting.decision.subject_mismatch", "The decision subject does not match this case."));
        if (ReviewerAccountId is null || decision.ReviewerAccountId != ReviewerAccountId)
            return DomainResult.Failure(new("vetting.decision.reviewer_mismatch", "The decision must be made by the assigned reviewer."));
        if (decision.DecidedAt < LastChangedAt || decision.DecidedAt > changedAt)
            return DomainResult.Failure(new("vetting.decision.timestamp.invalid", "Decision time must fall between the previous case change and this transition."));
        if (Status != VettingCaseStatus.InReview)
        {
            return DomainResult.Failure(
                new("vetting.decision.invalid_status", "Only a case in review can receive a decision."));
        }

        var nextStatus = decision.Outcome switch
        {
            DecisionOutcome.Approved => DomainResults.Success(VettingCaseStatus.Approved),
            DecisionOutcome.Rejected => DomainResults.Success(VettingCaseStatus.Rejected),
            DecisionOutcome.MoreInformationRequired => DomainResults.Success(VettingCaseStatus.AwaitingInformation),
            DecisionOutcome.Restricted => DomainResults.Success(VettingCaseStatus.Suspended),
            _ => DomainResults.Failure<VettingCaseStatus>(
                new("vetting.decision.outcome.invalid", "The decision outcome is not supported."))
        };

        if (!nextStatus.IsSuccess)
        {
            return nextStatus.TryGetError(out var error)
                ? DomainResult.Failure(error)
                : DomainResult.Failure(new("vetting.decision.outcome.invalid", "The decision outcome is not supported."));
        }

        if (!nextStatus.TryGetValue(out var status))
        {
            return DomainResult.Failure(new("vetting.decision.outcome.invalid", "The decision outcome is not supported."));
        }

        Status = status;
        LastChangedAt = changedAt;
        decisionHistory.Add(decision);
        return DomainResult.Success();
    }

    public DomainResult Suspend(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtcAfterLastChange(changedAt);
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
        var validTime = EnsureUtcAfterLastChange(changedAt);
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

    private DomainResult EnsureUtcAfterLastChange(DateTimeOffset value) =>
        value.Offset != TimeSpan.Zero
            ? DomainResult.Failure(new("vetting.timestamp.not_utc", "Time must be expressed in UTC."))
            : value < LastChangedAt
                ? DomainResult.Failure(new("vetting.timestamp.out_of_order", "A case change cannot precede its previous change."))
                : DomainResult.Success();
}
