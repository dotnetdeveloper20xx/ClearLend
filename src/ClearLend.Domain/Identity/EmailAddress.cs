using System.Net.Mail;
using ClearLend.Domain.Common;

namespace ClearLend.Domain.Identity;

public sealed record EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static DomainResult<EmailAddress> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainResults.Failure<EmailAddress>(
                new("identity.email.required", "An email address is required."));
        }

        var normalised = value.Trim().ToLowerInvariant();
        if (normalised.Length > 320)
        {
            return DomainResults.Failure<EmailAddress>(
                new("identity.email.too_long", "An email address cannot exceed 320 characters."));
        }

        try
        {
            var address = new MailAddress(normalised);
            if (!string.Equals(address.Address, normalised, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            return DomainResults.Failure<EmailAddress>(
                new("identity.email.invalid", "The email address is not valid."));
        }

        return DomainResults.Success<EmailAddress>(new(normalised));
    }

    public override string ToString() => Value;
}
