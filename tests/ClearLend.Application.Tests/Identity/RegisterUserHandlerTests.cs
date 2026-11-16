using ClearLend.Application.Abstractions;
using ClearLend.Application.Identity.Register;
using ClearLend.Domain.Identity;

namespace ClearLend.Application.Tests.Identity;

public sealed class RegisterUserHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ValidEmailCreatesActiveUser()
    {
        var accounts = new UserAccountRepositorySpy();
        var handler = new RegisterUserHandler(accounts, new UnitOfWorkSpy(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new RegisterUserCommand(" Alex@Example.com "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(accounts.Items);
        Assert.Equal("alex@example.com", result.Value!.Email);
        Assert.Equal(AccountStatus.Active, result.Value.Status);
        Assert.StartsWith("development|", accounts.Items[0].IdentityProviderSubject.Value);
    }

    [Fact]
    public async Task DuplicateEmailIsRejected()
    {
        var accounts = new UserAccountRepositorySpy();
        var handler = new RegisterUserHandler(accounts, new UnitOfWorkSpy(), new FixedTimeProvider(Now));

        Assert.True((await handler.Handle(new RegisterUserCommand("alex@example.com"), CancellationToken.None)).IsSuccess);
        var result = await handler.Handle(new RegisterUserCommand("ALEX@EXAMPLE.COM"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.email.already_registered", result.Error?.Code);
        Assert.Single(accounts.Items);
    }

    [Fact]
    public async Task InvalidEmailIsRejectedWithoutSaving()
    {
        var accounts = new UserAccountRepositorySpy();
        var handler = new RegisterUserHandler(accounts, new UnitOfWorkSpy(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new RegisterUserCommand("not-an-email"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.email.invalid", result.Error?.Code);
        Assert.Empty(accounts.Items);
    }

    private sealed class UserAccountRepositorySpy : IUserAccountRepository
    {
        public List<UserAccount> Items { get; } = [];

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(x => x.EmailAddress.Value == email));

        public Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(x => x.IdentityProviderSubject.Value == subject));
        public Task<UserAccount?> GetByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(x => x.IdentityProviderSubject.Value == subject));

        public Task AddAsync(UserAccount account, CancellationToken cancellationToken)
        {
            Items.Add(account);
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkSpy : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
