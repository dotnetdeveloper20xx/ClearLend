using ClearLend.Domain.Notifications;

namespace ClearLend.Domain.Tests.Notifications;

public sealed class NotificationTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DeliveryDoesNotMarkNotificationAsRead()
    {
        var notification = TestResult.Get(Notification.Create("user-1", "borrower.registered", NotificationChannel.InApp, At));

        Assert.True(notification.MarkDelivered(At.AddMinutes(1)).IsSuccess);
        Assert.Equal(NotificationStatus.Delivered, notification.Status);
        Assert.True(notification.MarkRead().IsSuccess);
        Assert.Equal(NotificationStatus.Read, notification.Status);
    }

    [Fact]
    public void FailedNotificationCannotBeMarkedRead()
    {
        var notification = TestResult.Get(Notification.Create("user-1", "borrower.registered", NotificationChannel.Email, At));
        Assert.True(notification.RecordFailure("provider unavailable").IsSuccess);

        var result = notification.MarkRead();

        Assert.False(result.IsSuccess);
        Assert.Equal("notification.read.invalid_status", result.Error?.Code);
    }
}
