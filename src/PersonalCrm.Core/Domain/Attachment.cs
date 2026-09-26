namespace PersonalCrm.Core.Domain;

/// <summary>
/// Metadata for a binary blob (photo, file, voice note) attached to an
/// interaction or contact. The actual bytes live in object storage
/// (Local filesystem by default; S3-compatible optional) — this row only
/// holds the storage key plus content metadata.
/// </summary>
public class Attachment
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Opaque key into the configured <c>IAttachmentStore</c>. For the local
    /// provider this is a relative path under <c>AttachmentStore:LocalRoot</c>;
    /// for S3 it's the object key inside the bucket.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    public string Filename { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Optional SHA-256 of the bytes for de-duplication / integrity checks.</summary>
    public string? Sha256 { get; set; }

    public Guid? InteractionId { get; set; }

    public Guid? ContactId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Interaction? Interaction { get; set; }
    public Contact? Contact { get; set; }
}
