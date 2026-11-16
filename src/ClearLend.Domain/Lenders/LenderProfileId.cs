namespace ClearLend.Domain.Lenders;

public readonly record struct LenderProfileId(Guid Value)
{
    public static LenderProfileId New() => new(Guid.NewGuid());
    public static LenderProfileId From(Guid value) => new(value);
}
