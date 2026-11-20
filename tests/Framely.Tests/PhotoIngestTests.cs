using Framely.Media;
using Xunit;

namespace Framely.Tests;

public sealed class PhotoIngestTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "framely-ingest-" + Guid.NewGuid().ToString("N"));
    private readonly IPhotoIngest _ingest;

    public PhotoIngestTests()
    {
        _ingest = MediaModule.CreateLocal(new MediaOptions { RootPath = _root, MaxUploadBytes = 4096 }, ManualTimeProvider.Default());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static PhotoUpload Upload(byte[] bytes, string contentType = "image/jpeg") =>
        new(new MemoryStream(bytes), contentType, bytes.Length, "frame.jpg");

    [Fact]
    public async Task Stored_file_has_no_exif_when_location_sharing_is_off()
    {
        var stored = await _ingest.IngestAsync(Upload(JpegBuilder.Photo(withExif: true)), keepLocationMetadata: false, CancellationToken.None);

        var onDisk = await File.ReadAllBytesAsync(Path.Combine(_root, stored.Key));
        Assert.False(JpegBuilder.ContainsAscii(onDisk, "Exif\0\0"));
        Assert.True(stored.LocationMetadataRemoved);
        Assert.StartsWith("/media/2026/11/", stored.Url);
        Assert.Equal(320, stored.Width);
    }

    [Fact]
    public async Task Stored_file_keeps_exif_when_location_sharing_is_on()
    {
        var stored = await _ingest.IngestAsync(Upload(JpegBuilder.Photo(withExif: true)), keepLocationMetadata: true, CancellationToken.None);

        var onDisk = await File.ReadAllBytesAsync(Path.Combine(_root, stored.Key));
        Assert.True(JpegBuilder.ContainsAscii(onDisk, "Exif\0\0"));
        Assert.False(stored.LocationMetadataRemoved);
    }

    [Theory]
    [InlineData("image/png", "UnsupportedType")]
    [InlineData("text/plain", "UnsupportedType")]
    [InlineData("image/jpeg", "TooLarge")]
    [InlineData("image/jpeg", "Empty")]
    [InlineData("image/jpeg", "NotAJpeg")]
    public async Task Rejects_uploads_that_break_the_policy(string contentType, string expected)
    {
        byte[] bytes = expected switch
        {
            "TooLarge" => [.. JpegBuilder.Photo(), .. new byte[5000]],
            "Empty" => [],
            "NotAJpeg" => "GIF89a not really a jpeg"u8.ToArray(),
            _ => JpegBuilder.Photo(),
        };

        PhotoRejectedException? caught = null;
        try
        {
            await _ingest.IngestAsync(Upload(bytes, contentType), keepLocationMetadata: false, CancellationToken.None);
        }
        catch (PhotoRejectedException ex)
        {
            caught = ex;
        }

        Assert.NotNull(caught);
        Assert.Equal(expected, caught!.Reason.ToString());
        Assert.False(Directory.Exists(_root) && Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Content_type_parameters_and_case_are_ignored()
    {
        var stored = await _ingest.IngestAsync(Upload(JpegBuilder.Photo(), "Image/JPEG; charset=binary"), false, CancellationToken.None);

        Assert.Equal("image/jpeg", stored.ContentType);
    }
}
