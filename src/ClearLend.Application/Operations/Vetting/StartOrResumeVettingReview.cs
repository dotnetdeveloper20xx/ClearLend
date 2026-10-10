using ClearLend.Application.Abstractions;
using ClearLend.Application.Behaviors;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;
using FluentValidation;
using MediatR;

namespace ClearLend.Application.Operations.Vetting;

public sealed record StartOrResumeVettingReviewCommand(
    VettingCaseId VettingCaseId,
    StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest
{
    public PermissionCode RequiredPermission => PermissionCode.ReviewCompliance;
}

public sealed class StartOrResumeVettingReviewCommandValidator : AbstractValidator<StartOrResumeVettingReviewCommand>
{
    public StartOrResumeVettingReviewCommandValidator()
    {
        RuleFor(x => x.VettingCaseId.Value).NotEmpty();
        RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty();
    }
}

public sealed class StartOrResumeVettingReviewHandler(
    IVettingCaseRepository cases,
    IStaffMemberRepository staff,
    IAuditEventWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null,
    IVettingWorkItemRepository? workItems = null)
    : IRequestHandler<StartOrResumeVettingReviewCommand, DomainResult>
{
    public async Task<DomainResult> Handle(StartOrResumeVettingReviewCommand command, CancellationToken cancellationToken)
    {
        var vettingCase = await cases.GetAsync(command.VettingCaseId, cancellationToken);
        if (vettingCase is null)
            return DomainResult.Failure(new("vetting.case.not_found", "The vetting case was not found."));

        var reviewer = await staff.GetAsync(command.ActorStaffMemberId, cancellationToken);
        if (reviewer is null || reviewer.Status != StaffStatus.Active)
            return DomainResult.Failure(new("vetting.reviewer.inactive", "Only an active staff member can start or resume a review."));
        if (!reviewer.Roles.Contains(StaffRole.ComplianceReviewer))
            return DomainResult.Failure(new("vetting.reviewer.role_ineligible", "The assigned reviewer must have the Compliance Reviewer role."));
        if (vettingCase.ReviewerAccountId != reviewer.UserAccountId)
            return DomainResult.Failure(new("vetting.review.reviewer_mismatch", "Only the active assigned reviewer can start or resume this case."));

        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var linkedWorkItem = workItems is null
            ? null
            : await workItems.FindActiveForCaseAsync(vettingCase.Id, cancellationToken);
        if (linkedWorkItem is not null &&
            (linkedWorkItem.Type != WorkItemType.Vetting ||
             linkedWorkItem.AssignedTo != reviewer.Id ||
             (vettingCase.Status == VettingCaseStatus.AwaitingInformation
                 ? linkedWorkItem.Status != WorkItemStatus.WaitingForInformation
                 : linkedWorkItem.Status != WorkItemStatus.InProgress) ||
             at.Offset != TimeSpan.Zero || at < linkedWorkItem.StatusChangedAt))
            return DomainResult.Failure(new("vetting.review.work_item_mismatch", "The linked vetting work item must be assigned to the reviewer and match the case's active state."));

        var (result, action) = vettingCase.Status switch
        {
            VettingCaseStatus.Open => (vettingCase.StartReview(reviewer.UserAccountId, at), "vetting.review.started"),
            VettingCaseStatus.AwaitingInformation => (vettingCase.ResumeReview(reviewer.UserAccountId, at), "vetting.review.resumed"),
            VettingCaseStatus.Suspended => (vettingCase.ResumeFromSuspension(reviewer.UserAccountId, at), "vetting.review.resumed_from_suspension"),
            _ => (DomainResult.Failure(new("vetting.review.transition.invalid_status", "The case is not in a state where review can start or resume.")), string.Empty)
        };
        if (!result.IsSuccess) return result;

        if (linkedWorkItem is not null && vettingCase.Status == VettingCaseStatus.AwaitingInformation)
        {
            var resumed = linkedWorkItem.ResumeAfterInformation(at);
            if (!resumed.IsSuccess) return resumed;
        }

        await OperationalAudit.AppendAndSaveAsync(
            audit,
            unitOfWork,
            action,
            "vetting_case",
            vettingCase.Id.Value.ToString("D"),
            command.ActorStaffMemberId,
            at,
            cancellationToken);

        return DomainResult.Success();
    }
}
