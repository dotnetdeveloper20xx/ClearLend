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
    UtcTimestamp DecidedAt);
