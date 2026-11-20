namespace Framely.Feed;

/// <summary>Author details copied onto the post at publish time (denormalised read model, no cross-module reads on the hot path).</summary>
public sealed record AuthorSnapshot(Guid Id, string Handle, string DisplayName, string City);

/// <summary>Where the photo lives (a CDN/origin URL from the Media module) and its pixel size.</summary>
public sealed record PhotoRef(string Url, int Width, int Height);

public sealed record Post(Guid Id, AuthorSnapshot Author, PhotoRef Photo, string Caption, string AltText, DateTimeOffset PublishedAt, long Sequence);

public sealed record PublishPost(AuthorSnapshot Author, PhotoRef Photo, string? Caption, string? AltText, string? IdempotencyKey);

/// <param name="FannedOutTo">How many home feeds received the post (followers plus the author).</param>
public sealed record PublishResult(Post Post, bool Replayed, int FannedOutTo);

public sealed record FeedPage(IReadOnlyList<Post> Items, string? NextCursor);

public enum FeedError
{
    InvalidCursor,
    CaptionTooLong,
    AltTextTooLong,
}

#pragma warning disable CA1032 // Domain exception: always created with an error code.
public sealed class FeedException(FeedError error, string message) : Exception(message)
#pragma warning restore CA1032
{
    public FeedError Error { get; } = error;
}

/// <summary>Port: the Feed module asks "who follows this author?" without knowing the Graph module exists.</summary>
public interface IFollowerSource
{
    IReadOnlyCollection<Guid> FollowersOf(Guid authorId);
}

public sealed class FeedOptions
{
    public const string Section = "Feed";
    public const int MaxCaptionLength = 500;
    public const int MaxAltTextLength = 300;

    public int PageSize { get; set; } = 25;

    /// <summary>Entries kept per pre-built feed; older entries fall off (a pull fallback would serve them later).</summary>
    public int MaxTimelineLength { get; set; } = 1000;

    /// <summary>How many recent posts are copied into a feed when someone follows an author.</summary>
    public int BackfillCount { get; set; } = 50;
}

/// <summary>Posts and pre-built home feeds (fan-out on write).</summary>
public interface IFeedService
{
    PublishResult Publish(PublishPost command);

    /// <summary>Imports a post with a known publish time (demo seed, future migrations). Fans out like <see cref="Publish"/>.</summary>
    Post ImportHistorical(PublishPost command, DateTimeOffset publishedAt);

    Post? FindPost(Guid postId);

    Post? FindByIdempotencyKey(Guid authorId, string idempotencyKey);

    /// <summary>Newest first, <see cref="FeedOptions.PageSize"/> per page. Pass the previous page's NextCursor for the next page.</summary>
    FeedPage GetHomeFeed(Guid viewerId, string? cursor);

    int Backfill(Guid followerId, Guid authorId);

    int RemoveAuthor(Guid followerId, Guid authorId);
}
