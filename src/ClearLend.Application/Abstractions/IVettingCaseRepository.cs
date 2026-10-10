using ClearLend.Domain.Vetting;

namespace ClearLend.Application.Abstractions;

public interface IVettingCaseRepository
{
    Task<VettingCase?> GetAsync(VettingCaseId id, CancellationToken cancellationToken);
}
