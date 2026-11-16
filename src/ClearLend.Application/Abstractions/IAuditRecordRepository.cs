using ClearLend.Domain.Audit;
namespace ClearLend.Application.Abstractions;
public interface IAuditRecordRepository { Task AddAsync(AuditRecord record, CancellationToken cancellationToken); }
