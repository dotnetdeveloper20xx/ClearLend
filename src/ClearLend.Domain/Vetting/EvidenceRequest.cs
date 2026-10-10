using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed record EvidenceRequest(
    VettingCaseId VettingCaseId,
    EvidenceType Type,
    UserAccountId RequestedByAccountId,
    string Reason,
    DateTimeOffset RequestedAt);
