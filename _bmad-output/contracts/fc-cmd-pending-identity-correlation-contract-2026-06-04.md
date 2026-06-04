# FC-CMD Pending Identity and Correlation Contract

Date: 2026-06-04
Status: v1 decided contract
Owner: FrontComposer maintainers

## MessageId

`MessageId` is the accepted command identity. Production command identity paths create it through
`IUlidFactory.NewUlid()` and it must match `^[0-9A-HJKMNP-TV-Z]{26}$` after canonicalization to
uppercase Crockford Base32.

`EventStoreCommandClient` creates the `MessageId` before submitting the command to EventStore and
returns the same value in `CommandResult`. `StubCommandService` uses the same factory and returns the
accepted `MessageId` from its simulated dispatch path. `PendingCommandStateService` keys pending
entries by canonical uppercase `MessageId`.

GUID generation, GUID parsing, and 32/36-character GUID formatting are out of contract for
`MessageId`.

## CorrelationId

`CorrelationId` is the generated form and lifecycle subscription key. Generated command forms create
it before `SubmittedAction` through `IUlidFactory.NewUlid()`, store it as the submitted correlation,
and pass it to `FcLifecycleWrapper` so sibling forms ignore unrelated lifecycle transitions.

`CorrelationId` must also match `^[0-9A-HJKMNP-TV-Z]{26}$` after canonicalization to uppercase
Crockford Base32. Pending registration rejects malformed `CorrelationId` values and does not mutate
pending state on rejection.

EventStore response `correlationId` values may be recorded as transport metadata, but generated form
lifecycle state remains keyed by the form-created `CorrelationId`.

## Scope

Pending-command state is circuit-local and scoped per user/circuit by DI. When an
`IUserContextAccessor` is available, tenant or user transitions fail closed by clearing outstanding
pending entries before accepting work in the new scope.

The v1 frontend uniqueness scope is circuit-local pending state. Backend uniqueness is carried by the
ULID `MessageId`. Durable cross-tab replay is not part of this v1 contract.

## Lifecycle Ownership

Generated forms own local lifecycle dispatch order:

1. Ensure lifecycle bridge and subscriber.
2. Dispatch `SubmittedAction` with `CorrelationId`.
3. Dispatch the command.
4. Register pending state from accepted `MessageId`.
5. Dispatch `AcknowledgedAction` only when pending registration permits it.

`ILifecycleStateService` remains keyed by `CorrelationId`. `PendingCommandStateService` is the only
framework service that maps accepted `MessageId` terminal outcomes back to lifecycle transitions.

## AlreadyApplied

Server already-applied or idempotent outcomes map to
`PendingCommandStatus.IdempotentConfirmed`. Resolving that status dispatches lifecycle
`CommandLifecycleState.Confirmed` with `idempotencyResolved=true`.

UI consumers must render the idempotent information path and must not emit the normal new-success
celebration for that transition.

Duplicate terminal observations for the same `MessageId` are counted and ignored after the first
terminal outcome. The first terminal outcome wins.

## Reconciliation

`PendingCommandOutcomeResolver` is the shared resolver for live nudge refresh, reconnect
reconciliation, fallback polling, and future EventStore command status lookup inputs.

Resolution uses `MessageId` first. Without `MessageId`, it may fall back to framework-controlled
`EntityKey` plus optional projection, lane, and status-slot metadata only when exactly one pending
candidate matches. Ambiguous or absent matches remain unresolved and must not mutate form or storage
state.

All terminal state changes route through `IPendingCommandStateService.ResolveTerminal`.

## Out Of Scope

Story 3.5 owns binding the concrete `GET /api/v1/commands/status/{id}` EventStore status endpoint.
Story 3.6 owns numeric confirming, degraded, and polling budgets.

Row-level `FcNewItemIndicator` producer wiring is out of scope until a later command outcome payload
contains the producer identity needed to mark rows precisely.

## Escalations

None. All v1 identity, correlation, scope, lifecycle, already-applied, and reconciliation decisions
needed by Stories 3.4 through 3.6 are decided here.
