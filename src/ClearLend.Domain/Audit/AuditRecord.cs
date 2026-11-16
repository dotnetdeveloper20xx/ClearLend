namespace ClearLend.Domain.Audit;

public sealed class AuditRecord
{
    private AuditRecord() { }
    private AuditRecord(Guid id, string actorSubject, string action, string targetId, DateTimeOffset occurredAt)
    { Id = id; ActorSubject = actorSubject; Action = action; TargetId = targetId; OccurredAt = occurredAt; }
    public Guid Id { get; private set; }
    public string ActorSubject { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string TargetId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public static AuditRecord Create(string actorSubject, string action, string targetId, DateTimeOffset occurredAt) => new(Guid.NewGuid(), actorSubject, action, targetId, occurredAt);
}
