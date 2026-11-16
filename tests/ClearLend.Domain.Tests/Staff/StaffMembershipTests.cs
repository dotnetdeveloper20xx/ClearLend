using ClearLend.Domain.Identity;
using ClearLend.Domain.Staff;

namespace ClearLend.Domain.Tests.Staff;

public sealed class StaffMembershipTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateStartsAnActiveMembership()
    {
        var membership = TestResult.Get(StaffMembership.Create(UserAccountId.New(), StaffRole.OperationsStaff, CreatedAt));

        Assert.Equal(StaffRole.OperationsStaff, membership.Role);
        Assert.Equal(StaffMembershipState.Active, membership.State);
        Assert.Equal(CreatedAt, membership.CreatedAt);
    }

    [Fact]
    public void CreateRejectsNonUtcTime()
    {
        var result = StaffMembership.Create(UserAccountId.New(), StaffRole.CreditManager, CreatedAt.ToOffset(TimeSpan.FromHours(1)));

        Assert.False(result.IsSuccess);
        Assert.Equal("staff.created_at.not_utc", result.Error?.Code);
    }

    [Fact]
    public void DisableChangesStateAndRecordsTime()
    {
        var membership = TestResult.Get(StaffMembership.Create(UserAccountId.New(), StaffRole.ApplicationOwner, CreatedAt));
        var disabledAt = CreatedAt.AddMinutes(10);

        Assert.True(membership.Disable(disabledAt).IsSuccess);
        Assert.Equal(StaffMembershipState.Disabled, membership.State);
        Assert.Equal(disabledAt, membership.DisabledAt);
    }

    [Fact]
    public void DisabledMembershipCannotBeDisabledAgain()
    {
        var membership = TestResult.Get(StaffMembership.Create(UserAccountId.New(), StaffRole.ApplicationOwner, CreatedAt));
        membership.Disable(CreatedAt.AddMinutes(1));

        var result = membership.Disable(CreatedAt.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Equal("staff.membership.already_disabled", result.Error?.Code);
    }

    [Fact]
    public void DisabledMembershipCanBeRestored()
    {
        var membership = TestResult.Get(StaffMembership.Create(UserAccountId.New(), StaffRole.OperationsStaff, CreatedAt));
        membership.Disable(CreatedAt.AddMinutes(1));

        var result = membership.Restore();

        Assert.True(result.IsSuccess);
        Assert.Equal(StaffMembershipState.Active, membership.State);
        Assert.Null(membership.DisabledAt);
    }
}
