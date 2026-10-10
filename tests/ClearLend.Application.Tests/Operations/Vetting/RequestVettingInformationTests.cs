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

public sealed class RequestVettingInformationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssignedReviewerRequestsEvidenceAndCaseWaitsWithSafeAudit()
    {
        var reviewer = ActiveReviewer();
        var caseFile = InReviewCase(reviewer);
        var cases = new VettingCaseRepository(caseFile);
        var staff = new StaffRepository(reviewer);
        var evidence = new EvidenceItemRepository();
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(cases, evidence, staff, audit, unitOfWork);
        const string reason = "Please provide a current proof of address for the review.";

        var result = await handler.Handle(new RequestVettingInformationCommand(
            caseFile.Id, [EvidenceType.Address, EvidenceType.Identity], reason, reviewer.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VettingCaseStatus.AwaitingInformation, caseFile.Status);
        var request = Assert.Single(caseFile.InformationRequestHistory);
        Assert.Equal(reviewer.UserAccountId, request.RequestedByAccountId);
        Assert.Equal(reason, request.Reason);
        Assert.Equal([EvidenceType.Address, EvidenceType.Identity], request.RequestedEvidenceTypes);
        Assert.Equal(2, evidence.Items.Count);
        Assert.All(evidence.Items, item =>
        {
            Assert.Equal(caseFile.Id, item.VettingCaseId);
            Assert.Equal(reviewer.UserAccountId, item.RequestedByAccountId);
            Assert.Equal(reason, item.RequestReason);
            Assert.Equal(EvidenceStatus.Requested, item.Status);
        });
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("vetting.information.requested", auditEvent.Action);
        Assert.DoesNotContain(reason, string.Join(" ", auditEvent.Details.Values));
        Assert.True(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsDifferentReviewerWithoutChangingCaseOrAddingEvidence()
    {
        var assignedReviewer = ActiveReviewer();
        var otherReviewer = ActiveReviewer();
        var caseFile = InReviewCase(assignedReviewer);
        var evidence = new EvidenceItemRepository();
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(new VettingCaseRepository(caseFile), evidence,
            new StaffRepository(assignedReviewer, otherReviewer), audit, unitOfWork);

        var result = await handler.Handle(new RequestVettingInformationCommand(
            caseFile.Id, [EvidenceType.Address], "Please provide proof of address.", otherReviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.information_request.reviewer_mismatch", result.Error?.Code);
        Assert.Equal(VettingCaseStatus.InReview, caseFile.Status);
        Assert.Empty(caseFile.InformationRequestHistory);
        Assert.Empty(evidence.Items);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsInactiveReviewer()
    {
        var reviewer = InvitedReviewer();
        var caseFile = InReviewCase(reviewer);
        var evidence = new EvidenceItemRepository();
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(new VettingCaseRepository(caseFile), evidence,
            new StaffRepository(reviewer), audit, unitOfWork);

        var result = await handler.Handle(new RequestVettingInformationCommand(
            caseFile.Id, [EvidenceType.Address], "Please provide proof of address.", reviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer.inactive", result.Error?.Code);
        Assert.Empty(evidence.Items);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task RejectsInformationRequestOutsideActiveReview()
    {
        var reviewer = ActiveReviewer();
        var caseFile = CaseAssignedTo(reviewer);
        var evidence = new EvidenceItemRepository();
        var audit = new AuditWriter();
        var unitOfWork = new UnitOfWork();
        var handler = CreateHandler(new VettingCaseRepository(caseFile), evidence,
            new StaffRepository(reviewer), audit, unitOfWork);

        var result = await handler.Handle(new RequestVettingInformationCommand(
            caseFile.Id, [EvidenceType.Address], "Please provide proof of address.", reviewer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.information_request.invalid_status", result.Error?.Code);
        Assert.Empty(evidence.Items);
        Assert.Empty(audit.Events);
        Assert.False(unitOfWork.WasSaved);
    }

    [Fact]
    public async Task AuthorizationPipelineRejectsAnUntrustedActor()
    {
        var command = new RequestVettingInformationCommand(
            VettingCaseId.New(), [EvidenceType.Address], "Please provide proof of address.", new StaffMemberId(Guid.NewGuid()));
        var services = new ServiceCollection();
        services.AddClearLendApplication();
        services.AddSingleton<ICurrentStaffActor>(new CurrentStaffActor(new StaffMemberId(Guid.NewGuid())));
        using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ISender>().Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("authorization.forbidden", result.Error?.Code);
    }

    private static RequestVettingInformationHandler CreateHandler(
        VettingCaseRepository cases,
        EvidenceItemRepository evidence,
        StaffRepository staff,
        AuditWriter audit,
        UnitOfWork unitOfWork) =>
        new(cases, evidence, staff, audit, unitOfWork, new FixedTimeProvider(Now));

    private static VettingCase InReviewCase(StaffMember reviewer)
    {
        var caseFile = CaseAssignedTo(reviewer);
        Assert.True(caseFile.StartReview(reviewer.UserAccountId, Now.AddMinutes(-2)).IsSuccess);
        return caseFile;
    }

    private static VettingCase CaseAssignedTo(StaffMember reviewer)
    {
        var caseFile = Get(VettingCase.Open(new VettingCaseOpening(
            UserAccountId.New(), VettingSubjectType.Borrower, Now.AddMinutes(-5))));
        Assert.True(caseFile.AssignReviewer(new ReviewerAssignment(
            reviewer.UserAccountId, UserAccountId.New(), Now.AddMinutes(-4))).IsSuccess);
        return caseFile;
    }

    private static StaffMember ActiveReviewer()
    {
        var member = Get(StaffMember.Invite(UserAccountId.New(), [StaffRole.ComplianceReviewer], Now.AddMinutes(-10)));
        Assert.True(member.Activate(Now.AddMinutes(-9)).IsSuccess);
        return member;
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

    private sealed class EvidenceItemRepository : IEvidenceItemRepository
    {
        public List<EvidenceItem> Items { get; } = [];
        public Task AddAsync(EvidenceItem evidenceItem, CancellationToken cancellationToken)
        { Items.Add(evidenceItem); return Task.CompletedTask; }
        public Task<EvidenceItem?> GetAsync(EvidenceItemId id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
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
