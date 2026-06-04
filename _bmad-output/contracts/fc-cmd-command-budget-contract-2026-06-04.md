# FC-CMD Command Budget Contract

Status: confirm-stable
Date: 2026-06-04
Owner: FrontComposer Product/UX + FrontComposer Shell + EventStore integration

## Decision

Story 3.6 adopts the v1 command lifecycle and polling budgets below.

| Budget | Value | Owner | Rationale |
| --- | ---: | --- | --- |
| Sync pulse threshold | `SyncPulseThresholdMs = 300` | Product/UX | Preserves existing brand-signal threshold for fast confirmations. |
| Still-syncing threshold | `StillSyncingThresholdMs = 2_000` | Product/UX | Preserves existing non-disruptive "Still syncing" surface. |
| Confirming to degraded/action-prompt threshold | `TimeoutActionThresholdMs = 10_000` | Product/UX | Preserves existing deterministic degraded prompt timing. |
| EventStore command-status poll cadence | `PendingCommandPollingIntervalMs = 1_000` | FrontComposer Shell + EventStore | Matches EventStore non-terminal `Retry-After: 1` cadence. |
| Max command-status polling duration | `MaxPendingCommandPollingDurationMs = 120_000` | FrontComposer Shell | Bounds unresolved pending commands; expiry resolves to `NeedsReview`. |
| Per-tick command poll cap | `MaxPendingCommandPollingPerTick = 25` | FrontComposer Shell | Preserves existing bounded oldest-first command polling work. |
| Retained pending-entry cap | `MaxPendingCommandEntries = 100` | FrontComposer Shell | Preserves existing circuit-local pending-entry cap. |
| Automatic client retry budget | `0` | Product/UX + FrontComposer Shell | Epic 3 does not auto-retry commands. |

## EventStore Cadence Hint

EventStore returns `Retry-After: 1` on non-terminal command-status responses. FrontComposer records
that hint as the default command-status polling cadence and exposes it as
`PendingCommandPollingIntervalMs = 1_000`. Local shell options remain authoritative, so adopters can
disable command-status polling with `PendingCommandPollingIntervalMs = 0` or adjust the cadence within
the validated range.

## Expiry Semantics

When a pending command remains unresolved for `MaxPendingCommandPollingDurationMs`, the command
polling coordinator resolves it to `PendingCommandStatus.NeedsReview` through the existing
`PendingCommandOutcomeResolver` and `PendingCommandStateService` mutation boundary. `NeedsReview`
remains visible in `FcPendingCommandSummary` and maps to the existing terminal review/rejection
lifecycle surface so the generated form does not remain in `Acknowledged` or `Syncing` forever.

Late terminal observations after expiry are duplicate terminal observations. The first terminal
outcome remains authoritative.

## Retry Scope

Automatic client command retry remains `0` for Epic 3. Epic 4 owns explicit retry/degraded retry
handling, destructive confirmation, abandonment policy, one-at-a-time execution, and authorization
gates.

## Escalations

None. Product/UX and EventStore did not provide alternative numeric budgets during implementation,
so the expected v1 defaults are the final values.
