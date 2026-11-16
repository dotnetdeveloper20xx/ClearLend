using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using MediatR;

namespace ClearLend.Application.Identity.Register;

public sealed class RegisterUserHandler(
    IUserAccountRepository userAccounts,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null) : IRequestHandler<RegisterUserCommand, DomainResult<RegisterUserResult>>
{
    private readonly TimeProvider clock = clock ?? TimeProvider.System;

    public async Task<DomainResult<RegisterUserResult>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(command.Email);
        if (!email.TryGetValue(out var emailAddress))
            return DomainResults.Failure<RegisterUserResult>(email.Error!);

        if (await userAccounts.ExistsByEmailAsync(emailAddress.Value, cancellationToken))
        {
            return DomainResults.Failure<RegisterUserResult>(
                new("identity.email.already_registered", "The email address is already registered."));
        }

        var registeredAt = clock.GetUtcNow();
        var subject = IdentityProviderSubject.Create($"development|{Guid.NewGuid():N}");
        if (!subject.TryGetValue(out var identitySubject))
            return DomainResults.Failure<RegisterUserResult>(subject.Error!);

        var account = UserAccount.Register(
            new UserAccountRegistration(identitySubject, emailAddress, registeredAt));
        if (!account.TryGetValue(out var userAccount))
            return DomainResults.Failure<RegisterUserResult>(account.Error!);

        var activated = userAccount.Activate(registeredAt);
        if (!activated.IsSuccess)
            return DomainResults.Failure<RegisterUserResult>(activated.Error!);

        await userAccounts.AddAsync(userAccount, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DomainResults.Success(new RegisterUserResult(
            userAccount.Id,
            userAccount.EmailAddress.Value,
            userAccount.Status));
    }
}
