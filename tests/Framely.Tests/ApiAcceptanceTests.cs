using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Framely.Tests;

/// <summary>Slice 1 acceptance criteria through real HTTP (multipart upload, JSON feed, /media origin).</summary>
public class ApiAcceptanceTests
{
    // Acceptance criterion 1
    [Fact]
    public async Task Given_A_follows_B_when_B_posts_then_the_post_is_first_in_As_home_feed()
    {
        await using var api = await ApiHarness.StartAsync();
        var a = api.AddMember("thandi.m", "Thandi Mahlangu");
        var b = api.AddMember("naledi.frames", "Naledi Dube");
        var put = await api.Client.SendAsync(api.As(a, HttpMethod.Put, $"/api/me/following/{b.Id}"));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var created = await api.PostPhotoAsync(b, JpegBuilder.Photo(), caption: "Wale Street, first light");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var post = await created.Content.ReadFromJsonAsync<PostDto>();

        var feed = await api.FeedAsync(a);
        Assert.Equal(post!.Id, feed.Items[0].Id);
        Assert.Equal("Wale Street, first light", feed.Items[0].Caption);
        Assert.Equal("naledi.frames", feed.Items[0].Author.Handle);
    }

    // Acceptance criterion 2
    [Fact]
    public async Task Given_C_does_not_follow_B_then_Bs_post_is_not_in_Cs_feed()
    {
        await using var api = await ApiHarness.StartAsync();
        var b = api.AddMember("naledi.frames", "Naledi Dube");
        var c = api.AddMember("kagiso.street", "Kagiso Sello");

        await api.PostPhotoAsync(b, JpegBuilder.Photo());

        Assert.Empty((await api.FeedAsync(c)).Items);
    }

    // Acceptance criterion 3
    [Fact]
    public async Task Given_a_post_the_stored_image_has_no_gps_exif_unless_location_sharing_is_on()
    {
        await using var api = await ApiHarness.StartAsync();
        var privateByDefault = api.AddMember("naledi.frames", "Naledi Dube");
        var sharer = api.AddMember("karoo.pieter", "Pieter van Wyk", locationSharing: true);

        var stripped = await (await api.PostPhotoAsync(privateByDefault, JpegBuilder.Photo(withExif: true))).Content.ReadFromJsonAsync<PostDto>();
        var kept = await (await api.PostPhotoAsync(sharer, JpegBuilder.Photo(withExif: true))).Content.ReadFromJsonAsync<PostDto>();

        var strippedBytes = await api.Client.GetByteArrayAsync(stripped!.ImageUrl);
        var keptBytes = await api.Client.GetByteArrayAsync(kept!.ImageUrl);
        Assert.StartsWith("/media/", stripped.ImageUrl);
        Assert.False(JpegBuilder.ContainsAscii(strippedBytes, "Exif\0\0"));
        Assert.False(JpegBuilder.ContainsAscii(strippedBytes, "GPS"));
        Assert.True(JpegBuilder.ContainsAscii(keptBytes, "Exif\0\0"));
    }

    // Acceptance criterion 4
    [Fact]
    public async Task Given_25_plus_posts_the_feed_pages_with_a_cursor_25_per_page()
    {
        await using var api = await ApiHarness.StartAsync();
        var a = api.AddMember("thandi.m", "Thandi Mahlangu");
        var b = api.AddMember("naledi.frames", "Naledi Dube");
        api.Graph.Follow(a.Id, b.Id);
        for (var i = 1; i <= 27; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await api.PostPhotoAsync(b, JpegBuilder.Photo(), caption: $"frame {i}")).StatusCode);
        }

        var first = await api.FeedAsync(a);
        var second = await api.FeedAsync(a, first.NextCursor);

        Assert.Equal(25, first.Items.Count);
        Assert.NotNull(first.NextCursor);
        Assert.Equal(2, second.Items.Count);
        Assert.Null(second.NextCursor);
        Assert.Equal("frame 27", first.Items[0].Caption);
        Assert.Equal("frame 1", second.Items[^1].Caption);
    }

    [Fact]
    public async Task Retrying_with_the_same_idempotency_key_returns_the_same_post()
    {
        await using var api = await ApiHarness.StartAsync();
        var b = api.AddMember("naledi.frames", "Naledi Dube");

        var first = await api.PostPhotoAsync(b, JpegBuilder.Photo(), idempotencyKey: "7d1c-retry");
        var retry = await api.PostPhotoAsync(b, JpegBuilder.Photo(), idempotencyKey: "7d1c-retry");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal((await first.Content.ReadFromJsonAsync<PostDto>())!.Id, (await retry.Content.ReadFromJsonAsync<PostDto>())!.Id);
        Assert.Single((await api.FeedAsync(b)).Items);
    }

    [Fact]
    public async Task Non_jpeg_uploads_get_415_problem_details()
    {
        await using var api = await ApiHarness.StartAsync();
        var b = api.AddMember("naledi.frames", "Naledi Dube");

        var response = await api.PostPhotoAsync(b, [0x89, 0x50, 0x4E, 0x47], contentType: "image/png");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task A_jpeg_label_on_non_jpeg_bytes_is_rejected()
    {
        await using var api = await ApiHarness.StartAsync();
        var b = api.AddMember("naledi.frames", "Naledi Dube");

        var response = await api.PostPhotoAsync(b, "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray());

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Uploads_over_the_size_limit_get_413()
    {
        await using var api = await ApiHarness.StartAsync(maxUploadBytes: 1024);
        var b = api.AddMember("naledi.frames", "Naledi Dube");

        var response = await api.PostPhotoAsync(b, [.. JpegBuilder.Photo(), .. new byte[2048]]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Missing_photo_or_long_caption_is_a_validation_problem()
    {
        await using var api = await ApiHarness.StartAsync();
        var b = api.AddMember("naledi.frames", "Naledi Dube");
        var noPhoto = api.As(b, HttpMethod.Post, "/api/posts");
        noPhoto.Content = new MultipartFormDataContent { { new StringContent("caption only"), "caption" } };

        var missing = await api.Client.SendAsync(noPhoto);
        var tooLong = await api.PostPhotoAsync(b, JpegBuilder.Photo(), caption: new string('x', 501));

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Contains("500", (await tooLong.Content.ReadFromJsonAsync<ProblemDto>())!.Detail);
    }

    [Fact]
    public async Task Requests_without_a_known_member_get_401()
    {
        await using var api = await ApiHarness.StartAsync();

        var none = await api.Client.GetAsync("/api/feed");
        var unknown = new HttpRequestMessage(HttpMethod.Get, "/api/feed");
        unknown.Headers.Add("X-Member-Id", Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.SendAsync(unknown)).StatusCode);
    }

    [Fact]
    public async Task A_bad_cursor_is_a_400_not_a_500()
    {
        await using var api = await ApiHarness.StartAsync();
        var a = api.AddMember("thandi.m", "Thandi Mahlangu");

        var response = await api.Client.SendAsync(api.As(a, HttpMethod.Get, "/api/feed?cursor=garbage"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Follow_backfills_the_feed_notifies_the_author_and_unfollow_removes_posts()
    {
        await using var api = await ApiHarness.StartAsync();
        var a = api.AddMember("thandi.m", "Thandi Mahlangu");
        var b = api.AddMember("naledi.frames", "Naledi Dube");
        await api.PostPhotoAsync(b, JpegBuilder.Photo(), caption: "posted before the follow");

        await api.Client.SendAsync(api.As(a, HttpMethod.Put, $"/api/me/following/{b.Id}"));
        var afterFollow = await api.FeedAsync(a);
        var notifications = await (await api.Client.SendAsync(api.As(b, HttpMethod.Get, "/api/me/notifications"))).Content.ReadAsStringAsync();
        var delete = await api.Client.SendAsync(api.As(a, HttpMethod.Delete, $"/api/me/following/{b.Id}"));
        var afterUnfollow = await api.FeedAsync(a);

        Assert.Equal("posted before the follow", Assert.Single(afterFollow.Items).Caption);
        Assert.Contains("Thandi Mahlangu started following you", notifications);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(afterUnfollow.Items);
    }

    [Fact]
    public async Task Following_yourself_is_400_and_an_unknown_member_is_404()
    {
        await using var api = await ApiHarness.StartAsync();
        var a = api.AddMember("thandi.m", "Thandi Mahlangu");

        var self = await api.Client.SendAsync(api.As(a, HttpMethod.Put, $"/api/me/following/{a.Id}"));
        var unknown = await api.Client.SendAsync(api.As(a, HttpMethod.Put, $"/api/me/following/{Guid.NewGuid()}"));

        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Location_sharing_can_be_switched_on_through_settings()
    {
        await using var api = await ApiHarness.StartAsync();
        var a = api.AddMember("thandi.m", "Thandi Mahlangu");
        var request = api.As(a, HttpMethod.Put, "/api/me/settings/location-sharing");
        request.Content = JsonContent.Create(new { enabled = true });

        var response = await api.Client.SendAsync(request);
        var me = await (await api.Client.SendAsync(api.As(a, HttpMethod.Get, "/api/me"))).Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"locationSharing\":true", me);
    }

    [Fact]
    public async Task Members_list_shows_who_the_viewer_follows()
    {
        await using var api = await ApiHarness.StartAsync();
        var a = api.AddMember("thandi.m", "Thandi Mahlangu");
        var b = api.AddMember("naledi.frames", "Naledi Dube");
        api.AddMember("kagiso.street", "Kagiso Sello");
        api.Graph.Follow(a.Id, b.Id);

        var json = await (await api.Client.SendAsync(api.As(a, HttpMethod.Get, "/api/members"))).Content.ReadAsStringAsync();

        Assert.Contains("\"handle\":\"naledi.frames\"", json);
        Assert.DoesNotContain("\"handle\":\"thandi.m\"", json);
        Assert.Contains("\"isFollowing\":true", json);
        Assert.Contains("\"isFollowing\":false", json);
    }

    [Fact]
    public async Task Health_endpoints_report_live_and_ready()
    {
        await using var api = await ApiHarness.StartAsync();

        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Media_is_served_with_long_lived_cache_headers_like_a_cdn_origin()
    {
        await using var api = await ApiHarness.StartAsync();
        var b = api.AddMember("naledi.frames", "Naledi Dube");
        var post = await (await api.PostPhotoAsync(b, JpegBuilder.Photo())).Content.ReadFromJsonAsync<PostDto>();

        var response = await api.Client.GetAsync(post!.ImageUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("immutable", response.Headers.CacheControl!.ToString());
    }
}
