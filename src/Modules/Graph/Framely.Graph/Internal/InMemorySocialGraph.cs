namespace Framely.Graph.Internal;

internal sealed class InMemorySocialGraph(IMemberDirectory members) : ISocialGraph
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _followers = [];
    private readonly Dictionary<Guid, HashSet<Guid>> _following = [];

    public FollowResult Follow(Guid followerId, Guid followeeId)
    {
        if (followerId == followeeId)
        {
            throw new GraphException(GraphError.CannotFollowSelf, "You can't follow yourself.");
        }

        if (members.Find(followerId) is null || members.Find(followeeId) is null)
        {
            throw new GraphException(GraphError.UnknownMember, "Unknown member.");
        }

        lock (_gate)
        {
            var added = Set(_followers, followeeId).Add(followerId);
            Set(_following, followerId).Add(followeeId);
            return added ? FollowResult.Created : FollowResult.AlreadyFollowing;
        }
    }

    public bool Unfollow(Guid followerId, Guid followeeId)
    {
        lock (_gate)
        {
            var removed = _followers.TryGetValue(followeeId, out var set) && set.Remove(followerId);
            if (_following.TryGetValue(followerId, out var outgoing))
            {
                outgoing.Remove(followeeId);
            }

            return removed;
        }
    }

    public bool IsFollowing(Guid followerId, Guid followeeId)
    {
        lock (_gate)
        {
            return _followers.TryGetValue(followeeId, out var set) && set.Contains(followerId);
        }
    }

    public IReadOnlyCollection<Guid> FollowersOf(Guid memberId) => Snapshot(_followers, memberId);

    public IReadOnlyCollection<Guid> Following(Guid memberId) => Snapshot(_following, memberId);

    private Guid[] Snapshot(Dictionary<Guid, HashSet<Guid>> index, Guid key)
    {
        lock (_gate)
        {
            return index.TryGetValue(key, out var set) ? set.ToArray() : [];
        }
    }

    private static HashSet<Guid> Set(Dictionary<Guid, HashSet<Guid>> index, Guid key)
    {
        if (!index.TryGetValue(key, out var set))
        {
            index[key] = set = [];
        }

        return set;
    }
}
