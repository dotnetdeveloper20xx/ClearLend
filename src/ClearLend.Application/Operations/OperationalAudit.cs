using ClearLend.Application.Abstractions;
using ClearLend.Domain.Operations;

namespace ClearLend.Application.Operations;

internal static class OperationalAudit
{
    public static async Task AppendAndSaveAsync(IAuditEventWriter audit, IUnitOfWork unitOfWork,
        string action, string subjectType, string subjectReference, StaffMemberId actorId,
        DateTimeOffset occurredAt, CancellationToken cancellationToken, string? reason = null)
        => await AppendAndSaveAsync(audit, unitOfWork, action, subjectType, subjectReference, actorId.Value.ToString("D"), occurredAt, cancellationToken, reason);

    public static async Task AppendAndSaveAsync(IAuditEventWriter audit, IUnitOfWork unitOfWork,
        string action, string subjectType, string subjectReference, string actorReference,
        DateTimeOffset occurredAt, CancellationToken cancellationToken, string? reason = null)
    {
        var details = reason is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["reason"] = reason.Trim() };
        var result = AuditEvent.Create(action, subjectType, subjectReference, actorReference, occurredAt, details);
        if (!result.TryGetValue(out var auditEvent)) throw new InvalidOperationException(result.Error?.Message ?? "Audit event could not be created.");
        await audit.AppendAsync(auditEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
