using ClearLend.Domain.Common;
using ClearLend.Domain.Staff;
using MediatR;

namespace ClearLend.Application.Staff.Create;

public sealed record CreateStaffCommand(string Email, string Password, IReadOnlyCollection<StaffRole> Roles)
    : IRequest<DomainResult<CreateStaffResult>>;

public sealed record CreateStaffResult(string UserAccountId, IReadOnlyCollection<StaffRole> Roles);
