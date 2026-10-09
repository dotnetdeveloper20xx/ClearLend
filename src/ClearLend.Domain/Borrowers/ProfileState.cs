namespace ClearLend.Domain.Borrowers;

public enum ProfileState
{
    Incomplete = 1,
    Completed = 2,
    ReadyForReview = 3,
    Active = 4,
    Suspended = 5,
    Closed = 6
}
