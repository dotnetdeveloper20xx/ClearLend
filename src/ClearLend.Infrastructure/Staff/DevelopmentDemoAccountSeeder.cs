using ClearLend.Application.Abstractions;
using ClearLend.Application.Borrowers.Register;
using ClearLend.Application.Lenders.Register;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace ClearLend.Infrastructure.Staff;

public sealed class DevelopmentDemoAccountSeeder(
    IConfiguration configuration,
    IUserAccountRepository accounts,
    ISender sender)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedBorrowerAsync(cancellationToken);
        await SeedLenderAsync(cancellationToken);
    }

    private async Task SeedBorrowerAsync(CancellationToken cancellationToken)
    {
        var email = configuration["DemoAccounts:Borrower:Email"];
        var password = configuration["DemoAccounts:Borrower:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || await IsRegisteredAsync(email, cancellationToken))
            return;

        var result = await sender.Send(new RegisterBorrowerCommand(
            email, password, "Demo", "Borrower", ConsentStatus.Granted, "demo-v1"), cancellationToken);
        EnsureSuccess(result.IsSuccess, "Borrower", result.Error?.Message);
    }

    private async Task SeedLenderAsync(CancellationToken cancellationToken)
    {
        var email = configuration["DemoAccounts:Lender:Email"];
        var password = configuration["DemoAccounts:Lender:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || await IsRegisteredAsync(email, cancellationToken))
            return;

        var result = await sender.Send(new RegisterLenderCommand(
            email, password, "ClearLend Demo Lending", "Demo Lending", "DEMO-001",
            "Demo", "Lender", "demo-v1"), cancellationToken);
        EnsureSuccess(result.IsSuccess, "Lender", result.Error?.Message);
    }

    private async Task<bool> IsRegisteredAsync(string email, CancellationToken cancellationToken)
    {
        var address = EmailAddress.Create(email);
        if (!address.TryGetValue(out var emailAddress))
            throw new InvalidOperationException($"A demo account email is invalid: {email}.");
        return await accounts.ExistsByEmailAsync(emailAddress.Value, cancellationToken);
    }

    private static void EnsureSuccess(bool succeeded, string accountType, string? error)
    {
        if (!succeeded)
            throw new InvalidOperationException($"Demo {accountType} seed failed: {error}");
    }
}
