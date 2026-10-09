using ClearLend.Domain.Identity;

namespace ClearLend.Application.Abstractions;

/// <summary>The outer host must authorize a one-time bootstrap through a trusted, non-public mechanism.</summary>
public interface IFirstOwnerBootstrapGate
{
    Task<bool> IsBootstrapAuthorizedAsync(UserAccountId firstOwnerAccountId, CancellationToken cancellationToken);
}
