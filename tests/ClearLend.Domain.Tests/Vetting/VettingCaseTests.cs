using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Vetting;

namespace ClearLend.Domain.Tests.Vetting;

public sealed class VettingCaseTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OpenCreatesBorrowerCaseInOpenState()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);

        Assert.Equal(VettingCaseStatus.Open, vettingCase.Status);
        Assert.Equal(VettingSubjectType.Borrower, vettingCase.SubjectType);
        Assert.Null(vettingCase.ReviewerAccountId);
    }

    [Fact]
    public void ReviewerCanBeAssignedAndReviewCanStart()
    {
        var vettingCase = CreateCase(VettingSubjectType.Lender);
        var reviewer = UserAccountId.New();

        Assert.True(vettingCase.AssignReviewer(new ReviewerAssignment(reviewer, UserAccountId.New(), OpenedAt.AddMinutes(1))).IsSuccess);
        Assert.True(vettingCase.StartReview(reviewer, OpenedAt.AddMinutes(2)).IsSuccess);

        Assert.Equal(reviewer, vettingCase.ReviewerAccountId);
        Assert.Equal(VettingCaseStatus.InReview, vettingCase.Status);
    }

    [Fact]
    public void ReviewCanRequestInformationThenResumeBeforeApproval()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        var reviewer = StartReview(vettingCase, OpenedAt.AddMinutes(1));
        Assert.True(vettingCase.RequestInformation(CreateInformationRequest(vettingCase, reviewer, OpenedAt.AddMinutes(2))).IsSuccess);
        Assert.Single(vettingCase.InformationRequestHistory);

        var blocked = vettingCase.ApplyDecision(CreateDecision(vettingCase, reviewer, DecisionOutcome.Approved, OpenedAt.AddMinutes(3)), OpenedAt.AddMinutes(3));
        Assert.False(blocked.IsSuccess);
        Assert.Equal("vetting.decision.invalid_status", blocked.Error?.Code);

        Assert.True(vettingCase.ResumeReview(reviewer, OpenedAt.AddMinutes(4)).IsSuccess);
        Assert.True(vettingCase.ApplyDecision(CreateDecision(vettingCase, reviewer, DecisionOutcome.Approved, OpenedAt.AddMinutes(5)), OpenedAt.AddMinutes(5)).IsSuccess);
    }

    [Fact]
    public void ReviewCanBeApproved()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        var reviewer = StartReview(vettingCase, OpenedAt.AddMinutes(1));

        Assert.True(vettingCase.ApplyDecision(CreateDecision(vettingCase, reviewer, DecisionOutcome.Approved, OpenedAt.AddMinutes(1)), OpenedAt.AddMinutes(2)).IsSuccess);
        Assert.Equal(VettingCaseStatus.Approved, vettingCase.Status);
    }

    [Fact]
    public void CompletedCaseCannotBeReassigned()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        var reviewer = StartReview(vettingCase, OpenedAt.AddMinutes(1));
        Assert.True(vettingCase.ApplyDecision(CreateDecision(vettingCase, reviewer, DecisionOutcome.Rejected, OpenedAt.AddMinutes(1)), OpenedAt.AddMinutes(2)).IsSuccess);

        var result = vettingCase.AssignReviewer(new ReviewerAssignment(UserAccountId.New(), UserAccountId.New(), OpenedAt.AddMinutes(3)));

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer_assignment.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void ReviewerAssignmentHistoryRecordsAssignerAndSupportsPreReviewReassignment()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        var firstReviewer = UserAccountId.New();
        var firstAssigner = UserAccountId.New();
        var secondReviewer = UserAccountId.New();
        var secondAssigner = UserAccountId.New();

        Assert.True(vettingCase.AssignReviewer(new ReviewerAssignment(firstReviewer, firstAssigner, OpenedAt.AddMinutes(1))).IsSuccess);
        Assert.True(vettingCase.StartReview(firstReviewer, OpenedAt.AddMinutes(2)).IsSuccess);
        Assert.True(vettingCase.RequestInformation(CreateInformationRequest(vettingCase, firstReviewer, OpenedAt.AddMinutes(3))).IsSuccess);
        Assert.True(vettingCase.AssignReviewer(new ReviewerAssignment(secondReviewer, secondAssigner, OpenedAt.AddMinutes(4))).IsSuccess);

        Assert.Equal(secondReviewer, vettingCase.ReviewerAccountId);
        Assert.Collection(vettingCase.ReviewerAssignmentHistory,
            first => { Assert.Equal(firstReviewer, first.ReviewerAccountId); Assert.Equal(firstAssigner, first.AssignedByAccountId); },
            second => { Assert.Equal(secondReviewer, second.ReviewerAccountId); Assert.Equal(secondAssigner, second.AssignedByAccountId); });
    }

    [Fact]
    public void ReviewerCannotBeReassignedWhileReviewIsInProgress()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        StartReview(vettingCase, OpenedAt.AddMinutes(1));

        var result = vettingCase.AssignReviewer(new ReviewerAssignment(UserAccountId.New(), UserAccountId.New(), OpenedAt.AddMinutes(2)));

        Assert.False(result.IsSuccess);
        Assert.Equal("vetting.reviewer_assignment.invalid_status", result.Error?.Code);
        Assert.Single(vettingCase.ReviewerAssignmentHistory);
    }

    [Fact]
    public void ClosingCaseRecordsClosedAt()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        var closedAt = OpenedAt.AddMinutes(1);

        Assert.True(vettingCase.Close(closedAt).IsSuccess);

        Assert.Equal(VettingCaseStatus.Closed, vettingCase.Status);
        Assert.Equal(closedAt, vettingCase.ClosedAt);
    }

    [Fact]
    public void SuspendedCaseCanResumeReview()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        var reviewer = UserAccountId.New();
        Assert.True(vettingCase.AssignReviewer(new ReviewerAssignment(reviewer, UserAccountId.New(), OpenedAt.AddMinutes(1))).IsSuccess);
        Assert.True(vettingCase.Suspend(OpenedAt.AddMinutes(1)).IsSuccess);

        Assert.True(vettingCase.ResumeFromSuspension(reviewer, OpenedAt.AddMinutes(2)).IsSuccess);
        Assert.Equal(VettingCaseStatus.InReview, vettingCase.Status);
    }

    [Fact]
    public void OnlyAssignedReviewerCanStartAndSuccessfulTransitionIsRecorded()
    {
        var vettingCase = CreateCase(VettingSubjectType.Borrower);
        var reviewer = UserAccountId.New();
        Assert.True(vettingCase.AssignReviewer(new ReviewerAssignment(reviewer, UserAccountId.New(), OpenedAt.AddMinutes(1))).IsSuccess);

        var rejected = vettingCase.StartReview(UserAccountId.New(), OpenedAt.AddMinutes(2));

        Assert.False(rejected.IsSuccess);
        Assert.Equal("vetting.review.reviewer_mismatch", rejected.Error?.Code);
        Assert.Empty(vettingCase.ReviewActivityHistory);

        Assert.True(vettingCase.StartReview(reviewer, OpenedAt.AddMinutes(2)).IsSuccess);
        var activity = Assert.Single(vettingCase.ReviewActivityHistory);
        Assert.Equal(reviewer, activity.ReviewerAccountId);
        Assert.Equal(VettingReviewAction.Started, activity.Action);
        Assert.Equal(OpenedAt.AddMinutes(2), activity.OccurredAt);
    }

    private static VettingCase CreateCase(VettingSubjectType subjectType) =>
        TestResult.Get(VettingCase.Open(new VettingCaseOpening(UserAccountId.New(), subjectType, OpenedAt)));

    private static VettingInformationRequest CreateInformationRequest(VettingCase vettingCase, UserAccountId reviewer, DateTimeOffset requestedAt) =>
        TestResult.Get(VettingInformationRequest.Create(vettingCase.Id, reviewer, [EvidenceType.Address], "Please provide a current proof of address.", requestedAt));

    private static UserAccountId StartReview(VettingCase vettingCase, DateTimeOffset at)
    {
        var reviewer = UserAccountId.New();
        Assert.True(vettingCase.AssignReviewer(new ReviewerAssignment(reviewer, UserAccountId.New(), at)).IsSuccess);
        Assert.True(vettingCase.StartReview(reviewer, at).IsSuccess);
        return reviewer;
    }

    private static VettingDecision CreateDecision(VettingCase vettingCase, UserAccountId reviewer, DecisionOutcome outcome, DateTimeOffset decidedAt) => TestResult.Get(VettingDecision.Record(
        new VettingDecisionDetails(
            vettingCase.Id,
            vettingCase.SubjectAccountId,
            vettingCase.SubjectType,
            outcome,
            TestResult.Get(DecisionReason.Create("Reviewed.")),
            reviewer,
            TestResult.Get(PolicyVersion.Create("2026.1")),
            TestResult.Get(UtcTimestamp.Create(decidedAt)))));
}
