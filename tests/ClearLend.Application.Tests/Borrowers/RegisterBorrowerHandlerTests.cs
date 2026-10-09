using ClearLend.Application.Abstractions;
using ClearLend.Application.Borrowers.Register;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;

namespace ClearLend.Application.Tests.Borrowers;

public sealed class RegisterBorrowerHandlerTests
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegistersAccountAndCompletedBorrowerProfile()
    {
        var accounts = new UserAccountRepositorySpy();
        var profiles = new BorrowerProfileRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(accounts, profiles, unitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(accounts.Items);
        Assert.Single(profiles.Items);
        Assert.Equal(ProfileState.Completed, result.Value!.ProfileState);
        Assert.Equal(accounts.Items[0].Id, profiles.Items[0].UserAccountId);
        Assert.Equal(ConsentStatus.Granted, profiles.Items[0].LatestConsent!.Status);
        Assert.Equal("Aisha", profiles.Items[0].Name!.GivenName);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task DeclinedConsentDoesNotBlockRegistration()
    {
        var profiles = new BorrowerProfileRepositorySpy();
        var handler = CreateHandler(new UserAccountRepositorySpy(), profiles, new UnitOfWorkSpy());

        var result = await handler.Handle(Command(ConsentStatus.Declined), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ConsentStatus.Declined, profiles.Items.Single().LatestConsent!.Status);
    }

    [Fact]
    public async Task InvalidEmailDoesNotPersistAnything()
    {
        var accounts = new UserAccountRepositorySpy();
        var profiles = new BorrowerProfileRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(accounts, profiles, unitOfWork);

        var result = await handler.Handle(Command(email: "invalid"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.email.invalid", result.Error?.Code);
        Assert.Empty(accounts.Items);
        Assert.Empty(profiles.Items);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task MissingConsentPolicyVersionDoesNotPersistAnything()
    {
        var accounts = new UserAccountRepositorySpy();
        var profiles = new BorrowerProfileRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(accounts, profiles, unitOfWork);

        var result = await handler.Handle(Command(policyVersion: " "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("consent.policy_version.required", result.Error?.Code);
        Assert.Empty(accounts.Items);
        Assert.Empty(profiles.Items);
        Assert.False(unitOfWork.WasSaved);
    }

    private static RegisterBorrowerHandler CreateHandler(
        UserAccountRepositorySpy accounts,
        BorrowerProfileRepositorySpy profiles,
        UnitOfWorkSpy unitOfWork) =>
        new(accounts, profiles, unitOfWork, new FixedTimeProvider(RegisteredAt));

    private static RegisterBorrowerCommand Command(
        ConsentStatus consentStatus = ConsentStatus.Granted,
        string email = "aisha@example.com",
        string policyVersion = "v1") =>
        new("auth0|aisha-123", email, "Aisha", "Khan", consentStatus, policyVersion);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class UserAccountRepositorySpy : IUserAccountRepository
    {
        public List<UserAccount> Items { get; } = [];
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Items.Any(x => x.EmailAddress.Value == email));
        public Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(Items.Any(x => x.IdentityProviderSubject.Value == subject));
        public Task AddAsync(UserAccount account, CancellationToken cancellationToken) { Items.Add(account); return Task.CompletedTask; }
    }

    private sealed class BorrowerProfileRepositorySpy : IBorrowerProfileRepository
    {
        public List<BorrowerProfile> Items { get; } = [];
        public Task AddAsync(BorrowerProfile profile, CancellationToken cancellationToken) { Items.Add(profile); return Task.CompletedTask; }
    }

    private sealed class UnitOfWorkSpy : IUnitOfWork
    {
        public bool WasSaved { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) { WasSaved = true; return Task.CompletedTask; }
    }
}
