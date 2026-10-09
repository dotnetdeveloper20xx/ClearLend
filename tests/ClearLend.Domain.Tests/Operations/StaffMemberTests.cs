using ClearLend.Domain.Identity;
using ClearLend.Domain.Operations;

namespace ClearLend.Domain.Tests.Operations;

public sealed class StaffMemberTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InviteStartsInInvitedState()
    {
        var staff = TestResult.Get(StaffMember.Invite(UserAccountId.New(), [StaffRole.OperationsStaff], At));

        Assert.Equal(StaffStatus.Invited, staff.Status);
        Assert.Equal([StaffRole.OperationsStaff], staff.Roles);
    }

    [Fact]
    public void StaffMustHaveAtLeastOneUniqueRole()
    {
        var result = StaffMember.Invite(UserAccountId.New(), [StaffRole.SupportAgent, StaffRole.SupportAgent], At);

        Assert.False(result.IsSuccess);
        Assert.Equal("staff.roles.invalid", result.Error?.Code);
    }

    [Fact]
    public void StaffLifecycleRequiresExpectedState()
    {
        var staff = TestResult.Get(StaffMember.Invite(UserAccountId.New(), [StaffRole.ComplianceReviewer], At));
        Assert.True(staff.Activate(At.AddMinutes(1)).IsSuccess);
        Assert.True(staff.Suspend(At.AddMinutes(2)).IsSuccess);

        var result = staff.Activate(At.AddMinutes(3));

        Assert.False(result.IsSuccess);
        Assert.Equal("staff.activate.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void ClosedStaffCannotBeReactivated()
    {
        var staff = TestResult.Get(StaffMember.Invite(UserAccountId.New(), [StaffRole.ApplicationOwner], At));
        Assert.True(staff.Close(At.AddMinutes(1)).IsSuccess);

        var result = staff.Activate(At.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Equal("staff.activate.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void RolesCanBeAddedAndRemovedButOneMustRemain()
    {
        var staff = TestResult.Get(StaffMember.Invite(UserAccountId.New(), [StaffRole.OperationsStaff], At));

        Assert.True(staff.AddRole(StaffRole.ComplianceReviewer).IsSuccess);
        Assert.True(staff.RemoveRole(StaffRole.OperationsStaff).IsSuccess);
        var result = staff.RemoveRole(StaffRole.ComplianceReviewer);

        Assert.False(result.IsSuccess);
        Assert.Equal("staff.role.required", result.Error?.Code);
        Assert.Single(staff.Roles);
    }
}
