using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public sealed record PolicyVersion
{
    private PolicyVersion(string value) => Value = value;

    public string Value { get; }

    public static DomainResult<PolicyVersion> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainResults.Failure<PolicyVersion>(
                new("vetting.policy_version.required", "The policy version is required."));
        }

        var normalised = value.Trim();
        if (normalised.Length > 100)
        {
            return DomainResults.Failure<PolicyVersion>(
                new("vetting.policy_version.too_long", "A policy version cannot exceed 100 characters."));
        }

        return DomainResults.Success<PolicyVersion>(new(normalised));
    }
}
