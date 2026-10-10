using ClearLend.Application.Abstractions;
using ClearLend.Application.Behaviors;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;
using FluentValidation;
using MediatR;

namespace ClearLend.Application.Operations.Vetting;

public sealed record RecordVettingDecisionCommand(
    VettingCaseId VettingCaseId,
    DecisionOutcome Outcome,
    string Reason,
    string PolicyVersion,
    StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult<VettingDecisionId>>, IAuthorizedRequest
{
    public PermissionCode RequiredPermission => PermissionCode.ReviewCompliance;
}

public sealed class RecordVettingDecisionCommandValidator : AbstractValidator<RecordVettingDecisionCommand>
{
    public RecordVettingDecisionCommandValidator()
    {
        RuleFor(x => x.VettingCaseId.Value).NotEmpty();
        RuleFor(x => x.Outcome).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PolicyVersion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty();
    }
}

public sealed class RecordVettingDecisionHandler(
    IVettingCaseRepository cases,
    IStaffMemberRepository staff,
    IVettingWorkItemRepository workItems,
    IAuditEventWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null)
    : IRequestHandler<RecordVettingDecisionCommand, DomainResult<VettingDecisionId>>
{
    public async Task<DomainResult<VettingDecisionId>> Handle(
        RecordVettingDecisionCommand command,
        CancellationToken cancellationToken)
    {
        var vettingCase = await cases.GetAsync(command.VettingCaseId, cancellationToken);
        if (vettingCase is null)
            return DomainResults.Failure<VettingDecisionId>(new("vetting.case.not_found", "The vetting case was not found."));

        var reviewer = await staff.GetAsync(command.ActorStaffMemberId, cancellationToken);
        if (reviewer is null || reviewer.Status != StaffStatus.Active)
            return DomainResults.Failure<VettingDecisionId>(new("vetting.reviewer.inactive", "Only an active staff member can decide a vetting case."));
        if (!reviewer.Roles.Contains(StaffRole.ComplianceReviewer))
            return DomainResults.Failure<VettingDecisionId>(new("vetting.reviewer.role_ineligible", "The assigned reviewer must have the Compliance Reviewer role."));
        if (vettingCase.ReviewerAccountId != reviewer.UserAccountId)
            return DomainResults.Failure<VettingDecisionId>(new("vetting.decision.reviewer_mismatch", "Only the assigned reviewer can decide this case."));

        var reasonResult = DecisionReason.Create(command.Reason);
        if (!reasonResult.TryGetValue(out var reason)) return DomainResults.Failure<VettingDecisionId>(reasonResult.Error!);
        var policyVersionResult = PolicyVersion.Create(command.PolicyVersion);
        if (!policyVersionResult.TryGetValue(out var policyVersion)) return DomainResults.Failure<VettingDecisionId>(policyVersionResult.Error!);

        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var timestampResult = UtcTimestamp.Create(at);
        if (!timestampResult.TryGetValue(out var decidedAt)) return DomainResults.Failure<VettingDecisionId>(timestampResult.Error!);
        var decisionResult = VettingDecision.Record(new VettingDecisionDetails(
            vettingCase.Id,
            vettingCase.SubjectAccountId,
            vettingCase.SubjectType,
            command.Outcome,
            reason,
            reviewer.UserAccountId,
            policyVersion,
            decidedAt));
        if (!decisionResult.TryGetValue(out var decision)) return DomainResults.Failure<VettingDecisionId>(decisionResult.Error!);

        var workItem = await workItems.FindActiveForCaseAsync(vettingCase.Id, cancellationToken);
        if (workItem is not null)
        {
            var validWorkItem = ValidateWorkItemDisposition(workItem, command.ActorStaffMemberId, command.Outcome, at);
            if (!validWorkItem.IsSuccess) return DomainResults.Failure<VettingDecisionId>(validWorkItem.Error!);
        }

        var applied = vettingCase.ApplyDecision(decision, at);
        if (!applied.IsSuccess) return DomainResults.Failure<VettingDecisionId>(applied.Error!);

        if (workItem is not null)
        {
            var disposition = ApplyWorkItemDisposition(workItem, command.Outcome, at);
            if (!disposition.IsSuccess) return DomainResults.Failure<VettingDecisionId>(disposition.Error!);
        }

        await OperationalAudit.AppendAndSaveAsync(
            audit,
            unitOfWork,
            "vetting.decision.recorded",
            "vetting_case",
            vettingCase.Id.Value.ToString("D"),
            command.ActorStaffMemberId,
            at,
            cancellationToken,
            $"outcome:{decision.Outcome};policy_version:{decision.PolicyVersion.Value}");

        return DomainResults.Success(decision.Id);
    }

    private static DomainResult ValidateWorkItemDisposition(
        WorkItem item,
        StaffMemberId reviewer,
        DecisionOutcome outcome,
        DateTimeOffset changedAt)
    {
        if (item.Type != WorkItemType.Vetting || item.AssignedTo != reviewer)
            return DomainResult.Failure(new("vetting.decision.work_item_owner_mismatch", "The linked vetting work item must remain assigned to the deciding reviewer."));
        if (item.Status is not (WorkItemStatus.InProgress or WorkItemStatus.WaitingForInformation))
            return DomainResult.Failure(new("vetting.decision.work_item.invalid_status", "The linked vetting work item is not active for this decision."));
        if (changedAt.Offset != TimeSpan.Zero || changedAt < item.StatusChangedAt)
            return DomainResult.Failure(new("vetting.decision.work_item.timestamp.invalid", "The decision time cannot precede the linked work item's latest change."));
        if (outcome is not (DecisionOutcome.Approved or DecisionOutcome.Rejected or DecisionOutcome.MoreInformationRequired or DecisionOutcome.Restricted))
            return DomainResult.Failure(new("vetting.decision.outcome.invalid", "The decision outcome is not supported."));
        return DomainResult.Success();
    }

    private static DomainResult ApplyWorkItemDisposition(WorkItem item, DecisionOutcome outcome, DateTimeOffset changedAt)
    {
        if (outcome is DecisionOutcome.Approved or DecisionOutcome.Rejected)
        {
            if (item.Status == WorkItemStatus.WaitingForInformation)
            {
                var resumed = item.ResumeAfterInformation(changedAt);
                if (!resumed.IsSuccess) return resumed;
            }
            return item.Complete(changedAt);
        }

        if (outcome == DecisionOutcome.MoreInformationRequired)
            return item.Status == WorkItemStatus.WaitingForInformation
                ? DomainResult.Success()
                : item.WaitForInformation(changedAt);

        // A Restricted decision leaves the task open and with its assigned reviewer for follow-up.
        return item.Status == WorkItemStatus.WaitingForInformation
            ? item.ResumeAfterInformation(changedAt)
            : DomainResult.Success();
    }
}
