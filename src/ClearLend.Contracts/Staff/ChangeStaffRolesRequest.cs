namespace ClearLend.Contracts.Staff;

public sealed record ChangeStaffRolesRequest(IReadOnlyCollection<string> Roles);
