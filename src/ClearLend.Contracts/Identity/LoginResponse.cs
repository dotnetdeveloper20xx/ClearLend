namespace ClearLend.Contracts.Identity;

public sealed record LoginResponse(string AccessToken, int ExpiresInSeconds, string AccountType);
