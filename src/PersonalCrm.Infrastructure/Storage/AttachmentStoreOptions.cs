namespace PersonalCrm.Infrastructure.Storage;

/// <summary>
/// Configuration bound from <c>AttachmentStore:*</c> in appsettings / env vars.
/// </summary>
public class AttachmentStoreOptions
{
    public const string SectionName = "AttachmentStore";

    /// <summary>"local" (default) or "s3".</summary>
    public string Provider { get; set; } = "local";

    public LocalOptions Local { get; set; } = new();

    public S3Options S3 { get; set; } = new();

    /// <summary>
    /// Base URL the app is reachable at. Used by <see cref="LocalAttachmentStore"/>
    /// when constructing presigned URLs (it serves files through an internal
    /// controller instead of generating S3-style presigned URLs).
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public class LocalOptions
    {
        /// <summary>Filesystem root. Defaults to <c>./data/attachments</c>.</summary>
        public string Root { get; set; } = "./data/attachments";
    }

    public class S3Options
    {
        public string Endpoint   { get; set; } = string.Empty;
        public string Bucket     { get; set; } = string.Empty;
        public string Region     { get; set; } = "us-east-1";
        public string AccessKey  { get; set; } = string.Empty;
        public string SecretKey  { get; set; } = string.Empty;

        /// <summary>
        /// Required for MinIO and most S3-compatible servers that don't
        /// support virtual-hosted-style addressing.
        /// </summary>
        public bool ForcePathStyle { get; set; } = true;
    }
}
