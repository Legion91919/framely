using Framely.Feed;
using Xunit;

namespace Framely.Tests;

public class FeedPaginationTests
{
    private static readonly Guid Viewer = Guid.NewGuid();
    private static readonly Guid Author = Guid.NewGuid();

    private readonly ManualTimeProvider _time = ManualTimeProvider.Default();
    private readonly FakeFollowers _followers = new();

    private IFeedService FeedWithPosts(int count)
    {
        _followers.Follow(Viewer, Author);
        var feed = FeedModule.CreateInMemory(_followers, _time);
        for (var i = 1; i <= count; i++)
        {
            feed.Publish(Posts.By(Author, $"post {i}"));
            _time.Advance(TimeSpan.FromSeconds(30));
        }

        return feed;
    }

    // Acceptance criterion 4
    [Fact]
    public void Given_30_posts_the_feed_pages_with_a_cursor_25_per_page()
    {
        var feed = FeedWithPosts(30);

        var first = feed.GetHomeFeed(Viewer, null);
        var second = feed.GetHomeFeed(Viewer, first.NextCursor);

        Assert.Equal(25, first.Items.Count);
        Assert.NotNull(first.NextCursor);
        Assert.Equal(5, second.Items.Count);
        Assert.Null(second.NextCursor);
        Assert.Equal("post 30", first.Items[0].Caption);
        Assert.Equal("post 1", second.Items[^1].Caption);
        Assert.Empty(first.Items.Select(p => p.Id).Intersect(second.Items.Select(p => p.Id)));
    }

    [Fact]
    public void Exactly_25_posts_fit_on_one_page_with_no_next_cursor()
    {
        var page = FeedWithPosts(25).GetHomeFeed(Viewer, null);

        Assert.Equal(25, page.Items.Count);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public void Posts_published_in_the_same_instant_keep_a_stable_newest_first_order()
    {
        _followers.Follow(Viewer, Author);
        var feed = FeedModule.CreateInMemory(_followers, _time);
        for (var i = 1; i <= 27; i++)
        {
            feed.Publish(Posts.By(Author, $"burst {i}"));
        }

        var first = feed.GetHomeFeed(Viewer, null);
        var second = feed.GetHomeFeed(Viewer, first.NextCursor);

        Assert.Equal("burst 27", first.Items[0].Caption);
        Assert.Equal(new[] { "burst 2", "burst 1" }, second.Items.Select(p => p.Caption).ToArray());
    }

    [Fact]
    public void A_post_published_while_paging_does_not_shift_older_pages()
    {
        var feed = FeedWithPosts(30);
        var first = feed.GetHomeFeed(Viewer, null);

        feed.Publish(Posts.By(Author, "brand new"));
        var second = feed.GetHomeFeed(Viewer, first.NextCursor);

        Assert.Equal(5, second.Items.Count);
        Assert.Equal("post 5", second.Items[0].Caption);
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("djEuYWJj")]
    [InlineData("")]
    public void A_tampered_cursor_is_rejected(string cursor)
    {
        var feed = FeedWithPosts(3);

        var ex = Assert.Throws<FeedException>(() => feed.GetHomeFeed(Viewer, cursor));

        Assert.Equal(FeedError.InvalidCursor, ex.Error);
    }

    [Fact]
    public void An_empty_feed_has_no_items_and_no_cursor()
    {
        var feed = FeedModule.CreateInMemory(_followers, _time);

        var page = feed.GetHomeFeed(Guid.NewGuid(), null);

        Assert.Empty(page.Items);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public void Timelines_are_capped_so_one_feed_cannot_grow_without_bound()
    {
        _followers.Follow(Viewer, Author);
        var feed = FeedModule.CreateInMemory(_followers, _time, new FeedOptions { MaxTimelineLength = 30 });
        for (var i = 1; i <= 40; i++)
        {
            feed.Publish(Posts.By(Author, $"post {i}"));
            _time.Advance(TimeSpan.FromSeconds(1));
        }

        var first = feed.GetHomeFeed(Viewer, null);
        var second = feed.GetHomeFeed(Viewer, first.NextCursor);

        Assert.Equal(5, second.Items.Count);
        Assert.Equal("post 11", second.Items[^1].Caption);
    }
}
