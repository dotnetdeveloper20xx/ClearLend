using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed record EvidenceRejection(
    string Reason,
    UserAccountId ReviewedByAccountId,
    DateTimeOffset ReviewedAt);
