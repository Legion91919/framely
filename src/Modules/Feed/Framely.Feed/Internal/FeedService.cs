using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Framely.Feed.Internal;

internal sealed class FeedService(IFeedStore store, IFollowerSource followers, TimeProvider time, FeedOptions options) : IFeedService
{
    // Process-wide meter (lives as long as the app), exported by OpenTelemetry in production.
    private static readonly Meter Meter = new("Framely.Feed");
    private static readonly Histogram<double> FanOutDuration =
        Meter.CreateHistogram<double>("framely.feed.fanout.duration", "ms", "Time to write a new post into every follower's feed (ADR-0001 trigger: p95 > 2 s).");
    private static readonly Histogram<int> FanOutWidth =
        Meter.CreateHistogram<int>("framely.feed.fanout.width", "feeds", "Home feeds written per post.");

    private readonly ConcurrentDictionary<Guid, Post> _posts = new();
    private readonly ConcurrentDictionary<(Guid Author, string Key), Guid> _idempotency = new();
    private readonly Dictionary<Guid, List<Post>> _byAuthor = [];
    private readonly object _publishGate = new();
    private long _sequence;

    public PublishResult Publish(PublishPost command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (caption, alt) = Validate(command);
        Post post;
        lock (_publishGate)
        {
            if (command.IdempotencyKey is { } key && FindByIdempotencyKey(command.Author.Id, key) is { } existing)
            {
                return new PublishResult(existing, Replayed: true, FannedOutTo: 0);
            }

            post = Store(command, caption, alt, time.GetUtcNow());
        }

        return new PublishResult(post, Replayed: false, FanOut(post));
    }

    public Post ImportHistorical(PublishPost command, DateTimeOffset publishedAt)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (caption, alt) = Validate(command);
        Post post;
        lock (_publishGate)
        {
            post = Store(command, caption, alt, publishedAt);
        }

        FanOut(post);
        return post;
    }

    public Post? FindPost(Guid postId) => _posts.TryGetValue(postId, out var post) ? post : null;

    public Post? FindByIdempotencyKey(Guid authorId, string idempotencyKey) =>
        _idempotency.TryGetValue((authorId, idempotencyKey), out var id) ? FindPost(id) : null;

    public FeedPage GetHomeFeed(Guid viewerId, string? cursor)
    {
        FeedCursor? after = cursor is null ? null : FeedCursor.Decode(cursor);
        var entries = store.Read(viewerId, after, options.PageSize + 1);
        var page = entries.Take(options.PageSize).ToList();
        var items = page.Select(e => FindPost(e.PostId)).OfType<Post>().ToList();
        var next = entries.Count > options.PageSize ? FeedCursor.After(page[^1]).Encode() : null;
        return new FeedPage(items, next);
    }

    public int Backfill(Guid followerId, Guid authorId)
    {
        List<Post> recent;
        lock (_publishGate)
        {
            recent = _byAuthor.TryGetValue(authorId, out var list)
                ? list.OrderByDescending(p => p.PublishedAt).ThenByDescending(p => p.Sequence).Take(options.BackfillCount).ToList()
                : [];
        }

        return recent.Sum(p => store.FanOut([followerId], FeedEntry.For(p)));
    }

    public int RemoveAuthor(Guid followerId, Guid authorId) => store.RemoveAuthor(followerId, authorId);

    private Post Store(PublishPost command, string caption, string alt, DateTimeOffset publishedAt)
    {
        var post = new Post(Guid.NewGuid(), command.Author, command.Photo, caption, alt, publishedAt, ++_sequence);
        _posts[post.Id] = post;
        if (!_byAuthor.TryGetValue(post.Author.Id, out var list))
        {
            _byAuthor[post.Author.Id] = list = [];
        }

        list.Add(post);
        if (command.IdempotencyKey is { } key)
        {
            _idempotency[(post.Author.Id, key)] = post.Id;
        }

        return post;
    }

    /// <summary>Fan-out on write: the author's followers plus the author. Synchronous in slice 1; a queue worker later.</summary>
    private int FanOut(Post post)
    {
        var started = time.GetTimestamp();
        var owners = followers.FollowersOf(post.Author.Id).Append(post.Author.Id).Distinct();
        var written = store.FanOut(owners, FeedEntry.For(post));
        FanOutDuration.Record(time.GetElapsedTime(started).TotalMilliseconds);
        FanOutWidth.Record(written);
        return written;
    }

    private static (string Caption, string Alt) Validate(PublishPost command)
    {
        var caption = (command.Caption ?? string.Empty).Trim();
        var alt = (command.AltText ?? string.Empty).Trim();
        if (caption.Length > FeedOptions.MaxCaptionLength)
        {
            throw new FeedException(FeedError.CaptionTooLong, $"Captions can be at most {FeedOptions.MaxCaptionLength} characters.");
        }

        if (alt.Length > FeedOptions.MaxAltTextLength)
        {
            throw new FeedException(FeedError.AltTextTooLong, $"Alt text can be at most {FeedOptions.MaxAltTextLength} characters.");
        }

        return (caption, alt);
    }
}
