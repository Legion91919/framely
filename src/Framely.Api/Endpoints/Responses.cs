using Framely.Feed;

namespace Framely.Api.Endpoints;

internal sealed record AuthorResponse(Guid Id, string Handle, string DisplayName, string City);

internal sealed record PostResponse(Guid Id, AuthorResponse Author, string ImageUrl, int Width, int Height, string AltText, string Caption, DateTimeOffset PublishedAt)
{
    public static PostResponse From(Post post) => new(
        post.Id,
        new AuthorResponse(post.Author.Id, post.Author.Handle, post.Author.DisplayName, post.Author.City),
        post.Photo.Url,
        post.Photo.Width,
        post.Photo.Height,
        post.AltText,
        post.Caption,
        post.PublishedAt);
}

internal sealed record FeedPageResponse(IReadOnlyList<PostResponse> Items, string? NextCursor);

internal sealed record MeResponse(Guid Id, string Handle, string DisplayName, string City, bool LocationSharing, int FollowerCount, int FollowingCount);

internal sealed record MemberResponse(Guid Id, string Handle, string DisplayName, string City, int FollowerCount, bool IsFollowing);

internal sealed record NotificationResponse(Guid Id, string Kind, string Text, DateTimeOffset CreatedAt);

internal sealed record LocationSharingRequest(bool Enabled);

internal sealed record DemoViewerResponse(Guid Id, string Handle, string DisplayName);
