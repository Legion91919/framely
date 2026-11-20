namespace Framely.Notifications;

public sealed record Notification(Guid Id, Guid RecipientId, string Kind, string Text, DateTimeOffset CreatedAt);

/// <summary>Sends a notification. Slice 1 keeps an in-app inbox; push and email come later behind this port.</summary>
public interface INotificationSender
{
    void Send(Guid recipientId, string kind, string text);
}

public interface INotificationInbox
{
    IReadOnlyList<Notification> ForRecipient(Guid recipientId, int take = 20);
}
