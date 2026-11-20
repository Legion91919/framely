using Framely.Feed;
using Framely.Graph;
using Framely.Notifications;

namespace Framely.Api.Workflows;

/// <summary>Follow: Graph edge, back-fill the follower's feed, notify the followed member. Unfollow reverses the feed part.</summary>
internal sealed class FollowWorkflow(ISocialGraph graph, IMemberDirectory members, IFeedService feed, INotificationSender notifications)
{
    public void Follow(Member follower, Guid followeeId)
    {
        if (graph.Follow(follower.Id, followeeId) == FollowResult.AlreadyFollowing)
        {
            return; // PUT is idempotent: no second back-fill or notification
        }

        feed.Backfill(follower.Id, followeeId);
        if (members.Find(followeeId) is not null)
        {
            notifications.Send(followeeId, "follow", $"{follower.DisplayName} started following you");
        }
    }

    public void Unfollow(Member follower, Guid followeeId)
    {
        graph.Unfollow(follower.Id, followeeId);
        feed.RemoveAuthor(follower.Id, followeeId);
    }
}
