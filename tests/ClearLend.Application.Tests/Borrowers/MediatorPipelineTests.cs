using ClearLend.Application;
using ClearLend.Application.Abstractions;
using ClearLend.Application.Borrowers.Register;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace ClearLend.Application.Tests.Borrowers;

public sealed class MediatorPipelineTests
{
    [Fact]
    public async Task ValidationPipelineRejectsInvalidCommandBeforeHandler()
    {
        var services = new ServiceCollection();
        services.AddClearLendApplication();
        services.AddSingleton<IUserAccountRepository, Accounts>();
        services.AddSingleton<IBorrowerProfileRepository, Profiles>();
        services.AddSingleton<IUnitOfWork, UnitOfWork>();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISender>();

        var result = await mediator.Send(new RegisterBorrowerCommand("", "invalid", "", "", ConsentStatus.Granted, ""));

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.failed", result.Error?.Code);
        Assert.Empty(provider.GetRequiredService<Accounts>().Items);
    }

    private sealed class Accounts : IUserAccountRepository
    {
        public List<UserAccount> Items { get; } = [];
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsByIdentityProviderSubjectAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task AddAsync(UserAccount account, CancellationToken cancellationToken) { Items.Add(account); return Task.CompletedTask; }
    }
    private sealed class Profiles : IBorrowerProfileRepository
    {
        public Task AddAsync(BorrowerProfile profile, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class UnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
