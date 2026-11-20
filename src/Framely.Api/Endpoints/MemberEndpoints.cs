using Framely.Api.Workflows;
using Framely.Graph;
using Framely.Notifications;

namespace Framely.Api.Endpoints;

internal static class MemberEndpoints
{
    public static RouteGroupBuilder MapMemberEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/me", (HttpContext http, ISocialGraph graph) => TypedResults.Ok(Me(http.Viewer(), graph)));

        api.MapPut("/me/settings/location-sharing", (HttpContext http, LocationSharingRequest body, IMemberDirectory members, ISocialGraph graph) =>
            TypedResults.Ok(Me(members.SetLocationSharing(http.Viewer().Id, body.Enabled), graph)));

        api.MapGet("/members", (HttpContext http, IMemberDirectory members, ISocialGraph graph) =>
        {
            var viewer = http.Viewer();
            return TypedResults.Ok(members.All()
                .Where(m => m.Id != viewer.Id)
                .Select(m => new MemberResponse(m.Id, m.Handle, m.DisplayName, m.City, graph.FollowersOf(m.Id).Count, graph.IsFollowing(viewer.Id, m.Id)))
                .ToList());
        });

        // PUT/DELETE on the relationship resource are naturally idempotent, so client retries are safe.
        api.MapPut("/me/following/{memberId:guid}", (HttpContext http, Guid memberId, FollowWorkflow follow) =>
        {
            follow.Follow(http.Viewer(), memberId);
            return TypedResults.NoContent();
        });

        api.MapDelete("/me/following/{memberId:guid}", (HttpContext http, Guid memberId, FollowWorkflow follow) =>
        {
            follow.Unfollow(http.Viewer(), memberId);
            return TypedResults.NoContent();
        });

        api.MapGet("/me/notifications", (HttpContext http, INotificationInbox inbox) =>
            TypedResults.Ok(inbox.ForRecipient(http.Viewer().Id).Select(n => new NotificationResponse(n.Id, n.Kind, n.Text, n.CreatedAt)).ToList()));

        return api;
    }

    private static MeResponse Me(Member m, ISocialGraph graph) =>
        new(m.Id, m.Handle, m.DisplayName, m.City, m.LocationSharing, graph.FollowersOf(m.Id).Count, graph.Following(m.Id).Count);
}

internal static class DemoEndpoints
{
    /// <summary>Development only: lets the web app's "viewing as" switcher list the seeded members.</summary>
    public static void MapDemoEndpoints(this WebApplication app) =>
        app.MapGet("/api/demo/viewers", (IMemberDirectory members) =>
            TypedResults.Ok(members.All().Select(m => new DemoViewerResponse(m.Id, m.Handle, m.DisplayName)).ToList()));
}
