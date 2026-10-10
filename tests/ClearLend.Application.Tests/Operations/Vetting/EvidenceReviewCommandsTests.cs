using ClearLend.Application;
using ClearLend.Application.Abstractions;
using ClearLend.Application.Operations.Vetting;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ClearLend.Application.Tests.Operations.Vetting;

public sealed class EvidenceReviewCommandsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssignedReviewerCanAcceptSubmittedEvidenceWithoutApprovingCase()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var evidenceItem = SubmittedEvidence(caseFile.Id, reviewer.UserAccountId);
        var evidence = new EvidenceItemRepository(evidenceItem);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateAcceptHandler(caseFile, evidence, [reviewer], audit, unitOfWork)
            .Handle(new AcceptEvidenceCommand(caseFile.Id, evidenceItem.Id, reviewer.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EvidenceStatus.Accepted, evidenceItem.Status);
        Assert.Equal(reviewer.UserAccountId, evidenceItem.ReviewedByAccountId);
        Assert.Equal(VettingCaseStatus.InReview, caseFile.Status);
        Assert.Equal("evidence.accepted", Assert.Single(audit.Events).Action);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task AssignedReviewerCanRejectWithReasonWithoutLeakingItToAudit()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var evidenceItem = SubmittedEvidence(caseFile.Id, reviewer.UserAccountId);
        var evidence = new EvidenceItemRepository(evidenceItem);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        const string reason = "The document is unreadable; please submit a clear image.";

        var result = await CreateRejectHandler(caseFile, evidence, [reviewer], audit, unitOfWork)
            .Handle(new RejectEvidenceCommand(caseFile.Id, evidenceItem.Id, reason, reviewer.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EvidenceStatus.Rejected, evidenceItem.Status);
        Assert.Equal(reason, evidenceItem.RejectionReason);
        Assert.Equal(reviewer.UserAccountId, evidenceItem.ReviewedByAccountId);
        Assert.Equal(VettingCaseStatus.InReview, caseFile.Status);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("evidence.rejected", auditEvent.Action);
        Assert.DoesNotContain(reason, string.Join(" ", auditEvent.Details.Values));
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsEvidenceBelongingToDifferentCase()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var otherCase = InReviewCase(reviewer);
        var evidenceItem = SubmittedEvidence(caseFile.Id, reviewer.UserAccountId);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateAcceptHandler(caseFile, new EvidenceItemRepository(evidenceItem), [reviewer], audit, unitOfWork)
            .Handle(new AcceptEvidenceCommand(otherCase.Id, evidenceItem.Id, reviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.case_mismatch", result.Error?.Code);
        Assert.Equal(EvidenceStatus.Submitted, evidenceItem.Status);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsReviewerWhoIsNotAssignedToTheCase()
    {
        var assignedReviewer = ActiveReviewer();
        var otherReviewer = ActiveReviewer();
        var caseFile = InReviewCase(assignedReviewer);
        var evidenceItem = SubmittedEvidence(caseFile.Id, assignedReviewer.UserAccountId);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateAcceptHandler(caseFile, new EvidenceItemRepository(evidenceItem), [assignedReviewer, otherReviewer], audit, unitOfWork)
            .Handle(new AcceptEvidenceCommand(caseFile.Id, evidenceItem.Id, otherReviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.review.reviewer_mismatch", result.Error?.Code);
        Assert.Equal(EvidenceStatus.Submitted, evidenceItem.Status);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsInactiveReviewer()
    {
        var reviewer = InvitedReviewer();
        var caseFile = InReviewCase(reviewer);
        var evidenceItem = SubmittedEvidence(caseFile.Id, reviewer.UserAccountId);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateAcceptHandler(caseFile, new EvidenceItemRepository(evidenceItem), [reviewer], audit, unitOfWork)
            .Handle(new AcceptEvidenceCommand(caseFile.Id, evidenceItem.Id, reviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer.inactive", result.Error?.Code);
        Assert.Equal(EvidenceStatus.Submitted, evidenceItem.Status);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsEvidenceThatHasNotBeenSubmitted()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var evidenceItem = RequestedEvidence(caseFile.Id, reviewer.UserAccountId);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateAcceptHandler(caseFile, new EvidenceItemRepository(evidenceItem), [reviewer], audit, unitOfWork)
            .Handle(new AcceptEvidenceCommand(caseFile.Id, evidenceItem.Id, reviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.review.invalid_status", result.Error?.Code);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectEvidenceRequiresAReason()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var evidenceItem = SubmittedEvidence(caseFile.Id, reviewer.UserAccountId);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateRejectHandler(caseFile, new EvidenceItemRepository(evidenceItem), [reviewer], audit, unitOfWork)
            .Handle(new RejectEvidenceCommand(caseFile.Id, evidenceItem.Id, " ", reviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.rejection_reason.required", result.Error?.Code);
        Assert.Equal(EvidenceStatus.Submitted, evidenceItem.Status);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task AuthorizationPipelineRejectsUntrustedEvidenceReviewer()
    {
        var command = new AcceptEvidenceCommand(
            VettingCaseId.New(), EvidenceItemId.New(), new StaffMemberId(Guid.NewGuid()));
        var services = new ServiceCollection();
        services.AddClearLendApplication();
        services.AddSingleton<ICurrentStaffActor>(new CurrentStaffActor(new StaffMemberId(Guid.NewGuid())));
        using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ISender>().Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("authorization.forbidden", result.Error?.Code);
    }

    private static AcceptEvidenceHandler CreateAcceptHandler(
        VettingCase caseFile, EvidenceItemRepository evidence, StaffMember[] staff, AuditWriter audit, UnitOfWork unitOfWork) =>
        new(new VettingCaseRepository(caseFile), evidence, new StaffRepository(staff), audit, unitOfWork, new FixedTimeProvider(Now));

    private static RejectEvidenceHandler CreateRejectHandler(
        VettingCase caseFile, EvidenceItemRepository evidence, StaffMember[] staff, AuditWriter audit, UnitOfWork unitOfWork) =>
        new(new VettingCaseRepository(caseFile), evidence, new StaffRepository(staff), audit, unitOfWork, new FixedTimeProvider(Now));

    private static VettingCase InReviewCase(StaffMember reviewer)
    {
        var caseFile = Get(VettingCase.Open(new VettingCaseOpening(
            UserAccountId.New(), VettingSubjectType.Borrower, Now.AddMinutes(-8))));
        Assert.True(caseFile.AssignReviewer(new ReviewerAssignment(
            reviewer.UserAccountId, UserAccountId.New(), Now.AddMinutes(-7))).IsSuccess);
        Assert.True(caseFile.StartReview(reviewer.UserAccountId, Now.AddMinutes(-6)).IsSuccess);
        return caseFile;
    }

    private static EvidenceItem RequestedEvidence(VettingCaseId caseId, UserAccountId requester) =>
        Get(EvidenceItem.Request(new EvidenceRequest(
            caseId, EvidenceType.Identity, requester, "Please provide proof of identity.", Now.AddMinutes(-5))));

    private static EvidenceItem SubmittedEvidence(VettingCaseId caseId, UserAccountId requester)
    {
        var evidence = RequestedEvidence(caseId, requester);
        var reference = Get(StorageReference.Create("opaque-evidence-reference"));
        Assert.True(evidence.Submit(new EvidenceSubmission(reference, Now.AddMinutes(-4))).IsSuccess);
        return evidence;
    }

    private static StaffMember ActiveReviewer()
    {
        var staff = Get(StaffMember.Invite(UserAccountId.New(), [StaffRole.ComplianceReviewer], Now.AddMinutes(-10)));
        Assert.True(staff.Activate(Now.AddMinutes(-9)).IsSuccess);
        return staff;
    }

    private static StaffMember InvitedReviewer() =>
        Get(StaffMember.Invite(UserAccountId.New(), [StaffRole.ComplianceReviewer], Now.AddMinutes(-10)));

    private static T Get<T>(DomainResult<T> result)
    {
        Assert.True(result.TryGetValue(out var value), result.Error?.Message);
        return value!;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class VettingCaseRepository(VettingCase caseFile) : IVettingCaseRepository
    {
        public Task<VettingCase?> GetAsync(VettingCaseId id, CancellationToken cancellationToken) =>
            Task.FromResult<VettingCase?>(caseFile.Id == id ? caseFile : null);
    }

    private sealed class EvidenceItemRepository(params EvidenceItem[] items) : IEvidenceItemRepository
    {
        private readonly List<EvidenceItem> evidenceItems = [.. items];
        public Task AddAsync(EvidenceItem evidenceItem, CancellationToken cancellationToken)
        { evidenceItems.Add(evidenceItem); return Task.CompletedTask; }
        public Task<EvidenceItem?> GetAsync(EvidenceItemId id, CancellationToken cancellationToken) =>
            Task.FromResult(evidenceItems.SingleOrDefault(item => item.Id == id));
    }

    private sealed class StaffRepository(params StaffMember[] members) : IStaffMemberRepository
    {
        public Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<StaffMember?> GetAsync(StaffMemberId id, CancellationToken cancellationToken) =>
            Task.FromResult(members.SingleOrDefault(member => member.Id == id));
        public Task<int> CountActiveApplicationOwnersAsync(StaffMemberId? excluding, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<int> CountApplicationOwnersAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<bool> ExistsForAccountAsync(UserAccountId accountId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class AuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        { Events.Add(auditEvent); return Task.CompletedTask; }
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public bool WasSaved { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        { WasSaved = true; return Task.CompletedTask; }
    }

    private sealed class CurrentStaffActor(StaffMemberId staffMemberId) : ICurrentStaffActor
    {
        public StaffMemberId? StaffMemberId => staffMemberId;
    }
}
