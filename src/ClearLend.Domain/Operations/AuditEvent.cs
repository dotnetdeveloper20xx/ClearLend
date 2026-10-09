using System.Collections.ObjectModel;
using ClearLend.Domain.Common;

namespace ClearLend.Domain.Operations;

public sealed class AuditEvent
{
    private AuditEvent(Guid id, string action, string subjectType, string subjectReference, string actorReference,
        DateTimeOffset occurredAt, string? correlationId, IReadOnlyDictionary<string, string> details)
    {
        Id = id; Action = action; SubjectType = subjectType; SubjectReference = subjectReference;
        ActorReference = actorReference; OccurredAt = occurredAt; CorrelationId = correlationId;
        Details = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(details, StringComparer.Ordinal));
    }

    public Guid Id { get; }
    public string Action { get; }
    public string SubjectType { get; }
    public string SubjectReference { get; }
    public string ActorReference { get; }
    public DateTimeOffset OccurredAt { get; }
    public string? CorrelationId { get; }
    public IReadOnlyDictionary<string, string> Details { get; }

    public static DomainResult<AuditEvent> Create(string? action, string? subjectType, string? subjectReference,
        string? actorReference, DateTimeOffset occurredAt, IReadOnlyDictionary<string, string>? details = null,
        string? correlationId = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > 100 ||
            string.IsNullOrWhiteSpace(subjectType) || subjectType.Length > 100 ||
            string.IsNullOrWhiteSpace(subjectReference) || subjectReference.Length > 200 ||
            string.IsNullOrWhiteSpace(actorReference) || actorReference.Length > 200)
            return DomainResults.Failure<AuditEvent>(new("audit.values.invalid", "Action, subject, and actor references are required and must fit their limits."));
        if (occurredAt.Offset != TimeSpan.Zero) return DomainResults.Failure<AuditEvent>(new("audit.timestamp.not_utc", "Audit time must be expressed in UTC."));
        if (id == Guid.Empty) return DomainResults.Failure<AuditEvent>(new("audit.id.empty", "An audit identifier cannot be empty."));
        if (string.IsNullOrWhiteSpace(correlationId) is false && correlationId.Length > 200)
            return DomainResults.Failure<AuditEvent>(new("audit.correlation_id.invalid", "Correlation identifier cannot exceed 200 characters."));
        var safeDetails = details is null ? new Dictionary<string, string>() : new Dictionary<string, string>(details, StringComparer.Ordinal);
        if (safeDetails.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 100 || x.Value is null || x.Value.Length > 1000))
            return DomainResults.Failure<AuditEvent>(new("audit.details.invalid", "Audit detail keys and values must fit their limits."));
        return DomainResults.Success(new AuditEvent(id ?? Guid.NewGuid(), action.Trim(), subjectType.Trim(),
            subjectReference.Trim(), actorReference.Trim(), occurredAt, correlationId?.Trim(), safeDetails));
    }
}
