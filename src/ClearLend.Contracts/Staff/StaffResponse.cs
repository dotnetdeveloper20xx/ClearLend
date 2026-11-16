namespace ClearLend.Contracts.Staff;

public sealed record StaffResponse(string UserAccountId, string Email, IReadOnlyCollection<string> Roles, string Status, DateTimeOffset CreatedAt);
