using ClearLend.Domain.Operations;

namespace ClearLend.Domain.Tests.Operations;

public sealed class WorkQueueTests
{
    [Fact]
    public void QueueCanAddAndRemoveStaffMembers()
    {
        var queue = TestResult.Get(WorkQueueDefinition.Create(WorkQueueType.Operations, "Operations", new DateTimeOffset(2026, 10, 9, 17, 0, 0, TimeSpan.Zero)));
        var staffMemberId = StaffMemberId.New();

        Assert.True(queue.AddMember(staffMemberId).IsSuccess);
        Assert.Contains(staffMemberId, queue.Members);
        Assert.True(queue.RemoveMember(staffMemberId).IsSuccess);
        Assert.Empty(queue.Members);
    }

    [Fact]
    public void QueueRejectsDuplicateMembership()
    {
        var queue = TestResult.Get(WorkQueueDefinition.Create(WorkQueueType.Compliance, "Compliance", new DateTimeOffset(2026, 10, 9, 17, 0, 0, TimeSpan.Zero)));
        var staffMemberId = StaffMemberId.New();
        Assert.True(queue.AddMember(staffMemberId).IsSuccess);

        var result = queue.AddMember(staffMemberId);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_queue.member.already_added", result.Error?.Code);
    }
}
