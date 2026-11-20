using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Framely.Feed.Internal;

/// <summary>Opaque keyset cursor: the (publish time, sequence) of the last item on the previous page.</summary>
internal readonly record struct FeedCursor(long PublishedAtTicks, long Sequence)
{
    private const string Version = "v1";

    public static FeedCursor After(FeedEntry last) => new(last.PublishedAtTicks, last.Sequence);

    public string Encode()
    {
        var raw = Encoding.ASCII.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{Version}.{PublishedAtTicks}.{Sequence}"));
        return Convert.ToBase64String(raw).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static FeedCursor Decode(string cursor)
    {
        var parts = TryDecodeText(cursor)?.Split('.');
        if (parts is not { Length: 3 } || parts[0] != Version
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
        {
            throw new FeedException(FeedError.InvalidCursor, "The feed cursor is not valid. Start again from the first page.");
        }

        return new FeedCursor(ticks, sequence);
    }

    private static string? TryDecodeText(string cursor)
    {
        if (string.IsNullOrEmpty(cursor) || cursor.Length > 200)
        {
            return null;
        }

        var base64 = cursor.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
        var bytes = Encoding.ASCII.GetBytes(base64);
        return Base64.IsValid(bytes) ? Encoding.ASCII.GetString(Convert.FromBase64String(base64)) : null;
    }
}
