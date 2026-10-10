using ClearLend.Application.Abstractions;
using ClearLend.Application.Behaviors;
using ClearLend.Domain.Common;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;
using FluentValidation;
using MediatR;

namespace ClearLend.Application.Operations.Vetting;

public sealed record AcceptEvidenceCommand(
    VettingCaseId VettingCaseId,
    EvidenceItemId EvidenceItemId,
    StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest
{
    public PermissionCode RequiredPermission => PermissionCode.ReviewCompliance;
}

public sealed record RejectEvidenceCommand(
    VettingCaseId VettingCaseId,
    EvidenceItemId EvidenceItemId,
    string Reason,
    StaffMemberId ActorStaffMemberId)
    : IRequest<DomainResult>, IAuthorizedRequest
{
    public PermissionCode RequiredPermission => PermissionCode.ReviewCompliance;
}

public sealed class AcceptEvidenceCommandValidator : AbstractValidator<AcceptEvidenceCommand>
{
    public AcceptEvidenceCommandValidator()
    {
        RuleFor(x => x.VettingCaseId.Value).NotEmpty();
        RuleFor(x => x.EvidenceItemId.Value).NotEmpty();
        RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty();
    }
}

public sealed class RejectEvidenceCommandValidator : AbstractValidator<RejectEvidenceCommand>
{
    public RejectEvidenceCommandValidator()
    {
        RuleFor(x => x.VettingCaseId.Value).NotEmpty();
        RuleFor(x => x.EvidenceItemId.Value).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ActorStaffMemberId.Value).NotEmpty();
    }
}

public sealed class AcceptEvidenceHandler(
    IVettingCaseRepository cases,
    IEvidenceItemRepository evidenceItems,
    IStaffMemberRepository staff,
    IAuditEventWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null)
    : IRequestHandler<AcceptEvidenceCommand, DomainResult>
{
    public async Task<DomainResult> Handle(AcceptEvidenceCommand command, CancellationToken cancellationToken)
    {
        var loaded = await EvidenceReviewLoader.LoadAsync(
            cases, evidenceItems, staff, command.VettingCaseId, command.EvidenceItemId,
            command.ActorStaffMemberId, cancellationToken);
        if (!loaded.TryGetValue(out var context)) return DomainResult.Failure(loaded.Error!);

        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var result = context.Evidence.Accept(context.Reviewer.UserAccountId, at);
        if (!result.IsSuccess) return result;

        await OperationalAudit.AppendAndSaveAsync(
            audit,
            unitOfWork,
            "evidence.accepted",
            "evidence_item",
            context.Evidence.Id.Value.ToString("D"),
            command.ActorStaffMemberId,
            at,
            cancellationToken,
            $"vetting_case_id:{context.Case.Id.Value:D}");
        return DomainResult.Success();
    }
}

public sealed class RejectEvidenceHandler(
    IVettingCaseRepository cases,
    IEvidenceItemRepository evidenceItems,
    IStaffMemberRepository staff,
    IAuditEventWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider? clock = null)
    : IRequestHandler<RejectEvidenceCommand, DomainResult>
{
    public async Task<DomainResult> Handle(RejectEvidenceCommand command, CancellationToken cancellationToken)
    {
        var loaded = await EvidenceReviewLoader.LoadAsync(
            cases, evidenceItems, staff, command.VettingCaseId, command.EvidenceItemId,
            command.ActorStaffMemberId, cancellationToken);
        if (!loaded.TryGetValue(out var context)) return DomainResult.Failure(loaded.Error!);

        var at = (clock ?? TimeProvider.System).GetUtcNow();
        var result = context.Evidence.Reject(new EvidenceRejection(
            command.Reason, context.Reviewer.UserAccountId, at));
        if (!result.IsSuccess) return result;

        await OperationalAudit.AppendAndSaveAsync(
            audit,
            unitOfWork,
            "evidence.rejected",
            "evidence_item",
            context.Evidence.Id.Value.ToString("D"),
            command.ActorStaffMemberId,
            at,
            cancellationToken,
            $"vetting_case_id:{context.Case.Id.Value:D}");
        return DomainResult.Success();
    }
}

internal sealed record EvidenceReviewContext(VettingCase Case, EvidenceItem Evidence, StaffMember Reviewer);

internal static class EvidenceReviewLoader
{
    public static async Task<DomainResult<EvidenceReviewContext>> LoadAsync(
        IVettingCaseRepository cases,
        IEvidenceItemRepository evidenceItems,
        IStaffMemberRepository staff,
        VettingCaseId caseId,
        EvidenceItemId evidenceId,
        StaffMemberId actorStaffMemberId,
        CancellationToken cancellationToken)
    {
        var evidence = await evidenceItems.GetAsync(evidenceId, cancellationToken);
        if (evidence is null)
            return DomainResults.Failure<EvidenceReviewContext>(new("evidence.not_found", "The evidence item was not found."));
        if (evidence.VettingCaseId != caseId)
            return DomainResults.Failure<EvidenceReviewContext>(new("evidence.case_mismatch", "The evidence item does not belong to the specified case."));

        var vettingCase = await cases.GetAsync(caseId, cancellationToken);
        if (vettingCase is null)
            return DomainResults.Failure<EvidenceReviewContext>(new("vetting.case.not_found", "The vetting case was not found."));
        if (vettingCase.Status != VettingCaseStatus.InReview)
            return DomainResults.Failure<EvidenceReviewContext>(new("evidence.review.case_not_in_review", "Evidence can only be reviewed while its case is in review."));

        var reviewer = await staff.GetAsync(actorStaffMemberId, cancellationToken);
        if (reviewer is null || reviewer.Status != StaffStatus.Active)
            return DomainResults.Failure<EvidenceReviewContext>(new("vetting.reviewer.inactive", "Only an active staff member can review evidence."));
        if (!reviewer.Roles.Contains(StaffRole.ComplianceReviewer))
            return DomainResults.Failure<EvidenceReviewContext>(new("vetting.reviewer.role_ineligible", "The reviewer must have the Compliance Reviewer role."));
        if (vettingCase.ReviewerAccountId != reviewer.UserAccountId)
            return DomainResults.Failure<EvidenceReviewContext>(new("vetting.review.reviewer_mismatch", "Only the assigned reviewer can review evidence for this case."));

        return DomainResults.Success(new EvidenceReviewContext(vettingCase, evidence, reviewer));
    }
}
