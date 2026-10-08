using ClearLend.Domain.Common;

namespace ClearLend.Domain.Identity;

public sealed record IdentityProviderSubject
{
    private IdentityProviderSubject(string value) => Value = value;

    public string Value { get; }

    public static DomainResult<IdentityProviderSubject> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainResults.Failure<IdentityProviderSubject>(
                new("identity.subject.required", "An identity-provider subject is required."));
        }

        var normalised = value.Trim();
        if (normalised.Length > 200)
        {
            return DomainResults.Failure<IdentityProviderSubject>(
                new("identity.subject.too_long", "An identity-provider subject cannot exceed 200 characters."));
        }

        return DomainResults.Success<IdentityProviderSubject>(new(normalised));
    }

    public override string ToString() => Value;
}
