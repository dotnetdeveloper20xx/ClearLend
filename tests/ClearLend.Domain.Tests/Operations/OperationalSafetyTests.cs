using ClearLend.Domain.Operations;

namespace ClearLend.Domain.Tests.Operations;

public sealed class OperationalSafetyTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TransferReturnsWorkToQueueAndPreservesHistory()
    {
        var from = WorkQueueId.New();
        var to = WorkQueueId.New();
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Vetting, "case-1", WorkItemPriority.High, At, queueId: from));
        Assert.True(item.AssignTo(StaffMemberId.New(), At.AddMinutes(1)).IsSuccess);

        Assert.True(item.TransferTo(to, "Specialist review", At.AddMinutes(2)).IsSuccess);

        Assert.Equal(to, item.QueueId);
        Assert.Null(item.AssignedTo);
        Assert.Equal(WorkItemStatus.Open, item.Status);
        Assert.Equal(from, Assert.Single(item.TransferHistory).FromQueueId);
    }

    [Fact]
    public void FailedTransferDoesNotChangeWork()
    {
        var queue = WorkQueueId.New();
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Registration, "profile-1", WorkItemPriority.Normal, At, queueId: queue));

        var result = item.TransferTo(WorkQueueId.New(), " ", At.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(queue, item.QueueId);
        Assert.Empty(item.TransferHistory);
    }

    [Fact]
    public void EscalationResolutionRetainsWhoResolvedAndWhy()
    {
        var item = TestResult.Get(WorkItem.Open(WorkItemType.Support, "ticket-1", WorkItemPriority.Normal, At, queueId: WorkQueueId.New()));
        var resolver = StaffMemberId.New();
        Assert.True(item.Escalate("Needs manager review", At.AddMinutes(1)).IsSuccess);

        Assert.True(item.ResolveEscalation(resolver, "Manager confirmed next steps", At.AddMinutes(2)).IsSuccess);

        var escalation = Assert.Single(item.EscalationHistory);
        Assert.False(item.IsEscalated);
        Assert.Equal(resolver, escalation.ResolvedBy);
        Assert.Equal("Manager confirmed next steps", escalation.Resolution);
    }

    [Fact]
    public void AuditDetailsAreCopiedAndReadOnly()
    {
        var source = new Dictionary<string, string> { ["reason"] = "review" };
        var audit = TestResult.Get(AuditEvent.Create("work_item.escalated", "work_item", "item-1", "staff-1", At, source));
        source["reason"] = "changed";

        Assert.Equal("review", audit.Details["reason"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)audit.Details)["reason"] = "changed");
    }

}
