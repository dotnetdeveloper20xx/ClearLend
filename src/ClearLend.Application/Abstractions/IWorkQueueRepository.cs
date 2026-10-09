using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IWorkQueueRepository
{
    Task<WorkQueueDefinition?> GetAsync(WorkQueueId id, CancellationToken cancellationToken);
}
