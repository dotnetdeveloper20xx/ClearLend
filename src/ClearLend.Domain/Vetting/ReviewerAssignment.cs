using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed record ReviewerAssignment(
    UserAccountId ReviewerAccountId,
    UserAccountId AssignedByAccountId,
    DateTimeOffset AssignedAt);
