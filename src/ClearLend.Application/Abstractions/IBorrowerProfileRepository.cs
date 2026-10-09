using ClearLend.Domain.Borrowers;

namespace ClearLend.Application.Abstractions;

public interface IBorrowerProfileRepository
{
    Task AddAsync(BorrowerProfile profile, CancellationToken cancellationToken);
}
