using ClearLend.Domain.Common;

namespace ClearLend.Domain.Operations;

public enum WorkItemStatus { Open = 1, InProgress = 2, Completed = 3, Cancelled = 4, WaitingForInformation = 5 }
public enum WorkItemPriority { Low = 1, Normal = 2, High = 3, Urgent = 4 }
public enum WorkItemType { Registration = 1, Vetting = 2, CreditReview = 3, Support = 4, Finance = 5, Servicing = 6 }
public readonly record struct WorkItemId(Guid Value) { public static WorkItemId New() => new(Guid.NewGuid()); }
public sealed record WorkAssignment(StaffMemberId StaffMemberId, DateTimeOffset AssignedAt);
public sealed record WorkEscalation(string Reason, DateTimeOffset EscalatedAt, StaffMemberId? ResponsibleStaffMemberId = null, DateTimeOffset? ResolvedAt = null, StaffMemberId? ResolvedBy = null, string? Resolution = null);
public sealed record WorkTransfer(WorkQueueId FromQueueId, WorkQueueId ToQueueId, DateTimeOffset TransferredAt, string Reason);

public sealed class WorkItem
{
    private readonly List<WorkAssignment> assignmentHistory = [];
    private readonly List<WorkTransfer> transferHistory = [];
    private WorkItem(WorkItemId id, WorkItemType type, string subjectReference, WorkItemPriority priority, WorkQueueId? queueId, DateTimeOffset createdAt)
    { Id = id; Type = type; SubjectReference = subjectReference; Priority = priority; QueueId = queueId; CreatedAt = createdAt; StatusChangedAt = createdAt; Status = WorkItemStatus.Open; }
    public WorkItemId Id { get; }
    public WorkItemType Type { get; }
    public string SubjectReference { get; }
    public WorkQueueId? QueueId { get; private set; }
    public WorkItemPriority Priority { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public StaffMemberId? AssignedTo { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset StatusChangedAt { get; private set; }
    public IReadOnlyList<WorkAssignment> AssignmentHistory => assignmentHistory.AsReadOnly();
    public IReadOnlyList<WorkTransfer> TransferHistory => transferHistory.AsReadOnly();
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
        if (id is { } suppliedId && suppliedId.Value == Guid.Empty) return DomainResults.Failure<WorkItem>(new("work_item.id.empty", "A work-item identifier cannot be empty."));
        if (queueId is { } suppliedQueue && suppliedQueue.Value == Guid.Empty) return DomainResults.Failure<WorkItem>(new("work_item.queue_id.empty", "A queue identifier cannot be empty."));
        return DomainResults.Success(new WorkItem(id ?? WorkItemId.New(), type, subjectReference.Trim(), priority, queueId, createdAt));
    }
    public DomainResult AssignTo(StaffMemberId staffMemberId, DateTimeOffset changedAt)
    {
        if (staffMemberId.Value == Guid.Empty) return DomainResult.Failure(new("work_item.assignee.required", "A valid staff assignee is required."));
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.assign.invalid_status", "A completed or cancelled work item cannot be assigned."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        AssignedTo = staffMemberId; assignmentHistory.Add(new WorkAssignment(staffMemberId, changedAt)); Status = WorkItemStatus.InProgress; StatusChangedAt = changedAt; return DomainResult.Success();
    }
    public DomainResult ChangePriority(WorkItemPriority priority, DateTimeOffset changedAt)
    {
        if (!Enum.IsDefined(priority)) return DomainResult.Failure(new("work_item.priority.invalid", "An unsupported work-item priority was supplied."));
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.priority.invalid_status", "A completed or cancelled work item cannot change priority."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        Priority = priority; StatusChangedAt = changedAt; return DomainResult.Success();
    }
    public DomainResult Complete(DateTimeOffset changedAt)
    {
        if (Status != WorkItemStatus.InProgress) return DomainResult.Failure(new("work_item.complete.invalid_status", "Only an in-progress work item can be completed."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        Status = WorkItemStatus.Completed; StatusChangedAt = changedAt; return DomainResult.Success();
    }

    public DomainResult WaitForInformation(DateTimeOffset changedAt)
    {
        if (Status != WorkItemStatus.InProgress || AssignedTo is null)
            return DomainResult.Failure(new("work_item.wait_for_information.invalid_status", "Only assigned in-progress work can wait for information."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        Status = WorkItemStatus.WaitingForInformation;
        StatusChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult ResumeAfterInformation(DateTimeOffset changedAt)
    {
        if (Status != WorkItemStatus.WaitingForInformation || AssignedTo is null)
            return DomainResult.Failure(new("work_item.resume_information.invalid_status", "Only work waiting for information with an assigned owner can resume."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        Status = WorkItemStatus.InProgress;
        StatusChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult Escalate(string? reason, DateTimeOffset escalatedAt, StaffMemberId? responsibleStaffMemberId = null)
    {
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.escalate.invalid_status", "A completed or cancelled work item cannot be escalated."));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) return DomainResult.Failure(new("work_item.escalation_reason.invalid", "An escalation reason of at most 500 characters is required."));
        if (!ValidChangeTime(escalatedAt)) return InvalidChangeTime();
        if (responsibleStaffMemberId is { Value: var responsibleId } && responsibleId == Guid.Empty) return DomainResult.Failure(new("work_item.escalation_owner.invalid", "Escalation responsibility must reference a valid staff member."));
        IsEscalated = true; EscalationReason = reason.Trim(); EscalatedAt = escalatedAt; escalationHistory.Add(new WorkEscalation(EscalationReason, escalatedAt, responsibleStaffMemberId)); Priority = WorkItemPriority.Urgent; StatusChangedAt = escalatedAt; return DomainResult.Success();
    }

    public DomainResult ResolveEscalation(StaffMemberId resolver, string? resolution, DateTimeOffset resolvedAt)
    {
        if (!IsEscalated || escalationHistory.Count == 0) return DomainResult.Failure(new("work_item.escalation.not_open", "There is no open escalation to resolve."));
        if (resolver.Value == Guid.Empty || string.IsNullOrWhiteSpace(resolution) || resolution.Trim().Length > 500)
            return DomainResult.Failure(new("work_item.escalation.resolution.invalid", "A valid resolver and resolution of at most 500 characters are required."));
        if (!ValidChangeTime(resolvedAt)) return InvalidChangeTime();
        var current = escalationHistory[^1];
        if (current.ResolvedAt is not null) return DomainResult.Failure(new("work_item.escalation.already_resolved", "The latest escalation has already been resolved."));
        escalationHistory[^1] = current with { ResolvedAt = resolvedAt, ResolvedBy = resolver, Resolution = resolution.Trim() };
        IsEscalated = false; StatusChangedAt = resolvedAt;
        return DomainResult.Success();
    }

    public DomainResult Unassign(DateTimeOffset changedAt)
    {
        if (Status != WorkItemStatus.InProgress || AssignedTo is null) return DomainResult.Failure(new("work_item.unassign.invalid_status", "Only assigned work can be returned to its queue."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        AssignedTo = null; Status = WorkItemStatus.Open; StatusChangedAt = changedAt; return DomainResult.Success();
    }

    public DomainResult TransferTo(WorkQueueId targetQueueId, string? reason, DateTimeOffset changedAt)
    {
        if (QueueId is null || QueueId.Value.Value == Guid.Empty || targetQueueId.Value == Guid.Empty || targetQueueId == QueueId)
            return DomainResult.Failure(new("work_item.transfer.queue.invalid", "A different valid source and destination queue are required."));
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.transfer.invalid_status", "Completed or cancelled work cannot be transferred."));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) return DomainResult.Failure(new("work_item.transfer.reason.invalid", "A transfer reason of at most 500 characters is required."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        transferHistory.Add(new WorkTransfer(QueueId.Value, targetQueueId, changedAt, reason.Trim()));
        QueueId = targetQueueId; AssignedTo = null; Status = WorkItemStatus.Open; StatusChangedAt = changedAt;
        return DomainResult.Success();
    }

    public DomainResult Cancel(string? reason, DateTimeOffset changedAt)
    {
        if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) return DomainResult.Failure(new("work_item.cancel.invalid_status", "Completed or cancelled work cannot be cancelled."));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) return DomainResult.Failure(new("work_item.cancel.reason.invalid", "A cancellation reason of at most 500 characters is required."));
        if (!ValidChangeTime(changedAt)) return InvalidChangeTime();
        Status = WorkItemStatus.Cancelled; AssignedTo = null; StatusChangedAt = changedAt; return DomainResult.Success();
    }

    private bool ValidChangeTime(DateTimeOffset value) => value.Offset == TimeSpan.Zero && value >= StatusChangedAt;
    private static DomainResult InvalidChangeTime() => DomainResult.Failure(new("work_item.timestamp.invalid", "Change time must be UTC and cannot precede the previous change."));
}
