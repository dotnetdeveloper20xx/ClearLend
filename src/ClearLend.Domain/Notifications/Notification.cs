using ClearLend.Domain.Common;

namespace ClearLend.Domain.Notifications;

public enum NotificationStatus { Pending = 1, Delivered = 2, Read = 3, Failed = 4, Cancelled = 5 }
public enum NotificationChannel { InApp = 1, Email = 2, Sms = 3 }
public readonly record struct NotificationId(Guid Value) { public static NotificationId New() => new(Guid.NewGuid()); }

public sealed class Notification
{
    private Notification(NotificationId id, string recipientReference, string templateKey, NotificationChannel channel, DateTimeOffset createdAt)
    { Id = id; RecipientReference = recipientReference; TemplateKey = templateKey; Channel = channel; CreatedAt = createdAt; Status = NotificationStatus.Pending; }
    public NotificationId Id { get; }
    public string RecipientReference { get; }
    public string TemplateKey { get; }
    public NotificationChannel Channel { get; }
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public int DeliveryAttempts { get; private set; }
    public string? LastError { get; private set; }
    public static DomainResult<Notification> Create(string? recipientReference, string? templateKey, NotificationChannel channel, DateTimeOffset createdAt, NotificationId? id = null)
    {
        if (string.IsNullOrWhiteSpace(recipientReference) || string.IsNullOrWhiteSpace(templateKey)) return DomainResults.Failure<Notification>(new("notification.values.required", "Recipient and template are required."));
        if (createdAt.Offset != TimeSpan.Zero) return DomainResults.Failure<Notification>(new("notification.created_at.not_utc", "Notification time must be expressed in UTC."));
        return DomainResults.Success(new Notification(id ?? NotificationId.New(), recipientReference.Trim(), templateKey.Trim(), channel, createdAt));
    }
    public DomainResult MarkDelivered(DateTimeOffset deliveredAt) { if (deliveredAt.Offset != TimeSpan.Zero) return DomainResult.Failure(new("notification.delivered_at.not_utc", "Delivery time must be expressed in UTC.")); if (Status != NotificationStatus.Pending) return DomainResult.Failure(new("notification.delivery.invalid_status", "Only pending notifications can be delivered.")); Status = NotificationStatus.Delivered; DeliveredAt = deliveredAt; DeliveryAttempts++; return DomainResult.Success(); }
    public DomainResult RecordFailure(string? error) { if (Status != NotificationStatus.Pending) return DomainResult.Failure(new("notification.failure.invalid_status", "Only pending notifications can fail delivery.")); DeliveryAttempts++; LastError = error; Status = NotificationStatus.Failed; return DomainResult.Success(); }
    public DomainResult MarkRead() { if (Status is NotificationStatus.Cancelled or NotificationStatus.Pending or NotificationStatus.Failed) return DomainResult.Failure(new("notification.read.invalid_status", "Only a delivered notification can be read.")); Status = NotificationStatus.Read; return DomainResult.Success(); }
}
