# Story 12.3 Pending-Command Provider Release Note

## Release Constraint

Hexalith FrontComposer v1 ships pending-command fallback polling with the `IPendingCommandStatusQuery` seam registered to `NullPendingCommandStatusQuery` by default. Provider-backed EventStore pending-command status is not claimed for v1 because the EventStore status endpoint URL, schema, validator, retry, and reconnect-epoch contract is not yet stable in repository evidence.

## Release Note Wording

Command dispatch, lifecycle registration, live projection nudges, reconnect reconciliation, and bounded fallback polling remain supported. Direct EventStore-backed pending-command status polling is an accepted v1 constraint: hosts must not represent the null provider as provider-backed readiness until a stable EventStore status resource contract is implemented and validated.

## Constraint Metadata

| Field | Value |
| --- | --- |
| Constraint name | `PENDING-STATUS-NULL-PROVIDER-V1` |
| Final outcome | Named accepted v1 constraint |
| Owner | Shell/EventStore integration owner |
| Trigger watcher | Release owner role |
| Linked row | `DW-0461` |
| Related rows | `DW-0465`, `DW-0232`, `DW-0469` |
| User/operator impact | Command lifecycle still depends on live nudges, reconnect reconciliation, and bounded fallback polling; no provider-backed direct status endpoint is certified. |
| Agent impact | Agents must not claim provider-backed pending-command status or use EventStore status-resource metadata as stable contract input for v1. |
| Release/package impact | Release notes must carry this constraint before package promotion claims command lifecycle readiness. |
| Expiry/revalidation trigger | Revalidate by 2026-06-30 or immediately when EventStore publishes a stable status endpoint URL/schema/validator/retry/epoch contract. |
| Reopen event | Reopen `DW-0461` before any provider-backed readiness claim, status-resource metadata consumption, or EventStore endpoint promotion. |
