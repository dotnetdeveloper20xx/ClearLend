using ClearLend.Domain.Finance;

namespace ClearLend.Domain.Tests.Finance;

public sealed class PaymentTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PaymentRequiresFullAllocationBeforeSettlement()
    {
        var payment = TestResult.Get(Payment.Create("borrower-1", 100, "GBP", At));
        Assert.True(payment.AddAllocation(new PaymentAllocation("platform", 50, 50)).IsSuccess);

        var result = payment.EnsureFullyAllocated();

        Assert.False(result.IsSuccess);
        Assert.Equal("payment.allocation.incomplete", result.Error?.Code);
    }

    [Fact]
    public void PaymentCanBeAllocatedAndSucceeded()
    {
        var payment = TestResult.Get(Payment.Create("borrower-1", 100, "GBP", At));
        Assert.True(payment.AddAllocation(new PaymentAllocation("platform", 100, 100)).IsSuccess);
        Assert.True(payment.EnsureFullyAllocated().IsSuccess);

        Assert.True(payment.MarkSucceeded("txn-1").IsSuccess);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }
}
