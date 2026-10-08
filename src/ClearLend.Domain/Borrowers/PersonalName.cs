using ClearLend.Domain.Common;

namespace ClearLend.Domain.Borrowers;

public sealed record PersonalName
{
    private PersonalName(string givenName, string familyName)
    {
        GivenName = givenName;
        FamilyName = familyName;
    }

    public string GivenName { get; }

    public string FamilyName { get; }

    public static DomainResult<PersonalName> Create(string? givenName, string? familyName)
    {
        if (string.IsNullOrWhiteSpace(givenName) || string.IsNullOrWhiteSpace(familyName))
        {
            return DomainResults.Failure<PersonalName>(
                new("borrower.name.required", "Given name and family name are required."));
        }

        var given = givenName.Trim();
        var family = familyName.Trim();
        if (given.Length > 100 || family.Length > 100)
        {
            return DomainResults.Failure<PersonalName>(
                new("borrower.name.too_long", "A personal name cannot exceed 100 characters per part."));
        }

        return DomainResults.Success<PersonalName>(new(given, family));
    }
}
