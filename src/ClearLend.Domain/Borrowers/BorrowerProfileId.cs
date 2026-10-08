using ClearLend.Domain.Common;

namespace ClearLend.Domain.Borrowers;

public readonly record struct BorrowerProfileId
{
    private BorrowerProfileId(Guid value) => Value = value;

    public Guid Value { get; }

    public static BorrowerProfileId New() => new(Guid.NewGuid());

    public static DomainResult<BorrowerProfileId> From(Guid value) =>
        value == Guid.Empty
            ? DomainResults.Failure<BorrowerProfileId>(
                new("borrower.profile_id.empty", "A borrower profile identifier cannot be empty."))
            : DomainResults.Success<BorrowerProfileId>(new(value));

    public override string ToString() => Value.ToString("D");
}
