namespace ClearLend.Domain.Common;

public readonly record struct UtcTimestamp(DateTimeOffset Value)
{
    public static DomainResult<UtcTimestamp> Create(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero
            ? DomainResults.Success(new UtcTimestamp(value))
            : DomainResults.Failure<UtcTimestamp>(
                new("timestamp.not_utc", "Timestamp must be expressed in UTC."));

    public override string ToString() => Value.ToString("O");
}
