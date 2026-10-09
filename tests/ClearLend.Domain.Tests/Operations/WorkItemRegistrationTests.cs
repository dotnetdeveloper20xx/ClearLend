using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Operations;

namespace ClearLend.Domain.Tests.Operations;

public sealed class WorkItemRegistrationTests
{
    [Fact]
    public void RegistrationWorkItemUsesBorrowerProfileAsSubject()
    {
        var profileId = BorrowerProfileId.New();
        var result = WorkItem.Open(WorkItemType.Registration, profileId.ToString(), WorkItemPriority.Normal, new DateTimeOffset(2026, 10, 9, 15, 0, 0, TimeSpan.Zero));

        var workItem = TestResult.Get(result);

        Assert.Equal(WorkItemType.Registration, workItem.Type);
        Assert.Equal(profileId.ToString(), workItem.SubjectReference);
    }
}
