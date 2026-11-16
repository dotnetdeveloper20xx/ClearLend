using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Identity.Login;

public sealed class LoginHandler(
    IIdentityAuthenticator authenticator,
    IAccessTokenIssuer tokenIssuer,
    IUserAccountRepository accounts,
    IBorrowerProfileRepository borrowers,
    ILenderProfileRepository lenders,
    IStaffMembershipRepository staffMemberships) : IRequestHandler<LoginCommand, DomainResult<LoginResult>>
{
    public async Task<DomainResult<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var identity = await authenticator.AuthenticateAsync(command.Email, command.Password, cancellationToken);
        if (!identity.TryGetValue(out var authentication))
            return DomainResults.Failure<LoginResult>(identity.Error!);

        var subject = authentication.Subject;

        var account = await accounts.GetByIdentityProviderSubjectAsync(subject, cancellationToken);
        if (account is null)
            return DomainResults.Failure<LoginResult>(new("identity.account.not_found", "The authenticated account could not be found."));

        var staff = await staffMemberships.GetByUserAccountIdAsync(account.Id, cancellationToken);
        if (staff.Count > 0 && !staff.Any(x => x.State == ClearLend.Domain.Staff.StaffMembershipState.Active))
            return DomainResults.Failure<LoginResult>(new("staff.access.disabled", "This staff account no longer has active access."));

        var token = tokenIssuer.Issue(subject);
        if (!token.TryGetValue(out var accessToken))
            return DomainResults.Failure<LoginResult>(token.Error!);

        var accountType = staff.Any(x => x.State == ClearLend.Domain.Staff.StaffMembershipState.Active)
            ? "Staff"
            : await lenders.GetByUserAccountIdAsync(account.Id, cancellationToken) is not null
            ? "Lender"
            : await borrowers.GetByUserAccountIdAsync(account.Id, cancellationToken) is not null
                ? "Borrower"
                : null;

        if (accountType is null)
            return DomainResults.Failure<LoginResult>(new("identity.profile.not_found", "The account has no borrower or lender profile."));

        return DomainResults.Success(new LoginResult(accessToken, 900, accountType!, authentication.MustChangePassword));
    }
}
