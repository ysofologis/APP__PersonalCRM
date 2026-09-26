using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace PersonalCrm.Infrastructure.Storage;

/// <summary>
/// S3-compatible implementation of <see cref="IAttachmentStore"/>.
/// Works with AWS S3, MinIO, Cloudflare R2, Backblaze B2, and any other
/// S3-compatible endpoint. Enabled by setting <c>AttachmentStore:Provider=s3</c>.
///
/// Keys follow the same convention as <see cref="LocalAttachmentStore"/> so
/// migration between providers is a metadata-only operation.
/// </summary>
public class S3AttachmentStore : IAttachmentStore
{
    private readonly IAmazonS3 _client;
    private readonly AttachmentStoreOptions.S3Options _options;

    public S3AttachmentStore(IAmazonS3 client, IOptions<AttachmentStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        _client  = client;
        _options = options.Value.S3;
    }

    public async Task<string> PutAsync(
        Stream data,
        string filename,
        string contentType,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        var now = DateTimeOffset.UtcNow;
        var key = $"{now:yyyy}/{now:MM}/{now:dd}/{Guid.NewGuid():N}-{Slugify(filename)}";

        var request = new PutObjectRequest
        {
            BucketName      = _options.Bucket,
            Key             = key,
            InputStream     = data,
            ContentType     = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            AutoCloseStream = false,
            UseChunkEncoding = false
        };

        await _client.PutObjectAsync(request, ct);
        return key;
    }

    public async Task<Stream> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var response = await _client.GetObjectAsync(_options.Bucket, key, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        try
        {
            await _client.DeleteObjectAsync(_options.Bucket, key, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Idempotent: missing keys do not throw.
        }
    }

    public Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var request = new GetPreSignedURLRequest
        {
            BucketName  = _options.Bucket,
            Key         = key,
            Expires     = DateTime.UtcNow.Add(expiry),
            Verb        = HttpVerb.GET,
            Protocol    = Protocol.HTTP
        };
        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(url);
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
