using ClearLend.Domain.Common;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Lenders;
using ClearLend.Domain.Identity;
using MediatR;

namespace ClearLend.Application.Lenders.Register;

public sealed record RegisterLenderCommand(string Email, string Password, string LegalBusinessName, string? TradingName,
    string? BusinessRegistrationNumber, string ContactGivenName, string ContactFamilyName, string ConsentPolicyVersion)
    : IRequest<DomainResult<RegisterLenderResult>>;
public sealed record RegisterLenderResult(UserAccountId UserAccountId, LenderProfileId LenderProfileId, ProfileState ProfileState);
