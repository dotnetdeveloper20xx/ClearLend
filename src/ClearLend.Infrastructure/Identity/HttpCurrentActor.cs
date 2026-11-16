using System.Security.Claims;
using ClearLend.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ClearLend.Infrastructure.Identity;

public sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public string? IdentityProviderSubject =>
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
}
