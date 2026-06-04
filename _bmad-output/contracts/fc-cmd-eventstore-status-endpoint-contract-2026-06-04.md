# FC-CMD EventStore Status Endpoint Contract

Status: confirm-stable  
Date: 2026-06-04  
Owner: FrontComposer / EventStore integration

## Endpoint

- Method/path: `GET /api/v1/commands/status/{correlationId}`
- Controller evidence: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`
- Submit evidence: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`
- FrontComposer query identity: pending-command `MessageId`

The EventStore route parameter is named `correlationId`. FrontComposer's current
`EventStoreCommandClient` does not send an explicit EventStore `CorrelationId` in the submit body,
and EventStore `CommandsController` defaults `CorrelationId` to `MessageId` before returning
`Location: /api/v1/commands/status/{result.CorrelationId}`. Therefore the stable FrontComposer
polling identifier is the pending `MessageId`, not the generated form/lifecycle `CorrelationId`.

## Authorization And Tenant Scope

- The status controller is `[Authorize]`.
- It reads authorized tenant claims from `eventstore:tenant`.
- Missing tenant claims return `403`.
- The controller searches only the authorized tenants and returns `404` when no status exists for
  the requested ID under those tenants.
- FrontComposer must use the existing EventStore bearer-token option path and must not introduce
  tenant/user storage or cross-circuit replay in pending state.

## Response Shape

Confirmed `CommandStatusResponse` fields:

- `correlationId`
- `status`
- `statusCode`
- `timestamp`
- `aggregateId`
- `eventCount`
- `rejectionEventType`
- `failureReason`
- `timeoutDuration`

`timeoutDuration` is ISO 8601 duration text, e.g. `PT30S`.

## Status Mapping

| EventStore status | Code | FrontComposer observation |
| --- | ---: | --- |
| `Received` | 0 | non-terminal, return `null` |
| `Processing` | 1 | non-terminal, return `null` |
| `EventsStored` | 2 | non-terminal, return `null` |
| `EventsPublished` | 3 | non-terminal, return `null` |
| `Completed` | 4 | `Confirmed` with `MessageId = pending.MessageId` |
| `Rejected` | 5 | `Rejected` with bounded plain-text title/detail |
| `PublishFailed` | 6 | `NeedsReview` |
| `TimedOut` | 7 | `NeedsReview` |

Unknown status names, mismatched status codes, malformed JSON, oversized bodies, and non-OK HTTP
statuses are non-mutating failures. The polling coordinator may catch/log provider exceptions; it
must not resolve pending state on protocol drift.

## Retry-After

EventStore adds `Retry-After: 1` on non-terminal status responses only. Story 3.5 records/parses
this as metadata only; Story 3.6 owns numeric polling budgets and scheduling semantics.

## Escalations

None. The observed EventStore contract matches Story 3.5 requirements.
