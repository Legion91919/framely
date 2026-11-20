namespace Framely.Media.Internal;

/// <summary>Blob storage port. Local disk here; production uses blob storage behind a CDN.</summary>
internal interface IPhotoStore
{
    string RootPath { get; }

    Task SaveAsync(string key, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);

    bool IsWritable();
}

internal sealed class LocalDiskPhotoStore(string rootPath) : IPhotoStore
{
    public string RootPath { get; } = Path.GetFullPath(rootPath);

    public async Task SaveAsync(string key, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        var target = Path.GetFullPath(Path.Combine(RootPath, key));
        if (!target.StartsWith(RootPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage key escapes the media root.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temp = target + ".tmp";
        await File.WriteAllBytesAsync(temp, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
        File.Move(temp, target, overwrite: false); // a finished file appears atomically
    }

    public bool IsWritable()
    {
        try
        {
            Directory.CreateDirectory(RootPath);
            var probe = Path.Combine(RootPath, ".ready-probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
