using System.Text;

namespace Framely.Tests;

/// <summary>Crafts tiny, structurally valid JPEG byte arrays segment by segment for stripper tests.</summary>
internal static class JpegBuilder
{
    public static readonly byte[] Soi = [0xFF, 0xD8];
    public static readonly byte[] Eoi = [0xFF, 0xD9];

    public static byte[] Segment(byte marker, byte[] payload)
    {
        var length = payload.Length + 2;
        return [0xFF, marker, (byte)(length >> 8), (byte)(length & 0xFF), .. payload];
    }

    public static byte[] App0Jfif() =>
        Segment(0xE0, [.. "JFIF"u8, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00]);

    /// <summary>APP1 with the "Exif\0\0" header, a big-endian TIFF header and a fake GPS payload.</summary>
    public static byte[] App1Exif(string gpsText = "GPS 33.9249 S 18.4241 E") =>
        Segment(0xE1, [.. "Exif\0\0"u8, .. "MM\0*\0\0\0\u0008"u8, .. Encoding.ASCII.GetBytes(gpsText)]);

    public static byte[] App1Xmp() =>
        Segment(0xE1, [.. "http://ns.example.org/xmp/1.0/\0"u8, .. "<x:xmpmeta exif:GPSLatitude=\"33,55.5S\"/>"u8]);

    public static byte[] Dqt() => Segment(0xDB, [0x00, .. Enumerable.Range(1, 64).Select(i => (byte)i)]);

    public static byte[] Sof0(int width, int height) =>
        Segment(0xC0, [0x08, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01]);

    public static byte[] Dht() => Segment(0xC4, [0x00, .. Enumerable.Repeat((byte)0x01, 16), 0x00]);

    public static byte[] SosHeader() => Segment(0xDA, [0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00]);

    /// <summary>Entropy-coded data with a stuffed 0xFF00 and a restart marker, which must survive untouched.</summary>
    public static byte[] ScanData() => [0x12, 0x34, 0xFF, 0x00, 0x56, 0xFF, 0xD0, 0x78, 0x9A];

    public static byte[] Join(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    /// <summary>SOI, APP0, [APP1 Exif], DQT, SOF0, DHT, SOS, scan data, EOI.</summary>
    public static byte[] Photo(bool withExif = true, int width = 320, int height = 240) => withExif
        ? Join(Soi, App0Jfif(), App1Exif(), Dqt(), Sof0(width, height), Dht(), SosHeader(), ScanData(), Eoi)
        : Join(Soi, App0Jfif(), Dqt(), Sof0(width, height), Dht(), SosHeader(), ScanData(), Eoi);

    public static bool ContainsAscii(byte[] haystack, string needle) =>
        haystack.AsSpan().IndexOf(Encoding.ASCII.GetBytes(needle)) >= 0;
}
