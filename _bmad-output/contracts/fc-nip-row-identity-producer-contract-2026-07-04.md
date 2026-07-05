# FC-NIP Row Identity Producer Contract

Date: 2026-07-04
Status: approved payload source for Story 9.2 implementation
Owner: FrontComposer maintainers
Story: 9.1 - Confirm the FC-NIP row-identity producer contract
Decision update: 2026-07-05

## Decision

FC-NIP owns automatic row-level fresh-item marking for generated DataGrid views. Story 9.2 may call
`INewItemIndicatorStateService.Add(...)` only when the producer has framework-controlled row identity:
a lane/view key, the exact projection row `EntityKey`, the accepted command `MessageId`, the projection
type name, and any status-slot metadata needed to avoid ambiguity.

The approved source is FrontComposer-owned pending-command row metadata populated from generated
grid/command runtime context. EventStore command status remains a lifecycle/status source by
`MessageId`; it is not the row-identity source. Story 9.2 is unblocked for implementation of this
approved source, but the implementation must first populate the pending-command metadata from
framework-controlled runtime context before calling `INewItemIndicatorStateService.Add(...)`.

The implementation must not infer row identity by diffing visible grid rows, marking every row in a
lane, or treating a projection nudge as row identity.

## Approved Payload Source

Story 9.2 must use this source of truth for the FC-NIP producer:

| Payload element | Approved source |
|---|---|
| View/lane key | The generated grid runtime lane/view key that will later render `FcNewItemIndicator`; this is the value persisted as `NewItemIndicatorEntry.ViewKey`. |
| Exact row `EntityKey` | The generated projection row identity resolved by the same framework-controlled key convention used by the generated grid for that projection. It must not be substituted with EventStore `AggregateId` unless that projection contract proves they are identical. |
| `MessageId` | The accepted command ULID returned by command dispatch and registered in pending-command state. |
| `ProjectionTypeName` | The generated projection type name for the lane that owns the row identity. |
| `ExpectedStatusSlot` | The generated/runtime status lane when the command outcome targets a status-filtered lane or the same entity can appear in multiple status lanes. |
| `PriorStatusSlot` | Optional producer diagnostic metadata for status moves; not required by `NewItemIndicatorEntry`. |
| `CreatedAt` | The trusted terminal observation timestamp when supplied; otherwise the local `TimeProvider` at indicator creation. |

The approved carrier is `PendingCommandRegistration` -> `PendingCommandEntry` ->
`PendingCommandOutcomeObservation` -> Story 9.2 producer. `PendingCommandRegistration` already has the
needed optional fields; Story 9.2 may change generated/runtime command wiring to populate them only
from framework-controlled context. The generator must not fabricate row metadata at form-emit time.

## Candidate Producer Input Disposition

| Input | Current shape | Disposition | Reason |
|---|---|---|---|
| EventStore command status | `CommandStatusResponse` exposes `CorrelationId`, `Status`, `StatusCode`, `Timestamp`, `AggregateId`, `EventCount`, `RejectionEventType`, `FailureReason`, and `TimeoutDuration`. | insufficient | The Shell polling adapter maps terminal status by pending `MessageId` only. `AggregateId` is insufficient as a universal FrontComposer row `EntityKey` because generated grid identity can be `AggregateId`, `Id`, `Key`, or object fallback depending on projection model. |
| Submit result payload | `SubmitCommandResponse.ResultPayload` is an optional untyped JSON value from domain-service result payload. | not a hidden contract | Do not use EventStore ResultPayload for FC-NIP unless EventStore and FrontComposer publish a bounded, typed row-identity payload. The current payload is optional and domain-defined. |
| Projection nudge | `IProjectionChangeNotifier` carries projection type; `IProjectionChangeNotifierWithTenant` adds tenant. | insufficient | The nudge can refresh a lane, but it carries no row key, command `MessageId`, status slot, or command/projection correlation. |
| Projection detail nudge metadata | `ProjectionChangedDetail` carries projection type, tenant, optional group scope, and opaque metadata. | insufficient today | FrontComposer deliberately treats metadata as opaque and adds no domain interpretation. FC-NIP may define typed metadata later, but Story 9.1 does not. |
| Pending-command registration metadata | `PendingCommandRegistration` supports optional `ProjectionTypeName`, `LaneKey`, `EntityKey`, `ExpectedStatusSlot`, and `PriorStatusSlot`. | approved source | Story 9.2 must populate these fields from generated grid/command runtime context, then use the pending-command outcome path as the FC-NIP producer source. |
| Generated command metadata | `CommandFormEmitter` records that generator-known metadata is limited to correlation, message id, and command type at form-emit time. | implementation target | The generator may emit runtime hooks/wiring that capture approved grid/command context, but it must not fabricate row/lane metadata from compile-time command metadata alone. |

## Minimum Valid Payload For Story 9.2

Story 9.2 may wire the automatic producer when it constructs this payload from the approved source
without guessing:

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
  `ExpectedStatusSlot`, and `PriorStatusSlot`. Story 9.2 must populate those fields only from the
  approved generated grid/command runtime context.
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

## Resolved Follow-Up

Story 9.2 is unblocked for implementation of the approved FrontComposer-owned pending-command row
metadata source. This resolves the previous blocking follow-up at the contract level; implementation
work remains open.

Owner: FrontComposer maintainer + EventStore maintainer
Date: 2026-07-04 (recorded); review-by: before Story 9.2 leaves backlog
Resolution date: 2026-07-05
Resolved decision: use FrontComposer-owned pending-command row metadata populated from generated
grid/command runtime context. EventStore remains lifecycle/status-only for this contract unless a
future bounded typed EventStore payload is explicitly introduced and pinned.

## Documentation Ownership

- FC-TBL owns the public DataGrid component and state primitive surface.
- FC-CMD owns command identity, lifecycle, pending state, and message/correlation semantics.
- FC-NIP owns automatic row-level `FcNewItemIndicator` producer wiring and row-identity payloads.

Story 9.1 confirmed this contract and recorded the original gap. The 2026-07-05 update approves the
payload source so Story 9.2 can wire the producer and generated-grid consumer in a follow-up
implementation pass.
