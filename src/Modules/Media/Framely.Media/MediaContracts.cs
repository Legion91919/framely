namespace Framely.Media;

/// <summary>
/// Media module settings (section "Media"). Local disk mimics the CDN origin in development;
/// production points <see cref="PublicBaseUrl"/> at the CDN and swaps the store for blob storage.
/// </summary>
public sealed class MediaOptions
{
    public const string Section = "Media";

    /// <summary>Folder for stored photos. Relative paths resolve against the app's content root.</summary>
    public string RootPath { get; set; } = "App_Data/media";

    /// <summary>Path the API serves stored files from (the local "CDN origin").</summary>
    public string OriginRequestPath { get; set; } = "/media";

    /// <summary>Base URL written into post image URLs: "/media" locally, a CDN host in production.</summary>
    public string PublicBaseUrl { get; set; } = "/media";

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

    public IList<string> AllowedContentTypes { get; } = ["image/jpeg"];
}

/// <summary>An incoming photo, already separated from the HTTP request.</summary>
public sealed record PhotoUpload(Stream Content, string? ContentType, long Length, string? FileName);

/// <summary>A photo after policy checks, metadata stripping and storage.</summary>
public sealed record StoredPhoto(string Key, string Url, string ContentType, long SizeBytes, int Width, int Height, bool LocationMetadataRemoved);

public enum PhotoRejection
{
    Empty,
    TooLarge,
    UnsupportedType,
    NotAJpeg,
}

#pragma warning disable CA1032 // Domain exception: always created with a reason, standard constructors would allow an invalid state.
public sealed class PhotoRejectedException(PhotoRejection reason, string message) : Exception(message)
#pragma warning restore CA1032
{
    public PhotoRejection Reason { get; } = reason;
}

/// <summary>Validates, strips location metadata from and stores an uploaded photo.</summary>
public interface IPhotoIngest
{
    Task<StoredPhoto> IngestAsync(PhotoUpload upload, bool keepLocationMetadata, CancellationToken cancellationToken);
}
