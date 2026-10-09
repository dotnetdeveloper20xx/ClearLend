namespace ClearLend.Application.Abstractions;

public interface IUnitOfWork
{
    // Implementations must commit all changes atomically or commit none of them.
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
