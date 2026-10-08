using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public readonly record struct VettingDecisionId
{
    private VettingDecisionId(Guid value) => Value = value;

    public Guid Value { get; }

    public static VettingDecisionId New() => new(Guid.NewGuid());

    public static DomainResult<VettingDecisionId> From(Guid value) =>
        value == Guid.Empty
            ? DomainResults.Failure<VettingDecisionId>(
                new("vetting.decision_id.empty", "A vetting-decision identifier cannot be empty."))
            : DomainResults.Success<VettingDecisionId>(new(value));

    public override string ToString() => Value.ToString("D");
}
