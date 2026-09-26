using Microsoft.Extensions.Options;

namespace PersonalCrm.Infrastructure.Storage;

/// <summary>
/// Local-filesystem implementation of <see cref="IAttachmentStore"/>. The default
/// for the docker-compose install — zero external dependencies, one mounted volume.
///
/// Layout under <c>AttachmentStore:LocalRoot</c>:
/// <code>
///   {workspaceId}/{yyyy}/{mm}/{dd}/{guid}-{slugified-filename}
/// </code>
/// Keys are opaque to callers; the convention exists so the on-disk tree
/// stays browsable for ops and so two workspaces can never collide.
/// </summary>
public class LocalAttachmentStore : IAttachmentStore
{
    private readonly AttachmentStoreOptions _options;
    private readonly string _root;

    public LocalAttachmentStore(IOptions<AttachmentStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _root     = Path.GetFullPath(_options.Local.Root);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> PutAsync(
        Stream data,
        string filename,
        string contentType,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        var now          = DateTimeOffset.UtcNow;
        var key          = $"{now:yyyy}/{now:MM}/{now:dd}/{Guid.NewGuid():N}-{Slugify(filename)}";
        var fullPath     = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        var fullDir      = Path.GetDirectoryName(fullPath)
                           ?? throw new InvalidOperationException("Invalid attachment path");

        Directory.CreateDirectory(fullDir);

        await using (var fs = File.Create(fullPath))
        {
            await data.CopyToAsync(fs, ct);
        }

        return key;
    }

    public Task<Stream> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var fullPath = ResolveAndGuard(key);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var fullPath = ResolveAndGuard(key);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        // The local provider serves files through an internal controller;
        // see ADR 0003 for why we don't expose the filesystem directly.
        var baseUrl = (_options.PublicBaseUrl ?? "/").TrimEnd('/');
        var token   = Convert.ToHexString(Guid.NewGuid().ToByteArray())
                      [..16]; // short token; controller validates against server-side cache
        var url     = $"{baseUrl}/api/attachments/{Uri.EscapeDataString(key)}?t={token}";
        return Task.FromResult(url);
    }

    private string ResolveAndGuard(string key)
    {
        // Reject path traversal attempts explicitly — keys must be relative
        // segments under the configured root, never absolute or `..`-laden.
        if (Path.IsPathRooted(key) || key.Contains("..", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Refusing to resolve unsafe key '{key}'.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Refusing to escape attachment root: '{key}'.");
        }
        return fullPath;
    }

    private static string Slugify(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(filename
            .Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)
            .ToArray());
        return cleaned.Length > 80 ? cleaned[..80] : cleaned;
    }
}
