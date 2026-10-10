using ClearLend.Application.Abstractions;
using ClearLend.Application.Operations.Vetting;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;

namespace ClearLend.Application.Tests.Operations.Vetting;

public sealed class InternalReviewJourneyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OfficeJourneyKeepsCaseWorkAndAuditConsistentThroughInformationRequestAndDecision()
    {
        var subject = UserAccountId.New();
        var vettingCase = Get(VettingCase.Open(new VettingCaseOpening(subject, VettingSubjectType.Borrower, Now.AddHours(-1))));
        var operations = ActiveStaff(StaffRole.OperationsStaff);
        var reviewer = ActiveStaff(StaffRole.ComplianceReviewer);
        var workItem = Get(WorkItem.Open(WorkItemType.Vetting, vettingCase.Id.Value.ToString("D"), WorkItemPriority.Normal, Now.AddHours(-1)));
        var store = new InMemoryReviewStore(vettingCase, workItem, operations, reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var clock = new FixedTimeProvider(Now);

        Assert.True((await new AssignVettingReviewerHandler(store, store, audit, unitOfWork, clock, store)
            .Handle(new(vettingCase.Id, reviewer.Id, operations.Id), default)).IsSuccess);
        Assert.True((await new StartOrResumeVettingReviewHandler(store, store, audit, unitOfWork, clock, store)
            .Handle(new(vettingCase.Id, reviewer.Id), default)).IsSuccess);
        Assert.Equal(WorkItemStatus.InProgress, workItem.Status);
        Assert.Equal(reviewer.Id, workItem.AssignedTo);

        Assert.True((await new RequestVettingInformationHandler(store, store, store, audit, unitOfWork, clock, store)
            .Handle(new(vettingCase.Id, [EvidenceType.Address], "Please provide proof of address.", reviewer.Id), default)).IsSuccess);
        var evidence = Assert.Single(store.EvidenceItems);
        Assert.Equal(WorkItemStatus.WaitingForInformation, workItem.Status);
        Assert.Equal(reviewer.Id, workItem.AssignedTo);

        var storage = Get(StorageReference.Create("opaque-test-reference"));
        Assert.True(evidence.Submit(new EvidenceSubmission(storage, Now)).IsSuccess);
        Assert.True((await new StartOrResumeVettingReviewHandler(store, store, audit, unitOfWork, clock, store)
            .Handle(new(vettingCase.Id, reviewer.Id), default)).IsSuccess);
        Assert.Equal(WorkItemStatus.InProgress, workItem.Status);
        Assert.Equal(reviewer.Id, workItem.AssignedTo);

        Assert.True((await new AcceptEvidenceHandler(store, store, store, audit, unitOfWork, clock)
            .Handle(new(vettingCase.Id, evidence.Id, reviewer.Id), default)).IsSuccess);
        var decision = await new RecordVettingDecisionHandler(store, store, store, audit, unitOfWork, clock)
            .Handle(new(vettingCase.Id, DecisionOutcome.Approved, "Evidence satisfies the review requirements.", "2026.1", reviewer.Id), default);

        Assert.True(decision.IsSuccess);
        Assert.Equal(VettingCaseStatus.Approved, vettingCase.Status);
        Assert.Single(vettingCase.DecisionHistory);
        Assert.Equal(EvidenceStatus.Accepted, evidence.Status);
        Assert.Equal(WorkItemStatus.Completed, workItem.Status);
        Assert.Equal(6, unitOfWork.SaveCount);
        Assert.Equal(6, audit.Events.Count);
        Assert.Contains(audit.Events, e => e.Action == "vetting.information.requested");
        Assert.DoesNotContain(audit.Events.SelectMany(e => e.Details.Values), value => value.Contains("satisfies the review requirements", StringComparison.Ordinal));
    }

    private static StaffMember ActiveStaff(StaffRole role)
    {
        var member = Get(StaffMember.Invite(UserAccountId.New(), [role], Now.AddHours(-2)));
        Assert.True(member.Activate(Now.AddHours(-1)).IsSuccess);
        return member;
    }

    private static T Get<T>(DomainResult<T> result)
    {
        Assert.True(result.TryGetValue(out var value), result.Error?.Message);
        return value!;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InMemoryReviewStore(VettingCase caseFile, WorkItem item, params StaffMember[] staffMembers)
        : IVettingCaseRepository, IEvidenceItemRepository, IStaffMemberRepository, IVettingWorkItemRepository
    {
        private readonly List<EvidenceItem> evidenceItems = [];
        public IReadOnlyList<EvidenceItem> EvidenceItems => evidenceItems;
        public Task<VettingCase?> GetAsync(VettingCaseId id, CancellationToken cancellationToken) => Task.FromResult(caseFile.Id == id ? caseFile : null);
        public Task AddAsync(EvidenceItem evidenceItem, CancellationToken cancellationToken) { evidenceItems.Add(evidenceItem); return Task.CompletedTask; }
        Task<EvidenceItem?> IEvidenceItemRepository.GetAsync(EvidenceItemId id, CancellationToken cancellationToken) => Task.FromResult(evidenceItems.SingleOrDefault(x => x.Id == id));
        public Task<WorkItem?> FindActiveForCaseAsync(VettingCaseId id, CancellationToken cancellationToken) => Task.FromResult(id == caseFile.Id && item.Status is not (WorkItemStatus.Completed or WorkItemStatus.Cancelled) ? item : null);
        public Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<StaffMember?> GetAsync(StaffMemberId id, CancellationToken cancellationToken) => Task.FromResult(staffMembers.SingleOrDefault(x => x.Id == id));
        public Task<int> CountActiveApplicationOwnersAsync(StaffMemberId? excluding, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<int> CountApplicationOwnersAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<bool> ExistsForAccountAsync(UserAccountId accountId, CancellationToken cancellationToken) => Task.FromResult(staffMembers.Any(x => x.UserAccountId == accountId));
    }

    private sealed class AuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken) { Events.Add(auditEvent); return Task.CompletedTask; }
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
    }
}
