using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public readonly record struct EvidenceItemId
{
    private EvidenceItemId(Guid value) => Value = value;

    public Guid Value { get; }

    public static EvidenceItemId New() => new(Guid.NewGuid());

    public static DomainResult<EvidenceItemId> From(Guid value) =>
        value == Guid.Empty
            ? DomainResults.Failure<EvidenceItemId>(
                new("evidence.item_id.empty", "An evidence-item identifier cannot be empty."))
            : DomainResults.Success<EvidenceItemId>(new(value));

    public override string ToString() => Value.ToString("D");
}
