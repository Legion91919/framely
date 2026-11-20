using Framely.Media.Internal;
using Xunit;
using static Framely.Tests.JpegBuilder;

namespace Framely.Tests;

public class JpegMetadataStripperTests
{
    // Acceptance criterion 3 (unit level): no GPS EXIF unless location sharing is on.
    [Fact]
    public void Strips_exif_app1_when_location_sharing_is_off()
    {
        var result = JpegMetadataStripper.Strip(Photo(withExif: true), keepLocationMetadata: false);

        Assert.False(ContainsAscii(result.Bytes, "Exif\0\0"));
        Assert.False(ContainsAscii(result.Bytes, "GPS"));
        Assert.Equal(1, result.RemovedApp1Segments);
    }

    [Fact]
    public void Keeps_every_other_segment_and_the_scan_data_byte_for_byte()
    {
        var result = JpegMetadataStripper.Strip(Photo(withExif: true), keepLocationMetadata: false);

        Assert.Equal(Photo(withExif: false), result.Bytes);
    }

    [Fact]
    public void Keeps_exif_untouched_when_location_sharing_is_on()
    {
        var input = Photo(withExif: true);

        var result = JpegMetadataStripper.Strip(input, keepLocationMetadata: true);

        Assert.Equal(input, result.Bytes);
        Assert.Equal(0, result.RemovedApp1Segments);
        Assert.True(ContainsAscii(result.Bytes, "Exif\0\0"));
    }

    [Fact]
    public void Removes_every_app1_segment_including_xmp_which_can_also_carry_gps()
    {
        var input = Join(Soi, App1Exif(), App0Jfif(), App1Xmp(), Sof0(10, 10), SosHeader(), ScanData(), Eoi);

        var result = JpegMetadataStripper.Strip(input, keepLocationMetadata: false);

        Assert.Equal(2, result.RemovedApp1Segments);
        Assert.False(ContainsAscii(result.Bytes, "GPSLatitude"));
        Assert.Equal(Join(Soi, App0Jfif(), Sof0(10, 10), SosHeader(), ScanData(), Eoi), result.Bytes);
    }

    [Fact]
    public void Reads_pixel_dimensions_from_the_frame_header()
    {
        var result = JpegMetadataStripper.Strip(Photo(width: 1600, height: 1067), keepLocationMetadata: false);

        Assert.Equal(1600, result.Width);
        Assert.Equal(1067, result.Height);
    }

    [Fact]
    public void Handles_fill_bytes_before_a_marker()
    {
        var input = Join(Soi, [0xFF, 0xFF], App1Exif(), Sof0(8, 8), SosHeader(), ScanData(), Eoi);

        var result = JpegMetadataStripper.Strip(input, keepLocationMetadata: false);

        Assert.Equal(Join(Soi, Sof0(8, 8), SosHeader(), ScanData(), Eoi), result.Bytes);
    }

    [Fact]
    public void Finds_markers_between_progressive_scans_and_strips_late_app1()
    {
        var input = Join(Soi, Sof0(8, 8), SosHeader(), ScanData(), Dht(), App1Exif(), SosHeader(), ScanData(), Eoi);

        var result = JpegMetadataStripper.Strip(input, keepLocationMetadata: false);

        Assert.Equal(Join(Soi, Sof0(8, 8), SosHeader(), ScanData(), Dht(), SosHeader(), ScanData(), Eoi), result.Bytes);
    }

    [Fact]
    public void Drops_bytes_appended_after_the_end_of_image()
    {
        var input = Join(Photo(withExif: false), "hidden trailer 33.92S"u8.ToArray());

        var result = JpegMetadataStripper.Strip(input, keepLocationMetadata: true);

        Assert.Equal(Photo(withExif: false), result.Bytes);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("empty")]
    [InlineData("truncated-segment")]
    [InlineData("bad-length")]
    [InlineData("no-eoi")]
    [InlineData("garbage-between-segments")]
    public void Rejects_input_that_is_not_a_well_formed_jpeg(string kind)
    {
        byte[] input = kind switch
        {
            "png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            "empty" => [],
            "truncated-segment" => Join(Soi, [0xFF, 0xE1, 0x00, 0x40, 0x45, 0x78]),
            "bad-length" => Join(Soi, [0xFF, 0xE0, 0x00, 0x01], Eoi),
            "no-eoi" => Join(Soi, Sof0(8, 8), SosHeader(), ScanData()),
            _ => Join(Soi, App0Jfif(), [0x00, 0x11], Eoi),
        };

        Assert.Throws<InvalidJpegException>(() => JpegMetadataStripper.Strip(input, keepLocationMetadata: false));
    }
}
