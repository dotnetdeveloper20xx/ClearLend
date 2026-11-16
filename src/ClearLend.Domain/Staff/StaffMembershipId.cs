using ClearLend.Domain.Common;

namespace ClearLend.Domain.Staff;

public readonly record struct StaffMembershipId
{
    private StaffMembershipId(Guid value) => Value = value;

    public Guid Value { get; }

    public static StaffMembershipId New() => new(Guid.NewGuid());

    public static DomainResult<StaffMembershipId> From(Guid value) => value == Guid.Empty
        ? DomainResults.Failure<StaffMembershipId>(new("staff.membership_id.empty", "A staff membership identifier cannot be empty."))
        : DomainResults.Success<StaffMembershipId>(new(value));
}
