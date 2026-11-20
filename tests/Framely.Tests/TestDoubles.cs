using Framely.Feed;

namespace Framely.Tests;

internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public static ManualTimeProvider Default() => new(new DateTimeOffset(2026, 11, 19, 6, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}

/// <summary>Follower lookup for feed unit tests: who follows whom, without the Graph module.</summary>
internal sealed class FakeFollowers : IFollowerSource
{
    private readonly Dictionary<Guid, HashSet<Guid>> _followers = [];

    public void Follow(Guid follower, Guid author)
    {
        if (!_followers.TryGetValue(author, out var set))
        {
            _followers[author] = set = [];
        }

        set.Add(follower);
    }

    public IReadOnlyCollection<Guid> FollowersOf(Guid authorId) =>
        _followers.TryGetValue(authorId, out var set) ? set.ToArray() : [];
}

internal static class Posts
{
    public static AuthorSnapshot Author(Guid id, string handle = "naledi.frames") => new(id, handle, "Naledi Dube", "Cape Town");

    public static PublishPost By(Guid authorId, string caption = "First light on the ridge", string? idempotencyKey = null) =>
        new(Author(authorId), new PhotoRef("/media/2026/11/test.jpg", 800, 1000), caption, "A ridge at dawn", idempotencyKey);
}
