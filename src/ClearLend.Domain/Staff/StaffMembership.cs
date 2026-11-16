using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Staff;

public sealed class StaffMembership
{
    private StaffMembership() { }

    private StaffMembership(StaffMembershipId id, UserAccountId userAccountId, StaffRole role, DateTimeOffset createdAt)
    {
        Id = id;
        UserAccountId = userAccountId;
        Role = role;
        CreatedAt = createdAt;
        State = StaffMembershipState.Active;
    }

    public StaffMembershipId Id { get; private set; }
    public UserAccountId UserAccountId { get; private set; }
    public StaffRole Role { get; private set; }
    public StaffMembershipState State { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DisabledAt { get; private set; }

    public static DomainResult<StaffMembership> Create(UserAccountId userAccountId, StaffRole role, DateTimeOffset createdAt, StaffMembershipId? id = null)
    {
        if (userAccountId.Value == Guid.Empty)
            return DomainResults.Failure<StaffMembership>(new("staff.account.required", "A staff membership requires a user account."));
        if (!Enum.IsDefined(role))
            return DomainResults.Failure<StaffMembership>(new("staff.role.invalid", "A valid staff role is required."));
        if (createdAt.Offset != TimeSpan.Zero)
            return DomainResults.Failure<StaffMembership>(new("staff.created_at.not_utc", "Creation time must be UTC."));

        return DomainResults.Success(new StaffMembership(id ?? StaffMembershipId.New(), userAccountId, role, createdAt));
    }

    public DomainResult Disable(DateTimeOffset disabledAt)
    {
        if (State == StaffMembershipState.Disabled)
            return DomainResult.Failure(new("staff.membership.already_disabled", "The staff membership is already disabled."));
        if (disabledAt.Offset != TimeSpan.Zero)
            return DomainResult.Failure(new("staff.disabled_at.not_utc", "The disabled time must be UTC."));

        State = StaffMembershipState.Disabled;
        DisabledAt = disabledAt;
        return DomainResult.Success();
    }

    public DomainResult Restore()
    {
        if (State == StaffMembershipState.Active)
            return DomainResult.Failure(new("staff.membership.already_active", "The staff membership is already active."));
        if (State == StaffMembershipState.Removed)
            return DomainResult.Failure(new("staff.membership.removed", "A removed staff membership cannot be restored."));

        State = StaffMembershipState.Active;
        DisabledAt = null;
        return DomainResult.Success();
    }

    public DomainResult Remove(DateTimeOffset removedAt)
    {
        if (State == StaffMembershipState.Removed)
            return DomainResult.Failure(new("staff.membership.already_removed", "The staff membership is already removed."));
        if (removedAt.Offset != TimeSpan.Zero)
            return DomainResult.Failure(new("staff.removed_at.not_utc", "The removed time must be UTC."));
        State = StaffMembershipState.Removed;
        DisabledAt = removedAt;
        return DomainResult.Success();
    }
}

public enum StaffMembershipState
{
    Active = 1,
    Disabled = 2,
    Removed = 3
}
