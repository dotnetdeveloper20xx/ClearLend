using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IWorkItemRepository
{
    Task AddAsync(WorkItem workItem, CancellationToken cancellationToken);
    Task<WorkItem?> GetAsync(WorkItemId id, CancellationToken cancellationToken);
}
