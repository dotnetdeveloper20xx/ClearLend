using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using ClearLend.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClearLend.Infrastructure.IntegrationTests.Identity;

public sealed class JwtAccessTokenIssuerTests
{
    private const string Subject = "identity-user-id";
    private const string SigningKey = "unit-test-signing-key-with-at-least-32-characters";

    [Fact]
    public void IssueCreatesTokenWithSubjectAndExpectedMetadata()
    {
        var issuer = CreateIssuer();

        var result = issuer.Issue(Subject);

        Assert.True(result.IsSuccess);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);
        Assert.Equal(Subject, token.Subject);
        Assert.Equal("ClearLend.Api", token.Issuer);
        Assert.Contains("ClearLend.Web", token.Audiences);
        Assert.True(token.ValidTo > DateTime.UtcNow);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void IssueRejectsMissingSubject()
    {
        var result = CreateIssuer().Issue(" ");

        Assert.False(result.IsSuccess);
        Assert.Equal("auth.subject.required", result.Error?.Code);
    }

    [Fact]
    public void IssuedTokenCanBeValidatedWithConfiguredKey()
    {
        var result = CreateIssuer().Issue(Subject);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "ClearLend.Api",
            ValidateAudience = true,
            ValidAudience = "ClearLend.Web",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token.RawData, parameters, out _);

        Assert.Equal(Subject, principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    private static JwtAccessTokenIssuer CreateIssuer() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "ClearLend.Api",
            Audience = "ClearLend.Web",
            SigningKey = SigningKey,
            AccessTokenMinutes = 15
        }));
}
