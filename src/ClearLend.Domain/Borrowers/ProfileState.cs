namespace ClearLend.Domain.Borrowers;

public enum ProfileState
{
    Incomplete = 1,
    ReadyForReview = 2,
    Active = 3,
    Suspended = 4,
    Closed = 5
}
