using ClearLend.Application.Abstractions;
using ClearLend.Application.Behaviors;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;
using FluentValidation;
using MediatR;

namespace ClearLend.Application.Operations.Vetting;

public sealed record AssignVettingReviewerCommand(
    VettingCaseId VettingCaseId,
    StaffMemberId ReviewerStaffMemberId,
    StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest
{
    public PermissionCode RequiredPermission => PermissionCode.ManageWorkItems;
}

public sealed class AssignVettingReviewerCommandValidator : AbstractValidator<AssignVettingReviewerCommand>
{
    public AssignVettingReviewerCommandValidator()
    {
        RuleFor(x => x.VettingCaseId.Value).NotEmpty();
        RuleFor(x => x.ReviewerStaffMemberId.Value).NotEmpty();
        RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty();
    }
}

public sealed class AssignVettingReviewerHandler(
    IVettingCaseRepository cases,
    IStaffMemberRepository staff,
    IAuditEventWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null,
    IVettingWorkItemRepository? workItems = null)
    : IRequestHandler<AssignVettingReviewerCommand, DomainResult>
{
    public async Task<DomainResult> Handle(AssignVettingReviewerCommand command, CancellationToken cancellationToken)
    {
        var vettingCase = await cases.GetAsync(command.VettingCaseId, cancellationToken);
        if (vettingCase is null)
            return DomainResult.Failure(new("vetting.case.not_found", "The vetting case was not found."));

        var reviewer = await staff.GetAsync(command.ReviewerStaffMemberId, cancellationToken);
        if (reviewer is null || reviewer.Status != StaffStatus.Active)
            return DomainResult.Failure(new("vetting.reviewer.inactive", "Only an active staff member can review a vetting case."));
        if (!reviewer.Roles.Contains(StaffRole.ComplianceReviewer))
            return DomainResult.Failure(new("vetting.reviewer.role_ineligible", "The reviewer must have the Compliance Reviewer role."));

        var actor = await staff.GetAsync(command.ActorStaffMemberId, cancellationToken);
        if (actor is null || actor.Status != StaffStatus.Active)
            return DomainResult.Failure(new("authorization.forbidden", "An active staff member must assign the reviewer."));

        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var linkedWorkItem = workItems is null
            ? null
            : await workItems.FindActiveForCaseAsync(vettingCase.Id, cancellationToken);
        if (linkedWorkItem is not null &&
            (linkedWorkItem.Type != WorkItemType.Vetting ||
             linkedWorkItem.Status is not (WorkItemStatus.Open or WorkItemStatus.InProgress) ||
             at.Offset != TimeSpan.Zero || at < linkedWorkItem.StatusChangedAt))
            return DomainResult.Failure(new("vetting.reviewer_assignment.work_item_invalid", "The linked vetting work item is not available for reviewer assignment."));

        var result = vettingCase.AssignReviewer(new ReviewerAssignment(
            reviewer.UserAccountId,
            actor.UserAccountId,
            at));
        if (!result.IsSuccess) return result;

        if (linkedWorkItem is not null)
        {
            var assignment = linkedWorkItem.AssignTo(reviewer.Id, at);
            if (!assignment.IsSuccess) return assignment;
        }

        await OperationalAudit.AppendAndSaveAsync(
            audit,
            unitOfWork,
            "vetting.reviewer_assigned",
            "vetting_case",
            vettingCase.Id.Value.ToString("D"),
            command.ActorStaffMemberId,
            at,
            cancellationToken,
            $"reviewer_staff_member_id:{reviewer.Id.Value:D}");

        return DomainResult.Success();
    }
}
