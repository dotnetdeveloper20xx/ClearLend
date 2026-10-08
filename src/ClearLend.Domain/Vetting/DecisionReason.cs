using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public sealed record DecisionReason
{
    private DecisionReason(string value) => Value = value;

    public string Value { get; }

    public static DomainResult<DecisionReason> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainResults.Failure<DecisionReason>(
                new("vetting.decision_reason.required", "A decision reason is required."));
        }

        var normalised = value.Trim();
        if (normalised.Length > 2000)
        {
            return DomainResults.Failure<DecisionReason>(
                new("vetting.decision_reason.too_long", "A decision reason cannot exceed 2000 characters."));
        }

        return DomainResults.Success<DecisionReason>(new(normalised));
    }
}
