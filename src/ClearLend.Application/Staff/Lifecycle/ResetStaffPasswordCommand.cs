using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Staff;
using ClearLend.Domain.Audit;
using MediatR;

namespace ClearLend.Application.Staff.Lifecycle;

public sealed record ResetStaffPasswordCommand(string UserAccountId, string NewPassword) : IRequest<DomainResult>;

public sealed class ResetStaffPasswordHandler(ICurrentActor actor, IUserAccountRepository accounts, IStaffMembershipRepository memberships, IIdentityAuthenticator authenticator, IAuditRecordRepository audit, TimeProvider? time = null) : IRequestHandler<ResetStaffPasswordCommand, DomainResult>
{
    public async Task<DomainResult> Handle(ResetStaffPasswordCommand request, CancellationToken cancellationToken)
    {
        var owner = await accounts.GetByIdentityProviderSubjectAsync(actor.IdentityProviderSubject ?? string.Empty, cancellationToken);
        if (!actor.IsAuthenticated || owner is null || !(await memberships.GetByUserAccountIdAsync(owner.Id, cancellationToken)).Any(x => x.State == StaffMembershipState.Active && x.Role == StaffRole.ApplicationOwner))
            return DomainResult.Failure(new("staff.authorization.forbidden", "Only an Application Owner can manage staff."));
        if (!Guid.TryParse(request.UserAccountId, out var id))
            return DomainResult.Failure(new("staff.account.invalid", "A valid staff account identifier is required."));
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 4)
            return DomainResult.Failure(new("identity.password.invalid", "The temporary password must contain at least 4 characters."));
        var target = ClearLend.Domain.Identity.UserAccountId.From(id).Value!;
        if (!(await memberships.GetByUserAccountIdAsync(target, cancellationToken)).Any())
            return DomainResult.Failure(new("staff.account.not_found", "The staff account was not found."));
        var targetAccount = await accounts.GetByIdAsync(target, cancellationToken);
        var result = targetAccount is null
            ? DomainResult.Failure(new("staff.account.not_found", "The staff account was not found."))
            : await authenticator.ResetPasswordAsync(targetAccount.IdentityProviderSubject.Value, request.NewPassword, cancellationToken);
        if (result.IsSuccess)
            await audit.AddAsync(AuditRecord.Create(actor.IdentityProviderSubject!, "StaffPasswordReset", request.UserAccountId, (time ?? TimeProvider.System).GetUtcNow()), cancellationToken);
        return result;
    }
}
