# Personal CRM — Project Brief

> A multi-user, self-hosted, PWA-first personal CRM.
> Built in Blazor Web App (.NET 9, Server + WASM hybrid) with MudBlazor.
> Free and open source.

---

## 1. Vision

Most CRMs are built for sales teams, not people. They optimize for pipelines,
deals, and revenue — and treat the people in your life as rows to be worked.
The result: people who matter to you slip through the cracks, and you only
notice when it's too late.

**Personal CRM** is the opposite. It's a tool for keeping the human in
*human relationship*. Who did I promise to call back? When's the last time
I spoke to my mentor? My sister's birthday is next week. The colleague I
owe a coffee.

It's not a sales tool. It's a memory aid for people who refuse to let
relationships decay.

### 1.1 Target users

| Segment | Pain |
|--------|------|
| **Freelancers & consultants** | Stay in touch with clients between projects without a sales CRM |
| **Founders & operators** | Maintain investor, advisor, and peer relationships deliberately |
| **Community organizers** | Track members, volunteers, sponsors across many touchpoints |
| **Researchers / academics** | Long-running professional relationships that span years |
| **Personal users** | Family, friends, mentors — people you simply don't want to forget |

### 1.2 Positioning vs alternatives

| Tool | Model | Gap this project fills |
|------|-------|-------------------------|
| Monica CRM | Self-hosted PHP | Dated UI, no PWA, single-user first |
| EspoCRM | Self-hosted, sales-focused | Heavy, sales-pipeline-shaped |
| folk, Clay, Dex | SaaS only | Lock-in, recurring fees, your contacts on their servers |
| Notion / Airtable | Generic | No relationship primitives, no reminders, no cadence logic |

**Our wedge:** modern UX, PWA-first, multi-user with privacy boundaries,
100% free, runs on your hardware.

---

## 2. Product Principles

1. **Your data, your hardware.** Self-hosted by default. No telemetry without
   explicit opt-in.
2. **Privacy by default.** Each user has a private workspace. Sharing is
   opt-in.
3. **PWA-first, native when needed.** Installable, offline-capable. Skip the
   app stores until there is a clear reason.
4. **Free means free.** AGPL-licensed core. No paid feature tier. No
   "enterprise edition." Sponsored development, optional hosted offering
   later.
5. **One person, one primary device, often offline.** Design for the train,
   the plane, the dodgy hotel Wi-Fi.

---

## 3. Business Requirements

### 3.1 Hard constraints (from product decision)

| Constraint | Implication |
|------------|-------------|
| **Multi-user** | First-class auth, tenant isolation, shared workspaces with roles |
| **Self-hosted** | Single `docker compose up` install; works on a €4 VPS or a homelab NAS |
| **PWA** | Installable, offline-first, push notifications, no app store |
| **Free OSS** | AGPL-3.0 (see §3.3); zero paid third-party dependencies |

### 3.2 Distribution

- **Docker image** published to GHCR (`ghcr.io/<org>/personal-crm`)
- **docker-compose.yml** for single-node install (Postgres + app)
- **Helm chart** for Kubernetes (post-MVP)
- **One-click installers**: YunoHost, CasaOS, Umami-style guides
- **Migration tool**: scripted upgrades between versions

### 3.3 Licensing

- **Core**: **Apache License 2.0** (locked 2026-09-26)
  - Maximizes adoption: anyone can use, modify, and self-host freely.
  - Includes an explicit patent grant — important as the project grows.
  - Aligns with the broader .NET / cloud-native ecosystem (Kubernetes, Swift,
    TensorFlow, .NET runtime, ASP.NET Core).
  - Trade-off acknowledged: SaaS competitors can fork without contributing
    back. We mitigate this through strong community governance (§3.5),
    public roadmap, and a hosted-cloud offering that funds upstream work —
    not through copyleft.
- **Brand & logo**: CC-BY-SA 4.0
- **Documentation**: CC-BY-SA 4.0

### 3.4 Monetization stance

OSS-funded, never paywalled:

- GitHub Sponsors / OpenCollective
- Optional **managed cloud** offering later (hosted by us, separate code path,
  does **not** gate features)
- Support contracts for organizations that want SLAs

The OSS version never becomes a crippled free tier. There is no tier above it
that you must pay for to unlock features.

### 3.5 Governance

- Public roadmap, public RFC process for breaking changes
- CODEOWNERS, contributor license agreement (CLA) — individual + corporate
- Code of Conduct (Contributor Covenant)
- Security policy (`SECURITY.md`) with disclosure timeline

---

## 4. Functional Requirements

Each requirement has a stable ID (`F-<domain>-<n>`) so it can be referenced
from issues, tests, and the changelog.

### 4.1 Accounts & authentication — `F-AUTH`

| ID | Requirement |
|----|-------------|
| F-AUTH-1 | Email + password registration with email verification |
| F-AUTH-2 | Password reset via email |
| F-AUTH-3 | OAuth providers (Google, GitHub, Apple) — opt-in per instance |
| F-AUTH-4 | Two-factor auth (TOTP) |
| F-AUTH-5 | Session management (revoke active sessions, view device list) |
| F-AUTH-6 | Per-user language and timezone preference |

### 4.2 Workspaces & multi-user — `F-WS`

| ID | Requirement |
|----|-------------|
| F-WS-1 | Each user has a default **private workspace** on signup |
| F-WS-2 | User can create additional workspaces (e.g. *Family*, *Volunteering*) |
| F-WS-3 | Workspaces are isolated: contacts and interactions in workspace A are invisible to workspace B |
| F-WS-4 | Workspace member roles: **Owner**, **Admin**, **Editor**, **Viewer** |
| F-WS-5 | Invite members via email link (signed, time-limited) |
| F-WS-6 | Audit log of member actions (add/remove, role change) |
| F-WS-7 | Workspace-level settings: name, icon, default reminder cadence |

### 4.3 Contacts — `F-CONTACT`

| ID | Requirement |
|----|-------------|
| F-CONTACT-1 | Core fields: name, photo, pronouns, birthday, anniversary |
| F-CONTACT-2 | Contact methods: emails, phone numbers, addresses, social handles, websites |
| F-CONTACT-3 | Multi-valued contact methods with primary designation (e.g. primary email) |
| F-CONTACT-4 | Free-form tags (workspace-scoped) |
| F-CONTACT-5 | **Circles** — a contact can belong to one or more (Family, Work, Mentors, ...) |
| F-CONTACT-6 | Custom fields per workspace (text, number, date, single-select) |
| F-CONTACT-7 | Rich-text notes per contact (Markdown) |
| F-CONTACT-8 | Soft delete + restore (30-day window) |
| F-CONTACT-9 | Merge duplicates (preview diff before commit) |

### 4.4 Interactions — `F-INTERACT`

| ID | Requirement |
|----|-------------|
| F-INTERACT-1 | Log interactions: call, meeting, email, message, gift, event, other |
| F-INTERACT-2 | Each interaction has: date, direction (inbound/outbound), summary, location, duration |
| F-INTERACT-3 | Link one or more contacts to an interaction |
| F-INTERACT-4 | Attach files / photos to an interaction |
| F-INTERACT-5 | Optional follow-up reminder created from an interaction ("call back in 2 weeks") |
| F-INTERACT-6 | Reverse-chronological timeline per contact |
| F-INTERACT-7 | Global activity feed across contacts in the workspace |

### 4.5 Reminders & cadences — `F-REMIND`

| ID | Requirement |
|----|-------------|
| F-REMIND-1 | **Stay-in-touch cadence** per contact (e.g. every 6 weeks) |
| F-REMIND-2 | When cadence lapses, a reminder surfaces in the dashboard |
| F-REMIND-3 | Important-date reminders: birthdays, anniversaries, custom dates |
| F-REMIND-4 | One-off reminders with date and note |
| F-REMIND-5 | Notification channels per user: in-app, **web push** (PWA), email |
| F-REMIND-6 | Snooze / dismiss / mark-done from the notification |

### 4.6 Views & search — `F-VIEW`

| ID | Requirement |
|----|-------------|
| F-VIEW-1 | List view with column chooser, sort, multi-filter, saved filters |
| F-VIEW-2 | Grid (cards) view with cover photos |
| F-VIEW-3 | Calendar view (interactions + reminders) — month / week / day |
| F-VIEW-4 | Map view (contacts with addresses) |
| F-VIEW-5 | Global search across contacts, notes, interactions (typo-tolerant) |
| F-VIEW-6 | Saved searches per workspace |

### 4.7 Relationship intelligence — `F-INSIGHT`

| ID | Requirement |
|----|-------------|
| F-INSIGHT-1 | **Relationship strength** computed from interaction frequency and recency |
| F-INSIGHT-2 | **"You haven't talked to X in a while"** dashboard widget |
| F-INSIGHT-3 | Birthday / anniversary upcoming widget (next 30 days) |
| F-INSIGHT-4 | Per-contact AI summary of recent interactions (LLM, optional) |

### 4.8 PWA capabilities — `F-PWA`

| ID | Requirement |
|----|-------------|
| F-PWA-1 | Installable (manifest, icons, splash screens) |
| F-PWA-2 | **Offline read** — recent contacts and timeline cached |
| F-PWA-3 | **Offline write** — new interactions queued, synced on reconnect |
| F-PWA-4 | **Web push notifications** via VAPID (opt-in) |
| F-PWA-5 | Background sync for queued writes |
| F-PWA-6 | Install prompt with custom "Add to Home Screen" UI |

### 4.9 Sharing & collaboration — `F-COLLAB`

| ID | Requirement |
|----|-------------|
| F-COLLAB-1 | Share a contact into a shared workspace with a single action |
| F-COLLAB-2 | Per-contact ACLs: editor / commenter / viewer (post-MVP) |
| F-COLLAB-3 | Comments on contacts and interactions (threaded) |
| F-COLLAB-4 | @mentions of workspace members in comments |
| F-COLLAB-5 | Activity audit log (who edited what, when) |

### 4.10 Import / export — `F-IO`

| ID | Requirement |
|----|-------------|
| F-IO-1 | Import **vCard (.vcf)** files |
| F-IO-2 | Import **CSV** with column mapping UI |
| F-IO-3 | Export full workspace as **JSON** (lossless) |
| F-IO-4 | Export contacts as **CSV** and **vCard** |
| F-IO-5 | Full workspace backup + restore (DB dump + attachments bundle) |
| F-IO-6 | Scheduled automated backups (configurable) |

### 4.11 Self-hosted admin — `F-ADMIN`

| ID | Requirement |
|----|-------------|
| F-ADMIN-1 | First-run wizard (create admin, set site URL, configure email) |
| F-ADMIN-2 | SMTP / email provider configuration |
| F-ADMIN-3 | Storage usage breakdown (DB + attachments) |
| F-ADMIN-4 | User management (disable, delete, change role) |
| F-ADMIN-5 | Instance settings: site name, logo, default language |
| F-ADMIN-6 | Update notifications (with one-click upgrade for Docker setups) |
| F-ADMIN-7 | Audit log viewer |
| F-ADMIN-8 | Optional anonymous usage stats (off by default) |

### 4.12 Internationalization — `F-I18N`

| ID | Requirement |
|----|-------------|
| F-I18N-1 | All UI strings externalized |
| F-I18N-2 | User-selectable language |
| F-I18N-3 | Right-to-left (RTL) support for Arabic / Hebrew |
| F-I18N-4 | Locale-aware date / time / number formatting |
| F-I18N-5 | English shipped at launch; community translations thereafter |

---

## 5. Non-functional Requirements

| Area | Target |
|------|--------|
| **First contentful paint (PWA, cold)** | ≤ 1.5 s on broadband |
| **Time to interactive (cached)** | ≤ 500 ms |
| **API p95 latency** | ≤ 200 ms for typical CRUD |
| **Offline-to-online sync conflict policy** | Last-write-wins with conflict log; per-field merge for notes |
| **Attachment storage** | Local filesystem by default; S3-compatible via env config |
| **Database** | PostgreSQL 15+ (SQLite acceptable for personal/single-user installs) |
| **Auth tokens** | Argon2id for passwords; short-lived JWT + refresh |
| **Encryption at rest** | Optional per-instance symmetric key for sensitive fields |
| **Backups** | Daily local + optional S3 upload |
| **Accessibility** | WCAG 2.1 AA |
| **Browser support** | Latest 2 versions of Chrome, Edge, Firefox, Safari |

---

## 6. Out of Scope (v1)

- Sales pipeline (deals, stages, revenue forecasting)
- Email **two-way sync** (IMAP / OAuth)
- Calendar **two-way sync** (Google Calendar, Outlook)
- SMS or telephony integrations
- Marketing automation (drip campaigns, mass mail)
- Native mobile apps (PWA only at v1)
- AI-assisted contact enrichment (Clearbit-style)

These are intentionally deferred. The product must ship sharp and small before
it grows new limbs.

---

## 7. Success Metrics

We optimize for **adoption and retention**, not revenue (it's OSS).

| Metric | Target (v1) |
|--------|-------------|
| **Time to first logged interaction** (signup → first entry) | < 2 min |
| **Weekly active workspaces / total** | ≥ 35% |
| **Push notification opt-in** (of eligible users) | ≥ 40% |
| **Self-hosted instances running** (opt-in ping) | Track, no target |
| **Docker pull rate** | Track, no target |
| **GitHub stars** | Track, no target |
| **Critical bugs per release** | 0 P0 at GA |

---

## 8. High-level Roadmap

| Version | Theme | Highlights |
|---------|-------|------------|
| **v0.1 (MVP, ~6 weeks)** | Single-user, single-workspace | Auth, contacts CRUD, interactions, timeline, basic reminders, Docker compose, English only |
| **v0.2 (Beta, +4 weeks)** | PWA + multi-user | Workspaces, invites, roles, PWA install, offline read, push |
| **v0.3 (RC, +4 weeks)** | Polish + intelligence | Cadences, relationship strength, CSV/vCard import-export, i18n scaffolding |
| **v1.0 (GA)** | Production-ready | Backup/restore, audit log, admin console, WCAG AA, documentation site |
| **v1.x (post-GA)** | Ecosystem | Helm chart, OAuth providers, AI summaries (opt-in), community translations |

---

## 9. Decisions Log

All open questions resolved on **2026-09-26**:

| # | Decision | Rationale |
|---|----------|-----------|
| Q1 | **Apache License 2.0** | Maximizes adoption; patent grant; ecosystem alignment |
| Q2 | **SQLite as the v1 database** | Zero-config single-file install; WAL mode handles moderate concurrency; EF Core keeps a clean Postgres path for later |
| Q3 | **S3-compatible storage from v1**, with local filesystem as the default backend | `IAttachmentStore` abstraction; users swap providers via env config without code changes |
| Q4 | **Email-received parsing deferred** past v1 | Out of scope until core CRUD + PWA ship; revisit in v1.x |
| Q5 | **Public roadmap from day one** | `ROADMAP.md` and GitHub Discussions live at launch; signals project maturity and welcomes contributions early |

---

## 10. Glossary

| Term | Meaning |
|------|---------|
| **Workspace** | A logical container for contacts, owned by one or more users |
| **Circle** | A named grouping of contacts (Family, Mentors, Work, …) |
| **Interaction** | A logged event with a contact: call, meeting, message, gift, etc. |
| **Cadence** | A user-defined "stay in touch" interval per contact |
| **Strength** | Computed score reflecting interaction recency and frequency |

---

*Document version: 0.1 — drafted 2026-09-26.*
*Status: draft for review.*
