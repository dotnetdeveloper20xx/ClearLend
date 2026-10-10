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

public sealed class StartOrResumeVettingReviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssignedReviewerCanStartOpenCaseAndAuditActivity()
    {
        var reviewer = ActiveReviewer();
        var caseFile = CaseAssignedTo(reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(caseFile, reviewer, audit, unitOfWork);

        var result = await handler.Handle(Command(caseFile, reviewer), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VettingCaseStatus.InReview, caseFile.Status);
        Assert.Equal(VettingReviewAction.Started, Assert.Single(caseFile.ReviewActivityHistory).Action);
        Assert.Equal("vetting.review.started", Assert.Single(audit.Events).Action);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task AssignedReviewerCanResumeCaseAwaitingInformation()
    {
        var reviewer = ActiveReviewer();
        var caseFile = CaseAssignedTo(reviewer);
        Assert.True(caseFile.StartReview(reviewer.UserAccountId, Now.AddMinutes(-3)).IsSuccess);
        var informationRequest = Get(VettingInformationRequest.Create(
            caseFile.Id, reviewer.UserAccountId, [EvidenceType.Address], "Please provide proof of address.", Now.AddMinutes(-2)));
        Assert.True(caseFile.RequestInformation(informationRequest).IsSuccess);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateHandler(caseFile, reviewer, audit, unitOfWork)
            .Handle(Command(caseFile, reviewer), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VettingReviewAction.ResumedAfterInformation, Assert.Single(caseFile.ReviewActivityHistory).Action);
        Assert.Equal("vetting.review.resumed", Assert.Single(audit.Events).Action);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task AssignedReviewerCanResumeSuspendedCaseExplicitly()
    {
        var reviewer = ActiveReviewer();
        var caseFile = CaseAssignedTo(reviewer);
        Assert.True(caseFile.Suspend(Now.AddMinutes(-3)).IsSuccess);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateHandler(caseFile, reviewer, audit, unitOfWork)
            .Handle(Command(caseFile, reviewer), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VettingReviewAction.ResumedAfterSuspension, Assert.Single(caseFile.ReviewActivityHistory).Action);
        Assert.Equal("vetting.review.resumed_from_suspension", Assert.Single(audit.Events).Action);
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsDifferentReviewerWithoutChangingCase()
    {
        var assignedReviewer = ActiveReviewer();
        var otherReviewer = ActiveReviewer();
        var caseFile = CaseAssignedTo(assignedReviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateHandler(caseFile, otherReviewer, audit, unitOfWork)
            .Handle(Command(caseFile, otherReviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.review.reviewer_mismatch", result.Error?.Code);
        Assert.Equal(VettingCaseStatus.Open, caseFile.Status);
        Assert.Empty(caseFile.ReviewActivityHistory);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsInactiveAssignedReviewer()
    {
        var reviewer = InvitedReviewer();
        var caseFile = CaseAssignedTo(reviewer);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateHandler(caseFile, reviewer, audit, unitOfWork)
            .Handle(Command(caseFile, reviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer.inactive", result.Error?.Code);
        Assert.Equal(VettingCaseStatus.Open, caseFile.Status);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsNonReviewerRole()
    {
        var staff = ActiveStaff(StaffRole.OperationsStaff);
        var caseFile = CaseAssignedTo(staff);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateHandler(caseFile, staff, audit, unitOfWork)
            .Handle(Command(caseFile, staff), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer.role_ineligible", result.Error?.Code);
        Assert.Empty(caseFile.ReviewActivityHistory);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsInvalidLifecycleState()
    {
        var reviewer = ActiveReviewer();
        var caseFile = CaseAssignedTo(reviewer);
        Assert.True(caseFile.StartReview(reviewer.UserAccountId, Now.AddMinutes(-2)).IsSuccess);
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();

        var result = await CreateHandler(caseFile, reviewer, audit, unitOfWork)
            .Handle(Command(caseFile, reviewer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.review.transition.invalid_status", result.Error?.Code);
        Assert.Single(caseFile.ReviewActivityHistory);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task AuthorizationPipelineRejectsUntrustedOrDifferentActor()
    {
        var caseFile = CaseAssignedTo(ActiveReviewer());
        var commandActor = new StaffMemberId(Guid.NewGuid());
        var services = new ServiceCollection();
        services.AddClearLendApplication();
        services.AddSingleton<ICurrentStaffActor>(new CurrentStaffActor(new StaffMemberId(Guid.NewGuid())));
        using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ISender>()
            .Send(new StartOrResumeVettingReviewCommand(caseFile.Id, commandActor));

        Assert.False(result.IsSuccess);
        Assert.Equal("authorization.forbidden", result.Error?.Code);
        Assert.Equal(VettingCaseStatus.Open, caseFile.Status);
    }

    private static StartOrResumeVettingReviewHandler CreateHandler(
        VettingCase caseFile,
        StaffMember reviewer,
        AuditWriter audit,
        UnitOfWork unitOfWork) =>
        new(new VettingCaseRepository(caseFile), new StaffRepository(reviewer), audit, unitOfWork, new FixedTimeProvider(Now));

    private static StartOrResumeVettingReviewCommand Command(VettingCase caseFile, StaffMember actor) =>
        new(caseFile.Id, actor.Id);

    private static VettingCase CaseAssignedTo(StaffMember reviewer)
    {
        var caseFile = Get(VettingCase.Open(
            new VettingCaseOpening(UserAccountId.New(), VettingSubjectType.Borrower, Now.AddMinutes(-5))));
        Assert.True(caseFile.AssignReviewer(new ReviewerAssignment(
            reviewer.UserAccountId, UserAccountId.New(), Now.AddMinutes(-4))).IsSuccess);
        return caseFile;
    }

    private static StaffMember ActiveReviewer() => ActiveStaff(StaffRole.ComplianceReviewer);

    private static StaffMember ActiveStaff(StaffRole role)
    {
        var staff = Get(StaffMember.Invite(UserAccountId.New(), [role], Now.AddMinutes(-10)));
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
