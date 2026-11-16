using ClearLend.Application.Abstractions;
using ClearLend.Application.Borrowers.Profile;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;

namespace ClearLend.Application.Tests.Borrowers;

public sealed class GetMyBorrowerProfileHandlerTests
{
    [Fact]
    public async Task UsesAuthenticatedSubjectToLoadOnlyThatBorrowerProfile()
    {
        var account = CreateAccount("identity-faz");
        var profile = CreateProfile(account.Id);
        var handler = new GetMyBorrowerProfileHandler(
            new CurrentActor("identity-faz"),
            new UserAccounts(account),
            new BorrowerProfiles(profile));

        var result = await handler.Handle(new GetMyBorrowerProfileQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(account.EmailAddress.Value, result.Value!.Email);
        Assert.Equal("Faz", result.Value.GivenName);
        Assert.Equal("Ahmed", result.Value.FamilyName);
    }

    [Fact]
    public async Task RejectsUnauthenticatedActor()
    {
        var handler = new GetMyBorrowerProfileHandler(new CurrentActor(null), new UserAccounts(), new BorrowerProfiles());

        var result = await handler.Handle(new GetMyBorrowerProfileQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("auth.required", result.Error?.Code);
    }

    private static UserAccount CreateAccount(string subject) =>
        UserAccount.Register(new UserAccountRegistration(
            IdentityProviderSubject.Create(subject).Value!,
            EmailAddress.Create("faz.profile@example.com").Value!,
            DateTimeOffset.UtcNow)).Value!;

    private static BorrowerProfile CreateProfile(UserAccountId userAccountId)
    {
        var profile = BorrowerProfile.Create(new BorrowerProfileCreation(
            userAccountId,
            DateTimeOffset.UtcNow,
            ConsentRecord.Record(ConsentStatus.Granted, "v1", DateTimeOffset.UtcNow).Value!)).Value!;
        profile.Complete(PersonalName.Create("Faz", "Ahmed").Value!, DateTimeOffset.UtcNow);
        return profile;
    }

    private sealed class CurrentActor(string? subject) : ICurrentActor
    {
        public bool IsAuthenticated => subject is not null;
        public string? IdentityProviderSubject => subject;
    }

    private sealed class UserAccounts(params UserAccount[] accounts) : IUserAccountRepository
    {
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<UserAccount?> GetByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(accounts.SingleOrDefault(x => x.IdentityProviderSubject.Value == subject));
        public Task<UserAccount?> GetByIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken) => Task.FromResult(accounts.SingleOrDefault(x => x.Id == userAccountId));
        public Task AddAsync(UserAccount account, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class BorrowerProfiles(params BorrowerProfile[] profiles) : IBorrowerProfileRepository
    {
        public Task<BorrowerProfile?> GetByUserAccountIdAsync(UserAccountId userAccountId, CancellationToken cancellationToken) => Task.FromResult(profiles.SingleOrDefault(x => x.UserAccountId == userAccountId));
        public Task AddAsync(BorrowerProfile profile, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
