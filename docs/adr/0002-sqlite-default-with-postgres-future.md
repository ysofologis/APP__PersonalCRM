# ADR 0002 — SQLite as the v1 database, with Postgres as a future escape hatch

- **Status:** Accepted
- **Date:** 2026-09-26
- **Deciders:** Maintainers

## Context

The product is **self-hosted**. The default install must succeed on:

- A €4 VPS with 1 vCPU / 1 GB RAM
- A homelab NAS (Synology, TrueNAS)
- A Raspberry Pi 4 with USB SSD

…and must survive a `docker compose down && docker compose up` with zero
operational ceremony. This pushes hard against introducing a separate
database container in the default install.

At the same time, the product is **multi-user**. We must believe the
chosen database can carry a workspace of 5 users and a few thousand
contacts without surprises.

The two viable options:

| Option | Install footprint | Multi-user ceiling | Backup story | Operational cost |
|--------|-------------------|--------------------|--------------|------------------|
| **SQLite** | Zero — a single file on disk | Moderate (WAL + pooling) | `VACUUM INTO` or file copy | Trivial |
| PostgreSQL | Separate container, tuning required | High | `pg_dump`, point-in-time recovery | Real |

## Decision

Ship **SQLite** as the v1 database, configured as:

- WAL journal mode
- `busy_timeout = 5000`
- `foreign_keys = ON`
- One connection per request via the scoped `DbContext`

Postgres remains a **future escape hatch** that requires only a config
change (`ConnectionStrings__Default` + provider switch to
`UseNpgsql(...)`), because:

- Schema is provider-agnostic (no SQLite-specific types in domain code).
- EF Core migrations are written against the relational model; SQLite
  generates portable SQL.
- All queries use parameterised LINQ, so cross-provider quirks (date
  arithmetic, JSON ops) are checked in integration tests.

The DbContext is the single seam: swapping providers happens in
`Program.cs` via a config-driven branch.

## Consequences

**Positive**

- One container. One volume. One backup target. Matches the product promise.
- Tests use SQLite in-memory (or on-disk) for fast, hermetic runs.
- Operations team (us, plus users) needs to learn only `cp` / `rsync` for backup.

**Negative**

- Multi-user writes serialize through a single writer lock. WAL helps a lot, but a 50-user workspace with hot concurrent edits would feel it.
- Some Postgres-only features (e.g. `LISTEN/NOTIFY` for real-time fan-out) are unavailable. We use Hangfire polling and SignalR for those concerns instead.
- Cross-provider test matrix is opt-in. We'll add a Postgres CI job once a user reports needing it.

**Operational**

- A nightly `DatabaseBackup` job (Hangfire) runs `VACUUM INTO` and copies the result to `data/backups/` (and optionally to S3 via `IAttachmentStore`).
- The image ships a `PersonalCrm.Migrations` CLI tool that runs `dotnet ef database update` — useful for blue/green deploys and offline migrations.

## When to revisit

Move to Postgres if **any** of these become true:

1. A real instance reports SQLite writer-lock contention at > 10 concurrent mutators.
2. We need cross-instance replication for HA.
3. A feature depends on Postgres-only data types or operators that aren't worth emulating.

When we do, the cutover is:

```csharp
// Before
opt.UseSqlite(connStr);

// After
opt.UseNpgsql(connStr);
```

…plus a one-time data export/import via the JSON backup format already
shipped in v1.

## Alternatives considered

1. **Postgres-only from day one.** Rejected: too heavy for the default install footprint; would force every user to operate a second container.
2. **SQLite + LiteFS / rqlite for HA.** Rejected: brings an operational dependency (consensus) that the target audience doesn't want.
3. **DuckDB.** Tempting for analytics, but no concurrent-writer story; not a general-purpose RDBMS.

## References

- [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md) §8 (SQLite for Multi-user)
- [SQLite WAL mode documentation](https://www.sqlite.org/wal.html)
