using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;

namespace ClearLend.Application.Abstractions;

/// <summary>
/// Application port for retrieving the unique active operations work item associated with a vetting case.
/// A future adapter must use an explicit case relationship, not infer one from free-form subject text.
/// </summary>
public interface IVettingWorkItemRepository
{
    Task<WorkItem?> FindActiveForCaseAsync(VettingCaseId vettingCaseId, CancellationToken cancellationToken);
}
