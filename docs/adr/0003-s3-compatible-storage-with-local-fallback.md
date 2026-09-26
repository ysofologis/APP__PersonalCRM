# ADR 0003 — S3-compatible storage from v1, with local filesystem as the default backend

- **Status:** Accepted
- **Date:** 2026-09-26
- **Deciders:** Maintainers

## Context

Personal CRM stores attachments — photos, voice notes, PDFs of contracts,
vCards imported in bulk. These can grow large (tens of MB per file) and
should not bloat the SQLite file (which would make backups slow and
backups of *just* the database impossible).

We need a storage abstraction that:

- Has a **zero-dep default** so `docker compose up` works without a second container.
- Allows **swapping to S3-compatible object storage** (AWS S3, MinIO, Cloudflare R2, Backblaze B2) without code changes.
- Survives the loss of a single container without losing user data.
- Doesn't expose the filesystem directly to the browser (no `Location: /var/lib/...` URLs).

## Decision

Define `IAttachmentStore` with four operations (`Put`, `Get`, `Delete`, `GetPresignedUrl`).
Ship two implementations:

1. **`LocalAttachmentStore`** — writes to `AttachmentStore:LocalRoot` (default `./data/attachments`).
   Serves files through an internal controller (`GET /api/attachments/{key}`) so we
   never expose absolute filesystem paths. **Selected by default** in `docker-compose.yml`.

2. **`S3AttachmentStore`** — wraps AWSSDK.S3 with `ForcePathStyle = true` so it works
   against MinIO and any other S3-compatible endpoint out of the box.
   **Selected** when `AttachmentStore:Provider=s3`. The same SDK talks to
   AWS S3, Cloudflare R2, Backblaze B2, MinIO, Wasabi, etc.

Selection happens **once**, at process start, in `AddAttachmentStore(...)`.
The rest of the app depends on `IAttachmentStore` only.

Keys follow the convention `{yyyy}/{MM}/{dd}/{guid}-{slug}` regardless of
provider, so a future migration from local to S3 is a metadata-only
operation (read every `Attachment.StorageKey`, re-upload bytes, update
the row). No user-visible URLs change because the app always goes through
`GetPresignedUrlAsync(...)`.

## Consequences

**Positive**

- Zero deps in the default install. One volume. One container.
- Future-proof: when a self-hoster outgrows the local volume (or wants
  off-site backups), they can switch to MinIO or AWS by setting env vars.
- The local provider does **not** serve raw filesystem paths — files
  flow through a controller. That preserves the abstraction at the HTTP
  layer too.
- `S3AttachmentStore.GetPresignedUrlAsync` returns real presigned URLs,
  so the browser downloads attachments directly from object storage —
  no proxying through the app server.

**Negative**

- Two implementations to maintain. We accept the cost because the surface
  is small (4 methods) and they share a key convention.
- The local provider needs a small controller for downloads; this is a
  few dozen lines in the App layer.
- Migrating *from* local to S3 in-place is not automatic; we provide
  the path (`IAttachmentStore.ListAllAsync` style) but defer the
  implementation to v1.x when there's user demand.

**Operational**

- Local: `./data/attachments` is a bind-mount in docker-compose.
- S3: bucket lifecycle policy should expire tombstones after 30 days.
- Backups: when using S3, snapshot the SQLite file only — attachments
  are already durable.

## When to revisit

- If we need **server-side encryption** of attachments at rest, we'd
  standardise on envelope encryption with a per-instance key, regardless
  of provider. The abstraction can absorb that without changing the interface.
- If we add **CDN-fronted downloads**, both providers gain a
  `GetPublicUrlAsync(key, ttl)` overload behind the same interface.

## Alternatives considered

1. **S3-only from day one.** Rejected: forces every self-hoster to run
   MinIO or pay AWS. Violates the "one container" promise.
2. **Local-only, add S3 later.** Tempting (YAGNI), but a) users with
   large attachment collections would hit the limit immediately, and b)
   the abstraction is cheap, so adding it now is cheaper than retrofitting.
3. **Use a database table for attachments (BLOBs in SQLite).** Rejected:
   bloats the DB file, makes backups slow, and gives up OS-level tooling
   (rsync, snapshots) that ops people already know.
4. **Direct-to-cloud uploads with pre-signed POST URLs.** Deferred to v1.x.
   Adds upload progress UX but isn't necessary for v1; the controller
   proxy is good enough until we hit the network-bandwidth bottleneck.

## References

- [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md) §7 (Storage Abstraction)
- [`src/PersonalCrm.Infrastructure/Storage/IAttachmentStore.cs`](../../src/PersonalCrm.Infrastructure/Storage/IAttachmentStore.cs)
- [`src/PersonalCrm.Infrastructure/Storage/LocalAttachmentStore.cs`](../../src/PersonalCrm.Infrastructure/Storage/LocalAttachmentStore.cs)
- [`src/PersonalCrm.Infrastructure/Storage/S3AttachmentStore.cs`](../../src/PersonalCrm.Infrastructure/Storage/S3AttachmentStore.cs)
