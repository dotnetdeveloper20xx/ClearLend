using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

public interface IStaffMemberRepository
{
    Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken);
    Task<StaffMember?> GetAsync(StaffMemberId id, CancellationToken cancellationToken);
}
