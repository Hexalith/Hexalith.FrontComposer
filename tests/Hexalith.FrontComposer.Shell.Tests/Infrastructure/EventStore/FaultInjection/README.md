# SignalR Fault Injection Test Harness (Story 5-7)

Deterministic test harness for the `IProjectionHubConnection` seam. Lives inside
`Hexalith.FrontComposer.Shell.Tests` for v0.1; the public-facing API shape is intentionally
prepared for a future `Hexalith.FrontComposer.Testing` package extraction.

## What it is

`FaultInjectingProjectionHubConnection` implements the **production** `IProjectionHubConnection`
abstraction. Tests script connection-state events, group join/leave outcomes, and projection
nudges — without a live SignalR server, real timers, sleeps, or wall-clock reads.

The harness is payload-less by construction: nudges carry only `(projectionType, tenantId)`. There
is no API surface to send projection payloads, command responses, or ProblemDetails bodies through
the fake. Negative tests in this folder enforce that constraint.

## Public surface

### Lifecycle staging

| Method                                               | Effect                                                            |
| ---------------------------------------------------- | ----------------------------------------------------------------- |
| `BlockUntil(checkpoint)`                             | Next matching operation pauses until `Release` is called          |
| `Release(checkpoint)`                                | Releases a previously blocked operation                           |
| `FailNext(checkpoint, exception)`                    | Next matching operation faults with `exception`                   |
| `CancelNext(checkpoint)`                             | Next matching operation completes canceled                        |
| `WaitForAsync(checkpoint, count, ct)`                | Resolves when the checkpoint has been crossed `count` times       |
| `GetHitCount(checkpoint)`                            | Cumulative hit count (diagnostic only — not for ordering)         |

Checkpoints are strongly typed:

* `HarnessCheckpoint.Start`, `.Stop`, `.Dispose`
* `HarnessCheckpoint.Join(projectionType, tenantId)`
* `HarnessCheckpoint.Leave(projectionType, tenantId)`
* `HarnessCheckpoint.Nudge(projectionType, tenantId)`
* `HarnessCheckpoint.ConnectionState(state)`
* `HarnessCheckpoint.FallbackTrigger` / `.FallbackTriggerFor(projectionType, tenantId)`

`projectionType` and `tenantId` are validated via
`EventStoreValidation.RequireNonColonSegment`; blank or colon-containing values fail closed.

### Nudge staging

| Method                                                       | Effect                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------- |
| `PublishNudgeAsync(projection, tenant)`                      | Fires handlers immediately (or applies a selector)      |
| `DropNextNudge(projection, tenant)`                          | Suppresses the next matching `PublishNudgeAsync`        |
| `DuplicateNextNudge(projection, tenant, count)`              | Next matching publication fires handlers `count` times  |
| `DelayNextNudge(projection, tenant)` → token                 | Captures next matching publication for later release    |
| `QueueNudge(projection, tenant)` → token                     | Pure deterministic queue (no synchronous publication)   |
| `ReleaseAsync(token, count)`                                 | Fire a queued nudge `count` times                       |
| `ReleaseInOrderAsync(IEnumerable<token>)`                    | Reorder: deliver tokens in the specified order          |
| `ReleaseAllQueuedAsync()`                                    | Flush all queued nudges in original FIFO order          |
| `Discard(token)`                                             | Drop a queued nudge without firing                      |

### Connection-state staging

| Method                                              | Effect                                                                |
| --------------------------------------------------- | --------------------------------------------------------------------- |
| `RaiseStateAsync(ProjectionHubConnectionStateChanged)` | Synchronously dispatches to all `OnConnectionStateChanged` subscribers |
| Helpers: `HarnessConnectionStates.Connected/Reconnecting/Reconnected/Closed` |

`StartAsync` synthesizes a `Connected` event automatically (mirrors
`SignalRProjectionHubConnectionFactory`). Reconnect cycles are driven by tests calling
`RaiseStateAsync` — no real SignalR retry timers run inside the harness. Raised states keep
`IsConnected` aligned with the production wrapper: `Connected`/`Reconnected` are connected;
`Reconnecting`/`Closed` are disconnected.

### Fallback checkpoints

`TriggerFallbackCheckpointAsync()` and `TriggerFallbackCheckpointAsync(projection, tenant)` do not
run production polling. They expose named checkpoints so tests can coordinate fallback seams that
are owned by `ProjectionFallbackPollingDriver` or `ProjectionFallbackRefreshScheduler`.

## Determinism contract

* All checkpoint coordination uses `TaskCompletionSource` with
  `TaskCreationOptions.RunContinuationsAsynchronously`.
* No `Task.Delay`, `Thread.Sleep`, `PeriodicTimer`, real timers, unbounded waits, or wall-clock
  reads exist inside the harness. Tests that need timers inject `FakeTimeProvider` into the
  consumer (e.g. `ProjectionFallbackPollingDriver`).
* Scripts are one-shot in FIFO order. `BlockUntil` arms a single block; subsequent calls without
  a `Release` queue more entries up to `MaxBoundedQueueDepth` (default 256).
* `FailNext`, `CancelNext`, and `DropNextNudge`/`DuplicateNextNudge`/`DelayNextNudge` selectors
  are likewise one-shot.

## Cancellation outcome matrix

The harness mirrors production semantics for `CancellationToken` propagation per operation. Each
row records what test code can rely on when the supplied token is canceled before, during, or
after the operation crosses the checkpoint.

| Operation                | Token canceled before crossing                          | Token canceled during a `BlockUntil`                           | Scripted `CancelNext`                                          |
| ------------------------ | ------------------------------------------------------- | -------------------------------------------------------------- | -------------------------------------------------------------- |
| `StartAsync`             | Faults `OperationCanceledException`; state unchanged    | Faults `OperationCanceledException`; `IsConnected` stays false | Faults `OperationCanceledException`; state unchanged           |
| `JoinGroupAsync`         | Faults `OperationCanceledException`; group not active   | Faults `OperationCanceledException`; group not active          | Faults `OperationCanceledException`; group not active          |
| `LeaveGroupAsync`        | Faults `OperationCanceledException`; group still active | Faults `OperationCanceledException`; group still active        | Faults `OperationCanceledException`; group still active        |
| `StopAsync`              | Faults `OperationCanceledException`; state unchanged    | Faults `OperationCanceledException`; state unchanged           | Faults `OperationCanceledException`; state unchanged           |
| `DisposeAsync`           | Disposal proceeds (best-effort, mirrors production)     | Blocks at `HarnessCheckpoint.Dispose` until release            | Script consumed; disposal remains best-effort                  |
| `PublishNudgeAsync`      | n/a (test-driven, not cancellation-bearing)             | Blocks at `HarnessCheckpoint.Nudge(...)` until release         | Scripted nudge checkpoint faults/cancels the test call         |
| `RaiseStateAsync`        | n/a (test-driven, not cancellation-bearing)             | Blocks at `HarnessCheckpoint.ConnectionState(...)` until release | Scripted state checkpoint faults/cancels the test call       |
| `TriggerFallbackCheckpointAsync` | Faults `OperationCanceledException`; no side effect | Blocks at fallback checkpoint until release                    | Faults `OperationCanceledException`                            |

Subscriber callbacks run inside the harness's dispatcher and **do not** observe the test's token
directly. A throwing handler is captured in `CapturedHandlerFailureCategories` (bounded category
list) and does not prevent later handlers from running.

Disposing the harness clears registered callbacks and suppresses later test-driven nudge/state
publications. Production lifecycle calls after harness disposal throw `ObjectDisposedException`.

## Bounded state and disposal diagnostics

`DisposeAsync` throws `HarnessDisposalException` when scenario state is left behind. The message
names checkpoint identifiers and bounded counts only — never tenant/group strings, payloads,
exception messages, or connection identifiers. Sources of disposal diagnostics:

* Scripted actions that were armed but never crossed (`Outstanding scripted actions`)
* Nudge selectors armed but never matched (`Outstanding nudge selectors`)
* Queued nudges not released or discarded (`Outstanding queued nudges`)
* `WaitForAsync` waiters whose target count was never reached (`Outstanding WaitFor awaiters`)

Any blocked `TaskCompletionSource` is failed with `ObjectDisposedException` so a test that runs
disposal in `finally` cannot hang.

## Forbidden semantics (fail-closed)

* `QueueNudge` / `DelayNextNudge` / `DropNextNudge` / `DuplicateNextNudge` reject blank or
  `:`-containing projection or tenant segments via `EventStoreValidation.RequireNonColonSegment`.
* Scripts beyond `MaxBoundedQueueDepth` raise an `InvalidOperationException` naming the offending
  queue. Diagnostics never include scripted exception messages or queued group strings.
* Reaching `Release` for a checkpoint whose head is a `FailNext`/`CancelNext` (not a `BlockUntil`)
  throws — tests cannot accidentally release a different scripted action as if it were a block.

## Common scenarios

### Initial start failure

```csharp
await using FaultInjectingProjectionHubConnection harness = new();
harness.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("token expired"));

// Production code calling SubscribeAsync now observes a sticky InitialStartFailed transition.
await Should.ThrowAsync<InvalidOperationException>(
    async () => await sut.SubscribeAsync("orders", "acme", ct));
```

### Failed rejoin → group degraded

```csharp
await sut.SubscribeAsync("orders", "acme", ct);
harness.FailNext(HarnessCheckpoint.Join("orders", "acme"), new InvalidOperationException("transient"));
await harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2"));

// Production marks "orders:acme" Degraded; subsequent nudges for that group are skipped.
```

### Reorder of two nudges

```csharp
NudgeQueueToken first = harness.QueueNudge("orders", "acme");
NudgeQueueToken second = harness.QueueNudge("billing", "acme");
await harness.ReleaseInOrderAsync(new[] { second, first });
```

### Partial delivery (one group degraded, one healthy)

```csharp
harness.DropNextNudge("orders", "acme");
await harness.PublishNudgeAsync("orders", "acme"); // dropped
await harness.PublishNudgeAsync("billing", "acme"); // delivered
```

### Disposal race

```csharp
harness.BlockUntil(HarnessCheckpoint.Join("orders", "acme"));
Task subscribe = sut.SubscribeAsync("orders", "acme", ct);
await sut.DisposeAsync(); // race: dispose during in-flight Join
// subscribe Task observes ObjectDisposedException via the harness fail-closed path
```

## What the harness deliberately does NOT do

* Mock or expose `Microsoft.AspNetCore.SignalR.Client.HubConnection` (production wrapper covers
  that boundary; harness sits *below* the wrapper, on the production seam).
* Carry projection payloads, command payloads, query/cache payloads, ProblemDetails bodies, raw
  exception messages, or tenant/user identifiers in published events.
* Drive `FakeTimeProvider`, `ProjectionFallbackPollingDriver`, `ProjectionFallbackRefreshScheduler`,
  or `IPendingCommandPollingCoordinator`. Tests own those primitives directly.
* Spawn background threads, schedule timers, hold static state across test instances, or mutate
  any production singleton.

## FR24-FR29 traceability

See [`FR24-29-trace.md`](FR24-29-trace.md).
