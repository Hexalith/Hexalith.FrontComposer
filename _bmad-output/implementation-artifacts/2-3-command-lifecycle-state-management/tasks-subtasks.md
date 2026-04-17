# Tasks / Subtasks

### Task 0: Prerequisites (AC: all)

- [x] 0.1: Confirm Story 2-1 + Story 2-2 are merged to main (or at least all `2-1-*.cs` generated-artifact emitters + `CommandFluxorActionsEmitter` are stable). `SourceTools.Tests` + `Shell.Tests` green on HEAD.
- [x] 0.2: Add `NUlid` package to `Directory.Packages.props` (pin exact `1.7.4`). Reference from `Hexalith.FrontComposer.Shell.csproj` (NOT Contracts — Contracts stays dependency-free per architecture.md §1144).
- [x] 0.3: Reserve diagnostic IDs HFC1016, HFC1017, HFC2004, HFC2005, HFC2006, HFC2007 in `DiagnosticDescriptors.cs` (SourceTools range 1000-1999 for HFC1016/1017; Shell range 2000-2999 for HFC2004/5/6/7 — architecture.md §648 ID-range table). HFC1016 and HFC1017 are analyzer-emitted (parse-time errors); HFC2004/5/6/7 are runtime `ILogger.LogError`/`LogWarning` with the HFC prefix in the message template. HFC1017 is the Hindsight H9 defense — reject `[Command]` on generic types at parse time. Update `AnalyzerReleases.Unshipped.md` with HFC1016 and HFC1017.
- [x] 0.4: Confirm `Fluxor.Blazor.Web 6.9.0` is pinned (Story 2-2 Task 0.4 already verified — inherited).
- [x] 0.5: Verify Story 2-1 `CommandFluxorActionsEmitter` emits the 6 action records `SubmittedAction`, `AcknowledgedAction`, `SyncingAction`, `ConfirmedAction`, `RejectedAction`, `ResetToIdleAction`. If any are missing, HALT and raise cross-story contract issue.

### Task 1: IR Model (AC: 2) — No Changes (See D18)

- [x] 1.1: Confirm `CommandModel` IR exposes `TypeName`, `Namespace`, `BoundedContext`, `FullyQualifiedName` — required for bridge emitter templating. Expected: all present from Stories 2-1/2-2.
- [x] 1.2: NO new IR fields added for this story — the bridge is a pure projection of existing IR. If Task 5 discovers a missing field, raise before proceeding.

### Task 2: Contracts API Surface (AC: 1, 2, 3)

- [x] 2.1: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/ILifecycleStateService.cs`:
  ```csharp
  public interface ILifecycleStateService : IDisposable, IAsyncDisposable
  {
      // Bespoke callback subscription (Decision D7 / ADR-018). NOT IObservable<T>.
      // Replays current state once immediately (if an entry exists), then invokes
      // onTransition on every subsequent Transition() for this correlationId.
      // Invocations are serialised with Transition() via the same internal lock so
      // the callback observes a consistent state sequence.
      // Disposing the returned IDisposable stops further invocations and removes the
      // subscription. The IDisposable is idempotent (double-dispose is safe).
      IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition);

      CommandLifecycleState GetState(string correlationId);
      string? GetMessageId(string correlationId);

      // Debug/inspection surface (Hindsight H10 — 2 LoC + 1 test, surfaces in Story 9.2 CLI inspection).
      IEnumerable<string> GetActiveCorrelationIds();

      // Idempotent: duplicate same-state calls are noop. Invalid transitions are dropped with HFC2004 log.
      void Transition(string correlationId, CommandLifecycleState newState, string? messageId = null);

      // ConnectionState + event deferred to Story 5-3 (Occam cut 2026-04-16; no v0.1 producer).
  }
  ```
- [x] 2.2: **CUT** (Occam review 2026-04-16) — original draft introduced `ICommandLifecycleAction` marker interface to enable cross-type subscription. In practice, the bridge emitter (Task 5.1) subscribes to each concrete `{Command}Actions.*Action` type directly via `IActionSubscriber.SubscribeToAction<T>`; no consumer reads the marker. Creating it would have forced re-approval of Story 2-1 `CommandFluxorActionsEmitter` snapshots with no downstream benefit. If a future story (Epic 8 MCP bridge) needs cross-type action enumeration, introduce the marker then with a real consumer. **Task 2.2 is intentionally empty.**
- [x] 2.3: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleTransition.cs`:
  ```csharp
  public sealed record CommandLifecycleTransition(
      string CorrelationId,
      CommandLifecycleState PreviousState,
      CommandLifecycleState NewState,
      string? MessageId,
      DateTimeOffset TimestampUtc,
      DateTimeOffset LastTransitionAt,
      bool IdempotencyResolved);
  ```
  `TimestampUtc` = when THIS transition was produced (always `_time.GetUtcNow()` at `Transition()` call). `LastTransitionAt` = monotonic anchor for 2-4 progressive-visibility thresholds; equals `TimestampUtc` on fresh transitions, carried forward from the originating transition during idempotent replay (Decision D15). `ResolvedByActor` was considered and cut per Occam T3 — no v0.1 producer.
- [x] 2.4: **CUT** (Occam T2 / Hindsight H2 convergent) — no `ConnectionState` enum or API surface in v0.1. Story 5-3 designs the connection-state contract from scratch with real `LastConnectedAt` / `ReconnectAttempt` / `Reason` requirements present. **Task 2.4 is intentionally empty.**
- [x] 2.5: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs`:
  ```csharp
  /// <summary>
  /// ULID generator seam. Default Shell implementation wraps NUlid.Ulid.NewUlid().
  /// Tests inject a deterministic implementation via constructor substitution.
  /// </summary>
  public interface IUlidFactory
  {
      string NewUlid();
  }
  ```
- [x] 2.6: Verify Contracts `.csproj` still has zero external PackageReferences (architecture.md §1144 — Contracts is dependency-free). Running `dotnet list package` inside Contracts must show only BCL.
- [x] 2.7: PublicApiAnalyzer: if the project has `PublicApiAnalyzers` wired (check existing `ShippedApi.txt`/`UnshippedApi.txt` in Contracts), append the 6 new public types + members to `UnshippedApi.txt`.
- [x] 2.8: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/LifecycleOptions.cs`:
  ```csharp
  public sealed class LifecycleOptions
  {
      /// <summary>Bounded LRU capacity for cross-CorrelationId duplicate MessageId detection. Default 1024. (Decision D10)</summary>
      public int MessageIdCacheCapacity { get; set; } = 1024;
  }
  ```
  `GracePeriod` and `PruneInterval` were evaluated and cut per Hindsight H3 / Occam T4 — see ADR-019. No time-based eviction in v0.1.

### Task 3: ULID Factory & Action Marker Interface Application (AC: 2, 4)

- [x] 3.1: Create `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`:
  ```csharp
  public sealed class UlidFactory : IUlidFactory
  {
      private readonly ILogger<UlidFactory>? _logger;
      public UlidFactory(ILogger<UlidFactory>? logger = null) { _logger = logger; }

      public string NewUlid()
      {
          try
          {
              return NUlid.Ulid.NewUlid().ToString();
          }
          catch (System.Security.Cryptography.CryptographicException ex)
          {
              // CM2: NUlid reads RandomNumberGenerator; exotic Windows security policies
              // can throw. Fall back to Guid so the command still completes; FR36 deterministic
              // dedup still works (Guids are unique); time-sortability is lost — an Epic 5 concern.
              _logger?.LogWarning(ex, "NUlid generation failed; falling back to Guid. Time-sortable MessageId lost for this submission.");
              return Guid.NewGuid().ToString("N");
          }
      }
  }
  ```
- [x] 3.2: Modify `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorActionsEmitter.cs` — **only** extend `ResetToIdleAction()` to `ResetToIdleAction(string CorrelationId)` so the bridge (Task 5.1) can forward CorrelationId when resetting. The other 5 action records already carry CorrelationId from Story 2-1 — leave them untouched (Occam T1 cut the marker interface that would have added a layer of modification). Patch:
  ```csharp
  // Current (Story 2-1 line 47):
  _ = sb.AppendLine("    public sealed record ResetToIdleAction();");
  // New:
  _ = sb.AppendLine("    public sealed record ResetToIdleAction(string CorrelationId);");
  ```
  This is a breaking change to the dispatch site in `CommandFormEmitter.EmitSubmitMethod` — specifically the dispatch inside the `catch (OperationCanceledException)` block at `CommandFormEmitter.cs:301` (verified 2026-04-16; originally estimated "line ~293" but the exact line is 301). There is **only one** dispatch site; the `_submittedCorrelationId` field was already captured on line 255 (`_submittedCorrelationId = correlationId;`) so the fix is mechanical. Update the existing `OnResetToIdle` reducer in `CommandFluxorFeatureEmitter` to accept the new param (it already pattern-matches on action type and returns initial state — adding a param doesn't change behavior; the guard `state.CorrelationId != action.CorrelationId ? state : ...` used by other reducers should NOT be applied here because ResetToIdle is deliberately correlation-resetting). **Blast radius reduced post-Occam:** only 2 snapshot baselines tied to the `ResetToIdleAction` dispatch need re-approval (Task 7.2).
- [x] 3.3: Modify `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` — line ~246 `var correlationId = Guid.NewGuid().ToString();`: NO change needed for CorrelationId (Decision D2: CorrelationId stays as `Guid.NewGuid().ToString()`, only MessageId switches to ULID). The form does NOT inject `IUlidFactory` — MessageId is generated **server-side** by the command service implementation (stub or real EventStore), not by the form.
- [x] 3.4: Modify `src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs` line 55 `string messageId = Guid.NewGuid().ToString();`:
  - Inject `IUlidFactory` via constructor (add as required parameter — breaking constructor change within Shell, but no external adopters use `StubCommandService` directly).
  - Replace with `string messageId = _ulidFactory.NewUlid();`
  - Update the 7 existing `StubCommandService` tests in `Services/StubCommandServiceTests.cs` to pass a stub `IUlidFactory`.
- [x] 3.5: Modify `src/Hexalith.FrontComposer.Shell/Services/DerivedValues/SystemValueProvider.cs` — inject `IUlidFactory` (required). Update the `"MessageId"` case:
  ```csharp
  // From: "MessageId" => new DerivedValueResult(true, Guid.NewGuid().ToString("N")),
  // To:   "MessageId" => new DerivedValueResult(true, _ulidFactory.NewUlid()),
  ```
  Other cases (`CommandId`, `CorrelationId`, `Timestamp`, etc.) are unchanged. **Note:** `SystemValueProvider` handles derived-field pre-fill on command model property whose name is `MessageId` — this is ADDITIONAL to the `StubCommandService` ULID path. Both must converge on `IUlidFactory`.
- [x] 3.6: Unit tests for `UlidFactory` (exactly 5 tests — +1 for Red Team R4 entropy verification):
  1. `NewUlid_ReturnsValidCrockfordBase32_26Chars` — regex `^[0-9A-HJKMNP-TV-Z]{26}$`
  2. `NewUlid_ReturnsMonotonicStrings_WhenCalledRapidly` — 10 sequential calls all sort lexicographically in ascending order (millisecond-precision timestamp prefix)
  3. `NewUlid_IsThreadSafe` — 100 parallel `NewUlid()` calls yield 100 distinct values
  4. `UlidFactory_ServiceRegistration_ResolvesAsIUlidFactory` — verify DI wiring resolves the right type
  5. `NewUlid_EntropyIsCryptographic_NotPredictableFromPriorOutputs` (Red Team R4) — generate 1000 ULIDs, extract the 80-bit entropy portions, assert statistical distribution is uniform (chi-square at p≥0.01). Ensures NUlid uses `RandomNumberGenerator` not `Random`; catches an accidental swap to a predictable source.

### Task 4: LifecycleStateService Implementation (AC: 1, 2, 3, 4)

- [x] 4.1: Create `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` as a sealed class implementing `ILifecycleStateService`. Core shape (revised 2026-04-16 for concurrency primitives + options + subscription contract):
  ```csharp
  public sealed class LifecycleStateService : ILifecycleStateService
  {
      // Singleton-resolve guard (Winston review 2026-04-16, Decision D20): process-static set catches
      // mis-registration as Singleton (AddHexalithFrontComposer wires Scoped). Two instances in the
      // same scope throws; two instances across separate scopes is legitimate.
      private static readonly ConditionalWeakTable<IServiceProvider, LifecycleStateService> _perScope = new();

      // Per-correlation entries. LifecycleEntry is a mutable class — fields mutated via Interlocked.
      private readonly ConcurrentDictionary<string, LifecycleEntry> _entries = new();

      // Per-correlation observer lists, updated via ImmutableInterlocked so Transition() enumerates a stable snapshot.
      private readonly ConcurrentDictionary<string, ImmutableList<Subscription>> _subs = new();

      // Bounded LRU for cross-CorrelationId duplicate-MessageId detection (Decision D10).
      // ConcurrentDictionary<string, byte> so membership is thread-safe; ConcurrentQueue<string>
      // tracks insertion order for LRU eviction under a short _seenLock when over cap.
      private readonly ConcurrentDictionary<string, byte> _seenMessageIds = new();
      private readonly ConcurrentQueue<string> _seenOrder = new();
      private readonly object _seenLock = new();

      private readonly TimeProvider _time;
      private readonly LifecycleOptions _options;
      private readonly ILogger<LifecycleStateService>? _logger;

      private int _disposed; // 0 = live, 1 = dispose requested

      // No PeriodicTimer / PruneLoop — scope-lifetime eviction per ADR-019.
      // ConnectionState deferred to Story 5-3 (Occam T2). No field / property / event in v0.1.

      public LifecycleStateService(
          IOptions<LifecycleOptions> options,
          TimeProvider? time = null,
          ILogger<LifecycleStateService>? logger = null)
      {
          _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
          _time = time ?? TimeProvider.System;
          _logger = logger;
          // No timer loop — scope-lifetime eviction per ADR-019.
      }

      // LifecycleEntry is a MUTABLE CLASS — fields mutated atomically via Interlocked.
      // Replacing with a `record` and using `TryUpdate` on the dictionary would work, but the
      // OutcomeNotifications increment is hot-path and Interlocked.CompareExchange on an int
      // field is measurably cheaper than record allocation + dictionary CAS per transition.
      internal sealed class LifecycleEntry
      {
          public CommandLifecycleState State;
          public string? MessageId;
          public DateTimeOffset LastUpdated;
          public DateTimeOffset OriginalTransitionAt; // for LastTransitionAt surfacing during replay
          public int OutcomeNotifications; // mutated via Interlocked.CompareExchange, never raw ++
      }

      // Mutable class (not record) so Disposed field is mutable without allocating on disposal.
      // Volatile-checked at invocation time (Task 4.5) to close the Red-Team R6 race between
      // IDisposable.Dispose() and Transition() snapshot-then-invoke enumeration.
      internal sealed class Subscription
      {
          public readonly string CorrelationId;
          public readonly Action<CommandLifecycleTransition> Callback;
          public int Disposed; // 0 = live, 1 = disposed; mutated via Volatile.Write.
          public Subscription(string correlationId, Action<CommandLifecycleTransition> callback)
          {
              CorrelationId = correlationId;
              Callback = callback;
          }
      }

      // GetActiveCorrelationIds — snapshot enumeration of _entries.Keys (Hindsight H10).
      public IEnumerable<string> GetActiveCorrelationIds() => _entries.Keys.ToArray();

      // ... Subscribe, Transition, GetState, GetMessageId, Dispose, DisposeAsync implementations
      // (PruneLoopAsync cut per ADR-019 revision 2026-04-16)
  }
  ```
- [x] 4.2: Create `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleBridgeRegistry.cs` (mirrors `LastUsedSubscriberRegistry` pattern from Story 2-2 — identical shape):
  ```csharp
  public sealed class LifecycleBridgeRegistry : IDisposable
  {
      private readonly IServiceProvider _services;
      private readonly HashSet<Type> _active = new();
      private readonly List<IDisposable> _activeInstances = new();

      public LifecycleBridgeRegistry(IServiceProvider services) => _services = services;

      public void Ensure<TBridge>() where TBridge : class, IDisposable
      {
          if (!_active.Add(typeof(TBridge))) return;
          try
          {
              var bridge = (IDisposable)ActivatorUtilities.CreateInstance(_services, typeof(TBridge));
              _activeInstances.Add(bridge);
          }
          catch
          {
              // CM1: if the bridge ctor throws (e.g., Fluxor SubscribeToAction fault),
              // roll back _active so a retry can actually re-attempt registration.
              _active.Remove(typeof(TBridge));
              throw;
          }
      }

      public void Dispose()
      {
          foreach (var d in _activeInstances) { try { d.Dispose(); } catch { } }
          _activeInstances.Clear();
          _active.Clear();
      }
  }
  ```
- [x] 4.3: Modify `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` inside `AddHexalithFrontComposer()` (after the existing Decision D37 block ~line 148):
  ```csharp
  // Story 2-3 — options binding defensive wire-up (Chaos CM7; ensures IOptions<LifecycleOptions>
  // always resolves even if adopter does not call AddOptions themselves).
  services.AddOptions<LifecycleOptions>();

  // Story 2-3 — ULID factory (Decision D2/D3).
  services.TryAddSingleton<IUlidFactory, UlidFactory>();

  // Story 2-3 — scoped lifecycle state service (Decision D12).
  services.TryAddScoped<ILifecycleStateService, LifecycleStateService>();
  services.TryAddScoped<LifecycleBridgeRegistry>();
  ```
- [x] 4.4: Modify `AddHexalithDomain<T>()` in the same file. **Verified 2026-04-16** — the existing loop at `ServiceCollectionExtensions.cs:46` already scans for `LastUsedSubscriber`-suffixed types and calls `services.TryAdd(ServiceDescriptor.Scoped(type, type))`. Extend the existing `if (type.Name.EndsWith("LastUsedSubscriber", ...))` block with a parallel branch for `type.Name.EndsWith("LifecycleBridge", StringComparison.Ordinal) && typeof(IDisposable).IsAssignableFrom(type)` that does the same `services.TryAdd(ServiceDescriptor.Scoped(type, type))`. Single-line addition in the loop body. No bootstrap rewrite needed.
- [x] 4.5: Implement `Transition(correlationId, newState, messageId)` with concurrency primitives (Decision D6, Murat P0):
  - Look up current entry; if absent, `GetOrAdd` creates one with `State=Idle, OutcomeNotifications=0, OriginalTransitionAt=now`.
  - Validate transition against state machine (see AC2 table). Invalid → log HFC2004, return (drop). Validation reads entry.State under the class's intrinsic lock (sealed class; lock on `entry` itself) — atomic snapshot.
  - For duplicate-MessageId detection (Decision D10 revision per T6 / Pre-mortem PM2 — detection-only, never synthesis):
    - If `messageId != null` and `_seenMessageIds.ContainsKey(messageId)` and `correlationId` IS in `_entries`: same-circuit same-CorrelationId replay (Blazor reconnect). The entry's `OutcomeNotifications` counter (D8) handles idempotency — set `IdempotencyResolved=true` on the transition when pushing to subscribers.
    - If `messageId != null` and `_seenMessageIds.ContainsKey(messageId)` and `correlationId` is NEW: cross-CorrelationId MessageId collision. Log HFC2005. **Do NOT synthesize terminal state from cache.** Treat as fresh submission — fresh entry, fresh `OutcomeNotifications=0`, normal state-machine progression. Epic 5 handles the cross-circuit case via durable server-side lookup.
    - Else (MessageId fresh): insert into `_seenMessageIds` + `_seenOrder` under `_seenLock`; evict oldest if over capacity.
  - On entering terminal state, use `Interlocked.CompareExchange(ref entry.OutcomeNotifications, 1, 0)` — if result was 0, this is the first terminal notification; push to subscribers. If result was 1, it's an idempotent replay; still push with `IdempotencyResolved=true` BUT do NOT increment further (counter stays at 1 — FR30 invariant).
  - Record `messageId` in `_seenMessageIds` + `_seenOrder` under `_seenLock`; evict oldest if over `_options.MessageIdCacheCapacity`.
  - Enumerate observers via `_subs.TryGetValue(correlationId, out var list)` → `list` is an `ImmutableList<Subscription>` snapshot (ImmutableInterlocked guarantees stable enumeration even if another thread Subscribes or Disposes concurrently). For each `Subscription sub`: check `if (Volatile.Read(ref sub.Disposed) != 0) continue;` (Red Team R6 defense — closes the race between `Unsubscriber.Dispose` marking disposed and the snapshot-enumeration firing the callback). Invoke `sub.Callback(transition)` **OUTSIDE any entry lock** (Chaos CM4 re-entrancy rule) inside a `try/catch` that logs callback faults to `ILogger` but does not propagate (Red Team R5 defense).
- [x] 4.6: Implement `Subscribe(correlationId, onTransition)` with the bespoke callback contract (Decision D7 / ADR-018):
  ```csharp
  public IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition)
  {
      if (correlationId is null) throw new ArgumentNullException(nameof(correlationId));
      if (onTransition is null) throw new ArgumentNullException(nameof(onTransition));
      if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(LifecycleStateService));

      var subscription = new Subscription(correlationId, onTransition);

      // ImmutableInterlocked.AddOrUpdate — thread-safe, enumerable under concurrent writes.
      ImmutableInterlocked.AddOrUpdate(
          ref _subs,
          correlationId,
          addValueFactory: _ => ImmutableList.Create(subscription),
          updateValueFactory: (_, existing) => existing.Add(subscription));

      // Replay-on-subscribe: emit the current entry state once, if an entry exists.
      if (_entries.TryGetValue(correlationId, out LifecycleEntry? entry))
      {
          CommandLifecycleState current;
          string? messageId;
          DateTimeOffset lastUpdated;
          DateTimeOffset originalAt;
          lock (entry)
          {
              current = entry.State;
              messageId = entry.MessageId;
              lastUpdated = entry.LastUpdated;
              originalAt = entry.OriginalTransitionAt;
          }
          try
          {
              onTransition(new CommandLifecycleTransition(
                  CorrelationId: correlationId,
                  PreviousState: CommandLifecycleState.Idle,
                  NewState: current,
                  MessageId: messageId,
                  TimestampUtc: _time.GetUtcNow(),
                  LastTransitionAt: originalAt,
                  IdempotencyResolved: false));
          }
          catch (Exception ex)
          {
              _logger?.LogError(ex, "Lifecycle subscribe replay callback faulted. CorrelationId={CorrelationId}", correlationId);
          }
      }

      return new Unsubscriber(this, subscription);
  }

  private sealed class Unsubscriber : IDisposable
  {
      private readonly LifecycleStateService _svc;
      private readonly Subscription _sub;
      private int _disposed;
      public Unsubscriber(LifecycleStateService svc, Subscription sub) { _svc = svc; _sub = sub; }
      public void Dispose()
      {
          if (Interlocked.Exchange(ref _disposed, 1) != 0) return; // idempotent
          // R6: mark disposed BEFORE removing from list so in-flight Transition()
          // enumerations skip the callback even if they already snapshotted the list.
          Volatile.Write(ref _sub.Disposed, 1);
          ImmutableInterlocked.AddOrUpdate(
              ref _svc._subs,
              _sub.CorrelationId,
              addValueFactory: _ => ImmutableList<Subscription>.Empty,
              updateValueFactory: (_, existing) => existing.Remove(_sub));
      }
  }
  ```
- [x] 4.7: **CUT** (ADR-019 revision 2026-04-16) — no `PruneLoopAsync`. Scope-lifetime eviction via `Dispose`/`DisposeAsync` (Task 4.8). **Task 4.7 is intentionally empty.** Helper `IsTerminal` moves inline to Task 4.5 where it is actually used.
- [x] 4.8: Implement `Dispose` + `DisposeAsync`. Scope-lifetime eviction only — no timer to cancel, no prune loop to await. Both paths converge on the same clear-dictionaries implementation (Pre-mortem PM5: sync Dispose MUST NOT block — no async loop to block on anyway now).
  ```csharp
  public ValueTask DisposeAsync()
  {
      Dispose();
      return ValueTask.CompletedTask;
  }

  public void Dispose()
  {
      if (Interlocked.Exchange(ref _disposed, 1) != 0) return; // idempotent
      _entries.Clear();
      _subs.Clear();
      _seenMessageIds.Clear();
      while (_seenOrder.TryDequeue(out _)) { }
  }
  ```
- [x] 4.9: Unit tests for `LifecycleStateService` (exactly 6 behavioural tests — +1 for T5 `GetActiveCorrelationIds`):
  1. `Transition_SubmittingAckSyncingConfirmed_EmitsTransitionsInOrder` — sequential happy path, observer receives 4 transitions in order
  2. `Subscribe_AfterTerminal_ReplaysTerminalOnceOnSubscribe` — subscribe post-Confirmed replays Confirmed exactly once
  3. `Transition_InvalidStateMachineEdge_DropsAndLogsHfc2004` — e.g. Idle → Confirmed rejected
  4. `GetState_UnknownCorrelationId_ReturnsIdle` — default (NOT throw)
  5. `DisposeAsync_WhileSubscriberActive_DoesNotThrow_ClearsAllState` — clean teardown (T4: no timer to cancel, just clear dictionaries)
  6. `GetActiveCorrelationIds_ReturnsSnapshot_LiveAfterTransitions` (T5 / Hindsight H10) — dispatch 3 Transitions across 3 CorrelationIds; assert `GetActiveCorrelationIds()` enumeration contains all 3; then dispose one subscription and assert the CorrelationId's entry still appears (entries outlive subscriptions).
- [x] 4.10: Duplicate-detection tests (exactly 4 tests — reduced from 5 after Murat promotion of "cross-CorrelationId idempotency" and "LRU eviction" to FsCheck properties #3 and #12):
  1. `Transition_SameMessageIdSameCorrelation_NoReEmit` — second Confirmed with same MsgId absorbed silently
  2. `Transition_SameMessageIdNewCorrelation_LogsHfc2005_TreatsAsFreshEntry` — Decision D10 revision per T6: no terminal synthesis. Fresh entry, `OutcomeNotifications=0`, HFC2005 log. Behavioural check pairs with FsCheck property #13.
  3. `Transition_DuplicateMessageIdPastGraceWindow_LogsHfc2005WarningAndTreatsAsFresh`
  4. `Transition_MessageIdCacheCap_DeterministicLruBoundary` (**Winston review — deterministic, non-FsCheck**) — insert EXACTLY `MessageIdCacheCapacity + 1` distinct MsgIds sequentially; assert the very-first MsgId was evicted and all later N are retained. Paired with property #12 for concurrent coverage.
- [x] 4.11: **CUT** (ADR-019 revision 2026-04-16) — no `PruneLoop`, no terminal-grace tests. Scope-lifetime eviction is covered by: (a) `DisposeAsync_ClearsAllState` test under Task 4.9 (#5), (b) the 1-command-lifecycle bUnit test under Task 8.2 which disposes the circuit naturally. **Task 4.11 is intentionally empty.** Net saving: −4 tests.
- [x] 4.12: Subscription-contract tests (exactly 4 tests — added one for Amelia's AC5 gap "multi-subscriber terminal receipt"):
  1. `Subscribe_AfterTransition_ReplaysCurrentStateImmediately_Once` — new subscriber sees exactly one replay transition followed by any further live transitions
  2. `MultipleSubscribersSameCorrelation_AllReceiveTransitions_SetEquality` — two subscribers, three transitions; both callback lists contain the same three transitions. Assert **set-equality, not list-equality** (Murat — CI thread scheduling makes strict ordering flake). Per-subscriber in-order assertion lives in FsCheck property #7.
  3. `Unsubscribe_StopsReceivingTransitions` — `IDisposable.Dispose()` on the subscription removes observer from list; subsequent transitions do not invoke the callback
  4. **`MultipleSubscribers_EachReceiveTerminalExactlyOnce` (Amelia AC5 gap)** — three subscribers on the same CorrelationId, one terminal transition dispatched, assert each subscriber's callback invoked EXACTLY once with the terminal transition (fills the AC5 property #4 gap where the property tests SERVICE state counters not OBSERVER receipts).

### Task 5: Per-Command Lifecycle Bridge Emitter (AC: 2, 3, 4)

- [x] 5.1: Create `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandLifecycleBridgeEmitter.cs`. Emit `{CommandTypeName}LifecycleBridge.g.cs` per `[Command]`. Template tokens reference `CommandFluxorModel.ActionsWrapperName` (the existing `{Namespace}.{CommandTypeName}Actions` fully-qualified wrapper type computed by `CommandFluxorTransform`). Template (netstandard2.0-safe — generator strings only; substitute `{ActionsWrapperName}` with `model.ActionsWrapperName` at emit time):
  ```csharp
  // Emitted output for a command at {CommandNamespace}.{CommandTypeName}:
  public sealed class {CommandTypeName}LifecycleBridge : IDisposable
  {
      private readonly Fluxor.IActionSubscriber _subscriber;
      private readonly Hexalith.FrontComposer.Contracts.Lifecycle.ILifecycleStateService _service;

      public {CommandTypeName}LifecycleBridge(
          Fluxor.IActionSubscriber subscriber,
          Hexalith.FrontComposer.Contracts.Lifecycle.ILifecycleStateService service)
      {
          _subscriber = subscriber;
          _service = service;
          _subscriber.SubscribeToAction<{ActionsWrapperName}.SubmittedAction>(this,
              a => _service.Transition(a.CorrelationId, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Submitting, null));
          _subscriber.SubscribeToAction<{ActionsWrapperName}.AcknowledgedAction>(this,
              a => _service.Transition(a.CorrelationId, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Acknowledged, a.MessageId));
          _subscriber.SubscribeToAction<{ActionsWrapperName}.SyncingAction>(this,
              a => _service.Transition(a.CorrelationId, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Syncing, null));
          _subscriber.SubscribeToAction<{ActionsWrapperName}.ConfirmedAction>(this,
              a => _service.Transition(a.CorrelationId, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Confirmed, null));
          _subscriber.SubscribeToAction<{ActionsWrapperName}.RejectedAction>(this,
              a => _service.Transition(a.CorrelationId, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Rejected, null));
          _subscriber.SubscribeToAction<{ActionsWrapperName}.ResetToIdleAction>(this,
              a => _service.Transition(a.CorrelationId, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Idle, null));
      }

      public void Dispose() => _subscriber.UnsubscribeFromAllActions(this);
  }
  ```
  **Template-placeholder convention (Amelia review 2026-04-16):** the original draft used `{CommandActionsWrapper}` as the placeholder name which was undefined — the emitter-model field is `CommandFluxorModel.ActionsWrapperName` (same field `CommandFluxorFeatureEmitter` uses on its line 58). Unified on `{ActionsWrapperName}` here so the dev agent doesn't have to guess which field to read.
- [x] 5.2: Modify `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` — inside the `[Command]`-attribute pipeline (the second `ForAttributeWithMetadataName` pipeline added in Story 2-1 Task 6), add a call to `CommandLifecycleBridgeEmitter.Emit(commandModel)` and register the hint `{CommandTypeName}.LifecycleBridge.g.cs` (note: hint uses dot-separated per Story 2-2 collision pattern — NOT the filename with literal dots).
- [x] 5.3: Modify `CommandFormEmitter.EmitSubmitMethod` to call `LifecycleBridgeRegistry.Ensure<{CommandTypeName}LifecycleBridge>()` BEFORE the existing `LastUsedSubscriberRegistry.Ensure<...>()` call on line 251. This makes the bridge lazy-activated on first submit (Decision D5). **Injection shape — verify at implementation time (Amelia review 2026-04-16):** the story originally claimed `LastUsedSubscriberRegistry` is resolved via a `[Inject]` field; inspect the current `CommandFormEmitter.EmitClassHeader` / field emission pass to confirm whether the 2-2 pattern uses `[Inject]` property, `[Inject]` field, or `GetService` in-method. Match the existing pattern for `LifecycleBridgeRegistry` — do NOT blindly add `[Inject] private LifecycleBridgeRegistry BridgeRegistry { get; set; } = default!;` without confirming the 2-2 convention. If 2-2 uses `GetService`, use `GetService` here too.
- [x] 5.4: Bridge emitter tests (exactly 3 tests):
  1. `Emit_CommandWithStandardActions_ProducesBridgeClassWithSixSubscriptions`
  2. `Emit_CommandNamespace_MatchesCommand` — emitted bridge in same namespace as the command
  3. `Emit_DeterministicOutput_RunningTwiceProducesIdenticalSource` — incremental-cache invariant
- [x] 5.6: **HFC1017 generic-command rejection (Hindsight H9).** In `CommandParser` (already modified by Story 2-2 Task 1), add a parse-time check: if the `[Command]`-annotated type has `Arity > 0` (generic type parameters), report HFC1017 "Command type '{Type}' cannot be generic — specialize or remove the type parameters." Do NOT emit the bridge for generic types. The downstream `CommandFormEmitter` / `CommandFluxorActionsEmitter` / `CommandLifecycleBridgeEmitter` all share this parse-gated pipeline, so rejecting here blocks every emitter uniformly. Add 1 test: `HFC1017_RejectsGenericCommand`. Story 2-2's command sample confirms non-generic commands remain unaffected.

- [x] 5.5: Bridge emitter snapshot tests (exactly 3 baselines — Murat review 2026-04-16 added the nested-namespace case to prove D16 emitter determinism):
  1. `LifecycleBridgeEmitterTests.Emit_IncrementCommand.verified.txt`
  2. `LifecycleBridgeEmitterTests.Emit_ConfigureCounterCommand.verified.txt` (FullPage density — exercises emission for a command with `[Icon]` attribute to ensure bridge doesn't pick up icon metadata)
  3. `LifecycleBridgeEmitterTests.Emit_NestedNamespace.verified.txt` — a command at `Counter.Domain.Batch.Operations.BulkIncrementCommand` (deeply nested namespace). Proves hint-name generation and `{ActionsWrapperName}` fully-qualified reference work without collision or truncation. Murat: "two vanilla commands prove nothing about edge cases."

### Task 6: Idempotency + Terminal Grace Window (AC: 3, 4)

- [x] 6.1: Covered by Task 4.10–4.11 tests (5 duplicate-detection + 4 terminal-grace tests). No additional sub-tasks; this is a cross-cutting concern.

### Task 7: Story 2-1 Regression Gate — Marker Interface Application (AC: all)

- [x] 7.1: Re-run all existing `CommandFluxorActionsEmitter` tests. Expected change: generated action records now have `: global::Hexalith.FrontComposer.Contracts.Lifecycle.ICommandLifecycleAction` after the record parameter list. Tests that snapshot the emitted source need re-approval. Story 2-1 did NOT land `.verified.txt` snapshots for action records (see deferred-work 2026-04-15 entry), so the regression is caught by `CommandFluxorEmitterTests` compile-time parseability checks — these should pass unchanged because adding a marker interface does not break record syntax.
- [x] 7.2: Re-run all `CommandFormEmitter` tests after the `ResetToIdleAction` signature change (empty → `(string CorrelationId)`). Expected: the two snapshot baselines added in Story 2-2 Session C need re-approval — `CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt` and `CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`. Run `dotnet test`; inspect diff; if only the ResetToIdleAction dispatch site changed (passes correlationId), accept.
- [x] 7.3: Re-run all `CommandFluxorFeatureEmitter` tests. `OnResetToIdle` reducer signature changed from `(State state, Actions.ResetToIdleAction action)` to same shape — no test change needed since the reducer ignores the new param. If tests break, update to match.
- [x] 7.4: Regression invariant: **all 229 SourceTools tests + 82 Shell tests + 12 Contracts tests = 323 existing tests continue to pass** (Story 2-2 end state per its Debug Log). If any fail for reasons unrelated to the 3 expected edits above, HALT.

### Task 8: Counter Sample E2E (AC: 1, 2, 3)

> **Scope reduced 2026-04-16 (Winston review):** the original Task 8.2 dev-mode `<FcDiagnosticsPanel>` was scope creep into Story 2-4's domain. Dropped. Task 8 now ships ONLY: (a) an optional debug log in the existing Counter effects, and (b) 3 bUnit e2e tests that prove the service works end-to-end with generated forms.

- [x] 8.1: **No functional change** to Counter.Web UI per Story 2-4 scope boundary. Counter sample's `CounterProjectionEffects.cs` MAY add a debug-level log reading `ILifecycleStateService.GetState(correlationId)` at `Confirmed` dispatch as a smoke signal. No UI changes. If the log call adds noise in dev runs, drop it — this is optional.
- [x] 8.2: End-to-end tests (3 tests) — verify lifecycle observability via bUnit:
  1. `CounterPage_IncrementCommandSubmitted_ServiceReachesConfirmed` — submit the 1-field inline popover; assert `GetState(correlationId) == Confirmed` within 2 seconds.
  2. `CounterPage_BatchIncrementSubmitted_SubscribeEmitsFiveTransitions` — inline call `service.Subscribe(correlationId, t => captured.Add(t))` BEFORE submit; count transitions and assert the sequence `[Submitting, Acknowledged, Syncing, Confirmed]` with at most one replay-on-subscribe entry prepended. Use `cut.WaitForAssertion` (Story 2-2 lesson — synchronous Find post-render races).
  3. `CounterPage_Rejected_SubscribeEmitsRejectedAndNoFurtherTransitions` — configure stub to reject via `SimulateRejection=true`; verify subscriber sees Rejected and NO later transitions despite any spurious bridge callbacks.

### Task 9 (Intentionally skipped)

- Reserved for test-project bootstrapping if new test projects are needed. Story 2-3 reuses the existing `Hexalith.FrontComposer.Shell.Tests` and `Hexalith.FrontComposer.SourceTools.Tests` projects — no new project.

### Task 10: Test Fixture Infrastructure (AC: 5)

- [x] 10.1: Add `TestUlidFactory` in `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/` that emits deterministic ULIDs from a seed — usage example: `new TestUlidFactory(seed: 0)` yields a predictable sequence. Use the NUlid deterministic seed overload `Ulid.NewUlid(DateTimeOffset, byte[])` with incrementing byte arrays.
- [x] 10.2: **CUT** (ADR-019 revision 2026-04-16) — no `FakeTimeProvider` dependency needed. After cutting `PruneLoop` (T4), there's no time-dependent code in `LifecycleStateService` to test deterministically. `TimeProvider` injection remains in the ctor for forward-compat with Epic 5's durable lookup but unused in v0.1. No package add to `Directory.Packages.props`. **Task 10.2 is intentionally empty.**

### Task 11: FsCheck State Machine Property Tests (AC: 5)

- [x] 11.1: Add `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/LifecycleStateMachinePropertyTests.cs`.
- [x] 11.2: Define an FsCheck generator `Arbitrary<LifecycleOperation>` that produces one of: `(correlationId, state, messageId?)` weighted towards valid sequences but includes 10% invalid / out-of-order ops.
- [x] 11.3: **15 property tests** (each is a `[Property(MaxTest=1000)]` call — running 1000 iterations per property; the three stress/concurrency properties + the re-entrant property use `MaxTest=100` CI / `1000` nightly per architecture.md §1419):
  1. `Property_ValidTransitionsOnly_StateIsReachable` — after applying random valid ops, `GetState` returns the last validly-applied state
  2. `Property_NoBackwardTransition_WithoutResetToIdle` — skipping ResetToIdle, state never regresses
  3. `Property_CrossCorrelationIsolation` — ops for CorrelationId A never affect GetState(B) (**promoted from duplicate-detection test #2 "cross-CorrelationId idempotency" — Murat promotion, quantifier-shaped**)
  4. `Property_ExactlyOneTerminalOutcome_PerCorrelation` — across any interleaving, each CorrelationId reaching terminal has `OutcomeNotifications == 1` exactly (across single-threaded dispatch)
  5. `Property_DuplicateMessageIdIdempotent` — replayed MsgIds never double-count outcomes
  6. `Property_ResetToIdleFromAnyState_ReturnsToIdle` — ResetToIdle is universally applicable
  7. `Property_SubscribeReceivesAllValidTransitions_InCausalOrder` — subscriber's callback sequence matches the validly-applied transition order for that CorrelationId. "Order" is **causal, NOT wall-clock** (Murat: wall-clock flakes under CI contention). Assert via per-subscriber sequence = expected valid-sequence filtered to that CorrelationId.
  8. `Property_InvalidTransitions_DroppedWithoutStateChange` — invalid ops leave GetState unchanged
  9. `Property_TerminalStatesStaySticky_UntilResetToIdle` — once Confirmed, stays Confirmed through any further non-Reset op
  10. `Property_DisposeClearsAllStateDeterministic` — for any random sequence of transitions + subscriptions across N CorrelationIds, `Dispose()` empties `_entries`, `_subs`, and `_seenMessageIds` regardless of prior state. Replaces the grace-window property (cut per T4 / ADR-019 revision).
  11. **`Property_ConcurrentTransition_Linearizability` (stress + Murat P0 single highest-leverage addition)** — N=32 threads issue random VALID transitions against a shared service with CorrelationIds drawn from a pool of 8. Assert: (a) for every CorrelationId reaching terminal, `OutcomeNotifications == 1` exactly, and (b) the per-CorrelationId transition sequence observed is a linearisation of some valid path through AC2's state machine. **Kills the three concurrency risks Murat flagged** — `OutcomeNotifications++` race, `_seenMessageIds` non-thread-safe collection, observer-list-collection-modified during enumeration. 100 iter CI / 1000 nightly.
  12. **`Property_MessageIdLruUnderCachePressure` (Winston review)** — insert 2× `MessageIdCacheCapacity` distinct MessageIds concurrently; assert cache size ≤ capacity AND no young entries (within last 10% of capacity) evicted. Catches non-thread-safe cache under concurrent insertion. 100 iter CI / 1000 nightly.
  13. **`Property_CrossCorrelationMessageIdReplay_TreatedAsFresh` (revised 2026-04-16 per T6 no-synthesis)** — apply a terminal sequence for CorrelationId A with MessageId M → replay `Transition(B, Acknowledged, M)` with B ≠ A → assert B's entry is a fresh entry at `State=Acknowledged`, `OutcomeNotifications=0`, `IdempotencyResolved=false`. Assert HFC2005 logged exactly once for the cross-correlation collision. Assert original A's entry is unchanged. Property generator randomises the interleaving of A's completion and B's arrival.
  14. **`Property_ScopeLifetimeDedup_SameCorrelationSameMsgId_NoDoubleOutcome` (T20, replaces prior grace-reuse property)** — within a single service scope, for any random interleaving of `Transition(sameCorrId, Confirmed, sameMsgId)` calls (simulating Blazor reconnect replay within the same circuit), assert `OutcomeNotifications == 1` and only the first terminal is pushed to subscribers. Replaces the prior `Property_GracePeriodRejectsReusedCorrelation` (cut along with PruneLoop per T4).
  15. **`Property_ReEntrantTransitionFromInsideCallback_NoDeadlock` (T11, Chaos CM4)** — FsCheck generates a random CorrelationId A and a subscriber whose callback synchronously calls `Transition(B, Submitting, null)` where B is drawn from a pool. Run the state machine; assert all transitions complete within 1 second (no deadlock) and both A's and B's final states match the expected serialisation. Enforces the "invoke callbacks OUTSIDE entry lock" rule from Task 4.5.
- [x] 11.4: On shrink failure, dump counter-example to `tests/Hexalith.FrontComposer.Shell.Tests/Snapshots/Lifecycle/FsCheckCounterExample_{timestamp}.txt`. Commit to git on CI failure (not pre-emptively).
- [x] 11.5: Add `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CommandLifecycleBridgeIntegrationTest.cs`:
  - Compile a small synthetic command via `CSharpGeneratorDriver`
  - Assert the emitted bridge source contains exactly 6 `SubscribeToAction<...>` calls
  - Assert none of the 6 subscriptions is a no-op (each has a `.Transition(...)` body)
  - Asserts bridge emitter is structurally correct without having to run bUnit
- [x] 11.6: Add DI/registration tests (exactly 3):
  1. `AddHexalithFrontComposer_RegistersILifecycleStateService_Scoped`
  2. `AddHexalithFrontComposer_RegistersIUlidFactory_Singleton`
  3. `AddHexalithFrontComposer_RegistersLifecycleBridgeRegistry_Scoped`
- [x] 11.7: Add **non-persistence invariant** test: `LifecycleStateService_DoesNotWriteToIStorageService` — construct with an `IStorageService` that throws on any Set/GetAsync call, run a full Submitted → Confirmed sequence, assert no throws (service never touched storage).

### Task 12: Axe-core / Accessibility (AC: none — no UI scope)

- [x] 12.1: **Not applicable.** Story 2-3 does NOT add UI chrome. The `<FcDiagnosticsPanel>` extension in Task 8.2 inherits Story 2-2's axe-compliance (already clean). No new axe-core scans needed.

### Task 13: Automated End-to-End Verification (AC: all)

- [x] 13.1: Test count rollup check: **~46 new tests** (D17 budget, revised 2026-04-16 after advanced-elicitation T1-T20 apply). CI gate: `dotnet test --list-tests | grep -c "Lifecycle"` ≥ 46. Distribution: 3.6 (5, +1 ULID entropy) + 4.9 (6, +1 GetActiveCorrelationIds) + 4.10 (4) + 4.11 (0, cut per T4) + 4.12 (4) + 5.4 (3) + 5.5 (3) + 5.6 (1, HFC1017 generic) + 8.2 (3) + 11.3 (15, +2 scope-dedup + re-entrant) + 11.5 (1) + 11.6 (3) + 11.7 (1) = 5 + 6 + 4 + 0 + 4 + 3 + 3 + 1 + 3 + 15 + 1 + 3 + 1 = **49**. (3 slack — the elicitation net added properties and a generic-rejection test while the PruneLoop cut dropped 4 grace-window tests.)
- [x] 13.2: Regression invariant: **existing 323 tests continue to pass** (Task 7.4). Full solution build: `dotnet build -c Release -p:TreatWarningsAsErrors=true` → 0 warnings.
- [x] 13.3: Automated E2E via Aspire MCP + Claude browser (per user memory preference `feedback_no_manual_validation`):
  - Scenario 1: Increment command → observe Submitting → Acknowledged → Syncing → Confirmed via `<FcDiagnosticsPanel>` reads
  - Scenario 2: Configure command rejection (temporarily configure stub `SimulateRejection=true`) → observe Submitted → Rejected, `IdempotencyResolved=false`
  - Scenario 3: Rapid double-click (race between submit disable and click) → assert only one CorrelationId enters Confirmed, no second outcome
  - Emit `2-3-e2e-results.json` with machine-readable predicates per scenario. Store under `_bmad-output/test-artifacts/`.

### Review Findings

- [x] [Review][Patch] Per-correlation subscriber list should use `ImmutableInterlocked` for `_subs` mutations (Decision D6 / Task 4 pseudocode) — **Fixed 2026-04-16:** `_subs` is now an `ImmutableDictionary<string, ImmutableList<Subscription>>` field updated via `ImmutableInterlocked.AddOrUpdate` / `ImmutableInterlocked.Update` (subscribe + unsubscribe). [`LifecycleStateService.cs`]
- [x] [Review][Defer→fixed] Task 4 ASCII flowchart — **aligned 2026-04-16** with Decision D10 (duplicate cross-CorrelationId: HFC2005 + fresh entry, no synthesis). Terminal row also updated: scope dispose retention replaces obsolete “grace=5min”.

---
