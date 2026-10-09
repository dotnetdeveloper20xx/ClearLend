using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IWorkQueueRepository
{
    Task AddAsync(WorkQueueDefinition queue, CancellationToken cancellationToken);
    Task<WorkQueueDefinition?> GetAsync(WorkQueueId id, CancellationToken cancellationToken);
}
