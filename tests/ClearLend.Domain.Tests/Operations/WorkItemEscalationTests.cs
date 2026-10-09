using ClearLend.Domain.Operations;

namespace ClearLend.Domain.Tests.Operations;

public sealed class WorkItemEscalationTests
{
    [Fact]
    public void EscalationRequiresReasonAndRaisesPriority()
    {
        var at = new DateTimeOffset(2026, 10, 9, 16, 0, 0, TimeSpan.Zero);
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Registration, "borrower-1", WorkItemPriority.Normal, at));

        Assert.True(item.Escalate("High-risk registration", at.AddMinutes(1)).IsSuccess);
        Assert.True(item.IsEscalated);
        Assert.Equal(WorkItemPriority.Urgent, item.Priority);
        Assert.Equal("High-risk registration", item.EscalationReason);
        Assert.Single(item.EscalationHistory);
        Assert.Equal(at.AddMinutes(1), item.StatusChangedAt);
    }

    [Fact]
    public void ReEscalationRetainsHistory()
    {
        var at = new DateTimeOffset(2026, 10, 9, 16, 0, 0, TimeSpan.Zero);
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Registration, "borrower-1", WorkItemPriority.Normal, at));

        Assert.True(item.Escalate("First review", at.AddMinutes(1)).IsSuccess);
        Assert.True(item.Escalate("Additional concern", at.AddMinutes(2)).IsSuccess);

        Assert.Equal(2, item.EscalationHistory.Count);
        Assert.Equal("Additional concern", item.EscalationReason);
    }

    [Fact]
    public void CompletedWorkCannotBeEscalated()
    {
        var at = new DateTimeOffset(2026, 10, 9, 16, 0, 0, TimeSpan.Zero);
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Registration, "borrower-1", WorkItemPriority.Normal, at));
        Assert.True(item.AssignTo(StaffMemberId.New(), at.AddMinutes(1)).IsSuccess);
        Assert.True(item.Complete(at.AddMinutes(2)).IsSuccess);

        var result = item.Escalate("Too late", at.AddMinutes(3));

        Assert.False(result.IsSuccess);
        Assert.Equal("work_item.escalate.invalid_status", result.Error?.Code);
    }
}
