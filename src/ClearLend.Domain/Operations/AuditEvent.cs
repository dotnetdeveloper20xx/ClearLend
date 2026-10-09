namespace ClearLend.Domain.Operations;

public sealed record AuditEvent(
    Guid Id,
    string Action,
    string SubjectType,
    string SubjectReference,
    string ActorReference,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string>? Details = null);
