using ClearLend.Application.Abstractions;
using ClearLend.Application.Behaviors;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;
using FluentValidation;
using MediatR;

namespace ClearLend.Application.Operations.Vetting;

public sealed record RequestVettingInformationCommand(
    VettingCaseId VettingCaseId,
    IReadOnlyCollection<EvidenceType> RequestedEvidenceTypes,
    string Reason,
    StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest
{
    public PermissionCode RequiredPermission => PermissionCode.ReviewCompliance;
}

public sealed class RequestVettingInformationCommandValidator : AbstractValidator<RequestVettingInformationCommand>
{
    public RequestVettingInformationCommandValidator()
    {
        RuleFor(x => x.VettingCaseId.Value).NotEmpty();
        RuleFor(x => x.RequestedEvidenceTypes)
            .NotNull()
            .Must(types => types.Count > 0 && types.Distinct().Count() == types.Count)
            .WithMessage("At least one unique evidence type must be requested.");
        RuleForEach(x => x.RequestedEvidenceTypes).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty();
    }
}

public sealed class RequestVettingInformationHandler(
    IVettingCaseRepository cases,
    IEvidenceItemRepository evidenceItems,
    IStaffMemberRepository staff,
    IAuditEventWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null,
    IVettingWorkItemRepository? workItems = null)
    : IRequestHandler<RequestVettingInformationCommand, DomainResult>
{
    public async Task<DomainResult> Handle(RequestVettingInformationCommand command, CancellationToken cancellationToken)
    {
        var vettingCase = await cases.GetAsync(command.VettingCaseId, cancellationToken);
        if (vettingCase is null)
            return DomainResult.Failure(new("vetting.case.not_found", "The vetting case was not found."));

        var reviewer = await staff.GetAsync(command.ActorStaffMemberId, cancellationToken);
        if (reviewer is null || reviewer.Status != StaffStatus.Active)
            return DomainResult.Failure(new("vetting.reviewer.inactive", "Only an active staff member can request information."));
        if (!reviewer.Roles.Contains(StaffRole.ComplianceReviewer))
            return DomainResult.Failure(new("vetting.reviewer.role_ineligible", "The assigned reviewer must have the Compliance Reviewer role."));
        if (vettingCase.ReviewerAccountId != reviewer.UserAccountId)
            return DomainResult.Failure(new("vetting.information_request.reviewer_mismatch", "Only the assigned reviewer can request information."));

        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var linkedWorkItem = workItems is null
            ? null
            : await workItems.FindActiveForCaseAsync(vettingCase.Id, cancellationToken);
        if (linkedWorkItem is not null &&
            (linkedWorkItem.Type != WorkItemType.Vetting ||
             linkedWorkItem.AssignedTo != reviewer.Id ||
             linkedWorkItem.Status != WorkItemStatus.InProgress ||
             at.Offset != TimeSpan.Zero || at < linkedWorkItem.StatusChangedAt))
            return DomainResult.Failure(new("vetting.information_request.work_item_mismatch", "The linked vetting work item must be in progress and assigned to the requesting reviewer."));

        var requestResult = VettingInformationRequest.Create(
            vettingCase.Id,
            reviewer.UserAccountId,
            command.RequestedEvidenceTypes,
            command.Reason,
            at);
        if (!requestResult.TryGetValue(out var request))
            return DomainResult.Failure(requestResult.Error!);

        var newEvidenceItems = new List<EvidenceItem>(request.RequestedEvidenceTypes.Count);
        foreach (var evidenceType in request.RequestedEvidenceTypes)
        {
            var evidenceResult = EvidenceItem.Request(new EvidenceRequest(
                vettingCase.Id,
                evidenceType,
                reviewer.UserAccountId,
                request.Reason,
                at));
            if (!evidenceResult.TryGetValue(out var evidenceItem))
                return DomainResult.Failure(evidenceResult.Error!);
            newEvidenceItems.Add(evidenceItem);
        }

        var transition = vettingCase.RequestInformation(request);
        if (!transition.IsSuccess) return transition;

        if (linkedWorkItem is not null)
        {
            var waiting = linkedWorkItem.WaitForInformation(at);
            if (!waiting.IsSuccess) return waiting;
        }

        foreach (var evidenceItem in newEvidenceItems)
            await evidenceItems.AddAsync(evidenceItem, cancellationToken);

        var requestedTypes = string.Join(",", request.RequestedEvidenceTypes);
        await OperationalAudit.AppendAndSaveAsync(
            audit,
            unitOfWork,
            "vetting.information.requested",
            "vetting_case",
            vettingCase.Id.Value.ToString("D"),
            command.ActorStaffMemberId,
            at,
            cancellationToken,
            $"evidence_types:{requestedTypes}");

        return DomainResult.Success();
    }
}
