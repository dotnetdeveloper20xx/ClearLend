namespace ClearLend.Domain.Vetting;

public sealed record EvidenceRequest(
    VettingCaseId VettingCaseId,
    EvidenceType Type,
    DateTimeOffset RequestedAt);
