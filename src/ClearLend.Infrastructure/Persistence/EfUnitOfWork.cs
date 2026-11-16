using ClearLend.Application.Abstractions;

namespace ClearLend.Infrastructure.Persistence;

public sealed class EfUnitOfWork(ClearLendDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
