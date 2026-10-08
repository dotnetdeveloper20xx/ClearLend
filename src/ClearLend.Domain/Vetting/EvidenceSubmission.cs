namespace ClearLend.Domain.Vetting;

public sealed record EvidenceSubmission(
    StorageReference StorageReference,
    DateTimeOffset SubmittedAt);
