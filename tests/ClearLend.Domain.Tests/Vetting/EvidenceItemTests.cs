using ClearLend.Domain.Vetting;

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
        var reference = StorageReference.Create("blob://evidence/123").Value!;

        Assert.True(item.Submit(reference, RequestedAt.AddMinutes(1)).IsSuccess);
        Assert.True(item.Accept(RequestedAt.AddMinutes(2)).IsSuccess);
        Assert.Equal(EvidenceStatus.Accepted, item.Status);
    }

    [Fact]
    public void RejectionRequiresReason()
    {
        var item = CreateSubmittedItem();

        var result = item.Reject(" ", RequestedAt.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.rejection_reason.required", result.Error?.Code);
    }

    [Fact]
    public void RejectedEvidenceMustBeReplacedWithANewItem()
    {
        var item = CreateSubmittedItem();
        Assert.True(item.Reject("Image is unreadable.", RequestedAt.AddMinutes(2)).IsSuccess);

        var replacement = StorageReference.Create("blob://evidence/456").Value!;
        var result = item.Submit(replacement, RequestedAt.AddMinutes(3));

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.submit.invalid_status", result.Error?.Code);
    }

    [Fact]
    public void AcceptedEvidenceCanExpireAndBeSuperseded()
    {
        var item = CreateSubmittedItem();
        Assert.True(item.Accept(RequestedAt.AddMinutes(2)).IsSuccess);
        Assert.True(item.Expire(RequestedAt.AddMinutes(3)).IsSuccess);
        Assert.True(item.Supersede(RequestedAt.AddMinutes(4)).IsSuccess);

        Assert.Equal(EvidenceStatus.Superseded, item.Status);
    }

    [Fact]
    public void SubmissionRequiresStorageReference()
    {
        var result = CreateItem().Submit(null, RequestedAt.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("evidence.storage_reference.required", result.Error?.Code);
    }

    private static EvidenceItem CreateItem() =>
        EvidenceItem.Request(VettingCaseId.New(), EvidenceType.Identity, RequestedAt).Value!;

    private static EvidenceItem CreateSubmittedItem()
    {
        var item = CreateItem();
        Assert.True(item.Submit(StorageReference.Create("blob://evidence/123").Value!, RequestedAt.AddMinutes(1)).IsSuccess);
        return item;
    }
}
