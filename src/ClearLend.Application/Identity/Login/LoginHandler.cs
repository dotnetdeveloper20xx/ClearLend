using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Identity.Login;

public sealed class LoginHandler(
    IIdentityAuthenticator authenticator,
    IAccessTokenIssuer tokenIssuer) : IRequestHandler<LoginCommand, DomainResult<LoginResult>>
{
    public async Task<DomainResult<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var identity = await authenticator.AuthenticateAsync(command.Email, command.Password, cancellationToken);
        if (!identity.TryGetValue(out var subject))
            return DomainResults.Failure<LoginResult>(identity.Error!);

        var token = tokenIssuer.Issue(subject);
        if (!token.TryGetValue(out var accessToken))
            return DomainResults.Failure<LoginResult>(token.Error!);

        return DomainResults.Success(new LoginResult(accessToken, 900));
    }
}
