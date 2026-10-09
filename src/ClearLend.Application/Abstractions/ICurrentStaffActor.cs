using ClearLend.Domain.Operations;

namespace ClearLend.Application.Abstractions;

/// <summary>Provides staff identity established by a trusted outer boundary for the current request.</summary>
public interface ICurrentStaffActor
{
    StaffMemberId? StaffMemberId { get; }
}
