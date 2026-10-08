using ClearLend.Domain.Common;

namespace ClearLend.Domain.Vetting;

public sealed record StorageReference
{
    private StorageReference(string value) => Value = value;

    public string Value { get; }

    public static DomainResult<StorageReference> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainResults.Failure<StorageReference>(
                new("evidence.storage_reference.required", "A storage reference is required."));
        }

        var normalised = value.Trim();
        if (normalised.Length > 500)
        {
            return DomainResults.Failure<StorageReference>(
                new("evidence.storage_reference.too_long", "A storage reference cannot exceed 500 characters."));
        }

        return DomainResults.Success<StorageReference>(new(normalised));
    }
}
