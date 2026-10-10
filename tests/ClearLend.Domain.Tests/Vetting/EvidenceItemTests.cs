using ClearLend.Domain.Vetting;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Tests.Vetting;

public sealed class EvidenceItemTests
{
    private static readonly DateTimeOffset RequestedAt = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EmptyEvidenceItemIdReturnsTypedFailure()
    {
        var result = EvidenceItemId.From(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.item_id.empty", result.Error?.Code);
    }

    [Fact]
    public void RequestCreatesRequestedEvidence()
    {
        var item = CreateItem();

        Assert.Equal(EvidenceStatus.Requested, item.Status);
        Assert.Null(item.StorageReference);
    }

    [Fact]
    public void RequestedEvidenceCanBeSubmittedAndAccepted()
    {
        var item = CreateItem();
        var reference = TestResult.Get(StorageReference.Create("blob://evidence/123"));

        Assert.True(item.Submit(new EvidenceSubmission(reference, RequestedAt.AddMinutes(1))).IsSuccess);
        Assert.True(item.Accept(UserAccountId.New(), RequestedAt.AddMinutes(2)).IsSuccess);
        Assert.Equal(EvidenceStatus.Accepted, item.Status);
    }

    [Fact]
    public void RejectionRequiresReason()
    {
        var item = CreateSubmittedItem();

        var result = item.Reject(new EvidenceRejection(" ", UserAccountId.New(), RequestedAt.AddMinutes(2)));

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.rejection_reason.required", result.Error?.Code);
    }

    [Fact]
    public void RejectedEvidenceMustBeReplacedWithANewItem()
    {
        var item = CreateSubmittedItem();
        Assert.True(item.Reject(new EvidenceRejection("Image is unreadable.", UserAccountId.New(), RequestedAt.AddMinutes(2))).IsSuccess);

        var replacement = TestResult.Get(StorageReference.Create("blob://evidence/456"));
        var result = item.Submit(new EvidenceSubmission(replacement, RequestedAt.AddMinutes(3)));

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.submit.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void AcceptedEvidenceCanExpireAndBeSuperseded()
    {
        var item = CreateSubmittedItem();
        Assert.True(item.Accept(UserAccountId.New(), RequestedAt.AddMinutes(2)).IsSuccess);
        Assert.True(item.Expire(RequestedAt.AddMinutes(3)).IsSuccess);
        Assert.True(item.Supersede(RequestedAt.AddMinutes(4)).IsSuccess);

        Assert.Equal(EvidenceStatus.Superseded, item.Status);
    }

    [Fact]
    public void SubmissionRequiresStorageReference()
    {
        var result = CreateItem().Submit(null);

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.submission.required", result.Error?.Code);
    }

    [Fact]
    public void EvidenceReviewRetainsReviewerIdentity()
    {
        var item = CreateSubmittedItem();
        var reviewer = UserAccountId.New();

        Assert.True(item.Accept(reviewer, RequestedAt.AddMinutes(2)).IsSuccess);

        Assert.Equal(reviewer, item.ReviewedByAccountId);
        Assert.Equal(RequestedAt.AddMinutes(2), item.ReviewedAt);
    }

    private static EvidenceItem CreateItem() =>
        TestResult.Get(EvidenceItem.Request(new EvidenceRequest(VettingCaseId.New(), EvidenceType.Identity, UserAccountId.New(), "Please provide proof of identity.", RequestedAt)));

    private static EvidenceItem CreateSubmittedItem()
    {
        var item = CreateItem();
        Assert.True(item.Submit(new EvidenceSubmission(
            TestResult.Get(StorageReference.Create("blob://evidence/123")), RequestedAt.AddMinutes(1))).IsSuccess);
        return item;
    }
}
