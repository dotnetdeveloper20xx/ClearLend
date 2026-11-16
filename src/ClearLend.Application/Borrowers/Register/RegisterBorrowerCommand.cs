using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Borrowers.Register;

public sealed record RegisterBorrowerCommand(
    string Email,
    string Password,
    string GivenName,
    string FamilyName,
    ConsentStatus ConsentStatus,
    string ConsentPolicyVersion) : IRequest<DomainResult<RegisterBorrowerResult>>;
