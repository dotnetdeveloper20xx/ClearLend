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

        var valid = details.Validate();
        if (!valid.IsSuccess) return DomainResults.Failure<VettingDecision>(valid.Error!);
        if (id is { } suppliedId && suppliedId.Value == Guid.Empty)
            return DomainResults.Failure<VettingDecision>(new("vetting.decision_id.empty", "A decision identifier cannot be empty."));

        return DomainResults.Success<VettingDecision>(
            new(id ?? VettingDecisionId.New(), details));
    }
}
