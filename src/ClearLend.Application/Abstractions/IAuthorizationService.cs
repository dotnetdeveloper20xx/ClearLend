using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(StaffMemberId staffMemberId, PermissionCode permission, CancellationToken cancellationToken);
}
