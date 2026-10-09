using ClearLend.Domain.Common;

namespace ClearLend.Domain.Operations;

public enum WorkQueueType { Operations = 1, Compliance = 2, CreditReview = 3, Finance = 4, Servicing = 5, Support = 6 }
public readonly record struct WorkQueueId(Guid Value) { public static WorkQueueId New() => new(Guid.NewGuid()); }

public sealed class WorkQueueDefinition
{
    private readonly List<StaffMemberId> members = [];
    private WorkQueueDefinition(WorkQueueId id, WorkQueueType type, string name, DateTimeOffset createdAt)
    { Id = id; Type = type; Name = name; CreatedAt = createdAt; }
    public WorkQueueId Id { get; }
    public WorkQueueType Type { get; }
    public string Name { get; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyList<StaffMemberId> Members => members.AsReadOnly();

    public static DomainResult<WorkQueueDefinition> Create(WorkQueueType type, string? name, DateTimeOffset createdAt, WorkQueueId? id = null)
    {
        if (!Enum.IsDefined(type)) return DomainResults.Failure<WorkQueueDefinition>(new("work_queue.type.invalid", "An unsupported queue type was supplied."));
        if (string.IsNullOrWhiteSpace(name)) return DomainResults.Failure<WorkQueueDefinition>(new("work_queue.name.required", "A queue name is required."));
        if (createdAt.Offset != TimeSpan.Zero) return DomainResults.Failure<WorkQueueDefinition>(new("work_queue.created_at.not_utc", "Queue creation time must be expressed in UTC."));
        return DomainResults.Success(new WorkQueueDefinition(id ?? WorkQueueId.New(), type, name.Trim(), createdAt));
    }

    public DomainResult AddMember(StaffMemberId staffMemberId)
    {
        if (staffMemberId.Value == Guid.Empty) return DomainResult.Failure(new("work_queue.member.required", "A valid staff member is required."));
        if (members.Contains(staffMemberId)) return DomainResult.Failure(new("work_queue.member.already_added", "The staff member is already a queue member."));
        members.Add(staffMemberId); return DomainResult.Success();
    }

    public DomainResult RemoveMember(StaffMemberId staffMemberId)
    {
        if (!members.Remove(staffMemberId)) return DomainResult.Failure(new("work_queue.member.not_found", "The staff member is not a member of this queue."));
        return DomainResult.Success();
    }
}
