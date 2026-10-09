using ClearLend.Application.Abstractions;
using ClearLend.Application.Operations;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;

namespace ClearLend.Application.Operations.Staff;

public sealed record BootstrapFirstOwnerCommand(UserAccountId UserAccountId) : IRequest<DomainResult<StaffMemberId>>;

public sealed class BootstrapFirstOwnerCommandValidator : AbstractValidator<BootstrapFirstOwnerCommand>
{
    public BootstrapFirstOwnerCommandValidator() => RuleFor(x => x.UserAccountId.Value).NotEmpty();
}

/// <summary>Creates the initial active owner only when a trusted outer boundary authorizes a closed bootstrap window.</summary>
public sealed class BootstrapFirstOwnerHandler(
    IFirstOwnerBootstrapGate gate,
    IStaffAccountEligibility eligibility,
    IStaffMemberRepository staff,
    IAuditEventWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null) : IRequestHandler<BootstrapFirstOwnerCommand, DomainResult<StaffMemberId>>
{
    public async Task<DomainResult<StaffMemberId>> Handle(BootstrapFirstOwnerCommand command, CancellationToken cancellationToken)
    {
        if (!await gate.IsBootstrapAuthorizedAsync(command.UserAccountId, cancellationToken))
            return DomainResults.Failure<StaffMemberId>(new("staff.bootstrap.forbidden", "First-owner bootstrap is not authorized."));
        if (await staff.CountApplicationOwnersAsync(cancellationToken) != 0)
            return DomainResults.Failure<StaffMemberId>(new("staff.bootstrap.already_completed", "First-owner bootstrap has already been used."));
        if (await staff.ExistsForAccountAsync(command.UserAccountId, cancellationToken))
            return DomainResults.Failure<StaffMemberId>(new("staff.account.already_member", "This account already has a staff membership."));
        var eligible = await eligibility.EnsureEligibleAsync(command.UserAccountId, cancellationToken);
        if (!eligible.IsSuccess) return DomainResults.Failure<StaffMemberId>(eligible.Error!);
        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var created = StaffMember.Invite(command.UserAccountId, [StaffRole.ApplicationOwner], at);
        if (!created.TryGetValue(out var owner)) return DomainResults.Failure<StaffMemberId>(created.Error!);
        var activated = owner.Activate(at);
        if (!activated.IsSuccess) return DomainResults.Failure<StaffMemberId>(activated.Error!);
        await staff.AddAsync(owner, cancellationToken);
        await OperationalAudit.AppendAndSaveAsync(audit, unitOfWork, "staff.first_owner_bootstrapped", "staff_member", owner.Id.Value.ToString("D"), "system:first-owner-bootstrap", at, cancellationToken);
        return DomainResults.Success(owner.Id);
    }
}
