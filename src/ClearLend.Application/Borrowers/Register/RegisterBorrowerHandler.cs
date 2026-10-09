using ClearLend.Application.Abstractions;
using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using MediatR;

namespace ClearLend.Application.Borrowers.Register;

public sealed class RegisterBorrowerHandler : IRequestHandler<RegisterBorrowerCommand, DomainResult<RegisterBorrowerResult>>
{
    private readonly IUserAccountRepository userAccounts;
    private readonly IBorrowerProfileRepository borrowerProfiles;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider clock;

    public RegisterBorrowerHandler(
        IUserAccountRepository userAccounts,
        IBorrowerProfileRepository borrowerProfiles,
        IUnitOfWork unitOfWork,
        TimeProvider? clock = null)
    {
        this.userAccounts = userAccounts ?? throw new ArgumentNullException(nameof(userAccounts));
        this.borrowerProfiles = borrowerProfiles ?? throw new ArgumentNullException(nameof(borrowerProfiles));
        this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        this.clock = clock ?? TimeProvider.System;
    }

    public async Task<DomainResult<RegisterBorrowerResult>> Handle(
        RegisterBorrowerCommand command,
        CancellationToken cancellationToken)
    {
        var subject = IdentityProviderSubject.Create(command.IdentityProviderSubject);
        if (!subject.TryGetValue(out var identitySubject)) return Failure(subject);

        var email = EmailAddress.Create(command.Email);
        if (!email.TryGetValue(out var emailAddress)) return Failure(email);

        if (await userAccounts.ExistsByEmailAsync(emailAddress.Value, cancellationToken))
            return DomainResults.Failure<RegisterBorrowerResult>(new("identity.email.already_registered", "The email address is already registered."));

        if (await userAccounts.ExistsByIdentityProviderSubjectAsync(identitySubject.Value, cancellationToken))
            return DomainResults.Failure<RegisterBorrowerResult>(new("identity.subject.already_registered", "The identity-provider subject is already registered."));

        var name = PersonalName.Create(command.GivenName, command.FamilyName);
        if (!name.TryGetValue(out var personalName)) return Failure(name);

        var registeredAt = clock.GetUtcNow();
        var consent = ConsentRecord.Record(command.ConsentStatus, command.ConsentPolicyVersion, registeredAt);
        if (!consent.TryGetValue(out var consentRecord)) return Failure(consent);

        var account = UserAccount.Register(new UserAccountRegistration(identitySubject, emailAddress, registeredAt));
        if (!account.TryGetValue(out var userAccount)) return Failure(account);

        var profile = BorrowerProfile.Create(new BorrowerProfileCreation(userAccount.Id, registeredAt, consentRecord));
        if (!profile.TryGetValue(out var borrowerProfile)) return Failure(profile);

        var completed = borrowerProfile.Complete(personalName, registeredAt);
        if (!completed.IsSuccess) return DomainResults.Failure<RegisterBorrowerResult>(completed.Error!);

        await userAccounts.AddAsync(userAccount, cancellationToken);
        await borrowerProfiles.AddAsync(borrowerProfile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DomainResults.Success(new RegisterBorrowerResult(borrowerProfile.Id, borrowerProfile.State));
    }

    private static DomainResult<RegisterBorrowerResult> Failure<T>(DomainResult<T> result) =>
        DomainResults.Failure<RegisterBorrowerResult>(result.Error!);
}
