using Framely.Notifications;
using Xunit;

namespace Framely.Tests;

public class NotificationsTests
{
    [Fact]
    public void Notifications_are_delivered_only_to_their_recipient_newest_first()
    {
        var time = ManualTimeProvider.Default();
        var (sender, inbox) = NotificationsModule.CreateInMemory(time);
        var naledi = Guid.NewGuid();
        var pieter = Guid.NewGuid();

        sender.Send(naledi, "follow", "Pieter van Wyk started following you");
        time.Advance(TimeSpan.FromMinutes(1));
        sender.Send(naledi, "follow", "Ayesha Patel started following you");

        var mine = inbox.ForRecipient(naledi);
        Assert.Equal(2, mine.Count);
        Assert.Equal("Ayesha Patel started following you", mine[0].Text);
        Assert.Empty(inbox.ForRecipient(pieter));
    }
}
