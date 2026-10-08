using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed class VettingDecision
{
    private VettingDecision(
        VettingDecisionId id,
        VettingDecisionDetails details)
    {
        Id = id;
        VettingCaseId = details.VettingCaseId;
        SubjectAccountId = details.SubjectAccountId;
        SubjectType = details.SubjectType;
        Outcome = details.Outcome;
        Reason = details.Reason;
        ReviewerAccountId = details.ReviewerAccountId;
        PolicyVersion = details.PolicyVersion;
        DecidedAt = details.DecidedAt.Value;
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
        VettingDecisionDetails? details,
        VettingDecisionId? id = null)
    {
        if (details is null)
        {
            return DomainResults.Failure<VettingDecision>(
                new("vetting.decision.details.required", "Decision details are required."));
        }

        if (details.Reason is null)
        {
            return DomainResults.Failure<VettingDecision>(
                new("vetting.decision_reason.required", "A decision reason is required."));
        }

        if (details.PolicyVersion is null)
        {
            return DomainResults.Failure<VettingDecision>(
                new("vetting.policy_version.required", "The policy version is required."));
        }

        return DomainResults.Success<VettingDecision>(
            new(id ?? VettingDecisionId.New(), details));
    }
}
