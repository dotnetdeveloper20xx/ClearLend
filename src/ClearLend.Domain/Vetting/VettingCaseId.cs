using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public readonly record struct VettingCaseId
{
    private VettingCaseId(Guid value) => Value = value;

    public Guid Value { get; }

    public static VettingCaseId New() => new(Guid.NewGuid());

    public static DomainResult<VettingCaseId> From(Guid value) =>
        value == Guid.Empty
            ? DomainResults.Failure<VettingCaseId>(
                new("vetting.case_id.empty", "A vetting-case identifier cannot be empty."))
            : DomainResults.Success<VettingCaseId>(new(value));

    public override string ToString() => Value.ToString("D");
}
