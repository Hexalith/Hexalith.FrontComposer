# Acceptance Criteria

### AC1: ILifecycleStateService API Surface & DI Registration

**Given** `AddHexalithFrontComposer()` has been called
**When** the DI container is inspected for `ILifecycleStateService`
**Then** the registration lifetime is `Scoped`
**And** resolving the service returns a `LifecycleStateService` instance
**And** it exposes the following public surface:

```csharp
public interface ILifecycleStateService : IDisposable, IAsyncDisposable
{
    // Bespoke callback subscription (Decision D7 / ADR-018). Not IObservable<T>.
    // Replays current state once immediately (if entry exists), then invokes onTransition
    // on every subsequent Transition() until the returned IDisposable is disposed.
    IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition);

    CommandLifecycleState GetState(string correlationId);
    string? GetMessageId(string correlationId);

    // Debug/inspection surface (Hindsight H10). Returns a snapshot enumeration.
    IEnumerable<string> GetActiveCorrelationIds();

    void Transition(string correlationId, CommandLifecycleState newState, string? messageId = null);
}
```

**And** on Blazor Server, the service scope is per-circuit (verified by integration test: two distinct circuits resolve two distinct instances)
**And** on Blazor WebAssembly, the service scope is per-application instance (effectively per-user)
**And** the service is NOT registered if `AddHexalithFrontComposer()` is NOT called (Story 2-3 does not add implicit dependencies to the Shell bootstrap)

**Failure modes covered by tests:**
- Resolving after scope disposal throws `ObjectDisposedException` on `Subscribe`/`Transition` calls (handled by `_disposed` sentinel in Task 4.6/4.5).
- Disposing the scope clears all dictionaries deterministically (no `PeriodicTimer` to cancel — T4 / ADR-019 revision 2026-04-16 cut the prune loop entirely in favor of scope-lifetime eviction).
- Resolving via a mis-registered Singleton scope throws `InvalidOperationException` from the constructor guard (Decision D20).
- *(Removed 2026-04-16: "Resolving before `AddHexalithFrontComposer()` throws Fix-framed message." The default DI resolve error is generic and we cannot intercept it without a custom `IServiceProviderFactory`. Deferred to Epic 9 analyzer.)*

### AC2: Five-State Lifecycle with ULID MessageId & Observable Transitions

**Given** a command submission begins
**When** the lifecycle is initialized via `Transition(correlationId, Submitting, messageId: null)` (dispatched by the bridge from a `SubmittedAction`)
**Then** the service records `LifecycleEntry(Submitting, MessageId=null, LastUpdated=now, OutcomeNotifications=0)`
**And** subscribers registered via `Subscribe(correlationId, onTransition)` receive `CommandLifecycleTransition(PreviousState=Idle, NewState=Submitting, CorrelationId, MessageId=null, TimestampUtc, LastTransitionAt, IdempotencyResolved=false)` at the moment of subscribe (replay) AND on each subsequent `Transition` call

**Given** the underlying `ICommandService.DispatchAsync` returns `CommandResult(MessageId=<ULID>, Status="Accepted")`
**When** the form dispatches `AcknowledgedAction(correlationId, messageId)` and the bridge forwards `Transition(correlationId, Acknowledged, messageId=<ULID>)`
**Then** `MessageId` is a valid Crockford Base32 26-char ULID (regex: `^[0-9A-HJKMNP-TV-Z]{26}$`)
**And** the MessageId is lexicographically time-sortable (ULID spec)
**And** `GetMessageId(correlationId)` returns the ULID
**And** observers see the Acknowledged transition pushed

**Given** intermediate Syncing and terminal Confirmed or Rejected transitions dispatched by the bridge
**When** the state machine progresses: `Idle → Submitting → Acknowledged → Syncing → Confirmed` or `Idle → Submitting → Acknowledged → Rejected`
**Then** each intermediate transition is observable
**And** `GetState(correlationId)` at any point reflects the most-recent valid transition
**And** transitions follow the state machine table (Task 11.2 property test as single source of truth):

| From \ To | Idle | Submitting | Acknowledged | Syncing | Confirmed | Rejected |
|---|---|---|---|---|---|---|
| **Idle** | ✓ (noop) | ✓ | ✗ (HFC2004) | ✗ (HFC2004) | ✗ (HFC2004) | ✗ (HFC2004) |
| **Submitting** | via ResetToIdle | ✓ (noop) | ✓ | ✗ (HFC2004) | ✓ (stub skip-syncing) | ✓ |
| **Acknowledged** | via ResetToIdle | ✗ (HFC2004) | ✓ (noop) | ✓ | ✓ (fast path) | ✓ |
| **Syncing** | via ResetToIdle | ✗ (HFC2004) | ✗ (HFC2004) | ✓ (noop) | ✓ | ✓ |
| **Confirmed** | via ResetToIdle | ✗ (HFC2004 per D14) | ✗ (HFC2004) | ✗ (HFC2004) | ✓ IDEMPOTENT (D8 no re-emit, D10 silent absorb if same MsgId) | ✗ (HFC2004, already terminal) |
| **Rejected** | via ResetToIdle | ✗ (HFC2004 per D14) | ✗ (HFC2004) | ✗ (HFC2004) | ✗ (HFC2004, already terminal) | ✓ IDEMPOTENT (D8) |

**Note:** The `Submitting → Confirmed` direct edge (without Syncing) is permitted because the stub and real EventStore may return Confirmed faster than the sync pulse threshold (Story 2-4 Decision to skip the pulse when <300ms per NFR11 applies to rendering, but the state machine must accept the transition).

### AC3: Exactly-One User-Visible Outcome (FR30/NFR44/NFR47)

**Given** any command reaches a terminal state (Confirmed or Rejected)
**When** `Transition` is called with the terminal state
**Then** `LifecycleEntry.OutcomeNotifications` is incremented from 0 to 1
**And** the terminal `CommandLifecycleTransition` is pushed to all current observers
**And** observers that subscribe AFTER the terminal transition replay the terminal state exactly once on subscribe

**Given** a duplicate terminal transition arrives for the same CorrelationId with the same MessageId during the grace window
**When** `Transition(correlationId, Confirmed, sameMessageId)` is called again
**Then** the transition is recognized as idempotent replay (D9)
**And** `OutcomeNotifications` is NOT incremented beyond 1
**And** NO new transition is pushed to observers
**And** late-subscribing observers still see the single terminal state via replay-on-subscribe (not duplicated)

**Given** `CommandLifecycleState` is ephemeral per architecture.md §536
**When** the service state is inspected
**Then** no `IStorageService` writes occur for lifecycle state (Roslyn-analyzer-enforceable; Task 11.7 checks via mock `IStorageService` — assertion count==0 for any lifecycle-related key prefix)
**And** on scope disposal, all lifecycle state is dropped (no persistence)

**Given** NFR47 "zero silent failures"
**When** ANY code path in `LifecycleStateService` drops a transition (invalid state, unknown CorrelationId, duplicate MessageId, past grace window)
**Then** a structured log event with `CommandType`, `CorrelationId`, `MessageId`, and an HFC-prefixed diagnostic ID is emitted (never a silent return)
**And** tests verify the log is emitted for each dropped-transition path (HFC2004, HFC2005, HFC2006)

### AC4: Deterministic ULID-Based Duplicate Detection (FR36/NFR45)

**Given** a command is submitted and reaches Acknowledged (MessageId generated by `IUlidFactory`)
**When** `Transition(correlationId, Acknowledged, messageId)` is called
**Then** `messageId` is added to `_seenMessageIds` (bounded LRU hash set, capacity 1024)

**Given** the set capacity is reached
**When** the 1025th distinct MessageId is added
**Then** the oldest MessageId (by insertion order tracked via `LinkedList<string>`) is evicted
**And** a debug-level log "lifecycle MessageId cache evicted oldest" is emitted (informational; NOT user-visible, NOT a warning)

**Given** a duplicate MessageId arrives with a NEW CorrelationId (e.g., client regenerated CorrelationId but reused MessageId — simulating EventStore replay)
**When** `Transition(correlationId=<new>, Acknowledged, messageId=<already-seen>)` is called
**Then** the service logs HFC2005 (duplicate MessageId across correlations)
**And** treats the call as a **fresh submission** under the new CorrelationId — creates a new `LifecycleEntry` with `State=Submitting` (or whatever the caller requested) and `OutcomeNotifications=0`, does NOT synthesize terminal state from prior cache (Decision D10 revision per Pre-mortem PM2 / Occam T6)
**And** the fresh entry proceeds through the state machine normally under its new CorrelationId
**And** Epic 5's durable lookup (Story 5-4) is the mechanism that will surface "this MessageId already resolved elsewhere"; v0.1 does not cross that boundary

**Given** a duplicate MessageId arrives within the SAME circuit and SAME CorrelationId (Blazor reconnect replay)
**When** `Transition(correlationId=<same>, Confirmed, messageId=<same>)` is called
**Then** the existing entry's `OutcomeNotifications` is already 1 (terminal)
**And** `Interlocked.CompareExchange(ref OutcomeNotifications, 1, 0)` returns 1 — not 0 — signaling idempotent replay
**And** the transition is pushed to subscribers with `IdempotencyResolved=true` (Decision D8)
**And** no second outcome is produced (FR30 preserved)

**Property-based coverage:** Task 11.3 uses FsCheck to generate 1000 random sequences of `(correlationId, messageId)` pairs with ~5% MessageId duplication rate and verifies the invariant: *for any MessageId observed reaching terminal, at most one user-visible outcome (OutcomeNotifications ≤ 1 across all CorrelationIds sharing that MessageId)*.

### AC5: State Machine Validity Under Property Testing

**Given** the five-state lifecycle state machine
**When** FsCheck generates random sequences of `(correlationId, targetState, optionalMessageId)` transitions (1000 iterations in CI, 10,000 in nightly — per architecture.md §1419 FsCheck coverage convention)
**Then** the following invariants hold for every generated sequence:
1. **No backward transition** — `GetState(correlationId)` never returns a state S' such that S' precedes the most-recent validly-applied state S in the topological order `Idle < Submitting < Acknowledged < Syncing < {Confirmed, Rejected}` (with the exception of `ResetToIdle` which explicitly resets).
2. **No post-terminal transition** — Once a correlationId reaches Confirmed or Rejected within a test iteration, further transitions to ANY non-terminal state are rejected (dropped with HFC2004 log).
3. **No cross-contamination** — Transitions for CorrelationId A NEVER mutate state for CorrelationId B (any B ≠ A). Verified by property: for any random interleaving, `GetState(A)` after the sequence matches the state computed by replaying only A's transitions in order.
4. **Exactly-one outcome invariant** — Across any sequence, for any CorrelationId that entered a terminal state, subscribers (registered via `Subscribe(correlationId, onTransition)`) received exactly 1 terminal transition notification (counted across all subscribers). Behavioural coverage for the per-subscriber receipt (not just service counter) is Task 4.12 test #4 (Amelia AC5 gap fix).
5. **Idempotency invariant** — For any two transitions `T1=(CorrelationId=X, State=Confirmed, MessageId=M)` and `T2=(CorrelationId=X, State=Confirmed, MessageId=M)` applied in sequence, T2 is recognized as idempotent and NOT counted as an outcome (OutcomeNotifications stays 1).

**Given** FsCheck shrinks a failing case
**When** CI emits the shrunk counter-example
**Then** the counter-example is written to `tests/Hexalith.FrontComposer.Shell.Tests/Snapshots/Lifecycle/FsCheckCounterExample_{timestamp}.txt` and committed to a dedicated `regression-fixtures/` subfolder on failure (per architecture.md §1419 convention)

**References:** FR23, FR30, FR36, UX-DR12, NFR44, NFR45, NFR47, architecture.md §141-143 (ULID + ETag), §397 D2 (Fluxor lifecycle), §536 (ephemeral state), §741 (action payload rule), §1419 (FsCheck conventions)

---
