# Roadmap

> This is a living document. Releases land roughly in the order listed below,
> but priorities shift based on user feedback. The authoritative source of
> truth for in-flight work is the [GitHub Project board](#project-board).

**Status legend:** ☐ planned · ◐ in progress · ✅ shipped

---

## v0.1 — MVP (~6 weeks)

> Single-user, single-workspace. Just enough to prove the model.

- ◐ Project skeleton (this commit)
- ☐ Auth: email/password with email verification
- ☐ Workspace bootstrap on first signup
- ☐ Contact CRUD (name, photo, methods, tags, circles)
- ☐ Interaction logging (call, meeting, message, gift, event)
- ☐ Per-contact timeline view
- ☐ Basic reminders (one-off + birthday)
- ☐ Docker Compose single-container install
- ☐ First-run wizard
- ☐ English only
- ☐ CI: build + unit tests on Linux

**Done when:** I can install via `docker compose up`, create a contact, log
an interaction, set a reminder, and receive it by email.

---

## v0.2 — PWA + Multi-user (+4 weeks)

> Where it stops being a toy and starts being a product.

- ☐ Workspaces (multiple per user)
- ☐ Member invitations via signed email link
- ☐ Roles: Owner / Admin / Editor / Viewer
- ☐ PWA manifest + installable
- ☐ Service Worker + IndexedDB cache
- ☐ Offline **read** for cached contacts/timelines
- ☐ Offline **write** with optimistic rendering + outbox replay
- ☐ Background Sync API + online-event fallback (Safari)
- ☐ Web push notifications (VAPID)
- ☐ In-app notification center

**Done when:** I can install the PWA on my phone, log an interaction while
in airplane mode, and have it sync the moment I regain signal.

---

## v0.3 — Polish + Intelligence (+4 weeks)

> The version I'd let my friends use.

- ☐ Stay-in-touch cadences with auto-flagging
- ☐ Relationship strength score (recency × frequency × type)
- ☐ Dashboard widgets: "haven't talked to in a while", upcoming birthdays
- ☐ Calendar view (interactions + reminders)
- ☐ Map view (contacts with addresses)
- ☐ vCard / CSV import
- ☐ CSV / vCard / JSON export
- ☐ Workspace audit log
- ☐ Custom fields per workspace
- ☐ Saved filters and views
- ☐ i18n scaffolding (English + community translations)

---

## v1.0 — GA

> Production-ready self-hosted product.

- ☐ Backup / restore (DB + attachments)
- ☐ Scheduled automated backups
- ☐ 2FA (TOTP) with backup codes
- ☐ OAuth providers (Google, GitHub, Apple) — opt-in per instance
- ☐ SMTP configuration UI
- ☐ Admin console (users, storage, audit log viewer)
- ☐ WCAG 2.1 AA compliance
- ☐ Documentation site
- ☐ Helm chart for Kubernetes
- ☐ One-click installers (YunoHost, CasaOS)
- ☐ Migration story for v0.x → v1.0

**Done when:** A non-technical user can self-host on a €4 VPS, survive
database loss via backups, and pass a basic security audit.

---

## v1.x — Ecosystem (post-GA)

- ☐ AI-assisted summaries of recent interactions (opt-in, BYO-LLM)
- ☐ Email-received parsing (parse forwarded mail into interactions)
- ☐ Calendar two-way sync (Google / Outlook)
- ☐ Native mobile wrapper (Tauri or Capacitor) — only if PWA gap is real
- ☐ S3-compatible backup target
- ☐ Public API + webhooks
- ☐ Plugin / extension system

---

## Won't do (v1.x and beyond)

These are **deliberately out of scope**:

- ❌ Sales pipeline (deals, stages, revenue forecasting)
- ❌ Marketing automation (drip campaigns, mass mail)
- ❌ SMS or telephony integrations
- ❌ AI-assisted contact enrichment (Clearbit-style)
- ❌ Paywalled features — OSS stays free; funded by sponsorships and optional hosted cloud

---

## Project board

In-flight work: <https://github.com/<org>/personal-crm/projects>

## RFCs

Major changes go through a public RFC process before implementation.
See [`docs/adr/`](docs/adr/) for design decisions and [`docs/rfcs/`](docs/rfcs/)
for proposals under discussion.

---

*Last updated: 2026-09-26.*
