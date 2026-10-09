using ClearLend.Domain.Borrowers;

namespace ClearLend.Application.Borrowers.Register;

public sealed record RegisterBorrowerResult(
    BorrowerProfileId BorrowerProfileId,
    ProfileState ProfileState);
