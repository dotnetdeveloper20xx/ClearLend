using ClearLend.Application.Abstractions;
using ClearLend.Application.Operations.Vetting;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Operations;
using ClearLend.Domain.Vetting;

namespace ClearLend.Application.Tests.Operations.Vetting;

public sealed class AssignVettingReviewerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssignsActiveComplianceReviewerAndAuditsTheAssignment()
    {
        var caseFile = OpenCase();
        var actor = ActiveStaff(StaffRole.OperationsStaff);
        var reviewer = ActiveStaff(StaffRole.ComplianceReviewer);
        var cases = new VettingCaseRepository(caseFile);
        var staff = new StaffRepository(actor, reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(cases, staff, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, actor, reviewer), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(reviewer.UserAccountId, caseFile.ReviewerAccountId);
        var assignment = Assert.Single(caseFile.ReviewerAssignmentHistory);
        Assert.Equal(actor.UserAccountId, assignment.AssignedByAccountId);
        Assert.Equal(Now, assignment.AssignedAt);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("vetting.reviewer_assigned", auditEvent.Action);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsInactiveReviewerWithoutChangingCaseOrSaving()
    {
        var caseFile = OpenCase();
        var actor = ActiveStaff(StaffRole.OperationsStaff);
        var reviewer = InvitedStaff(StaffRole.ComplianceReviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(new VettingCaseRepository(caseFile), new StaffRepository(actor, reviewer), audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, actor, reviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer.inactive", result.Error?.Code);
        Assert.Null(caseFile.ReviewerAccountId);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsStaffWithoutComplianceReviewerRole()
    {
        var caseFile = OpenCase();
        var actor = ActiveStaff(StaffRole.OperationsStaff);
        var reviewer = ActiveStaff(StaffRole.SupportAgent);
        var handler = CreateHandler(new VettingCaseRepository(caseFile), new StaffRepository(actor, reviewer), new AuditWriter(), new UnitOfWork());

        var result = await handler.Handle(Command(caseFile, actor, reviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer.role_ineligible", result.Error?.Code);
        Assert.Null(caseFile.ReviewerAccountId);
    }

    [Fact]
    public async Task RejectsReviewerWhoIsTheCaseSubject()
    {
        var caseFile = OpenCase();
        var actor = ActiveStaff(StaffRole.OperationsStaff);
        var reviewer = ActiveStaff(StaffRole.ComplianceReviewer, caseFile.SubjectAccountId);
        var handler = CreateHandler(new VettingCaseRepository(caseFile), new StaffRepository(actor, reviewer), new AuditWriter(), new UnitOfWork());

        var result = await handler.Handle(Command(caseFile, actor, reviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer_assignment.invalid", result.Error?.Code);
        Assert.Null(caseFile.ReviewerAccountId);
    }

    [Fact]
    public async Task RejectsMissingCaseWithoutLookingUpOrSavingChanges()
    {
        var actor = ActiveStaff(StaffRole.OperationsStaff);
        var reviewer = ActiveStaff(StaffRole.ComplianceReviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(new VettingCaseRepository(null), new StaffRepository(actor, reviewer), audit, unitOfWork);

        var result = await handler.Handle(Command(VettingCaseId.New(), actor, reviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.case.not_found", result.Error?.Code);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task DuplicateAssignmentIsRejectedWithoutAddingHistory()
    {
        var caseFile = OpenCase();
        var actor = ActiveStaff(StaffRole.OperationsStaff);
        var reviewer = ActiveStaff(StaffRole.ComplianceReviewer);
        Assert.True(caseFile.AssignReviewer(new ReviewerAssignment(reviewer.UserAccountId, actor.UserAccountId, Now.AddMinutes(-1))).IsSuccess);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(new VettingCaseRepository(caseFile), new StaffRepository(actor, reviewer), audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, actor, reviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer_assignment.unchanged", result.Error?.Code);
        Assert.Single(caseFile.ReviewerAssignmentHistory);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    private static AssignVettingReviewerHandler CreateHandler(
        VettingCaseRepository cases,
        StaffRepository staff,
        AuditWriter audit,
        UnitOfWork unitOfWork) =>
        new(cases, staff, audit, unitOfWork, new FixedTimeProvider(Now));

    private static AssignVettingReviewerCommand Command(VettingCase caseFile, StaffMember actor, StaffMember reviewer) =>
        new(caseFile.Id, reviewer.Id, actor.Id);

    private static AssignVettingReviewerCommand Command(VettingCaseId caseId, StaffMember actor, StaffMember reviewer) =>
        new(caseId, reviewer.Id, actor.Id);

    private static VettingCase OpenCase() => Get(VettingCase.Open(
        new VettingCaseOpening(UserAccountId.New(), VettingSubjectType.Borrower, Now.AddMinutes(-2))));

    private static StaffMember ActiveStaff(StaffRole role, UserAccountId? accountId = null)
    {
        var staff = Get(StaffMember.Invite(accountId ?? UserAccountId.New(), [role], Now.AddMinutes(-10)));
        Assert.True(staff.Activate(Now.AddMinutes(-9)).IsSuccess);
        return staff;
    }

    private static StaffMember InvitedStaff(StaffRole role) =>
        Get(StaffMember.Invite(UserAccountId.New(), [role], Now.AddMinutes(-10)));

    private static T Get<T>(DomainResult<T> result)
    {
        Assert.True(result.TryGetValue(out var value), result.Error?.Message);
        return value!;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class VettingCaseRepository(VettingCase? caseFile) : IVettingCaseRepository
    {
        public Task<VettingCase?> GetAsync(VettingCaseId id, CancellationToken cancellationToken) =>
            Task.FromResult(caseFile?.Id == id ? caseFile : null);
    }

    private sealed class StaffRepository(params StaffMember[] staffMembers) : IStaffMemberRepository
    {
        public Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<StaffMember?> GetAsync(StaffMemberId id, CancellationToken cancellationToken) =>
            Task.FromResult(staffMembers.SingleOrDefault(member => member.Id == id));
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
}
