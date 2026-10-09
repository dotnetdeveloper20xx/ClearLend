using ClearLend.Application.Abstractions;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Operations;
using FluentValidation;
using MediatR;
using ClearLend.Application.Behaviors;

namespace ClearLend.Application.Operations.Staff;

public sealed record InviteStaffMemberCommand(UserAccountId UserAccountId, IReadOnlyCollection<StaffRole> Roles, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult<StaffMemberId>>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageStaff; }
public sealed record ActivateStaffMemberCommand(StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageStaff; }
public sealed record SuspendStaffMemberCommand(StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageStaff; }
public sealed record CloseStaffMemberCommand(StaffMemberId StaffMemberId, StaffMemberId ActorStaffMemberId) : IRequest<DomainResult>, IAuthorizedRequest { public PermissionCode RequiredPermission => PermissionCode.ManageStaff; }

public sealed class InviteStaffMemberCommandValidator : AbstractValidator<InviteStaffMemberCommand>
{
    public InviteStaffMemberCommandValidator() { RuleFor(x => x.Roles).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); }
}

public sealed class ActivateStaffMemberCommandValidator : AbstractValidator<ActivateStaffMemberCommand>
{ public ActivateStaffMemberCommandValidator() { RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class SuspendStaffMemberCommandValidator : AbstractValidator<SuspendStaffMemberCommand>
{ public SuspendStaffMemberCommandValidator() { RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }
public sealed class CloseStaffMemberCommandValidator : AbstractValidator<CloseStaffMemberCommand>
{ public CloseStaffMemberCommandValidator() { RuleFor(x => x.StaffMemberId.Value).NotEmpty(); RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty(); } }

public sealed class InviteStaffMemberHandler(IStaffMemberRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    : IRequestHandler<InviteStaffMemberCommand, DomainResult<StaffMemberId>>
{
    public async Task<DomainResult<StaffMemberId>> Handle(InviteStaffMemberCommand command, CancellationToken cancellationToken)
    {
        var result = StaffMember.Invite(command.UserAccountId, command.Roles, (clock ?? TimeProvider.System).GetUtcNow());
        if (!result.TryGetValue(out var staffMember)) return DomainResults.Failure<StaffMemberId>(result.Error!);
        await repository.AddAsync(staffMember, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResults.Success(staffMember.Id);
    }
}

public abstract class StaffStatusHandler<TCommand>(IStaffMemberRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null)
    where TCommand : IRequest<DomainResult>
{
    protected abstract DomainResult Apply(StaffMember staffMember, DateTimeOffset at);
    protected async Task<DomainResult> Execute(StaffMemberId id, CancellationToken cancellationToken)
    {
        var staffMember = await repository.GetAsync(id, cancellationToken);
        if (staffMember is null) return DomainResult.Failure(new("staff.not_found", "The staff member was not found."));
        var result = Apply(staffMember, (clock ?? TimeProvider.System).GetUtcNow());
        if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainResult.Success();
    }
}

public sealed class ActivateStaffMemberHandler(IStaffMemberRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null) : StaffStatusHandler<ActivateStaffMemberCommand>(repository, unitOfWork, clock), IRequestHandler<ActivateStaffMemberCommand, DomainResult>
{ protected override DomainResult Apply(StaffMember staffMember, DateTimeOffset at) => staffMember.Activate(at); public Task<DomainResult> Handle(ActivateStaffMemberCommand command, CancellationToken cancellationToken) => Execute(command.StaffMemberId, cancellationToken); }
public sealed class SuspendStaffMemberHandler(IStaffMemberRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null) : StaffStatusHandler<SuspendStaffMemberCommand>(repository, unitOfWork, clock), IRequestHandler<SuspendStaffMemberCommand, DomainResult>
{ protected override DomainResult Apply(StaffMember staffMember, DateTimeOffset at) => staffMember.Suspend(at); public Task<DomainResult> Handle(SuspendStaffMemberCommand command, CancellationToken cancellationToken) => Execute(command.StaffMemberId, cancellationToken); }
public sealed class CloseStaffMemberHandler(IStaffMemberRepository repository, IUnitOfWork unitOfWork, TimeProvider? clock = null) : StaffStatusHandler<CloseStaffMemberCommand>(repository, unitOfWork, clock), IRequestHandler<CloseStaffMemberCommand, DomainResult>
{ protected override DomainResult Apply(StaffMember staffMember, DateTimeOffset at) => staffMember.Close(at); public Task<DomainResult> Handle(CloseStaffMemberCommand command, CancellationToken cancellationToken) => Execute(command.StaffMemberId, cancellationToken); }
