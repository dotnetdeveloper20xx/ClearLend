using ClearLend.Domain.Operations;

namespace ClearLend.Domain.Tests.Operations;

public sealed class WorkItemTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OpenStartsAsOpen()
    {
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Registration, "borrower-1", WorkItemPriority.Normal, At));

        Assert.Equal(WorkItemStatus.Open, item.Status);
        Assert.Null(item.AssignedTo);
    }

    [Fact]
    public void AssignmentMovesWorkToInProgress()
    {
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Registration, "borrower-1", WorkItemPriority.Normal, At));

        Assert.True(item.AssignTo(StaffMemberId.New(), At.AddMinutes(1)).IsSuccess);
        Assert.Equal(WorkItemStatus.InProgress, item.Status);
        Assert.NotNull(item.AssignedTo);
        Assert.Single(item.AssignmentHistory);
    }

    [Fact]
    public void OnlyInProgressWorkCanBeCompleted()
    {
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Registration, "borrower-1", WorkItemPriority.Normal, At));

        var result = item.Complete(At.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("work_item.complete.invalid_status", result.Error?.Code);
    }
}
