using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Common;

namespace ClearLend.Domain.Tests.Borrowers;

public sealed class ConsentRecordTests
{
    private static readonly DateTimeOffset RecordedAt = new(2026, 10, 9, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ConsentStatus.Granted)]
    [InlineData(ConsentStatus.Declined)]
    public void RecordsConsentDecision(ConsentStatus status)
    {
        var result = ConsentRecord.Record(status, "v1", RecordedAt);

        var consent = TestResult.Get(result);

        Assert.Equal(status, consent.Status);
        Assert.Equal("v1", consent.PolicyVersion);
        Assert.Equal(RecordedAt, consent.RecordedAt);
        Assert.NotEqual(Guid.Empty, consent.Id);
    }

    [Fact]
    public void RequiresPolicyVersion()
    {
        var result = ConsentRecord.Record(ConsentStatus.Granted, " ", RecordedAt);

        Assert.False(result.IsSuccess);
        Assert.Equal("consent.policy_version.required", result.Error?.Code);
    }

    [Fact]
    public void RequiresUtcRecordedAt()
    {
        var result = ConsentRecord.Record(ConsentStatus.Granted, "v1", RecordedAt.ToOffset(TimeSpan.FromHours(1)));

        Assert.False(result.IsSuccess);
        Assert.Equal("consent.recorded_at.not_utc", result.Error?.Code);
    }
}
