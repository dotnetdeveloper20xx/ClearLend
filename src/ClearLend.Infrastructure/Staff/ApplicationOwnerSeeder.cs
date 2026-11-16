using ClearLend.Application.Abstractions;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Staff;
using ClearLend.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace ClearLend.Infrastructure.Staff;

public sealed class ApplicationOwnerSeeder(
    IConfiguration configuration,
    IUserAccountRepository accounts,
    IIdentityAccountService identity,
    IStaffMembershipRepository memberships,
    IUnitOfWork unitOfWork)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["StaffSeed:Email"];
        var password = configuration["StaffSeed:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var address = EmailAddress.Create(email);
        if (!address.TryGetValue(out var emailAddress))
            throw new InvalidOperationException("StaffSeed:Email is not a valid email address.");
        if (await accounts.ExistsByEmailAsync(emailAddress.Value, cancellationToken))
            return;

        var identityResult = await identity.CreateAsync(emailAddress.Value, password, cancellationToken);
        if (!identityResult.Succeeded)
            throw new InvalidOperationException($"Application Owner seed failed: {identityResult.ErrorMessage}");
        var subject = IdentityProviderSubject.Create(identityResult.Subject);
        if (!subject.TryGetValue(out var providerSubject))
            throw new InvalidOperationException("Application Owner seed returned an invalid identity subject.");

        var account = UserAccount.Register(new UserAccountRegistration(providerSubject, emailAddress, DateTimeOffset.UtcNow));
        if (!account.TryGetValue(out var userAccount))
            throw new InvalidOperationException(account.Error!.Message);
        var membership = StaffMembership.Create(userAccount.Id, StaffRole.ApplicationOwner, DateTimeOffset.UtcNow);
        if (!membership.TryGetValue(out var staffMembership))
            throw new InvalidOperationException(membership.Error!.Message);

        await accounts.AddAsync(userAccount, cancellationToken);
        await memberships.AddAsync(staffMembership, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
