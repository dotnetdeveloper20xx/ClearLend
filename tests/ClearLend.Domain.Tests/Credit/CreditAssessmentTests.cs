using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Credit;

namespace ClearLend.Domain.Tests.Credit;

public sealed class CreditAssessmentTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AssessmentCanStartOnlyOnce()
    {
        var request = TestResult.Get(CreditAssessmentRequest.Request(BorrowerProfileId.New(), At));
        Assert.True(request.Start(At.AddMinutes(1)).IsSuccess);

        var result = request.Start(At.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Equal("credit.start.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void AssessmentCanCompleteWithResult()
    {
        var request = TestResult.Get(CreditAssessmentRequest.Request(BorrowerProfileId.New(), At));
        Assert.True(request.Start(At.AddMinutes(1)).IsSuccess);
        var assessment = TestResult.Get(CreditAssessmentResult.Create(request.Id, 650, 1000, "gbp", At.AddMinutes(2)));

        Assert.True(request.Complete(assessment, At.AddMinutes(3)).IsSuccess);
        Assert.Equal(CreditAssessmentStatus.Completed, request.Status);
    }
}
