using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Application.Abstractions;

public interface ICurrentActor
{
    bool IsAuthenticated { get; }
    string? IdentityProviderSubject { get; }
}
