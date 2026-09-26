namespace PersonalCrm.Infrastructure.Storage;

/// <summary>
/// Abstraction over binary attachment storage. Two implementations are shipped:
///   - <see cref="LocalAttachmentStore"/> — writes to a local directory (default).
///   - <see cref="S3AttachmentStore"/>    — uses any S3-compatible endpoint
///                                          (MinIO, AWS S3, Cloudflare R2, Backblaze B2).
/// Selected per-instance via <c>AttachmentStore:Provider</c> in configuration.
/// </summary>
public interface IAttachmentStore
{
    /// <summary>
    /// Stores <paramref name="data"/> under a generated key and returns it.
    /// The implementation is responsible for choosing the key so that
    /// collisions and hot-spotting are avoided.
    /// </summary>
    Task<string> PutAsync(
        Stream data,
        string filename,
        string contentType,
        CancellationToken ct = default);

    /// <summary>Opens the stored object for reading. Throws if the key is unknown.</summary>
    Task<Stream> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Removes the stored object. Idempotent — missing keys do not throw.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Returns a time-limited URL the browser can use to download the object
    /// directly. The Local implementation serves via the app's own endpoint;
    /// the S3 implementation returns a real presigned URL.
    /// </summary>
    Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default);
}
