using System.Collections.Concurrent;

namespace Framely.Notifications.Internal;

/// <summary>Minimal stub: a capped, newest-first in-memory inbox per member.</summary>
internal sealed class InMemoryNotifications(TimeProvider time) : INotificationSender, INotificationInbox
{
    private const int MaxPerMember = 100;
    private readonly ConcurrentDictionary<Guid, LinkedList<Notification>> _inboxes = new();

    public void Send(Guid recipientId, string kind, string text)
    {
        var inbox = _inboxes.GetOrAdd(recipientId, _ => new LinkedList<Notification>());
        lock (inbox)
        {
            inbox.AddFirst(new Notification(Guid.NewGuid(), recipientId, kind, text, time.GetUtcNow()));
            while (inbox.Count > MaxPerMember)
            {
                inbox.RemoveLast();
            }
        }
    }

    public IReadOnlyList<Notification> ForRecipient(Guid recipientId, int take = 20)
    {
        if (!_inboxes.TryGetValue(recipientId, out var inbox))
        {
            return [];
        }

        lock (inbox)
        {
            return inbox.Take(take).ToList();
        }
    }
}
