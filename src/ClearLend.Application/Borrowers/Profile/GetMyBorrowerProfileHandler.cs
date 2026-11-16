using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using MediatR;

namespace ClearLend.Application.Borrowers.Profile;

public sealed class GetMyBorrowerProfileHandler(
    ICurrentActor currentActor,
    IUserAccountRepository userAccounts,
    IBorrowerProfileRepository borrowerProfiles) : IRequestHandler<GetMyBorrowerProfileQuery, DomainResult<BorrowerProfileResult>>
{
    public async Task<DomainResult<BorrowerProfileResult>> Handle(GetMyBorrowerProfileQuery query, CancellationToken cancellationToken)
    {
        if (!currentActor.IsAuthenticated || string.IsNullOrWhiteSpace(currentActor.IdentityProviderSubject))
            return DomainResults.Failure<BorrowerProfileResult>(new("auth.required", "Authentication is required."));

        var account = await userAccounts.GetByIdentityProviderSubjectAsync(currentActor.IdentityProviderSubject, cancellationToken);
        if (account is null)
            return DomainResults.Failure<BorrowerProfileResult>(new("borrower.profile.not_found", "Borrower profile was not found."));

        var profile = await borrowerProfiles.GetByUserAccountIdAsync(account.Id, cancellationToken);
        if (profile?.Name is null)
            return DomainResults.Failure<BorrowerProfileResult>(new("borrower.profile.not_found", "Borrower profile was not found."));

        return DomainResults.Success(new BorrowerProfileResult(
            account.Id.Value.ToString(), profile.Id.Value.ToString(), account.EmailAddress.Value,
            profile.Name.GivenName, profile.Name.FamilyName, account.Status.ToString(), profile.State.ToString(),
            profile.LatestConsent.Status.ToString(), profile.CreatedAt));
    }
}
