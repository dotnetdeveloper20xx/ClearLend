using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Vetting;

namespace ClearLend.Domain.Tests.Vetting;

public sealed class VettingDecisionTests
{
    private static readonly DateTimeOffset DecidedAt = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RecordCreatesImmutableDecision()
    {
        var decision = CreateDecision();

        Assert.Equal(DecisionOutcome.Approved, decision.Outcome);
        Assert.Equal("Initial borrower review passed.", decision.Reason.Value);
        Assert.Equal("2026.1", decision.PolicyVersion.Value);
    }

    [Fact]
    public void DecisionRequiresReason()
    {
        var result = VettingDecision.Record(new VettingDecisionDetails(
            VettingCaseId.New(), UserAccountId.New(), VettingSubjectType.Borrower,
            DecisionOutcome.Rejected, null!, UserAccountId.New(),
            TestResult.Get(PolicyVersion.Create("2026.1")), TestResult.Get(UtcTimestamp.Create(DecidedAt))));

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.decision_reason.required", result.Error?.Code);
    }

    [Fact]
    public void DecisionRequiresPolicyVersion()
    {
        var result = VettingDecision.Record(new VettingDecisionDetails(
            VettingCaseId.New(), UserAccountId.New(), VettingSubjectType.Borrower,
            DecisionOutcome.Approved, TestResult.Get(DecisionReason.Create("Review passed.")),
            UserAccountId.New(), null!, TestResult.Get(UtcTimestamp.Create(DecidedAt))));

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.policy_version.required", result.Error?.Code);
    }

    [Fact]
    public void DecisionRequiresUtcTime()
    {
        var nonUtc = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.FromHours(1));
        var result = UtcTimestamp.Create(nonUtc);

        Assert.False(result.IsSuccess);
        Assert.Equal("timestamp.not_utc", result.Error?.Code);
    }

    private static VettingDecision CreateDecision() => TestResult.Get(VettingDecision.Record(new VettingDecisionDetails(
        VettingCaseId.New(), UserAccountId.New(), VettingSubjectType.Borrower,
        DecisionOutcome.Approved, TestResult.Get(DecisionReason.Create("Initial borrower review passed.")),
        UserAccountId.New(), TestResult.Get(PolicyVersion.Create("2026.1")), TestResult.Get(UtcTimestamp.Create(DecidedAt)))));
}
