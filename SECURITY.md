# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| latest `main` | ✅ |
| latest released tag | ✅ |
| older releases | ❌ |

The project is in **pre-alpha**. There are no production releases yet.
Security fixes land on `main` and are tagged as they ship.

## Reporting a vulnerability

**Please do not file a public GitHub issue for security vulnerabilities.**

Email **security@personal-crm.example** (PGP key: link TBD). Include:

- A clear description of the issue and its impact
- Steps to reproduce, or a proof-of-concept
- The commit hash / version affected
- Your name / handle for credit (optional)

We will:

1. Acknowledge within **72 hours**
2. Provide a triage assessment within **7 days**
3. Coordinate a fix and a disclosure date with you
4. Credit you in the release notes (unless you prefer to remain anonymous)

We follow a **90-day** disclosure window from the day we acknowledge the
report. If a fix requires more time, we will negotiate a new window with you.

## Threat model (in scope)

Personal CRM is a self-hosted web application. The threat model covers:

| Concern | In scope |
|---------|----------|
| Authentication bypass | ✅ |
| Authorization (workspace isolation) bypass | ✅ |
| SQL/NoSQL injection | ✅ |
| Cross-site scripting (XSS) | ✅ |
| Cross-site request forgery (CSRF) | ✅ |
| Server-side request forgery (SSRF) | ✅ |
| Remote code execution | ✅ |
| Cryptographic weaknesses (password hashing, token storage) | ✅ |
| PII exfiltration | ✅ |
| Supply chain (NuGet package compromise) | ✅ |
| Privilege escalation | ✅ |

## Out of scope

- Vulnerabilities in third-party services users integrate with (SMTP, S3, OAuth providers)
- Attacks requiring physical access to the host
- Denial of service against the host itself (use a reverse proxy)
- Self-XSS / social engineering of the operator
- Reports from automated scanners without a working PoC

## Security design principles

These are non-negotiable in this project:

1. **Argon2id** for passwords; no MD5/SHA fallback
2. **httpOnly, Secure, SameSite=Lax** cookies for sessions
3. **CSP** with per-request nonce; Blazor's inline styles/scripts whitelisted
4. **EF Core parameterization** for all queries — no string concatenation
5. **Rate limiting** on authentication endpoints
6. **Audit log** for every mutation
7. **Field-level encryption** for sensitive fields (TOTP secret, OAuth tokens)
8. **HTTPS-only** in production (HSTS)
9. **Per-instance signing keys** persisted to disk with restricted permissions
10. **No telemetry** unless the instance admin explicitly opts in

## Hardening guides

Coming after v1.0:

- Reverse proxy recommendations (Caddy, Traefik, Nginx)
- Running behind Cloudflare / Tailscale
- Encrypted-at-rest backup strategy
- Multi-user isolation audit checklist

## Security hall of fame

We thank the following researchers for responsible disclosure (none yet —
be the first!):

_— empty —_
