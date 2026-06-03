---
title: 'FrontComposer — Implementation Readiness Request'
project: 'Hexalith.FrontComposer'
author: 'Administrator'
date: '2026-06-03'
type: 'readiness-request'
status: 'open'
---

# FrontComposer — Implementation Readiness Request

> **Lead ask:** Confirm these reusable contracts against your Shell source — `FC-LYT`,
> `FC-CMD`, `FC-A11Y`, `FC-L10N`, `FC-DOC` (+ own the `FC-CNC` one-at-a-time policy),
> decide the retry/timeout/polling budgets with Product/UX + EventStore, and answer our
> Shell-integration spike questions. **Do not build `<AuditTimeline>` / `<ConsequencePreview>`
> now** — the fallbacks are approved; track the rich components as fast-follow.

## Asks — ordered by what they unblock

| Priority | Ask | Owner | Unblocks |
|----------|-----|-------|----------|
| 🔴 1 | **FC-LYT** — confirm full-width / constrained `<PageLayout>` contract (`Shell/Components/Layout/FrontComposerShell.razor`) | FrontComposer + Product/UX | even the read-only MVP |
| 🔴 1 | **FC-A11Y / FC-L10N / FC-DOC** — confirm accessibility primitives, shell-vs-Tenants string ownership (`FcShellResources.resx`), component docs | FrontComposer + Tenants author | every story's ready-gate |
| 🔴 1 | **Shell-integration spike** — verify `AddHexalithFrontComposer*` / manifest / projection-routing / `FC-TBL` APIs (your Story 1.0; FC answers API questions) | Tenants dev (FC supports) | bootstrap (Story 1.1) |
| 🟠 2 | **FC-CMD** — confirm command-lifecycle contract: pending-identity / correlation-key shape (26-char checkout shape not yet approved), uniqueness scope (per-tenant / user / circuit?), lifecycle ownership, `alreadyApplied`, reconciliation | FrontComposer | all commands (Epics 3–5) |
| 🟠 2 | **FC-CNC** — confirm one-at-a-time is the v1 contract (fallback already approved; batching = fast-follow) | FrontComposer + Product/UX | rapid / destructive sequences |
| 🟠 2 | **Numeric budgets** — confirming→degraded threshold, polling budget, retry budget (none approved yet) | Product/UX + FrontComposer + EventStore | command phases |
| 🟡 3 | **EventStore status contract** — confirm the command-status query the FC polling coordinator binds to (`GET /api/v1/commands/status/{id}` already exists → confirm-stable, not build-new) | EventStore maintainers | command confirmation |

## Notes

- **Approved fallbacks / out-of-scope-for-now:** `<AuditTimeline>` and `<ConsequencePreview>`
  rich components are explicitly *not* built for v1 — fallbacks are approved; track as fast-follow.
- **Legend:** 🔴 1 = blocks the read-only MVP / bootstrap · 🟠 2 = blocks command epics (3–5) ·
  🟡 3 = confirm-stable (existing surface), not build-new.
