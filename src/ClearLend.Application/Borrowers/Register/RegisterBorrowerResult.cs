using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;

namespace ClearLend.Application.Borrowers.Register;

public sealed record RegisterBorrowerResult(
    UserAccountId UserAccountId,
    BorrowerProfileId BorrowerProfileId,
    ProfileState ProfileState);
