using ClearLend.Domain.Identity;
using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public sealed record VettingDecisionDetails(
    VettingCaseId VettingCaseId,
    UserAccountId SubjectAccountId,
    VettingSubjectType SubjectType,
    DecisionOutcome Outcome,
    DecisionReason Reason,
    UserAccountId ReviewerAccountId,
    PolicyVersion PolicyVersion,
    UtcTimestamp DecidedAt)
{
    public DomainResult Validate()
    {
        if (VettingCaseId.Value == Guid.Empty || SubjectAccountId.Value == Guid.Empty || ReviewerAccountId.Value == Guid.Empty)
            return DomainResult.Failure(new("vetting.decision.identifiers.required", "Case, subject, and reviewer identifiers must be valid."));
        if (!Enum.IsDefined(SubjectType) || !Enum.IsDefined(Outcome))
            return DomainResult.Failure(new("vetting.decision.values.invalid", "Decision subject type and outcome must be supported."));
        if (Reason is null)
            return DomainResult.Failure(new("vetting.decision_reason.required", "Decision reason is required."));
        if (PolicyVersion is null)
            return DomainResult.Failure(new("vetting.policy_version.required", "Policy version is required."));
        if (DecidedAt.Value == default || DecidedAt.Value.Offset != TimeSpan.Zero)
            return DomainResult.Failure(new("vetting.decision.timestamp.not_utc", "Decision time must be expressed in UTC."));
        return DomainResult.Success();
    }
}
