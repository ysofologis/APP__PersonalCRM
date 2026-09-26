# ADR 0001 — Blazor Web App with Interactive Server + Interactive WebAssembly render modes

- **Status:** Accepted
- **Date:** 2026-09-26
- **Deciders:** Maintainers

## Context

The project is a PWA that must work offline on a phone, run on the open
internet behind a self-hosted URL, and stay maintainable by a small team.
We need to choose *how* the UI runs.

The three viable options on .NET 9:

| Option | Server load | Offline-capable | Complexity | Notes |
|--------|-------------|-----------------|------------|-------|
| Blazor Server only | High (every interaction = SignalR frame) | No | Low | Stateful, fragile to network blips, single host only |
| Blazor WebAssembly standalone | None (UI runs in browser) | Yes (with SW + IDB) | Medium | All API auth via JWT in browser; no SignalR push; UI bundle ships big |
| Blazor Web App **hybrid** (`InteractiveServer` + `InteractiveWebAssembly`) | Medium — SignalR for hot pages only | Yes — pages can be promoted to WASM at will | Medium | .NET 9 template default; per-component render mode |

## Decision

Adopt **Blazor Web App with both render modes** enabled (the .NET 9
`blazor` template default). Default per-component render mode is
`InteractiveServer`; pages that must work offline or do heavy client work
(contact editor, search) opt into `InteractiveWebAssembly` at the route or
component level.

Concretely:

- `App.razor` runs `InteractiveServer` (always-on shell, layout, nav).
- Per-page directive selects the render mode (`@rendermode=...InteractiveWebAssembly`).
- `program.cs` registers both render modes with `AddInteractiveServerComponents()` + `AddInteractiveWebAssemblyComponents()`.
- Auth uses a short-lived JWT in `Authorization` header (for WASM fetch) **and** a refresh cookie (so the server-rendered routes also work).

## Consequences

**Positive**

- Online UX: server-rendered navigation feels instant and renders without a JS bundle.
- Offline UX: critical pages opt into WASM and gain IndexedDB + Service Worker reach.
- Future flexibility: any page can move from Server to WASM with a one-line attribute change. No big-bang migration.

**Negative**

- Two execution contexts to reason about (Server vs WASM). Code in `PersonalCrm.Core` and `PersonalCrm.Shared` must work in both.
- Bundle size: the WASM runtime (~2 MB compressed) ships only to users who land on a WASM-rendered route; the server shell is small.
- Authentication must satisfy both modes — JWT for WASM, cookies for server. We standardise on "JWT in `Authorization` + refresh in httpOnly cookie" so both modes use the same envelope.

**Operational**

- Single deployable process; no separate WASM host.
- CDN-friendly: static assets live under `wwwroot/` and can be served from any reverse proxy.

## Alternatives considered

1. **Server-only with optimistic UI.** Rejected: can't deliver the offline experience that is the product's headline feature.
2. **Pure WASM (no Server).** Rejected: hurts perceived performance for simple navigation, requires re-implementing SSR concerns (meta tags, social previews) the framework would otherwise give us for free.
3. **MPA — server-rendered Razor Pages + a sprinkle of WASM islands.** Tempting (lower complexity), but loses Blazor's component model and the unified mental model it provides. Worth revisiting only if Server render mode ever proves to be a bottleneck.

## References

- [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md) §6 (PWA & Offline Sync)
- [Microsoft Learn: Blazor render modes in .NET 9](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)
