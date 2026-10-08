using ClearLend.Domain.Common;

namespace ClearLend.Domain.Identity;

public sealed class UserAccount
{
    private UserAccount(
        UserAccountId id,
        IdentityProviderSubject identityProviderSubject,
        EmailAddress emailAddress,
        DateTimeOffset createdAt)
    {
        Id = id;
        IdentityProviderSubject = identityProviderSubject;
        EmailAddress = emailAddress;
        CreatedAt = createdAt;
        Status = AccountStatus.Pending;
        StatusChangedAt = createdAt;
    }

    public UserAccountId Id { get; }

    public IdentityProviderSubject IdentityProviderSubject { get; }

    public EmailAddress EmailAddress { get; private set; }

    public AccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset StatusChangedAt { get; private set; }

    public static DomainResult<UserAccount> Register(
        IdentityProviderSubject? identityProviderSubject,
        EmailAddress? emailAddress,
        DateTimeOffset registeredAt,
        UserAccountId? id = null)
    {
        if (registeredAt.Offset != TimeSpan.Zero)
        {
            return DomainResults.Failure<UserAccount>(
                new("identity.registration_time.not_utc", "Registration time must be expressed in UTC."));
        }

        if (identityProviderSubject is null || emailAddress is null)
        {
            return DomainResults.Failure<UserAccount>(
                new("identity.account.values_required", "Identity subject and email address are required."));
        }

        return DomainResults.Success<UserAccount>(
            new(id ?? UserAccountId.New(), identityProviderSubject, emailAddress, registeredAt));
    }

    public DomainResult ChangeEmail(EmailAddress? emailAddress)
    {
        var usable = EnsureUsable();
        if (!usable.IsSuccess)
        {
            return usable;
        }

        if (emailAddress is null)
        {
            return DomainResult.Failure(new("identity.email.required", "An email address is required."));
        }

        EmailAddress = emailAddress;
        return DomainResult.Success();
    }

    public DomainResult Activate(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;
        var validStatus = EnsureStatus(AccountStatus.Pending, "identity.account.activate.invalid_status", "Only a pending account can be activated.");
        if (!validStatus.IsSuccess) return validStatus;
        TransitionTo(AccountStatus.Active, changedAt);
        return DomainResult.Success();
    }

    public DomainResult Suspend(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;
        var validStatus = EnsureStatus(AccountStatus.Active, "identity.account.suspend.invalid_status", "Only an active account can be suspended.");
        if (!validStatus.IsSuccess) return validStatus;
        TransitionTo(AccountStatus.Suspended, changedAt);
        return DomainResult.Success();
    }

    public DomainResult Close(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;
        if (Status == AccountStatus.Closed)
        {
            return DomainResult.Failure(new("identity.account.already_closed", "The account is already closed."));
        }

        TransitionTo(AccountStatus.Closed, changedAt);
        return DomainResult.Success();
    }

    private DomainResult EnsureUsable()
    {
        if (Status is AccountStatus.Suspended or AccountStatus.Closed)
        {
            return DomainResult.Failure(new("identity.account.not_modifiable", "A suspended or closed account cannot be changed."));
        }

        return DomainResult.Success();
    }

    private DomainResult EnsureStatus(AccountStatus expected, string code, string message)
    {
        if (Status != expected)
        {
            return DomainResult.Failure(new(code, message));
        }

        return DomainResult.Success();
    }

    private void TransitionTo(AccountStatus status, DateTimeOffset changedAt)
    {
        Status = status;
        StatusChangedAt = changedAt;
    }

    private static DomainResult EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            return DomainResult.Failure(new("identity.timestamp.not_utc", "Time must be expressed in UTC."));
        }

        return DomainResult.Success();
    }
}
