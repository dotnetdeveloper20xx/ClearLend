using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Application.Abstractions;

/// <summary>Checks trusted account state at the application boundary before granting internal staff access.</summary>
public interface IStaffAccountEligibility
{
    Task<DomainResult> EnsureEligibleAsync(UserAccountId accountId, CancellationToken cancellationToken);
}
