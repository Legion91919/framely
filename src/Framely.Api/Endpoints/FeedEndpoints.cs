using Framely.Feed;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Framely.Api.Endpoints;

internal static class FeedEndpoints
{
    public static RouteGroupBuilder MapFeedEndpoints(this RouteGroupBuilder api)
    {
        // Pre-built home feed: newest first, 25 per page, opaque keyset cursor.
        api.MapGet("/feed", (HttpContext http, string? cursor, IFeedService feed) =>
        {
            var page = feed.GetHomeFeed(http.Viewer().Id, string.IsNullOrEmpty(cursor) ? null : cursor);
            return TypedResults.Ok(new FeedPageResponse(page.Items.Select(PostResponse.From).ToList(), page.NextCursor));
        });

        api.MapGet("/posts/{id:guid}", Results<Ok<PostResponse>, NotFound> (Guid id, IFeedService feed) =>
            feed.FindPost(id) is { } post ? TypedResults.Ok(PostResponse.From(post)) : TypedResults.NotFound());

        return api;
    }
}
