# Personal CRM

> A multi-user, self-hosted, PWA-first personal CRM.
> Built in **Blazor Web App (.NET 9)** with **MudBlazor**.
> **Free and open source** under Apache 2.0.

⚠️ **Pre-alpha.** Not ready for production use. APIs and data shapes will change.

---

## Why

Most CRMs are built for sales teams, not people. **Personal CRM** is a tool
for keeping the human in *human relationship* — track interactions, remember
cadences, and never let important relationships silently decay.

It's not a sales tool. It's a memory aid for people who refuse to let
relationships decay.

## Features (planned)

- 👥 Multi-user with workspace isolation and roles (Owner / Admin / Editor / Viewer)
- 📇 Contacts with circles, tags, custom fields
- 📞 Interaction timeline (calls, meetings, messages, gifts, events)
- ⏰ Stay-in-touch cadences and reminders
- 📱 PWA: installable, offline-first with conflict resolution
- 🔔 Web push notifications
- 🔐 2FA (TOTP), OAuth providers (Google, GitHub, Apple), Argon2id passwords
- 💾 Your data, your hardware — SQLite by default, S3-compatible for attachments
- 🌐 Apache 2.0 — free as in freedom

See [`docs/PROJECT.md`](docs/PROJECT.md) for the full functional spec and
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the technical design.

## Quick start

Requires [Docker](https://docs.docker.com/get-docker/) and
[Docker Compose](https://docs.docker.com/compose/install/).

```bash
git clone https://github.com/<org>/personal-crm
cd personal-crm
docker compose -f docker/docker-compose.yml up
```

Open <http://localhost:8080> and complete the first-run wizard.

## Stack

- **.NET 9** — Blazor Web App (Server + WASM hybrid)
- **MudBlazor** — Material Design UI components
- **EF Core** + **SQLite** (default); Postgres-ready
- **AWSSDK.S3** — S3-compatible object storage (MinIO, AWS, R2, B2)
- **Hangfire** — background jobs
- **Serilog** + **OpenTelemetry** — logging and tracing

## Documentation

| Doc | Purpose |
|-----|---------|
| [`docs/PROJECT.md`](docs/PROJECT.md) | Business and functional requirements |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Technical architecture and decisions |
| [`docs/ROADMAP.md`](docs/ROADMAP.md) | Public roadmap |
| [`docs/adr/`](docs/adr/) | Architecture Decision Records |

## Contributing

We welcome contributions of any size. See [CONTRIBUTING.md](CONTRIBUTING.md)
for the workflow, coding standards, and how to file issues.

Please read our [Code of Conduct](CODE_OF_CONDUCT.md) before participating.

## Security

Found a vulnerability? Please follow the responsible disclosure process in
[SECURITY.md](SECURITY.md) — **do not file a public issue**.

## License

Copyright © 2026 The Personal CRM Contributors.

Licensed under the [Apache License, Version 2.0](LICENSE). You may use,
modify, and distribute this software under the terms of that license.
A copy of the license is included in this repository.
