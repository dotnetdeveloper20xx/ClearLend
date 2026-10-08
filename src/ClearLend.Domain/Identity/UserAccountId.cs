namespace ClearLend.Domain.Identity;

public readonly record struct UserAccountId(Guid Value)
{
    public static UserAccountId New() => new(Guid.NewGuid());

    public static UserAccountId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("An account identifier cannot be empty.", nameof(value))
            : new(value);

    public override string ToString() => Value.ToString("D");
}
