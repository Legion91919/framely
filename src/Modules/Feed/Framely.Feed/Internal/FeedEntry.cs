namespace Framely.Feed.Internal;

/// <summary>One line in a pre-built home feed. Ordered newest first by (publish time, sequence).</summary>
internal readonly record struct FeedEntry(Guid PostId, Guid AuthorId, long PublishedAtTicks, long Sequence)
{
    public static FeedEntry For(Post post) => new(post.Id, post.Author.Id, post.PublishedAt.UtcTicks, post.Sequence);

    /// <summary>True when this entry sorts after (is older than) the cursor position.</summary>
    public bool IsOlderThan(FeedCursor cursor) =>
        PublishedAtTicks < cursor.PublishedAtTicks || (PublishedAtTicks == cursor.PublishedAtTicks && Sequence < cursor.Sequence);
}

internal sealed class NewestFirst : IComparer<FeedEntry>
{
    public static readonly NewestFirst Instance = new();

    public int Compare(FeedEntry x, FeedEntry y)
    {
        var byTime = y.PublishedAtTicks.CompareTo(x.PublishedAtTicks);
        return byTime != 0 ? byTime : y.Sequence.CompareTo(x.Sequence);
    }
}
