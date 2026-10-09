using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IAuditEventWriter
{
    // Implementations stage the event in the same unit of work; durable atomicity is an adapter responsibility.
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
