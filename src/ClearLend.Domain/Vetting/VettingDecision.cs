using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed class VettingDecision
{
    private VettingDecision(
        VettingDecisionId id,
        VettingCaseId vettingCaseId,
        UserAccountId subjectAccountId,
        VettingSubjectType subjectType,
        DecisionOutcome outcome,
        DecisionReason reason,
        UserAccountId reviewerAccountId,
        PolicyVersion policyVersion,
        DateTimeOffset decidedAt)
    {
        Id = id;
        VettingCaseId = vettingCaseId;
        SubjectAccountId = subjectAccountId;
        SubjectType = subjectType;
        Outcome = outcome;
        Reason = reason;
        ReviewerAccountId = reviewerAccountId;
        PolicyVersion = policyVersion;
        DecidedAt = decidedAt;
    }

    public VettingDecisionId Id { get; }

    public VettingCaseId VettingCaseId { get; }

    public UserAccountId SubjectAccountId { get; }

    public VettingSubjectType SubjectType { get; }

    public DecisionOutcome Outcome { get; }

    public DecisionReason Reason { get; }

    public UserAccountId ReviewerAccountId { get; }

    public PolicyVersion PolicyVersion { get; }

    public DateTimeOffset DecidedAt { get; }

    public static DomainResult<VettingDecision> Record(
        VettingCaseId vettingCaseId,
        UserAccountId subjectAccountId,
        VettingSubjectType subjectType,
        DecisionOutcome outcome,
        DecisionReason? reason,
        UserAccountId reviewerAccountId,
        PolicyVersion? policyVersion,
        DateTimeOffset decidedAt,
        VettingDecisionId? id = null)
    {
        if (decidedAt.Offset != TimeSpan.Zero)
        {
            return DomainResults.Failure<VettingDecision>(
                new("vetting.decision_time.not_utc", "Decision time must be expressed in UTC."));
        }

        if (reason is null)
        {
            return DomainResults.Failure<VettingDecision>(
                new("vetting.decision_reason.required", "A decision reason is required."));
        }

        if (policyVersion is null)
        {
            return DomainResults.Failure<VettingDecision>(
                new("vetting.policy_version.required", "The policy version is required."));
        }

        return DomainResults.Success<VettingDecision>(
            new(id ?? VettingDecisionId.New(), vettingCaseId, subjectAccountId, subjectType, outcome, reason, reviewerAccountId, policyVersion, decidedAt));
    }
}
