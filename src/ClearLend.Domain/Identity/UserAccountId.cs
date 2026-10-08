using ClearLend.Domain.Common;

namespace ClearLend.Domain.Identity;

public readonly record struct UserAccountId
{
    private UserAccountId(Guid value) => Value = value;

    public Guid Value { get; }

    public static UserAccountId New() => new(Guid.NewGuid());

    public static DomainResult<UserAccountId> From(Guid value) =>
        value == Guid.Empty
            ? DomainResults.Failure<UserAccountId>(
                new("identity.account_id.empty", "An account identifier cannot be empty."))
            : DomainResults.Success<UserAccountId>(new(value));

    public override string ToString() => Value.ToString("D");
}
