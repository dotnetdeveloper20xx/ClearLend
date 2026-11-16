namespace ClearLend.Contracts.Staff;

public sealed record CreateStaffRequest(string Email, string Password, IReadOnlyCollection<string> Roles);

public sealed record CreateStaffResponse(string UserAccountId, IReadOnlyCollection<string> Roles);
