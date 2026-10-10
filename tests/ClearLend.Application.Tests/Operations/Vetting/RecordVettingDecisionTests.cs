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

public sealed class RecordVettingDecisionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 14, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(DecisionOutcome.Approved, VettingCaseStatus.Approved, WorkItemStatus.Completed)]
    [InlineData(DecisionOutcome.Rejected, VettingCaseStatus.Rejected, WorkItemStatus.Completed)]
    [InlineData(DecisionOutcome.MoreInformationRequired, VettingCaseStatus.AwaitingInformation, WorkItemStatus.WaitingForInformation)]
    [InlineData(DecisionOutcome.Restricted, VettingCaseStatus.Suspended, WorkItemStatus.InProgress)]
    public async Task RecordsDecisionAndAppliesAgreedWorkItemPolicy(
        DecisionOutcome outcome,
        VettingCaseStatus expectedCaseStatus,
        WorkItemStatus expectedWorkItemStatus)
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var workItem = AssignedWorkItem(reviewer.Id);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, reviewer, workItem, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, reviewer, outcome), CancellationToken.None);

        Assert.True(result.TryGetValue(out var decisionId), result.Error?.Message);
        Assert.NotEqual(Guid.Empty, decisionId.Value);
        Assert.Equal(expectedCaseStatus, caseFile.Status);
        Assert.Equal(expectedWorkItemStatus, workItem.Status);
        Assert.Equal(reviewer.Id, workItem.AssignedTo);
        var decision = Assert.Single(caseFile.DecisionHistory);
        Assert.Equal(outcome, decision.Outcome);
        Assert.Equal("2026.1", decision.PolicyVersion.Value);
        Assert.Equal(reviewer.UserAccountId, decision.ReviewerAccountId);
        Assert.Equal("vetting.decision.recorded", Assert.Single(audit.Events).Action);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RecordsDecisionEvenWhenNoLinkedWorkItemExists()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, reviewer, null, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, reviewer, DecisionOutcome.Approved), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VettingCaseStatus.Approved, caseFile.Status);
        Assert.Single(caseFile.DecisionHistory);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsDifferentReviewerWithoutChangingCaseOrWorkItem()
    {
        var assignedReviewer = ActiveReviewer();
        var otherReviewer = ActiveReviewer();
        var caseFile = InReviewCase(assignedReviewer);
        var workItem = AssignedWorkItem(assignedReviewer.Id);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, otherReviewer, workItem, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, otherReviewer, DecisionOutcome.Approved), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.decision.reviewer_mismatch", result.Error?.Code);
        Assert.Equal(VettingCaseStatus.InReview, caseFile.Status);
        Assert.Empty(caseFile.DecisionHistory);
        Assert.Equal(WorkItemStatus.InProgress, workItem.Status);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsWorkItemOwnedByAnotherStaffMemberBeforeApplyingDecision()
    {
        var reviewer = ActiveReviewer();
        var otherOwner = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var workItem = AssignedWorkItem(otherOwner.Id);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, reviewer, workItem, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, reviewer, DecisionOutcome.Approved), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.decision.work_item_owner_mismatch", result.Error?.Code);
        Assert.Equal(VettingCaseStatus.InReview, caseFile.Status);
        Assert.Empty(caseFile.DecisionHistory);
        Assert.Equal(WorkItemStatus.InProgress, workItem.Status);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsDecisionWhenCaseIsNotInReview()
    {
        var reviewer = ActiveReviewer();
        var caseFile = AssignedCase(reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, reviewer, null, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, reviewer, DecisionOutcome.Approved), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.decision.invalid_status", result.Error?.Code);
        Assert.Empty(caseFile.DecisionHistory);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsInactiveReviewer()
    {
        var reviewer = InvitedReviewer();
        var caseFile = InReviewCase(reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, reviewer, null, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, reviewer, DecisionOutcome.Approved), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer.inactive", result.Error?.Code);
        Assert.Empty(caseFile.DecisionHistory);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsMissingDecisionReason()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, reviewer, null, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, reviewer, DecisionOutcome.Approved) with { Reason = " " }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.decision_reason.required", result.Error?.Code);
        Assert.Empty(caseFile.DecisionHistory);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task AuthorizationPipelineRejectsUntrustedDecisionActor()
    {
        var command = new RecordVettingDecisionCommand(
            VettingCaseId.New(), DecisionOutcome.Approved, "Review passed.", "2026.1", new StaffMemberId(Guid.NewGuid()));
        var services = new ServiceCollection();
        services.AddClearLendApplication();
        services.AddSingleton<ICurrentStaffActor>(new CurrentStaffActor(new StaffMemberId(Guid.NewGuid())));
        using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ISender>().Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("authorization.forbidden", result.Error?.Code);
    }

    private static RecordVettingDecisionHandler CreateHandler(
        VettingCase caseFile, StaffMember reviewer, WorkItem? workItem, AuditWriter audit, UnitOfWork unitOfWork) =>
        new(new VettingCaseRepository(caseFile), new StaffRepository(reviewer),
            new VettingWorkItemRepository(workItem), audit, unitOfWork, new FixedTimeProvider(Now));

    private static RecordVettingDecisionCommand Command(
        VettingCase caseFile, StaffMember reviewer, DecisionOutcome outcome) =>
        new(caseFile.Id, outcome, "The review has been completed against the applicable policy.", "2026.1", reviewer.Id);

    private static VettingCase InReviewCase(StaffMember reviewer)
    {
        var caseFile = AssignedCase(reviewer);
        Assert.True(caseFile.StartReview(reviewer.UserAccountId, Now.AddMinutes(-2)).IsSuccess);
        return caseFile;
    }

    private static VettingCase AssignedCase(StaffMember reviewer)
    {
        var caseFile = Get(VettingCase.Open(new VettingCaseOpening(
            UserAccountId.New(), VettingSubjectType.Borrower, Now.AddMinutes(-5))));
        Assert.True(caseFile.AssignReviewer(new ReviewerAssignment(
            reviewer.UserAccountId, UserAccountId.New(), Now.AddMinutes(-4))).IsSuccess);
        return caseFile;
    }

    private static WorkItem AssignedWorkItem(StaffMemberId owner)
    {
        var item = Get(WorkItem.Open(WorkItemType.Vetting, "synthetic-case-reference", WorkItemPriority.Normal, Now.AddMinutes(-6)));
        Assert.True(item.AssignTo(owner, Now.AddMinutes(-3)).IsSuccess);
        return item;
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

    private sealed class StaffRepository(StaffMember member) : IStaffMemberRepository
    {
        public Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<StaffMember?> GetAsync(StaffMemberId id, CancellationToken cancellationToken) =>
            Task.FromResult<StaffMember?>(member.Id == id ? member : null);
        public Task<int> CountActiveApplicationOwnersAsync(StaffMemberId? excluding, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<int> CountApplicationOwnersAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<bool> ExistsForAccountAsync(UserAccountId accountId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class VettingWorkItemRepository(WorkItem? item) : IVettingWorkItemRepository
    {
        public Task<WorkItem?> FindActiveForCaseAsync(VettingCaseId vettingCaseId, CancellationToken cancellationToken) => Task.FromResult(item);
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
