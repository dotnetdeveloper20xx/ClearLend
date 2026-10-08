namespace ClearLend.Domain.Vetting;

public enum VettingCaseStatus
{
    Open = 1,
    InReview = 2,
    AwaitingInformation = 3,
    Approved = 4,
    Rejected = 5,
    Suspended = 6,
    Closed = 7
}
