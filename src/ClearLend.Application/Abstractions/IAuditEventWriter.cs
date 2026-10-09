using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IAuditEventWriter
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
