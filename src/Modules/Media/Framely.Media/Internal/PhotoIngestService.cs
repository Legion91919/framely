using Microsoft.Extensions.Options;

namespace Framely.Media.Internal;

internal sealed class PhotoIngestService(IPhotoStore store, IOptions<MediaOptions> options, TimeProvider time) : IPhotoIngest
{
    private readonly MediaOptions _options = options.Value;

    public async Task<StoredPhoto> IngestAsync(PhotoUpload upload, bool keepLocationMetadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(upload);
        var contentType = NormaliseContentType(upload.ContentType);
        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new PhotoRejectedException(PhotoRejection.UnsupportedType, $"Only JPEG photos are accepted (got '{contentType}').");
        }

        if (upload.Length == 0)
        {
            throw new PhotoRejectedException(PhotoRejection.Empty, "The photo file is empty.");
        }

        if (upload.Length > _options.MaxUploadBytes)
        {
            throw TooLarge();
        }

        var bytes = await ReadBoundedAsync(upload.Content, cancellationToken).ConfigureAwait(false);
        if (bytes.Length < 3 || bytes[0] != 0xFF || bytes[1] != 0xD8 || bytes[2] != 0xFF)
        {
            throw new PhotoRejectedException(PhotoRejection.NotAJpeg, "The file is labelled as JPEG but is not a JPEG image.");
        }

        JpegStripResult stripped;
        try
        {
            stripped = JpegMetadataStripper.Strip(bytes, keepLocationMetadata);
        }
        catch (InvalidJpegException ex)
        {
            throw new PhotoRejectedException(PhotoRejection.NotAJpeg, "The JPEG file is damaged: " + ex.Message);
        }

        var now = time.GetUtcNow();
        var key = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}.jpg";
        await store.SaveAsync(key, stripped.Bytes, cancellationToken).ConfigureAwait(false);

        return new StoredPhoto(
            key,
            $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}",
            "image/jpeg",
            stripped.Bytes.Length,
            stripped.Width,
            stripped.Height,
            LocationMetadataRemoved: stripped.RemovedApp1Segments > 0);
    }

    private PhotoRejectedException TooLarge() =>
        new(PhotoRejection.TooLarge, $"Photos can be at most {_options.MaxUploadBytes / (1024 * 1024.0):0.##} MB.");

    private async Task<byte[]> ReadBoundedAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > _options.MaxUploadBytes)
            {
                throw TooLarge(); // the declared length lied
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static string NormaliseContentType(string? contentType)
    {
        var value = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        return value.Length == 0 ? "application/octet-stream" : value;
    }
}
