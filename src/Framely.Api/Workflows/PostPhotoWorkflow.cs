using Framely.Feed;
using Framely.Graph;
using Framely.Media;

namespace Framely.Api.Workflows;

/// <summary>Post a photo: Media (check, strip EXIF, store) then Feed (save, fan out). Idempotent per member + key.</summary>
internal sealed class PostPhotoWorkflow(IPhotoIngest media, IFeedService feed)
{
    public async Task<PublishResult> PostAsync(Member author, PhotoUpload photo, string? caption, string? altText, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (idempotencyKey is not null && feed.FindByIdempotencyKey(author.Id, idempotencyKey) is { } existing)
        {
            return new PublishResult(existing, Replayed: true, FannedOutTo: 0); // retry: don't store the file twice
        }

        if ((caption?.Trim().Length ?? 0) > FeedOptions.MaxCaptionLength)
        {
            throw new FeedException(FeedError.CaptionTooLong, $"Captions can be at most {FeedOptions.MaxCaptionLength} characters.");
        }

        var stored = await media.IngestAsync(photo, keepLocationMetadata: author.LocationSharing, cancellationToken).ConfigureAwait(false);
        return feed.Publish(new PublishPost(
            new AuthorSnapshot(author.Id, author.Handle, author.DisplayName, author.City),
            new PhotoRef(stored.Url, stored.Width, stored.Height),
            caption,
            altText,
            idempotencyKey));
    }
}
