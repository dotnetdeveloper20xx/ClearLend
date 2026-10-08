namespace ClearLend.Domain.Vetting;

public sealed record EvidenceRejection(
    string Reason,
    DateTimeOffset ReviewedAt);
