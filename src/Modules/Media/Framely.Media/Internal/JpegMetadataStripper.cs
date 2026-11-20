using System.Buffers;

namespace Framely.Media.Internal;

#pragma warning disable CA1032 // Only thrown by the stripper with a message.
internal sealed class InvalidJpegException(string message) : Exception(message);
#pragma warning restore CA1032

internal sealed record JpegStripResult(byte[] Bytes, int RemovedApp1Segments, int Width, int Height);

/// <summary>
/// Pure C# JPEG marker walker. Removes every APP1 segment (EXIF, which holds the GPS IFD, and XMP, which can
/// repeat it) unless the member shares location. Everything else, including entropy-coded scan data, is copied
/// byte for byte. Bytes after EOI are dropped. Throws <see cref="InvalidJpegException"/> for malformed input.
/// </summary>
internal static class JpegMetadataStripper
{
    private const byte Marker = 0xFF;
    private const byte Soi = 0xD8;
    private const byte Eoi = 0xD9;
    private const byte Sos = 0xDA;
    private const byte App1 = 0xE1;
    private const byte Tem = 0x01;

    public static JpegStripResult Strip(ReadOnlySpan<byte> jpeg, bool keepLocationMetadata)
    {
        if (jpeg.Length < 4 || jpeg[0] != Marker || jpeg[1] != Soi)
        {
            throw new InvalidJpegException("Missing start-of-image marker.");
        }

        var output = new ArrayBufferWriter<byte>(jpeg.Length);
        output.Write([Marker, Soi]);
        int pos = 2, removed = 0, width = 0, height = 0;

        while (true)
        {
            if (pos >= jpeg.Length || jpeg[pos] != Marker)
            {
                throw new InvalidJpegException($"Expected a marker at offset {pos}.");
            }

            while (pos < jpeg.Length && jpeg[pos] == Marker)
            {
                pos++; // 0xFF fill bytes may precede any marker
            }

            if (pos >= jpeg.Length)
            {
                throw new InvalidJpegException("Image ends inside a marker.");
            }

            var marker = jpeg[pos++];
            if (marker == Eoi)
            {
                output.Write([Marker, Eoi]);
                return new JpegStripResult(output.WrittenSpan.ToArray(), removed, width, height);
            }

            if (IsStandalone(marker))
            {
                output.Write([Marker, marker]);
                continue;
            }

            if (pos + 2 > jpeg.Length)
            {
                throw new InvalidJpegException("Image ends inside a segment length.");
            }

            var length = (jpeg[pos] << 8) | jpeg[pos + 1];
            if (length < 2 || pos + length > jpeg.Length)
            {
                throw new InvalidJpegException($"Segment 0x{marker:X2} has an invalid length {length}.");
            }

            var segment = jpeg.Slice(pos, length);
            pos += length;

            if (marker == App1 && !keepLocationMetadata)
            {
                removed++;
                continue;
            }

            if (IsStartOfFrame(marker) && length >= 7)
            {
                height = (segment[3] << 8) | segment[4];
                width = (segment[5] << 8) | segment[6];
            }

            output.Write([Marker, marker]);
            output.Write(segment);

            if (marker == Sos)
            {
                var scanEnd = FindNextMarker(jpeg, pos);
                output.Write(jpeg[pos..scanEnd]);
                pos = scanEnd;
            }
        }
    }

    /// <summary>Entropy-coded data ends at the first 0xFF that is not stuffing (0xFF00), fill, or a restart marker.</summary>
    private static int FindNextMarker(ReadOnlySpan<byte> jpeg, int from)
    {
        for (var i = from; i < jpeg.Length - 1; i++)
        {
            if (jpeg[i] != Marker)
            {
                continue;
            }

            var next = jpeg[i + 1];
            if (next != 0x00 && next != Marker && next is not (>= 0xD0 and <= 0xD7))
            {
                return i;
            }
        }

        throw new InvalidJpegException("Scan data has no end-of-image marker.");
    }

    private static bool IsStandalone(byte marker) => marker == Tem || marker is >= 0xD0 and <= 0xD7;

    // SOF0..SOF15 except DHT (C4), JPG (C8) and DAC (CC).
    private static bool IsStartOfFrame(byte marker) => marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;
}
