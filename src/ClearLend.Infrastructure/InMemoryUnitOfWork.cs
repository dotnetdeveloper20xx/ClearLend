using ClearLend.Application.Abstractions;

namespace ClearLend.Infrastructure;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
