namespace ClearLend.Application.Identity.Login;

public sealed record LoginResult(string AccessToken, int ExpiresInSeconds);
