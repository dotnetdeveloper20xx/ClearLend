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
        var result = VettingDecision.Record(
            VettingCaseId.New(),
            UserAccountId.New(),
            VettingSubjectType.Borrower,
            DecisionOutcome.Rejected,
            null,
            UserAccountId.New(),
            PolicyVersion.Create("2026.1").Value!,
            DecidedAt);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.decision_reason.required", result.Error?.Code);
    }

    [Fact]
    public void DecisionRequiresPolicyVersion()
    {
        var result = VettingDecision.Record(
            VettingCaseId.New(),
            UserAccountId.New(),
            VettingSubjectType.Borrower,
            DecisionOutcome.Approved,
            DecisionReason.Create("Review passed.").Value!,
            UserAccountId.New(),
            null,
            DecidedAt);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.policy_version.required", result.Error?.Code);
    }

    [Fact]
    public void DecisionRequiresUtcTime()
    {
        var nonUtc = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.FromHours(1));
        var result = VettingDecision.Record(
            VettingCaseId.New(),
            UserAccountId.New(),
            VettingSubjectType.Borrower,
            DecisionOutcome.MoreInformationRequired,
            DecisionReason.Create("More evidence is required.").Value!,
            UserAccountId.New(),
            PolicyVersion.Create("2026.1").Value!,
            nonUtc);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.decision_time.not_utc", result.Error?.Code);
    }

    private static VettingDecision CreateDecision() => VettingDecision.Record(
        VettingCaseId.New(),
        UserAccountId.New(),
        VettingSubjectType.Borrower,
        DecisionOutcome.Approved,
        DecisionReason.Create("Initial borrower review passed.").Value!,
        UserAccountId.New(),
        PolicyVersion.Create("2026.1").Value!,
        DecidedAt).Value!;
}
