using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Lenders.Profile;

public sealed record GetMyLenderProfileQuery : IRequest<DomainResult<LenderProfileResult>>;
public sealed record LenderProfileResult(string UserAccountId, string LenderProfileId, string Email, string LegalBusinessName,
    string? TradingName, string? BusinessRegistrationNumber, string ContactGivenName, string ContactFamilyName,
    string AccountStatus, string ProfileState, string ConsentStatus, DateTimeOffset RegistrationDate);

public sealed class GetMyLenderProfileHandler(ICurrentActor actor, IUserAccountRepository accounts, ILenderProfileRepository lenders)
    : IRequestHandler<GetMyLenderProfileQuery, DomainResult<LenderProfileResult>>
{
    public async Task<DomainResult<LenderProfileResult>> Handle(GetMyLenderProfileQuery query, CancellationToken cancellationToken)
    {
        if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.IdentityProviderSubject)) return DomainResults.Failure<LenderProfileResult>(new("auth.required", "Authentication is required."));
        var account = await accounts.GetByIdentityProviderSubjectAsync(actor.IdentityProviderSubject, cancellationToken); if (account is null) return NotFound();
        var profile = await lenders.GetByUserAccountIdAsync(account.Id, cancellationToken); if (profile is null) return NotFound();
        return DomainResults.Success(new LenderProfileResult(account.Id.Value.ToString(), profile.Id.Value.ToString(), account.EmailAddress.Value, profile.LegalBusinessName, profile.TradingName, profile.BusinessRegistrationNumber, profile.PrimaryContact.GivenName, profile.PrimaryContact.FamilyName, account.Status.ToString(), profile.State.ToString(), profile.LatestConsent.Status.ToString(), profile.CreatedAt));
    }
    private static DomainResult<LenderProfileResult> NotFound() => DomainResults.Failure<LenderProfileResult>(new("lender.profile.not_found", "Lender profile was not found."));
}
