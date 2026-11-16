using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Identity.Login;

public sealed class LoginHandler(
    IIdentityAuthenticator authenticator,
    IAccessTokenIssuer tokenIssuer,
    IUserAccountRepository accounts,
    IBorrowerProfileRepository borrowers,
    ILenderProfileRepository lenders) : IRequestHandler<LoginCommand, DomainResult<LoginResult>>
{
    public async Task<DomainResult<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var identity = await authenticator.AuthenticateAsync(command.Email, command.Password, cancellationToken);
        if (!identity.TryGetValue(out var subject))
            return DomainResults.Failure<LoginResult>(identity.Error!);

        var token = tokenIssuer.Issue(subject);
        if (!token.TryGetValue(out var accessToken))
            return DomainResults.Failure<LoginResult>(token.Error!);

        var account = await accounts.GetByIdentityProviderSubjectAsync(subject, cancellationToken);
        if (account is null)
            return DomainResults.Failure<LoginResult>(new("identity.account.not_found", "The authenticated account could not be found."));

        var accountType = await lenders.GetByUserAccountIdAsync(account.Id, cancellationToken) is not null
            ? "Lender"
            : await borrowers.GetByUserAccountIdAsync(account.Id, cancellationToken) is not null
                ? "Borrower"
                : null;

        if (accountType is null)
            return DomainResults.Failure<LoginResult>(new("identity.profile.not_found", "The account has no borrower or lender profile."));

        return DomainResults.Success(new LoginResult(accessToken, 900, accountType));
    }
}
