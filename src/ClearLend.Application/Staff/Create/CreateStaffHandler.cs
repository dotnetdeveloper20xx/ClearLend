using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Staff;
using ClearLend.Domain.Audit;
using MediatR;

namespace ClearLend.Application.Staff.Create;

public sealed class CreateStaffHandler(
    ICurrentActor actor,
    IUserAccountRepository accounts,
    IStaffMembershipRepository memberships,
    IIdentityAccountService identity,
    IUnitOfWork unitOfWork,
    IAuditRecordRepository audit,
    TimeProvider? time = null) : IRequestHandler<CreateStaffCommand, DomainResult<CreateStaffResult>>
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;

    public async Task<DomainResult<CreateStaffResult>> Handle(CreateStaffCommand command, CancellationToken cancellationToken)
    {
        var owner = await GetOwnerAsync(cancellationToken);
        if (!owner.TryGetValue(out var ownerAccount))
            return DomainResults.Failure<CreateStaffResult>(owner.Error!);

        var email = EmailAddress.Create(command.Email);
        if (!email.TryGetValue(out var address))
            return Failure(email);
        if (await accounts.ExistsByEmailAsync(address.Value, cancellationToken))
            return DomainResults.Failure<CreateStaffResult>(new("staff.email.already_registered", "The staff email address is already registered."));

        var createdAt = clock.GetUtcNow();
        var identityResult = await identity.CreateAsync(address.Value, command.Password, cancellationToken, mustChangePassword: true);
        if (!identityResult.Succeeded)
            return DomainResults.Failure<CreateStaffResult>(new(identityResult.ErrorCode!, identityResult.ErrorMessage!));

        var subject = IdentityProviderSubject.Create(identityResult.Subject);
        if (!subject.TryGetValue(out var providerSubject))
            return Failure(subject);
        var account = UserAccount.Register(new UserAccountRegistration(providerSubject, address, createdAt));
        if (!account.TryGetValue(out var userAccount))
            return Failure(account);

        var createdRoles = new List<StaffRole>();
        foreach (var role in command.Roles.Distinct())
        {
            var membership = StaffMembership.Create(userAccount.Id, role, createdAt);
            if (!membership.TryGetValue(out var staffMembership))
                return Failure(membership);
            await memberships.AddAsync(staffMembership, cancellationToken);
            createdRoles.Add(role);
        }

        await accounts.AddAsync(userAccount, cancellationToken);
        await audit.AddAsync(AuditRecord.Create(actor.IdentityProviderSubject!, "StaffCreated", userAccount.Id.Value.ToString("D"), createdAt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResults.Success(new CreateStaffResult(userAccount.Id.Value.ToString("D"), createdRoles));
    }

    private async Task<DomainResult<UserAccount>> GetOwnerAsync(CancellationToken cancellationToken)
    {
        if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.IdentityProviderSubject))
            return DomainResults.Failure<UserAccount>(new("staff.authorization.required", "An authenticated Application Owner is required."));

        var account = await accounts.GetByIdentityProviderSubjectAsync(actor.IdentityProviderSubject, cancellationToken);
        if (account is null)
            return DomainResults.Failure<UserAccount>(new("staff.authorization.required", "An authenticated Application Owner is required."));

        var membershipsForOwner = await memberships.GetByUserAccountIdAsync(account.Id, cancellationToken);
        return membershipsForOwner.Any(x => x.State == StaffMembershipState.Active && x.Role == StaffRole.ApplicationOwner)
            ? DomainResults.Success(account)
            : DomainResults.Failure<UserAccount>(new("staff.authorization.forbidden", "Only an Application Owner can manage staff."));
    }

    private static DomainResult<CreateStaffResult> Failure<T>(DomainResult<T> result) => DomainResults.Failure<CreateStaffResult>(result.Error!);
}
