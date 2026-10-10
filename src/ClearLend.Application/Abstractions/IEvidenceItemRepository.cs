using ClearLend.Domain.Vetting;

namespace ClearLend.Application.Abstractions;

public interface IEvidenceItemRepository
{
    Task AddAsync(EvidenceItem evidenceItem, CancellationToken cancellationToken);
    Task<EvidenceItem?> GetAsync(EvidenceItemId id, CancellationToken cancellationToken);
}
