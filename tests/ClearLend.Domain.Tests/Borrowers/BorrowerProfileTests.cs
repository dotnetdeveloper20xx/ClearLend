using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Vetting;

namespace ClearLend.Domain.Tests.Borrowers;

public sealed class BorrowerProfileTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateStartsIncomplete()
    {
        var profile = CreateProfile();

        Assert.Equal(ProfileState.Incomplete, profile.State);
        Assert.Null(profile.Name);
    }

    [Fact]
    public void CanRecordConsentOnBorrowerProfile()
    {
        var profile = CreateProfile();
        var consent = TestResult.Get(ConsentRecord.Record(ConsentStatus.Granted, "v1", CreatedAt));

        var result = profile.RecordConsent(consent);

        Assert.True(result.IsSuccess);
        Assert.Same(consent, profile.LatestConsent);
    }

    [Fact]
    public void CompletionRequiresAnIncompleteProfile()
    {
        var profile = CreateProfile();
        var name = TestResult.Get(PersonalName.Create("Aisha", "Khan"));
        Assert.True(profile.Complete(name, CreatedAt.AddMinutes(1)).IsSuccess);

        var result = profile.Complete(name, CreatedAt.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Equal("borrower.profile.complete.invalid_state", result.Error?.Code);
    }

    [Fact]
    public void CreationRequiresAUserAccount()
    {
        var result = BorrowerProfile.Create(new BorrowerProfileCreation(
            default,
            CreatedAt,
            TestResult.Get(ConsentRecord.Record(ConsentStatus.Declined, "v1", CreatedAt))));

        Assert.False(result.IsSuccess);
        Assert.Equal("borrower.user_account.required", result.Error?.Code);
    }


    [Fact]
    public void CompleteAndSubmitForReviewMovesProfileToReadyForReview()
    {
        var profile = CreateProfile();
        var name = TestResult.Get(PersonalName.Create("Aisha", "Khan"));

        Assert.True(profile.Complete(name, CreatedAt.AddMinutes(1)).IsSuccess);
        Assert.True(profile.SubmitForReview(CreatedAt.AddMinutes(1)).IsSuccess);
        Assert.Equal(ProfileState.ReadyForReview, profile.State);
    }

    [Fact]
    public void CannotSubmitIncompleteProfile()
    {
        var result = CreateProfile().SubmitForReview(CreatedAt.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("borrower.profile.submit.invalid_state", result.Error?.Code);
    }

    [Fact]
    public void ActivationRequiresReadyForReviewState()
    {
        var profile = CreateProfile();
        var result = profile.Activate(CreateDecision(profile.UserAccountId, DecisionOutcome.Rejected), CreatedAt.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("borrower.profile.activate.invalid_decision", result.Error?.Code);
    }

    [Fact]
    public void ApprovedLifecycleCanActivateSuspendAndCloseProfile()
    {
        var profile = CreateProfile();
        var decision = CreateDecision(profile.UserAccountId, DecisionOutcome.Approved);
        Assert.True(profile.Complete(TestResult.Get(PersonalName.Create("Aisha", "Khan")), CreatedAt.AddMinutes(1)).IsSuccess);
        Assert.True(profile.SubmitForReview(CreatedAt.AddMinutes(1)).IsSuccess);
        Assert.True(profile.Activate(decision, CreatedAt.AddMinutes(2)).IsSuccess);
        Assert.True(profile.Suspend(CreatedAt.AddMinutes(3)).IsSuccess);
        Assert.True(profile.Close(CreatedAt.AddMinutes(4)).IsSuccess);

        Assert.Equal(ProfileState.Closed, profile.State);
    }

    [Fact]
    public void PersonalNameRequiresBothParts()
    {
        var result = PersonalName.Create("Aisha", " ");

        Assert.False(result.IsSuccess);
        Assert.Equal("borrower.name.required", result.Error?.Code);
    }

    private static BorrowerProfile CreateProfile() =>
        TestResult.Get(BorrowerProfile.Create(new BorrowerProfileCreation(
            UserAccountId.New(),
            CreatedAt,
            TestResult.Get(ConsentRecord.Record(ConsentStatus.Declined, "v1", CreatedAt)))));

    private static VettingDecision CreateDecision(UserAccountId subjectAccountId, DecisionOutcome outcome) => TestResult.Get(VettingDecision.Record(
        new VettingDecisionDetails(
            VettingCaseId.New(),
            subjectAccountId,
            VettingSubjectType.Borrower,
            outcome,
            TestResult.Get(DecisionReason.Create("Reviewed.")),
            UserAccountId.New(),
            TestResult.Get(PolicyVersion.Create("2026.1")),
            TestResult.Get(UtcTimestamp.Create(CreatedAt)))));
}
