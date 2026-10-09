using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IWorkItemRepository
{
    Task AddAsync(WorkItem workItem, CancellationToken cancellationToken);
    Task<WorkItem?> GetAsync(WorkItemId id, CancellationToken cancellationToken);
    Task<WorkItem?> FindActiveAsync(WorkItemType type, string subjectReference, CancellationToken cancellationToken);
    Task<bool> HasActiveAssignmentAsync(WorkQueueId queueId, StaffMemberId staffMemberId, CancellationToken cancellationToken);
    Task<bool> HasAnyActiveAssignmentAsync(StaffMemberId staffMemberId, CancellationToken cancellationToken);
}
