# FC-NIP Row Identity Producer Contract

Date: 2026-07-04
Status: confirmed with upstream blocking gap
Owner: FrontComposer maintainers
Story: 9.1 - Confirm the FC-NIP row-identity producer contract

## Decision

FC-NIP owns automatic row-level fresh-item marking for generated DataGrid views. Story 9.2 may call
`INewItemIndicatorStateService.Add(...)` only when the producer has framework-controlled row identity:
a lane/view key, the exact projection row `EntityKey`, the accepted command `MessageId`, the projection
type name, and any status-slot metadata needed to avoid ambiguity.

The current FrontComposer and pinned EventStore seams do not prove that payload end to end. Story 9.2
remains blocked until a framework-controlled producer can supply the minimum payload below. The
implementation must not infer row identity by diffing visible grid rows, marking every row in a lane,
or treating a projection nudge as row identity.

## Candidate Producer Input Disposition

| Input | Current shape | Disposition | Reason |
|---|---|---|---|
| EventStore command status | `CommandStatusResponse` exposes `CorrelationId`, `Status`, `StatusCode`, `Timestamp`, `AggregateId`, `EventCount`, `RejectionEventType`, `FailureReason`, and `TimeoutDuration`. | insufficient | The Shell polling adapter maps terminal status by pending `MessageId` only. `AggregateId` is insufficient as a universal FrontComposer row `EntityKey` because generated grid identity can be `AggregateId`, `Id`, `Key`, or object fallback depending on projection model. |
| Submit result payload | `SubmitCommandResponse.ResultPayload` is an optional untyped JSON value from domain-service result payload. | not a hidden contract | Do not use EventStore ResultPayload for FC-NIP unless EventStore and FrontComposer publish a bounded, typed row-identity payload. The current payload is optional and domain-defined. |
| Projection nudge | `IProjectionChangeNotifier` carries projection type; `IProjectionChangeNotifierWithTenant` adds tenant. | insufficient | The nudge can refresh a lane, but it carries no row key, command `MessageId`, status slot, or command/projection correlation. |
| Projection detail nudge metadata | `ProjectionChangedDetail` carries projection type, tenant, optional group scope, and opaque metadata. | insufficient today | FrontComposer deliberately treats metadata as opaque and adds no domain interpretation. FC-NIP may define typed metadata later, but Story 9.1 does not. |
| Pending-command registration metadata | `PendingCommandRegistration` supports optional `ProjectionTypeName`, `LaneKey`, `EntityKey`, `ExpectedStatusSlot`, and `PriorStatusSlot`. | potential input, not proven | The resolver can use framework-controlled metadata when present, but the generated command path currently registers only `CorrelationId`, `MessageId`, and `CommandTypeName`. |
| Generated command metadata | `CommandFormEmitter` records that generator-known metadata is limited to correlation, message id, and command type at form-emit time. | insufficient today | The generator lacks runtime row/lane context for projection identity and must not fabricate it. |

## Minimum Valid Payload For Story 9.2

Story 9.2 may wire the automatic producer only when it can construct this payload without guessing:

| Field | Required | Contract |
|---|---:|---|
| `ViewKey` or lane key | yes | Non-empty lane key used for `NewItemIndicatorEntry.ViewKey`; must match the generated grid lane that will render the indicator. |
| `EntityKey` | yes | Non-empty exact generated row key for the target projection row, not merely an EventStore aggregate id unless proven identical for that projection. |
| `MessageId` | yes | Accepted command ULID used by pending-command state and lifecycle reconciliation. |
| `ProjectionTypeName` | yes | Framework-controlled projection type name for disambiguating shared entity keys and multi-projection command outcomes. |
| `ExpectedStatusSlot` | optional | Required when the command outcome only applies to a status-filtered lane or when the same entity can appear in multiple status lanes. |
| `PriorStatusSlot` | optional | Diagnostic/audit metadata for moves between status lanes; not required by `INewItemIndicatorStateService.Add(...)`. |
| `CreatedAt` | yes | Use the terminal observation timestamp when supplied by the trusted producer; otherwise use the local `TimeProvider` at Add time. |
| `TenantId` | scope assumption | The state service is circuit-local and clears on tenant/user transition when `IUserContextAccessor` is available; producers must not cross tenant scopes. |
| `UserId` | scope assumption | User scope follows circuit/user context; producers must not share entries across users. |
| Duplicate behavior | yes | `first-wins` is a producer requirement, not the consumer default. `NewItemIndicatorStateService.Add(...)` keys entries by `(ViewKey, EntityKey)` and is last-wins: a repeat `Add(...)` for the same row removes the existing entry, replaces its data, and resets the 10s auto-dismiss timer. So the Story 9.2 producer must deduplicate terminal outcomes by `MessageId` and call `Add(...)` exactly once per `(ViewKey, EntityKey)` row so the first terminal outcome wins; a duplicate `MessageId` observation must not re-`Add` (which would reset the TTL) or create additional visible indicators. |

`NewItemIndicatorEntry` currently persists only `ViewKey`, `EntityKey`, `MessageId`, and `CreatedAt`.
`ProjectionTypeName` and status-slot metadata are producer-side matching requirements unless Story 9.2
extends the entry type intentionally.

## Existing FrontComposer Seam Verification

- `INewItemIndicatorStateService.Add(...)` remains the consumer-side primitive and rejects empty
  `ViewKey` and `EntityKey`. It keys entries by `(ViewKey, EntityKey)` and is last-wins: a repeat
  `Add(...)` for the same row replaces the entry and resets the 10s auto-dismiss timer, so the
  producer — not the consumer — is responsible for `first-wins` de-duplication by `MessageId`.
- `PendingCommandRegistration` already has optional `ProjectionTypeName`, `LaneKey`, `EntityKey`,
  `ExpectedStatusSlot`, and `PriorStatusSlot`, but generated commands do not populate those fields.
- `PendingCommandOutcomeResolver` resolves by `MessageId` first. Without `MessageId`, it may fall back
  to `EntityKey` plus optional projection/lane/status metadata only when exactly one pending command
  matches; absent or ambiguous identity does not mutate state.
- `EventStorePendingCommandStatusQuery` reads EventStore status by pending `MessageId` and emits
  terminal `PendingCommandOutcomeObservation` values with `MessageId` only. It does not forward
  `AggregateId`, projection type, lane/view key, or status-slot metadata.

## EventStore Side Verification

The pinned `references/Hexalith.EventStore` source confirms:

- `CommandStatusResponse` is built from `CommandStatusRecord` and exposes `CorrelationId`, `Status`,
  `StatusCode`, `Timestamp`, `AggregateId`, `EventCount`, `RejectionEventType`, `FailureReason`, and
  `TimeoutDuration`.
- `CommandsController` returns `SubmitCommandResponse(correlationId, resultPayload)` where
  `ResultPayload` is optional, bounded, parsed JSON from a domain-service result payload.

AggregateId is insufficient for FC-NIP as a universal row `EntityKey`. SourceTools generated grids
choose item identity from projection model conventions, and a projection row can represent a view
model whose stable key is not necessarily the command aggregate id. FC-NIP therefore requires a typed
producer payload that explicitly names the projection row key for the target generated grid.

## Blocking Follow-Up

Story 9.2 remains blocked by design until the row-identity producer payload is supplied by a
framework-controlled seam.

Owner: FrontComposer maintainer + EventStore maintainer
Date: 2026-07-04 (recorded); review-by: before Story 9.2 leaves backlog
Required decision: define and pin a typed command outcome/projection metadata payload that carries
`ProjectionTypeName`, lane/view key, exact row `EntityKey`, command `MessageId`, and any status-slot
metadata needed for FC-NIP. If EventStore supplies the payload, it must be documented as a bounded,
typed contract rather than hidden in `ResultPayload`.

## Documentation Ownership

- FC-TBL owns the public DataGrid component and state primitive surface.
- FC-CMD owns command identity, lifecycle, pending state, and message/correlation semantics.
- FC-NIP owns automatic row-level `FcNewItemIndicator` producer wiring and row-identity payloads.

Story 9.1 confirms this contract and records the upstream gap. Story 9.2 wires the producer and
generated-grid consumer only after the payload is available.
