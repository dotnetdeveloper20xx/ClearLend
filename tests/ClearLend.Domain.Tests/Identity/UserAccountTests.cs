using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Tests.Identity;

public sealed class UserAccountTests
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterCreatesPendingAccount()
    {
        var account = CreateAccount("Person@Example.com");

        Assert.Equal(AccountStatus.Pending, account.Status);
        Assert.Equal("person@example.com", account.EmailAddress.Value);
        Assert.Equal(RegisteredAt, account.CreatedAt);
        Assert.Equal(RegisteredAt, account.StatusChangedAt);
    }

    [Fact]
    public void AccountCanBeActivatedThenSuspendedThenClosed()
    {
        var account = CreateAccount();
        var activatedAt = RegisteredAt.AddMinutes(1);
        var suspendedAt = activatedAt.AddMinutes(1);
        var closedAt = suspendedAt.AddMinutes(1);

        Assert.True(account.Activate(activatedAt).IsSuccess);
        Assert.True(account.Suspend(suspendedAt).IsSuccess);
        Assert.True(account.Close(closedAt).IsSuccess);

        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Equal(closedAt, account.StatusChangedAt);
    }

    [Fact]
    public void PendingAccountCannotBeSuspended()
    {
        var account = CreateAccount();

        var result = account.Suspend(RegisteredAt.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.account.suspend.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void ClosedAccountCannotChangeEmail()
    {
        var account = CreateAccount();
        Assert.True(account.Close(RegisteredAt.AddMinutes(1)).IsSuccess);

        var result = account.ChangeEmail(CreateEmail("new@example.com"));

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.account.not_modifiable", result.Error?.Code);
    }

    [Fact]
    public void RegistrationRequiresUtcTime()
    {
        var nonUtc = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.FromHours(1));

        var result = UserAccount.Register(new UserAccountRegistration(
            CreateSubject("subject"), CreateEmail("person@example.com"), nonUtc));

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.registration_time.not_utc", result.Error?.Code);
    }

    [Fact]
    public void RegistrationRequiresIdentityAndEmailValues()
    {
        var result = UserAccount.Register(new UserAccountRegistration(
            null!, null!, RegisteredAt));

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.account.values_required", result.Error?.Code);
    }

    [Fact]
    public void PendingAccountCannotBeActivatedTwice()
    {
        var account = CreateAccount();
        Assert.True(account.Activate(RegisteredAt.AddMinutes(1)).IsSuccess);

        var result = account.Activate(RegisteredAt.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.account.activate.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void ClosedAccountCannotBeReactivated()
    {
        var account = CreateAccount();
        Assert.True(account.Close(RegisteredAt.AddMinutes(1)).IsSuccess);

        var result = account.Activate(RegisteredAt.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.account.activate.invalid_status", result.Error?.Code);
    }

    private static UserAccount CreateAccount(string email = "person@example.com") =>
        TestResult.Get(UserAccount.Register(new UserAccountRegistration(
            CreateSubject("auth0|user-123"), CreateEmail(email), RegisteredAt)));

    private static IdentityProviderSubject CreateSubject(string value) =>
        TestResult.Get(IdentityProviderSubject.Create(value));

    private static EmailAddress CreateEmail(string value) =>
        TestResult.Get(EmailAddress.Create(value));
}
