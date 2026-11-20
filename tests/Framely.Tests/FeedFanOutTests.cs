using Framely.Feed;
using Xunit;

namespace Framely.Tests;

public class FeedFanOutTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid C = Guid.NewGuid();

    private readonly ManualTimeProvider _time = ManualTimeProvider.Default();
    private readonly FakeFollowers _followers = new();

    private IFeedService NewFeed(FeedOptions? options = null) => FeedModule.CreateInMemory(_followers, _time, options);

    // Acceptance criterion 1
    [Fact]
    public void Given_A_follows_B_when_B_posts_then_the_post_is_first_in_As_home_feed()
    {
        _followers.Follow(A, B);
        var feed = NewFeed();
        feed.Publish(Posts.By(C, "older post by someone else"));
        _followers.Follow(A, C);
        _time.Advance(TimeSpan.FromMinutes(5));

        var published = feed.Publish(Posts.By(B, "Bo-Kaap at dawn")).Post;

        var page = feed.GetHomeFeed(A, cursor: null);
        Assert.Equal(published.Id, page.Items[0].Id);
        Assert.Equal("Bo-Kaap at dawn", page.Items[0].Caption);
    }

    // Acceptance criterion 2
    [Fact]
    public void Given_C_does_not_follow_B_then_Bs_post_is_not_in_Cs_feed()
    {
        _followers.Follow(A, B);
        var feed = NewFeed();

        var published = feed.Publish(Posts.By(B)).Post;

        Assert.DoesNotContain(feed.GetHomeFeed(C, null).Items, p => p.Id == published.Id);
        Assert.Contains(feed.GetHomeFeed(A, null).Items, p => p.Id == published.Id);
    }

    [Fact]
    public void Authors_see_their_own_post_in_their_home_feed()
    {
        var feed = NewFeed();

        var result = feed.Publish(Posts.By(B));

        Assert.Equal(result.Post.Id, feed.GetHomeFeed(B, null).Items[0].Id);
        Assert.Equal(1, result.FannedOutTo);
    }

    [Fact]
    public void Fan_out_writes_one_entry_per_follower_plus_the_author()
    {
        _followers.Follow(A, B);
        _followers.Follow(C, B);
        var feed = NewFeed();

        var result = feed.Publish(Posts.By(B));

        Assert.Equal(3, result.FannedOutTo);
    }

    [Fact]
    public void Same_idempotency_key_returns_the_original_post_without_a_second_fan_out()
    {
        _followers.Follow(A, B);
        var feed = NewFeed();

        var first = feed.Publish(Posts.By(B, idempotencyKey: "retry-1"));
        var replay = feed.Publish(Posts.By(B, idempotencyKey: "retry-1"));

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.Post.Id, replay.Post.Id);
        Assert.Single(feed.GetHomeFeed(A, null).Items);
    }

    [Fact]
    public void Following_back_fills_recent_posts_and_unfollowing_removes_them()
    {
        var feed = NewFeed();
        feed.Publish(Posts.By(B, "one"));
        _time.Advance(TimeSpan.FromMinutes(1));
        feed.Publish(Posts.By(B, "two"));

        var added = feed.Backfill(A, B);
        var afterFollow = feed.GetHomeFeed(A, null).Items.Select(p => p.Caption).ToArray();
        var removed = feed.RemoveAuthor(A, B);

        Assert.Equal(2, added);
        Assert.Equal(new[] { "two", "one" }, afterFollow);
        Assert.Equal(2, removed);
        Assert.Empty(feed.GetHomeFeed(A, null).Items);
    }

    [Fact]
    public void Caption_over_500_characters_is_rejected()
    {
        var feed = NewFeed();

        var ex = Assert.Throws<FeedException>(() => feed.Publish(Posts.By(B, new string('x', 501))));

        Assert.Equal(FeedError.CaptionTooLong, ex.Error);
    }

    [Fact]
    public void Caption_is_trimmed_and_may_be_empty()
    {
        var feed = NewFeed();

        var post = feed.Publish(Posts.By(B, "   ")).Post;

        Assert.Equal(string.Empty, post.Caption);
    }

    [Fact]
    public void Published_time_comes_from_the_time_provider()
    {
        var feed = NewFeed();

        var post = feed.Publish(Posts.By(B)).Post;

        Assert.Equal(_time.GetUtcNow(), post.PublishedAt);
    }
}
