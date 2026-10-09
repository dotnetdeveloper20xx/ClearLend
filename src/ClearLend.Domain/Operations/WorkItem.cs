using ClearLend.Domain.Common;

namespace ClearLend.Domain.Operations;

public enum WorkItemStatus { Open = 1, InProgress = 2, Completed = 3, Cancelled = 4 }
public enum WorkItemPriority { Low = 1, Normal = 2, High = 3, Urgent = 4 }
public enum WorkItemType { Registration = 1, Vetting = 2, CreditReview = 3, Support = 4, Finance = 5, Servicing = 6 }
public readonly record struct WorkItemId(Guid Value) { public static WorkItemId New() => new(Guid.NewGuid()); }
public sealed record WorkAssignment(StaffMemberId StaffMemberId, DateTimeOffset AssignedAt);
public sealed record WorkEscalation(string Reason, DateTimeOffset EscalatedAt);

public sealed class WorkItem
{
    private readonly List<WorkAssignment> assignmentHistory = [];
    private WorkItem(WorkItemId id, WorkItemType type, string subjectReference, WorkItemPriority priority, WorkQueueId? queueId, DateTimeOffset createdAt)
    { Id = id; Type = type; SubjectReference = subjectReference; Priority = priority; QueueId = queueId; CreatedAt = createdAt; StatusChangedAt = createdAt; Status = WorkItemStatus.Open; }
    public WorkItemId Id { get; }
    public WorkItemType Type { get; }
    public string SubjectReference { get; }
    public WorkQueueId? QueueId { get; }
    public WorkItemPriority Priority { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public StaffMemberId? AssignedTo { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset StatusChangedAt { get; private set; }
    public IReadOnlyList<WorkAssignment> AssignmentHistory => assignmentHistory.AsReadOnly();
    public bool IsEscalated { get; private set; }
    public string? EscalationReason { get; private set; }
    public DateTimeOffset? EscalatedAt { get; private set; }
    public IReadOnlyList<WorkEscalation> EscalationHistory => escalationHistory.AsReadOnly();
    private readonly List<WorkEscalation> escalationHistory = [];

    public static DomainResult<WorkItem> Open(WorkItemType type, string? subjectReference, WorkItemPriority priority, DateTimeOffset createdAt, WorkItemId? id = null, WorkQueueId? queueId = null)
    {
        if (!Enum.IsDefined(type)) return DomainResults.Failure<WorkItem>(new("work_item.type.invalid", "An unsupported work-item type was supplied."));
        if (!Enum.IsDefined(priority)) return DomainResults.Failure<WorkItem>(new("work_item.priority.invalid", "An unsupported work-item priority was supplied."));
        if (string.IsNullOrWhiteSpace(subjectReference)) return DomainResults.Failure<WorkItem>(new("work_item.subject.required", "A work-item subject is required."));
        if (createdAt.Offset != TimeSpan.Zero) return DomainResults.Failure<WorkItem>(new("work_item.created_at.not_utc", "Creation time must be expressed in UTC."));
        return DomainResults.Success(new WorkItem(id ?? WorkItemId.New(), type, subjectReference.Trim(), priority, queueId, createdAt));
    }
    public DomainResult AssignTo(StaffMemberId staffMemberId, DateTimeOffset changedAt)
    {
        if (staffMemberId.Value == Guid.Empty) return DomainResult.Failure(new("work_item.assignee.required", "A valid staff assignee is required."));
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.assign.invalid_status", "A completed or cancelled work item cannot be assigned."));
        if (changedAt.Offset != TimeSpan.Zero) return DomainResult.Failure(new("work_item.timestamp.not_utc", "Time must be expressed in UTC."));
        AssignedTo = staffMemberId; assignmentHistory.Add(new WorkAssignment(staffMemberId, changedAt)); Status = WorkItemStatus.InProgress; StatusChangedAt = changedAt; return DomainResult.Success();
    }
    public DomainResult ChangePriority(WorkItemPriority priority, DateTimeOffset changedAt)
    {
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.priority.invalid_status", "A completed or cancelled work item cannot change priority."));
        if (changedAt.Offset != TimeSpan.Zero) return DomainResult.Failure(new("work_item.timestamp.not_utc", "Time must be expressed in UTC."));
        Priority = priority; StatusChangedAt = changedAt; return DomainResult.Success();
    }
    public DomainResult Complete(DateTimeOffset changedAt)
    {
        if (Status != WorkItemStatus.InProgress) return DomainResult.Failure(new("work_item.complete.invalid_status", "Only an in-progress work item can be completed."));
        if (changedAt.Offset != TimeSpan.Zero) return DomainResult.Failure(new("work_item.timestamp.not_utc", "Time must be expressed in UTC."));
        Status = WorkItemStatus.Completed; StatusChangedAt = changedAt; return DomainResult.Success();
    }

    public DomainResult Escalate(string? reason, DateTimeOffset escalatedAt)
    {
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.escalate.invalid_status", "A completed or cancelled work item cannot be escalated."));
        if (string.IsNullOrWhiteSpace(reason)) return DomainResult.Failure(new("work_item.escalation_reason.required", "An escalation reason is required."));
        if (escalatedAt.Offset != TimeSpan.Zero) return DomainResult.Failure(new("work_item.timestamp.not_utc", "Time must be expressed in UTC."));
        IsEscalated = true; EscalationReason = reason.Trim(); EscalatedAt = escalatedAt; escalationHistory.Add(new WorkEscalation(EscalationReason, escalatedAt)); Priority = WorkItemPriority.Urgent; StatusChangedAt = escalatedAt; return DomainResult.Success();
    }
}
