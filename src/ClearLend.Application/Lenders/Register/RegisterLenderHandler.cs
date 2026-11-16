using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Lenders;
using MediatR;

namespace ClearLend.Application.Lenders.Register;

public sealed class RegisterLenderHandler(IUserAccountRepository accounts, ILenderProfileRepository lenders,
    IUnitOfWork unitOfWork, IIdentityAccountService identity, TimeProvider? time = null)
    : IRequestHandler<RegisterLenderCommand, DomainResult<RegisterLenderResult>>
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;
    public async Task<DomainResult<RegisterLenderResult>> Handle(RegisterLenderCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(command.Email); if (!email.TryGetValue(out var address)) return Fail(email);
        if (await accounts.ExistsByEmailAsync(address.Value, cancellationToken))
            return DomainResults.Failure<RegisterLenderResult>(new("identity.email.already_registered", "This email is already registered as a borrower or lender. One person cannot have both accounts."));
        var contact = PersonalName.Create(command.ContactGivenName, command.ContactFamilyName); if (!contact.TryGetValue(out var name)) return Fail(contact);
        var created = clock.GetUtcNow(); var consent = ConsentRecord.Record(ConsentStatus.Granted, command.ConsentPolicyVersion, created); if (!consent.TryGetValue(out var record)) return Fail(consent);
        var identityResult = await identity.CreateAsync(address.Value, command.Password, cancellationToken);
        if (!identityResult.Succeeded) return DomainResults.Failure<RegisterLenderResult>(new(identityResult.ErrorCode!, identityResult.ErrorMessage!));
        var subject = IdentityProviderSubject.Create(identityResult.Subject); if (!subject.TryGetValue(out var providerSubject)) return Fail(subject);
        var account = UserAccount.Register(new UserAccountRegistration(providerSubject, address, created)); if (!account.TryGetValue(out var user)) return Fail(account);
        var profile = LenderProfile.Create(user.Id, command.LegalBusinessName, command.TradingName, command.BusinessRegistrationNumber, name, created, record); if (!profile.TryGetValue(out var lender)) return Fail(profile);
        await accounts.AddAsync(user, cancellationToken); await lenders.AddAsync(lender, cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResults.Success(new RegisterLenderResult(user.Id, lender.Id, lender.State));
    }
    private static DomainResult<RegisterLenderResult> Fail<T>(DomainResult<T> result) => DomainResults.Failure<RegisterLenderResult>(result.Error!);
}
