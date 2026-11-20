namespace Framely.Feed.Internal;

/// <summary>
/// The feed-store port: one pre-built, newest-first list of entries per member.
/// In memory for slice 1; a key-value store (sorted sets) or a Postgres table in production.
/// </summary>
internal interface IFeedStore
{
    /// <summary>Writes the entry into every owner's feed. Returns how many feeds were written.</summary>
    int FanOut(IEnumerable<Guid> ownerIds, FeedEntry entry);

    /// <summary>Up to <paramref name="take"/> entries older than <paramref name="after"/> (or the newest when null).</summary>
    IReadOnlyList<FeedEntry> Read(Guid ownerId, FeedCursor? after, int take);

    int RemoveAuthor(Guid ownerId, Guid authorId);
}

internal sealed class InMemoryFeedStore(int maxLength) : IFeedStore
{
    private readonly Dictionary<Guid, SortedSet<FeedEntry>> _feeds = [];
    private readonly object _gate = new();

    public int FanOut(IEnumerable<Guid> ownerIds, FeedEntry entry)
    {
        var written = 0;
        lock (_gate)
        {
            foreach (var owner in ownerIds)
            {
                if (!_feeds.TryGetValue(owner, out var feed))
                {
                    _feeds[owner] = feed = new SortedSet<FeedEntry>(NewestFirst.Instance);
                }

                if (feed.Add(entry))
                {
                    written++;
                }

                while (feed.Count > maxLength)
                {
                    feed.Remove(feed.Max); // Max under NewestFirst is the oldest entry
                }
            }
        }

        return written;
    }

    public IReadOnlyList<FeedEntry> Read(Guid ownerId, FeedCursor? after, int take)
    {
        lock (_gate)
        {
            if (!_feeds.TryGetValue(ownerId, out var feed))
            {
                return [];
            }

            IEnumerable<FeedEntry> query = feed;
            if (after is { } cursor)
            {
                query = query.SkipWhile(e => !e.IsOlderThan(cursor));
            }

            return query.Take(take).ToList();
        }
    }

    public int RemoveAuthor(Guid ownerId, Guid authorId)
    {
        lock (_gate)
        {
            return _feeds.TryGetValue(ownerId, out var feed) ? feed.RemoveWhere(e => e.AuthorId == authorId) : 0;
        }
    }
}
