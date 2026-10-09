using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Operations;

public enum StaffStatus { Invited = 1, Active = 2, Suspended = 3, Closed = 4 }

public enum StaffRole
{
    ApplicationOwner = 1,
    OperationsStaff = 2,
    ComplianceReviewer = 3,
    CreditReviewer = 4,
    FinanceOperator = 5,
    ServicingOperator = 6,
    SupportAgent = 7
}

public readonly record struct StaffMemberId(Guid Value)
{
    public static StaffMemberId New() => new(Guid.NewGuid());
}

public sealed class StaffMember
{
    private readonly List<StaffRole> roles;

    private StaffMember(StaffMemberId id, UserAccountId userAccountId, DateTimeOffset createdAt, IEnumerable<StaffRole> roles)
    {
        Id = id;
        UserAccountId = userAccountId;
        CreatedAt = createdAt;
        StatusChangedAt = createdAt;
        Status = StaffStatus.Invited;
        this.roles = roles.Distinct().ToList();
    }

    public StaffMemberId Id { get; }
    public UserAccountId UserAccountId { get; }
    public StaffStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset StatusChangedAt { get; private set; }
    public IReadOnlyList<StaffRole> Roles => roles.AsReadOnly();

    public DomainResult AddRole(StaffRole role)
    {
        if (!Enum.IsDefined(role)) return DomainResult.Failure(new("staff.role.invalid", "An unsupported staff role was supplied."));
        if (roles.Contains(role)) return DomainResult.Failure(new("staff.role.already_added", "The staff member already has this role."));
        if (Status == StaffStatus.Closed) return DomainResult.Failure(new("staff.role.closed", "Roles cannot be changed for a closed staff member."));
        roles.Add(role);
        return DomainResult.Success();
    }

    public DomainResult RemoveRole(StaffRole role)
    {
        if (Status == StaffStatus.Closed) return DomainResult.Failure(new("staff.role.closed", "Roles cannot be changed for a closed staff member."));
        if (!Enum.IsDefined(role)) return DomainResult.Failure(new("staff.role.invalid", "An unsupported staff role was supplied."));
        if (!roles.Remove(role)) return DomainResult.Failure(new("staff.role.not_found", "The staff member does not have this role."));
        if (roles.Count == 0) { roles.Add(role); return DomainResult.Failure(new("staff.role.required", "A staff member must retain at least one role.")); }
        return DomainResult.Success();
    }

    public static DomainResult<StaffMember> Invite(UserAccountId userAccountId, IEnumerable<StaffRole>? roles, DateTimeOffset createdAt, StaffMemberId? id = null)
    {
        if (userAccountId.Value == Guid.Empty) return DomainResults.Failure<StaffMember>(new("staff.user_account.required", "A user account is required for a staff member."));
        if (roles is null) return DomainResults.Failure<StaffMember>(new("staff.roles.required", "At least one staff role is required."));
        var selectedRoles = roles.ToArray();
        if (selectedRoles.Any(role => !Enum.IsDefined(role))) return DomainResults.Failure<StaffMember>(new("staff.roles.invalid", "An unsupported staff role was supplied."));
        if (selectedRoles.Length == 0 || selectedRoles.Distinct().Count() != selectedRoles.Length) return DomainResults.Failure<StaffMember>(new("staff.roles.invalid", "At least one unique staff role is required."));
        if (createdAt.Offset != TimeSpan.Zero) return DomainResults.Failure<StaffMember>(new("staff.created_at.not_utc", "Creation time must be expressed in UTC."));
        return DomainResults.Success(new StaffMember(id ?? StaffMemberId.New(), userAccountId, createdAt, selectedRoles));
    }

    public DomainResult Activate(DateTimeOffset changedAt) => Transition(StaffStatus.Active, StaffStatus.Invited, changedAt, "staff.activate.invalid_status", "Only an invited staff member can be activated.");
    public DomainResult Suspend(DateTimeOffset changedAt) => Transition(StaffStatus.Suspended, StaffStatus.Active, changedAt, "staff.suspend.invalid_status", "Only an active staff member can be suspended.");
    public DomainResult Reinstate(DateTimeOffset changedAt) => Transition(StaffStatus.Active, StaffStatus.Suspended, changedAt, "staff.reinstate.invalid_status", "Only a suspended staff member can be reinstated.");
    public DomainResult Close(DateTimeOffset changedAt)
    {
        if (Status == StaffStatus.Closed) return DomainResult.Failure(new("staff.close.already_closed", "The staff member is already closed."));
        return Transition(StaffStatus.Closed, Status, changedAt, "staff.close.invalid_status", "The staff member cannot be closed from the current state.");
    }

    private DomainResult Transition(StaffStatus next, StaffStatus expected, DateTimeOffset changedAt, string code, string message)
    {
        if (Status != expected) return DomainResult.Failure(new(code, message));
        if (changedAt.Offset != TimeSpan.Zero) return DomainResult.Failure(new("staff.timestamp.not_utc", "Time must be expressed in UTC."));
        if (changedAt < StatusChangedAt) return DomainResult.Failure(new("staff.timestamp.out_of_order", "A status change cannot precede the previous change."));
        Status = next;
        StatusChangedAt = changedAt;
        return DomainResult.Success();
    }
}
