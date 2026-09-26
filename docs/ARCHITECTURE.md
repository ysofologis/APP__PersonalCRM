# Architecture

> The technical counterpart to [`PROJECT.md`](PROJECT.md).
> Read PROJECT.md first for *what* and *why*; this doc covers *how*.

**Locked decisions (2026-09-26):**

- **License:** Apache 2.0
- **Database:** SQLite (default), EF Core keeps Postgres path open
- **Object storage:** S3-compatible from v1; local FS as default backend
- **Roadmap:** public from day one

---

## 1. Goals & Non-goals

### Goals

- Install with **one command** (`docker compose up`)
- Work **offline** as a PWA with seamless re-sync
- Stay **multi-user safe** under SQLite (WAL + connection pooling)
- Be **freely extensible** without paying for anything
- Be **auditable** end-to-end (every mutation leaves a trail)

### Non-goals (v1)

- Sales pipeline, deal stages, revenue forecasting
- Two-way email sync (IMAP / OAuth)
- Two-way calendar sync (Google / Outlook)
- Native mobile apps (PWA only)
- AI-assisted contact enrichment

---

## 2. Solution Layout

```
PersonalCrm.sln
├── src/
│   ├── PersonalCrm.App/             # ASP.NET Core host + Blazor hybrid
│   ├── PersonalCrm.Core/            # Domain (pure, no infra deps)
│   ├── PersonalCrm.Infrastructure/  # EF Core, S3, email, jobs
│   ├── PersonalCrm.Shared/          # DTOs/contracts shared Server ↔ WASM
│   └── PersonalCrm.Migrations/      # EF Core migration bundle (CLI tool)
├── tests/
│   ├── PersonalCrm.Tests.Unit/
│   ├── PersonalCrm.Tests.Integration/
│   └── PersonalCrm.Tests.E2E/       # Playwright
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── docs/
│   ├── PROJECT.md
│   ├── ARCHITECTURE.md (this file)
│   ├── ROADMAP.md
│   └── adr/                         # Architecture Decision Records
└── .github/workflows/ci.yml
```

### 2.1 Project responsibilities

| Project | Responsibility |
|---------|----------------|
| **PersonalCrm.Core** | Domain entities, value objects, domain services, domain events. **Zero infra deps.** |
| **PersonalCrm.Infrastructure** | EF Core `DbContext`, entity configurations, migrations. `IAttachmentStore` (Local + S3). `IEmailSender`. Background job runners. |
| **PersonalCrm.Shared** | DTOs, contracts, enums shared between Server and WASM. |
| **PersonalCrm.App** | Blazor Web App host (Server + WASM hybrid). Minimal API endpoints. Auth pipeline. Service Worker registration. Health checks. OpenTelemetry. |
| **PersonalCrm.Migrations** | CLI tool that runs EF Core migrations at startup or on demand. |
| **PersonalCrm.Tests.Unit** | Domain unit tests. No DB, no HTTP. |
| **PersonalCrm.Tests.Integration** | WebApplicationFactory + SQLite in-memory; API integration tests. |
| **PersonalCrm.Tests.E2E** | Playwright; PWA install + offline sync scenarios. |

---

## 3. Tech Stack

| Concern | Choice | Rationale |
|---------|--------|-----------|
| Runtime | **.NET 9** | Current LTS; Blazor Web App template mature |
| UI | **MudBlazor 7.x** | Material Design, MIT, strong DataGrid, free |
| ORM | **EF Core 9 + SQLite provider** | Same `DbContext` code can switch to Postgres later |
| DB | **SQLite 3.45+** | Single-file, zero-config, WAL for concurrency |
| Object storage | **AWSSDK.S3** | Works with AWS S3, MinIO, Backblaze B2, Cloudflare R2 |
| Hashing | **Konscious.Security.Cryptography.Argon2** | Argon2id — modern PHC winner |
| Email | **MailKit** | SMTP, sendmail, pickup directory |
| Background jobs | **Hangfire** (SQLite storage) | Cron, retries, dashboard |
| Validation | **FluentValidation** | Composable, testable |
| Logging | **Serilog + OpenTelemetry** | Structured, OTLP-exportable |
| Metrics | **prometheus-net** | `/metrics` endpoint, opt-in |
| Auth | **ASP.NET Core Identity** + custom JWT/refresh | Well-known, supports 2FA, OAuth providers |
| API docs | **Scalar** | Modern OpenAPI 3.1 UI |
| E2E | **Playwright** | Cross-browser PWA testing |

All packages are **free and open source**. Zero paid dependencies.

---

## 4. Data Model (high-level ER)

```
User ──< UserSession >── User ──< WorkspaceMember >── Workspace ──< WorkspaceMember
                                                                  │
                                              ┌───────────────────┼───────────────────┐
                                              │                   │                   │
                                              ▼                   ▼                   ▼
                                          Circle ──< ContactCircle >── Contact   Invitation
                                                                  │
                                              ┌───────────────────┼───────────────────┐
                                              │                   │                   │
                                              ▼                   ▼                   ▼
                                          Tag ──< ContactTag  Note            Interaction ──< InteractionContact >── (back)
                                                                  │                  │
                                                                  ▼                  ▼
                                                          CustomFieldValue      Attachment (metadata)
                                                                                 Outbox (offline-sync mirror)
                                                                                 Reminder
                                                                                 Comment
                                                                                 AuditLog
```

### 4.1 Key tables

- `users`, `user_sessions`, `refresh_tokens`
- `workspaces`, `workspace_members` (with role enum)
- `workspace_invitations` (signed, time-limited)
- `contacts`, `contact_methods` (emails, phones, addresses)
- `circles`, `contact_circles`
- `tags`, `contact_tags`
- `custom_fields`, `custom_field_values`
- `interactions`, `interaction_contacts`, `interaction_attachments`
- `attachments` — metadata only; bytes in object storage
- `reminders`
- `notes`
- `outbox` — offline-sync queue mirror
- `audit_logs`
- `comments`, `comment_mentions`

### 4.2 Relationship strength

Computed property on `Contact`, **not stored**:

```csharp
double ComputeStrength(Contact c, IEnumerable<Interaction> recent) =>
    recent.Sum(i => Decay(i.OccurredAt) * Weight(i.Kind));
```

- Decay = `exp(-ageDays / 90)`
- Weights: `call=1.0, meeting=1.2, gift=1.5, message=0.5, email=0.4, event=0.8`
- Recomputed on read or on a 5-min background tick; cached per workspace

---

## 5. Authentication & Authorization

### 5.1 Password flow

1. User registers → password hashed with **Argon2id** (memory=64MB, iterations=3, parallelism=4)
2. Email verification link (24h expiry, signed)
3. Login → short-lived **JWT (15 min)** in `Authorization` header + **refresh token (30 days)** in `httpOnly`, `Secure`, `SameSite=Lax` cookie
4. Refresh tokens stored **hashed** in `refresh_tokens`; rotated on each use; revocable per session

### 5.2 Two layers of authorization

- **Instance-level**: instance admin vs regular user
- **Workspace-level roles**: `Owner > Admin > Editor > Viewer`

Enforced via:
- ASP.NET Core authorization policies on controllers
- A **global query filter** in EF Core scoping every read/write to workspaces the user belongs to:

```csharp
modelBuilder.Entity<Contact>()
    .HasQueryFilter(c => _currentUser.WorkspaceIds.Contains(c.WorkspaceId));
```

### 5.3 Two-factor auth

TOTP via `Otp.NET`. Secret encrypted at rest using instance-level symmetric key (DataProtection). Backup codes generated on enrollment.

### 5.4 OAuth providers

Optional, configured per instance via admin settings. Default: **off**. Supported: Google, GitHub, Apple.

---

## 6. PWA & Offline Sync

### 6.1 Install + read offline

- `manifest.webmanifest` with icons (192, 512, maskable), theme color, `display=standalone`
- Service Worker (`/sw.js`) precaches:
  - WASM shell + runtime
  - App manifest
  - App icons
- **IndexedDB** stores:
  - `contacts` — last 200 viewed contacts' summaries
  - `interactions` — last 30 days of interactions per visible contact
  - `outbox` — pending writes

### 6.2 Offline write — the interesting bit

When the user is offline and creates an interaction:

1. WASM client generates a **client-generated ID** (UUIDv7)
2. Writes to IndexedDB `outbox` with `status = pending`
3. UI optimistically renders the interaction
4. Service Worker registers a **Background Sync** trigger (`'sync-outbox'`)
5. When online, SW replays the outbox: `POST /api/workspaces/{ws}/interactions` with the same ID
6. Server uses the **client-generated ID as idempotency key** to dedupe

If Background Sync API is unsupported (Safari), WASM client retries on the next `online` event with exponential backoff.

### 6.3 Conflict resolution

- Server stores a monotonic `version` per mutable record
- Client stores last-known `version`
- On sync:
  - **Same version** → apply, bump
  - **Server ahead, non-note fields** → last-write-wins; client shows "Replaced" toast
  - **Note content conflict** → 3-way merge (base + server + client) with manual fallback UI

---

## 7. Storage Abstraction

```csharp
public interface IAttachmentStore
{
    Task<string> PutAsync(Stream data, string filename, string contentType, CancellationToken ct);
    Task<Stream> GetAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct);
}
```

Implementations:

| Implementation | Selected by | Notes |
|----------------|-------------|-------|
| `LocalAttachmentStore` | `AttachmentStore:Provider=local` (default) | Writes to `./data/attachments`; zero external deps |
| `S3AttachmentStore` | `AttachmentStore:Provider=s3` | AWSSDK.S3; works with MinIO, AWS, R2, B2 |

Selection is **per-instance**, via env config. No code changes to swap.

---

## 8. SQLite for Multi-user

Single SQLite file at `./data/personal-crm.db` (overridable).

Pragmas applied at startup:

```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA foreign_keys = ON;
PRAGMA busy_timeout = 5000;
PRAGMA temp_store = MEMORY;
```

EF Core connection:

```csharp
options.UseSqlite(connString, o => o.CommandTimeout(30));
```

### 8.1 Concurrency model

- Connection pool via `Microsoft.Data.Sqlite`
- One connection per HTTP request (scoped `DbContext`)
- WAL allows many concurrent readers + one writer

If/when this stops being enough (50+ concurrent writers), the path forward is `UseNpgsql` — schema is provider-agnostic.

### 8.2 Backup

Two strategies, both shipped:

- **Online**: `VACUUM INTO` + WAL checkpoint, copied to timestamped file
- **Live mirror**: SQLite backup API streamed to a destination

Daily cron via Hangfire; retention configurable. Optional push to S3 via the same `IAttachmentStore`.

---

## 9. Background Jobs (Hangfire)

Hosted in the same process; SQLite-backed dashboard at `/hangfire` (admin-only).

| Job | Schedule | Purpose |
|-----|----------|---------|
| `ReminderDispatcher` | every 5 min | Send due reminders (push + email) |
| `CadenceEvaluator` | every 15 min | Flag lapsed cadences |
| `OutboxReconciler` | every 5 min | Clear orphaned outbox entries |
| `DatabaseBackup` | daily 03:00 UTC | Snapshot + optional S3 push |
| `AuditLogCompactor` | weekly | Archive entries > 90 days |

---

## 10. Observability

- **Logs**: Serilog → console (JSON in production) + rolling file
- **Traces**: OpenTelemetry → OTLP if `Otel:OtlpEndpoint` set, otherwise no-op
- **Metrics**: prometheus-net at `/metrics` (opt-in via instance setting)
- **Health**:
  - `/healthz` — liveness, always 200
  - `/readyz` — readiness, checks DB + storage + SMTP

---

## 11. Security

- Argon2id passwords; no MD5/SHA fallback
- Cookies: `Secure`, `HttpOnly`, `SameSite=Lax`
- HSTS in production
- CSP with per-request nonce; Blazor's `app-styles`/`app-scripts` whitelisted
- Antiforgery tokens on state-changing form posts
- Rate limiting on `/api/auth/*` (10/min/IP)
- Webhook signatures for future outbound webhooks
- Field-level encryption for sensitive fields (TOTP secret, OAuth tokens) using `Microsoft.AspNetCore.DataProtection` with a per-instance key at `./data/keys/`

---

## 12. Deployment

### 12.1 docker-compose (default — Local FS)

```yaml
services:
  app:
    build: .
    ports: ["8080:8080"]
    volumes:
      - ./data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Data Source=/app/data/personal-crm.db
      - AttachmentStore__Provider=local
      - AttachmentStore__LocalRoot=/app/data/attachments
      - Smtp__Host=...
      - Smtp__Port=587
      - Smtp__Username=...
      - Smtp__Password=...
      - Auth__JwtSigningKey=...
      - Auth__JwtIssuer=https://crm.example.com
```

### 12.2 S3 mode

Set `AttachmentStore__Provider=s3` and add:

```yaml
      - AttachmentStore__S3__Endpoint=http://minio:9000
      - AttachmentStore__S3__Bucket=personal-crm
      - AttachmentStore__S3__Region=us-east-1
      - AttachmentStore__S3__AccessKey=...
      - AttachmentStore__S3__SecretKey=...
      - AttachmentStore__S3__ForcePathStyle=true   # for MinIO
```

Spin up a MinIO service in the same compose file when desired.

### 12.3 First-run wizard

On first launch, if no admin exists, app redirects to `/setup`:

1. Create admin account
2. Set site name + URL
3. Configure SMTP (optional, can defer)
4. Choose attachment provider (Local / S3)
5. Generate `Auth:JwtSigningKey` automatically (and persist to `./data/keys/`)

---

## 13. Testing Strategy

| Layer | Tool | Coverage target |
|-------|------|-----------------|
| Domain unit | xUnit | 80% on Core |
| Validators / mappers | xUnit + FluentValidation.TestHelper | 100% on validators |
| Repository / EF | xUnit + SQLite in-memory | key paths |
| API integration | xUnit + WebApplicationFactory | happy + 1 unhappy per endpoint |
| PWA E2E | Playwright | signup → log interaction → install → offline log → online sync |
| Load | k6 | reminder scheduler at 10k contacts |

CI runs all of the above on every PR; required to merge.

---

## 14. ADRs (Architecture Decision Records)

Lived in `docs/adr/`. Numbered. Initial three:

- `0001-blazor-hybrid-render-modes.md` — why InteractiveServer + InteractiveWebAssembly
- `0002-sqlite-default-with-postgres-future.md` — DB choice + escape hatch
- `0003-s3-compatible-storage-with-local-fallback.md` — attachment store

Each ADR: **Context → Decision → Consequences → Alternatives considered.**

---

## 15. Open Architectural Questions

| # | Question | Resolve before |
|---|----------|----------------|
| A1 | Hangfire vs `IHostedService` + `System.Threading.Channels` for v1? Hangfire adds schema; channels are simpler but no dashboard | v0.1 start |
| A2 | Pure-WASM render vs hybrid (Server + WASM) for v0.1? Hybrid is more complex but unlocks offline | v0.1 start |
| A3 | Custom auth or full ASP.NET Core Identity? Identity is heavy but well-known | v0.1 start |
| A4 | MinIO in default compose or local FS only? Default install should be **one container** | v0.1 start |
| A5 | Notes storage: Markdown in TEXT column, or JSON for structured extensions? | v0.1 start |

---

*Document version: 0.1 — drafted 2026-09-26.*
