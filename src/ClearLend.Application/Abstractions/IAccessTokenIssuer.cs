using ClearLend.Domain.Common;

namespace ClearLend.Application.Abstractions;

public interface IAccessTokenIssuer
{
    DomainResult<string> Issue(string identityProviderSubject);
}
