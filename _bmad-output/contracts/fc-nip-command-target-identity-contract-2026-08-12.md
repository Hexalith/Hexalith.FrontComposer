# FC-NIP Explicit Command Target Identity Contract

Date: 2026-08-12
Status: approved successor decision
Owner: FrontComposer Product + Architecture
Story: 9.3 - Define explicit command target identity
Base authority: `fc-nip-row-identity-producer-contract-2026-07-04.md`
Approval provenance: approved at the human Story 9.3 `bmad-build` plan checkpoint on 2026-08-12 for
the Product + Architecture-owned decision. This records approved semantics, not Story 9.3 completion.

## Decision

The 2026-07-05 FC-NIP row-context decision remains authoritative for its historical scope. This
successor decision closes the target-identity gap for standalone create, same-row, cross-row,
status-move, and delete commands without treating the UI row that launched a command as its target.

SourceTools will generate one command-target descriptor from an explicit command-to-projection
declaration. The descriptor names the target projection and a resolution mode. Dynamic target values
are resolved only through a FrontComposer-owned typed
`ICommandTargetIdentityProvider<TCommand>`. The only alternative mode is explicitly declared
`SameAsSource`, which copies a source snapshot captured by the generated projection runtime before
dispatch. There is no ambient-source fallback. One command resolves at most one target; multi-target
commands require a separate decision.

The generated/runtime boundary resolves and validates one immutable `CommandTargetSnapshot` before
invoking asynchronous command dispatch. Acceptance then associates the returned `MessageId` with that
snapshot. Terminal adapters add a separate `ObservedAt` and a typed `CommandMateriality` value of
`Material`, `NoOp`, or `Unknown`. Target intent and terminal materiality are independent: neither one
may be inferred from the other.

This document names the decision contract for Stories 9.4-9.8. Story 9.3 does not add a public runtime
API, change EventStore, or implement generated/runtime behavior.

## Generated Command-Target Descriptor

The generated descriptor is immutable and contains:

- the command type identity;
- the explicitly declared `ProjectionTypeName`;
- the resolution mode: typed provider or `SameAsSource`;
- the registered typed-provider identity when provider mode is selected; and
- any declaration-fixed change kind or canonical view/lane selection.

SourceTools may generate registration from the declaration, but it may not discover a target through
command-property names, generic reflection over command fields, routes, or component placement. A
missing, duplicate, or incompatible declaration fails closed for FC-NIP and produces no
indicator-eligible target.

## Immutable Target Snapshot

The pre-dispatch snapshot contains no `MessageId` and has these fields:

| Field | Framework-owned source | Validation |
|---|---|---|
| `ProjectionTypeName` | Exact projection named by the generated command-target descriptor. | Required; must resolve to that registered projection. |
| `ViewKey` | Canonical generated view/lane identity selected by the descriptor and, when dynamic, returned through the typed provider then validated against the declared projection. | Required and non-empty. A route or visible grid is not a view-key source. |
| `EntityKey` | Exact target key returned by the typed provider, or copied from the generated projection key snapshot only in declared `SameAsSource` mode. | Required and non-empty; EventStore `AggregateId` is not a substitute unless a later projection contract explicitly proves identity. |
| `ChangeKind` | Declaration-fixed or typed-provider value: `Create`, `Update`, `StatusMove`, or `Delete`. | Required and known. `NoOp` is terminal materiality, not a change kind. |
| `PriorStatus` | Typed-provider value, or copied from the explicit source snapshot for `SameAsSource`. | Required for `StatusMove`; otherwise optional. |
| `ExpectedStatus` | Typed-provider destination value, or a declaration-fixed destination validated for the target view. | Required for `StatusMove` and whenever lane eligibility depends on destination status; otherwise optional. |
| `CapturedAt` | FrontComposer `TimeProvider` at successful target resolution. | Required. It is never supplied by command fields or overwritten by a terminal timestamp. |

A standalone create is indicator-eligible under this contract only when its exact `EntityKey` is
known before dispatch, including when a framework-owned preallocation mechanism supplies that key.
When a server allocates the exact key only after dispatch, FC-NIP suppresses the indicator. A typed
post-dispatch identity proof is outside this pre-dispatch contract and requires a separately approved
successor or amendment before it can become authoritative.

The typed provider receives the typed command and framework-owned resolution services. It returns
target intent only. It does not return `MessageId`, `ObservedAt`, or terminal materiality, and it does
not receive an ambient row as an undeclared convenience input. Provider failure, cancellation, missing
registration, an empty/unknown field, or a projection/view mismatch makes the target unknown and
suppresses publication.

In `SameAsSource` mode, the generated source context must be available and copied exactly once during
target resolution immediately before dispatch. That copy contains `ProjectionTypeName`, canonical
`ViewKey`, exact `EntityKey`, and applicable status, and target resolution sets only
declaration-authorized values. If the source context is unavailable at that instant, target resolution
fails closed for FC-NIP. After capture, the immutable copy is authoritative and is never re-read from
or revalidated against a mutable or virtualized row or cascading component context.

Target declaration or resolution failure changes only FC-NIP eligibility. The framework emits a
bounded diagnostic and suppresses indicator publication, while command dispatch, transport
acceptance, and command lifecycle continue under their existing semantics. Missing target identity
does not reinterpret an accepted command as rejected, a confirmed command as failed, or any other
transport/lifecycle outcome.

## Historical Carrier Compatibility

The successor concepts use the existing internal carrier without silently changing historical field
meaning:

| Successor concept | Historical carrier | Compatibility rule |
|---|---|---|
| Canonical target `ViewKey` | `LaneKey` | `LaneKey` carries the canonical target view/lane and becomes `NewItemIndicatorEntry.ViewKey` at eligible publication. |
| `PriorStatus` | `PriorStatusSlot` | The prior status retains its diagnostic/audit role. |
| `ExpectedStatus` | `ExpectedStatusSlot` | The expected status retains destination-lane eligibility semantics. |
| `ObservedAt` | `NewItemIndicatorEntry.CreatedAt` | The eligible terminal observation timestamp remains the indicator creation/TTL timestamp. |
| `CapturedAt` | No historical field | This is a distinct new internal snapshot value and never aliases `CreatedAt` or `ObservedAt`. |
| `ProjectionTypeName` | `ProjectionTypeName` | The existing name and target-projection disambiguation role are retained. |
| `EntityKey` | `EntityKey` | The existing name and exact target projection-row identity role are retained. |
| Accepted `MessageId` | `MessageId` | The existing accepted-command identity and terminal-correlation role are retained. |

## Capture And Observation Order

The required order is:

1. Select exactly one generated command-target descriptor for the typed command.
2. Resolve the typed provider once, or copy the explicitly declared `SameAsSource` snapshot once.
3. Canonicalize and validate every required target field, then stamp `CapturedAt`.
4. Retain that immutable snapshot locally before invoking `ICommandService` or any other asynchronous
   dispatch boundary.
5. Only after accepted dispatch, validate and attach the returned ULID `MessageId` when registering
   pending state. A pre-accept failure does not create an indicator target.
6. Route each terminal callback, poll, reconnect, or other adapter observation through
   `IPendingCommandOutcomeResolver`. The adapter supplies typed materiality and a distinct
   `ObservedAt`; it never changes the captured target.

`ObservedAt` is the trusted terminal timestamp when the adapter has one, otherwise the Shell
`TimeProvider` at observation. It remains distinct from `CapturedAt`. The existing indicator TTL and
creation disposition use the eligible terminal observation, not the earlier target-capture instant.

After acceptance, the validated `MessageId` and immutable snapshot association participates in the
bounded early-observation buffer/replay path. A terminal observation racing durable pending
registration cannot lose or replace the captured target. Re-observing the same `MessageId` with the
same snapshot is duplicate input; associating that `MessageId` with a different snapshot is a conflict
and suppresses FC-NIP publication.

## Terminal Materiality

`CommandMateriality` is a closed terminal classification:

- `Material` means the typed terminal adapter has affirmative evidence that projection-affecting work
  occurred. A positive EventStore `EventCount` is one possible adapter proof.
- `NoOp` means the typed adapter has affirmative no-work evidence, such as `EventCount == 0` or an
  equivalent bounded typed callback.
- `Unknown` means evidence is absent, malformed, unsupported, contradictory, or cannot be mapped
  without guessing.

Lifecycle text is never parsed to determine materiality. An opaque result payload, status wording,
transport acceptance, or a projection refresh/nudge is not materiality evidence. Both `NoOp` and
`Unknown` suppress the indicator. Rejected and needs-review lifecycle outcomes suppress it regardless
of target or materiality.

## Complete Outcome Disposition Matrix

| Scenario | Target resolution | Terminal evidence | Indicator disposition |
|---|---|---|---|
| Standalone create | Typed provider resolves a valid `Create` snapshot before dispatch. | Confirmed + `Material`. | Publish only for the declared target view and entity. Missing or unknown target suppresses. |
| Same-row update | Descriptor explicitly selects `SameAsSource`; the named pre-dispatch source snapshot is copied as an `Update` target. | Confirmed + `Material`. | Publish for that copied target. Never fall back to ambient source-row placement. |
| Cross-row update | Typed provider resolves an `Update` target whose `EntityKey` may differ from the source. | Confirmed + `Material`. | Publish only for the provider-resolved target. Undeclared source reuse is invalid and suppresses. |
| Status move | Typed provider resolves the target, `PriorStatus`, destination `ExpectedStatus`, and destination `ViewKey`. | Confirmed + `Material`. | Publish only in the destination lane and preserve both statuses. Missing destination status suppresses. |
| Delete | Typed provider resolves a valid `Delete` target. | Confirmed + `Material`. | Preserve target metadata for lifecycle/audit; never publish a fresh-row indicator. |
| Idempotent confirmation | A valid non-delete target was captured before dispatch. | `IdempotentConfirmed` + `Material`. | Apply the same eligibility and existing ten-second TTL disposition as material confirmation; duplicate observation handling does not extend TTL. `NoOp` or `Unknown` suppresses. |
| Rejected / needs review | Any valid or invalid declared target. | `Rejected` or `NeedsReview`. | Never publish an indicator; preserve the lifecycle state. |
| No-op | Any declared target. | Typed `NoOp`, including `EventCount == 0`, or `Unknown`. | Never publish an indicator. Status text and opaque payloads cannot upgrade it to `Material`. |

Indicator eligibility therefore requires all of: a valid immutable target captured before dispatch,
an accepted `MessageId`, an eligible confirmed lifecycle disposition, typed `Material` evidence, and a
non-`Delete` change kind. A status move additionally requires its destination. Story 9.6 owns atomic
first-wins enforcement for active `(ViewKey, EntityKey)` entries; this decision does not implement it.

The historical producer first-wins rule remains in force: the same accepted `MessageId` must never
republish an indicator or extend its TTL, including after that indicator is dismissed or expires. Any
unlisted outcome suppresses indicators by default. Pre-accept failure, cancellation, timeout,
malformed-message, unsupported, and future lifecycle outcomes are therefore ineligible unless a later
approved contract explicitly adds them.

## Forbidden Identity And Materiality Sources

No implementation may infer or repair target identity or materiality from:

- ambient generated source-row placement or an undeclared cascading row context;
- command-property names such as `Id`, `EntityId`, `AggregateId`, or `Status`;
- current routes, query strings, selected tabs, visible rows, or virtualized-row instances;
- visible-row diffs, projection nudges, unrelated refreshes, or broad lane marking;
- EventStore `AggregateId` as universal projection `EntityKey`;
- opaque or domain-defined result payloads; or
- lifecycle/status text.

Unknown identity or materiality always fails closed. There is no best-effort or source-row fallback.

## Downstream Ownership

- **Story 9.4:** implement the internal/generated descriptor, typed provider resolution, immutable
  snapshot transport, accepted `MessageId` association, typed terminal materiality adapters, and the
  single `IPendingCommandOutcomeResolver` producer boundary. Any public API shape still requires
  explicit human approval.
- **Story 9.5:** make indicator mutations observable and enforce tenant/user scope before reads and
  renders.
- **Story 9.6:** enforce atomic per-row first-wins behavior without replacing provenance or extending
  the ten-second TTL.
- **Story 9.7:** reconcile story identifiers and changed-file/commit scope mechanically.
- **Story 9.8:** prove the composed generated-command, pending-state, terminal-outcome, indicator, and
  already-rendered grid behavior in focused and live acceptance evidence.

FR-13, FR-26, and Epic 9 remain open through Story 9.8. This decision changes no package boundary,
schema fingerprint, public API baseline, deployment contract, or EventStore contract.
