using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public enum VettingReviewAction
{
    Started = 1,
    ResumedAfterInformation = 2,
    ResumedAfterSuspension = 3
}

public sealed record VettingReviewActivity(
    UserAccountId ReviewerAccountId,
    VettingReviewAction Action,
    DateTimeOffset OccurredAt);
