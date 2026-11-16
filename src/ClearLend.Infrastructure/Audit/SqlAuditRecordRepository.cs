using ClearLend.Application.Abstractions;
using ClearLend.Domain.Audit;
using ClearLend.Infrastructure.Persistence;
namespace ClearLend.Infrastructure.Audit;
public sealed class SqlAuditRecordRepository(ClearLendDbContext db) : IAuditRecordRepository
{ public Task AddAsync(AuditRecord record, CancellationToken cancellationToken) { db.AuditRecords.Add(record); return Task.CompletedTask; } }
