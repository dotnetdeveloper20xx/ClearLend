using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClearLend.Infrastructure.Identity;

public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options) : IAccessTokenIssuer
{
    public DomainResult<string> Issue(string identityProviderSubject)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(identityProviderSubject))
            return DomainResults.Failure<string>(new("auth.subject.required", "An identity subject is required."));

        if (string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience) ||
            string.IsNullOrWhiteSpace(settings.SigningKey) || settings.SigningKey.Length < 32)
            return DomainResults.Failure<string>(new("auth.token.configuration.invalid", "JWT configuration is invalid."));

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(settings.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, identityProviderSubject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now, expires, credentials);
        return DomainResults.Success(new JwtSecurityTokenHandler().WriteToken(token));
    }
}
