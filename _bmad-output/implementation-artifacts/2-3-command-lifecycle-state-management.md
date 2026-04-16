# Story 2.3: Command Lifecycle State Management

Status: ready-for-dev

---

## Dev Agent Cheat Sheet (Read First — 2 pages)

> Amelia-facing terse summary. Authoritative spec is the full document below. Every line links to a section for detail.

**Goal:** introduce a cross-command **`ILifecycleStateService`** that tracks every in-flight command by CorrelationId, exposes an observable stream of state transitions, guarantees **exactly one** user-visible outcome per command (FR30), generates **real ULID** `MessageId`s (FR36) for deterministic duplicate detection, and validates the five-state machine under FsCheck property testing. The service is the seam Story 2-4's `FcLifecycleWrapper` consumes.

**Scope boundary:** Epic 2 happy path (stable connection). No `ConnectionState` API surface in v0.1 (Occam review 2026-04-16: stub-only seam with no v0.1 consumer is cargo-culting). Story 5-3 introduces the connection-state contract with its actual Reason/ReconnectAttempt/LastConnectedAt requirements. Degraded/disconnected UX is Epic 5.

**Binding contract with Stories 2-1 and 2-2 (ADR-017):**
- Story 2-1's `{Command}Actions.{Submitted|Acknowledged|Syncing|Confirmed|Rejected|ResetToIdle}` action records continue to exist and carry `CorrelationId` + typed payloads. **Only change:** `ResetToIdleAction()` → `ResetToIdleAction(string CorrelationId)` so the bridge can forward the CorrelationId when resetting. A marker interface across all 6 action records was evaluated (Round 2) and **cut** per Occam review 2026-04-16 — no consumer reads the marker in-repo; the bridge subscribes to each concrete type directly. Saves one layer of Story 2-1 regression.
- Story 2-1's per-command generated Fluxor feature (`{Command}LifecycleFeature`) continues to own per-type state. `ILifecycleStateService` is an **orthogonal cross-command aggregator** — it does NOT replace the generated feature, it observes the same actions and projects to a correlation-keyed view.
- Story 2-1's form emitter (`CommandFormEmitter`) keeps dispatching actions exactly as today. New per-command `{Command}LifecycleBridge.g.cs` subscriber (L05 hand-written-service + emitted-per-type wiring) forwards lifecycle actions to `ILifecycleStateService.Transition(...)`. Bridge registration is idempotent + lazy via new scoped `LifecycleBridgeRegistry` (mirrors Story 2-2 Decision D35 `LastUsedSubscriberRegistry`).
- Story 2-2's `InlinePopoverRegistry`, `LastUsedSubscriberRegistry`, `FrontComposerStorageKey` helpers are untouched. No edits to `CommandRendererEmitter`.

**ADR-017 one-liner:** Per-command Fluxor feature = state slice. `ILifecycleStateService` = correlation-keyed index over all slices.

**Files to create / extend** (count reduced 2026-04-16 after advanced-elicitation cuts T1/T2):

| Path | Action |
|---|---|
| `src/Hexalith.FrontComposer.Contracts/Lifecycle/ILifecycleStateService.cs` | Create (Task 2.1) |
| `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleTransition.cs` | Create — subscription payload record (Task 2.3) |
| `src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs` | Create — ULID seam (Task 2.5) |
| `src/Hexalith.FrontComposer.Contracts/Lifecycle/LifecycleOptions.cs` | Create — MessageIdCacheCapacity (Task 2.8; grace period cut per T4) |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` | Create — scoped service (Task 4.1) |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleBridgeRegistry.cs` | Create — idempotent lazy-register (Task 4.2) |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs` | Create — real ULID generator (Task 3.1) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandLifecycleBridgeEmitter.cs` | Create — per-command bridge (Task 5.1) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorActionsEmitter.cs` | Modify — `ResetToIdleAction()` → `ResetToIdleAction(string CorrelationId)` only (Task 3.2) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Modify — `SystemValueProvider.MessageId` path + form uses `IUlidFactory` for CorrelationId (Task 3.3) |
| `src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs` | Modify — MessageId via `IUlidFactory` (Task 3.4) |
| `src/Hexalith.FrontComposer.Shell/Services/DerivedValues/SystemValueProvider.cs` | Modify — `MessageId` branch uses `IUlidFactory`; `CorrelationId` stays Guid (Task 3.5) |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Modify — wire `ILifecycleStateService`, `IUlidFactory`, `LifecycleBridgeRegistry` (Task 4.3) |
| `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` | Modify — run `CommandLifecycleBridgeEmitter` per `[Command]` (Task 5.2) |
| `Directory.Packages.props` | Add `NUlid 1.7.4` (Task 0.2) |

**AC quick index (details in ACs section below):**

| AC | One-liner | Task(s) |
|---|---|---|
| AC1 | `ILifecycleStateService` API: `Subscribe(correlationId, Action<T>)` → `IDisposable`, `GetState`, `GetMessageId`, `GetActiveCorrelationIds`, `Transition`; scoped per circuit (Server) / per user (WASM). `ConnectionState` deferred to Story 5-3 (Occam cut) | 2, 4 |
| AC2 | 5-state transitions `Idle → Submitting → Acknowledged → Syncing → Confirmed/Rejected`; ULID `MessageId` generated per submission; each transition pushed to subscribers via `Subscribe(correlationId, onTransition)` | 3, 4, 5 |
| AC3 | Terminal state (Confirmed/Rejected) triggers **exactly one** outcome notification; ephemeral state evicted after terminal + idempotent grace window; NEVER persisted to `IStorageService`; no silent failures (FR30/NFR47) | 4, 6 |
| AC4 | Deterministic ULID-based duplicate detection — duplicate submission with same ULID does NOT produce a second user-visible effect (FR36/NFR45) | 4, 6, 7 |
| AC5 | Property-based test (FsCheck) verifies state machine never enters invalid states (no Confirmed-after-Rejected, no Submitting-after-Confirmed, no state-regressions) | 11 |

**Scope guardrails (do NOT implement — see Known Gaps):**
- Real SignalR `HubConnectionState` observation, reconnection/disconnect handling → **Story 5-3** (no `ConnectionState` API surface in v0.1 — Occam T2 cut; 5-3 designs the contract)
- `FcLifecycleWrapper` visual feedback (spinner, sync pulse, "Still syncing..." text, `aria-live`) → **Story 2-4**
- Rejection domain-message formatting, destructive confirmation dialog, form abandonment protection → **Story 2-5**
- Lifecycle persistence across page reloads or cross-device replay → **Never** (architecture decision: CommandLifecycleState is ephemeral — see architecture.md §536)
- Agent-surface two-call lifecycle tool (`lifecycle/subscribe`) → **Epic 8** (`ILifecycleStateService` is the reusable substrate)
- Replacing `{Command}LifecycleFeature` with pure `ILifecycleStateService` — per-command feature stays (ADR-017)

**6 new diagnostics reserved (HFC1016 + HFC1017 in `AnalyzerReleases.Unshipped.md`; runtime-logged use LogError with HFC prefix only):**
- **HFC1016 Error (analyzer)** — `[Command]` type cannot emit lifecycle bridge because `CommandParser` failed to classify the type (propagates parser error chain)
- **HFC1017 Error (analyzer, added per T14 / Hindsight H9)** — `[Command]` type is generic (`Arity > 0`); specialize or remove type parameters. Prevents bridge hint-name collision on generic specialization.
- **HFC2004 Error (runtime log)** — `ILifecycleStateService.Transition` rejected an invalid transition (e.g. Confirmed-after-Rejected); not thrown, logged + swallowed per FR30 "exactly one outcome"
- **HFC2005 Warning (runtime log)** — duplicate MessageId detected; second submission dropped (FR36/NFR45)
- **HFC2006 Warning (runtime log)** — transition arrived for unknown CorrelationId (bridge may have been unregistered mid-flight); logged + swallowed
- **HFC2007 Warning (runtime log)** — entry exists without prior `Submitted` observed; indicates direct-dispatch bypass of the bridge (Decision D19 single-writer invariant)

**Test expectation: ~46 new tests, cumulative ~509** (Story 2-2 ends at ~463 per its spec). Post-party-mode + advanced-elicitation T1-T20 apply: net changes were −4 terminal-grace tests (T4 PruneLoop cut), +1 ULID entropy (R4/T16), +1 GetActiveCorrelationIds (T5/H10), +1 HFC1017 generic-reject (H9/T14), +2 FsCheck properties (re-entrant-no-deadlock T11/T20 + scope-dedup T20). Breakdown at Task 13.1.

**Start here:** Task 0 (prereqs + NUlid) → Task 1 (IR extension: no changes — pure bridge emission) → Task 2 (Contracts API) → Task 3 (IUlidFactory + emitter MessageId switch + marker interface application) → Task 4 (LifecycleStateService hand-written) → Task 5 (per-command bridge emitter) → Task 6 (duplicate dedup + terminal grace window) → Task 7 (Story 2-1 regression gate re-approval for marker interface) → Task 8 (Counter sample observable demo — optional diagnostic panel) → Tasks 10-12 (tests) → Task 13 (automated E2E).

**The 20 Decisions and 3 ADRs in the sections below are BINDING. Do not revisit without raising first.** (D19 HFC2007 divergence, D20 singleton-resolve guard added 2026-04-16 post-party-mode review.)

---

## Story

As a developer,
I want a lifecycle state service that tracks each command through five states with ULID-based idempotency and guarantees exactly one user-visible outcome,
so that every command submission is traceable, replay-safe, and never produces silent failures or duplicate effects.

---

## Critical Decisions (READ FIRST — Do NOT Revisit)

These decisions are BINDING. Tasks reference them by number. If implementation uncovers a reason to change one, raise it before coding, not after.

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **`ILifecycleStateService` uses `string correlationId`**, NOT `Guid`. Matches the existing emitter output (`Guid.NewGuid().ToString()` in generated forms and `string CorrelationId` on all Story 2-1 action records). Epic 2.3 AC wording amended from "Observe(Guid correlationId)" to `string` — the underlying value is still a Guid representation, but the service surface is unified on `string` to eliminate impedance-mismatch between Guid / ULID / caller-supplied CorrelationIds. **Pre-existing format duality preserved (Amelia review 2026-04-16):** `CommandFormEmitter.cs:254` emits `Guid.NewGuid().ToString()` (36 chars with hyphens); `SystemValueProvider.cs` emits `"CorrelationId" => Guid.NewGuid().ToString("N")` (32 chars, no hyphens). Two different representations coexist today and are treated as opaque strings by the service. Story 2-3 does NOT normalise them — normalisation would break Story 2-1 snapshots. Adopters who care must format consistently in their own CorrelationId seeding; the service never parses them. | Binding with Story 2-1 `SubmittedAction(string CorrelationId, ...)`. Changing 2-1 to Guid would break 23 existing tests and 2 emitter snapshots. Format duality is intentional-until-a-future-story explicitly owns normalisation. |
| D2 | **Two identifiers, two types.** `CorrelationId` remains `string` representation of a Guid (V4 random, Blazor-circuit local). `MessageId` is a real ULID (`IUlidFactory.NewUlid().ToString()` — Crockford Base32, lexicographically time-sortable). The two identifiers are NOT interchangeable: `CorrelationId` is the caller-side correlation, `MessageId` is the server-accepted ULID returned by `ICommandService.DispatchAsync`. Duplicate detection (FR36) keys on `MessageId`, NOT `CorrelationId`. | PRD §1254 FR36 explicitly says *"unique message identifiers for command idempotency"*; NFR45 says *"Idempotent handling via ULID message IDs with deterministic duplicate detection"*. CorrelationId is a UI-side correlation; ULID MessageId is the replay-safe identity. |
| D3 | **ULID library: `NUlid 1.7.4`**, added to `Directory.Packages.props`. Exposed via `IUlidFactory` seam in Contracts. Default impl in Shell (`UlidFactory`) calls `NUlid.Ulid.NewUlid()`. Tests inject a deterministic `TestUlidFactory` via constructor substitution for FsCheck/bUnit determinism. | First-party .NET ULID type does NOT exist as of .NET 10.0.5. `NUlid` is the de-facto standard (1.5M+ downloads, MIT, maintained since 2019). Hand-rolling Crockford Base32 is 30+ lines of arithmetic that snapshot tests cannot cheaply prove equivalent to the spec. |
| D4 | **`ILifecycleStateService` is hand-written in Shell**, NOT emitted per command. Per-command bridges (`{Command}LifecycleBridge.g.cs`) are the ONLY emitted artifact added by this story. The bridge subscribes to the 6 per-command Fluxor actions (Submitted/Acknowledged/Syncing/Confirmed/Rejected/ResetToIdle) and forwards them to the singleton service via `.Transition(correlationId, newState, messageId)`. | L05 hand-written-service + emitted-per-type wiring pattern. AOT-safe, no reflection in hot path, Fluxor-typed. Reuses Story 2-2's LastUsedSubscriber pattern verbatim. |
| D5 | **`LifecycleBridgeRegistry` provides idempotent + lazy `Ensure<TBridge>()` activation**, mirroring Story 2-2's `LastUsedSubscriberRegistry` (Decision D35 from 2-2). Bridge is activated on first `{Command}Actions.SubmittedAction` dispatch, NOT at circuit start. Self-unsubscribes on registry Dispose. | Prevents hot-reload subscriber accumulation (Pre-mortem from 2-2 PM-4 reused). Reduces startup latency on large domains (Chaos CM-7 reused). |
| D6 | **State shape: `ConcurrentDictionary<string, LifecycleEntry>`** keyed by `CorrelationId`. `LifecycleEntry` is a mutable class (NOT a record) with fields `State`, `MessageId`, `LastUpdated`, `OriginalTransitionAt`, `OutcomeNotifications (int, mutated via Interlocked.CompareExchange)`. `_seenMessageIds` is `ConcurrentDictionary<string, byte>` (NOT `HashSet<string>` — HashSet is not thread-safe; Murat review 2026-04-16); ordering for LRU eviction is maintained by a separate `ConcurrentQueue<string>` tracking insertion order and trimmed under a short lock when over cap. Per-correlation observer list is `ImmutableList<Subscription>` updated via `ImmutableInterlocked.Update` so `Transition()` enumerates a stable snapshot. No Fluxor feature for `ILifecycleStateService` itself — state IS the dictionary. Fluxor is the *event* bus (actions); the service is the *correlation index*. | Fluxor features enforce single-reducer-per-action semantics that don't fit a cross-command correlation-keyed map. `OutcomeNotifications++` without `Interlocked` is the exact bug class that breaks FR30 under concurrent replays (Murat P0). HashSet not being thread-safe would silently corrupt dedup under Parallel.For dispatch. `ImmutableInterlocked` for the observer list prevents `InvalidOperationException: collection modified` when a callback subscribes while another thread enumerates (Amelia review). |
| D7 | **Subscription uses a bespoke callback shape, NOT `IObservable<T>`.** `Subscribe(correlationId, Action<CommandLifecycleTransition>)` returns `IDisposable`. On subscribe the service invokes the callback once with the current entry's state (replay), then invokes it again on every subsequent `Transition(...)` for that correlationId until the returned `IDisposable` is disposed. ~50 LoC, zero external deps. Exposing `IObservable<T>` without `System.Reactive` invites consumers to reach for `.Where()/.Select()/.Throttle()` → pulls Rx in through the back door and defeats the trim-friendliness goal (Winston review 2026-04-16). Bespoke callback stays narrow and explicit. | Adding `System.Reactive` (≥180 KB + transitive `Microsoft.Bcl.AsyncInterfaces` 7.x conflict risk with .NET 10 BCL) for a push-update primitive is over-kill. `IObservable<T>` without Rx is a leaky abstraction. `IAsyncEnumerable<T>` was also considered (Winston) but rejected: `await foreach` ergonomics force consumers to own an async loop per subscription, which Blazor components (FcLifecycleWrapper) cannot cleanly reconcile with `OnAfterRenderAsync` + `InvokeAsync(StateHasChanged)` re-entrancy. A callback+IDisposable is what component code actually wants. |
| D8 | **Exactly-one-outcome semantics (FR30) enforced at the service boundary.** `LifecycleEntry` carries an `int OutcomeNotifications = 0` counter mutated via `Interlocked.CompareExchange` (Decision D6 concurrency primitive). On entering a terminal state (Confirmed/Rejected), the service emits the transition to subscribers and increments the counter from 0→1. Subsequent transitions that would re-enter Confirmed/Rejected for the same `CorrelationId` are **dropped with HFC2004 log**, NOT re-emitted. `Subscribe(...)` callers that register AFTER terminal replay the terminal state exactly once on subscribe. | NFR47 ("zero silent failures") AND NFR44 ("exactly one user-visible outcome") are the two horns of the dilemma: drop duplicates (D8) but emit at least one (AC3). The counter is the reconciliation mechanism. Interlocked is required — plain `++` under concurrent replay races FR30 (Murat P0). |
| D9 | **No time-based eviction — entries live for scope lifetime.** After a `CorrelationId` reaches Confirmed or Rejected, the `LifecycleEntry` is retained until the scope (Blazor circuit / WASM user session) is disposed. Late-arriving duplicate Confirmed/Rejected for the same `CorrelationId + MessageId` tuple is recognized as idempotent (silently absorbed, no second outcome, `OutcomeNotifications` counter pinned at 1). `CorrelationId + MessageId` tuple mismatch emits HFC2004 (programmer error — different MessageId same CorrelationId). Cross-CorrelationId idempotency (Decision D10) via `_seenMessageIds` LRU covers longer-tail replay horizons independently. **Memory analysis:** 1 cmd/sec sustained × 1-hour session = 3600 entries × ~300 bytes = ~1 MB per circuit. Acceptable for Blazor Server circuit memory. Chatty-domain adopters get the same scope-lifetime bound as the rest of their Fluxor state; if this becomes a real problem, introduce LRU on `_entries` itself (configurable) — Epic 9 concern. | Hindsight H3: original 60 s + PeriodicTimer-prune design was clever but over-engineered. Scope lifetime = tab lifetime ≤ browser session; Blazor's own teardown is the correct eviction mechanism. Cutting the timer eliminates `PeriodicTimer`, `PruneLoopAsync`, dispose-order complexity, 4 grace-window tests, and the `Microsoft.Extensions.TimeProvider.Testing` dependency. Architecture §536 "evicted on terminal state" is honored via the scope-teardown contract — "evicted" is per-circuit, not per-command. |
| D10 | **Duplicate MessageId detection is detection-only, never synthesis.** A bounded `ConcurrentDictionary<string, byte> _seenMessageIds` + `ConcurrentQueue<string> _seenOrder` (LRU, capacity `LifecycleOptions.MessageIdCacheCapacity` default 1024) records every MessageId that has been seen. When a `Transition(correlationId, newState, messageId)` carries a `messageId` already in the cache but `correlationId` is NEW, the service **logs HFC2005 and treats the transition as fresh** — does NOT synthesize a terminal entry from cache, does NOT pre-populate `OutcomeNotifications`. The fresh entry proceeds through the state machine normally under its new CorrelationId. Pre-mortem PM2 (capability-token leak) showed that synthesis from cache was a security foot-gun — an attacker bypassing the bridge with a stolen MessageId would receive synthesized terminal payload for free. Detection-only preserves FR36 "deterministic duplicate detection" (caller can ask "have we seen this MsgId?" semantics via log) without leaking terminal state across correlations. Same-CorrelationId replay (the common case under Blazor circuit reconnect) still dedupes via the per-entry `OutcomeNotifications` counter (Decision D8) — no synthesis needed because the entry still exists. Cross-circuit replay is Epic 5's durable-lookup problem, not v0.1's synthesis problem. | NFR45: "Idempotent handling via ULID message IDs with deterministic duplicate detection" — "detection" is what the cache does; "idempotent handling" for the same-circuit case is `OutcomeNotifications`. Cross-circuit cross-CorrelationId synthesis was a nice-sounding idea that trusts the MessageId as a capability token — that's a Pre-mortem PM2 leak. Drop the synthesis. Epic 5 does the right thing with server-side event-store lookup. |
| D11 | **No `ConnectionState` API surface in v0.1** (Occam review 2026-04-16 + Hindsight H2 convergent cut). The original draft shipped `ConnectionState { Connected, Disconnected, Reconnecting }` + `ConnectionState` property + `ConnectionStateChanged` event as a Connected-always stub seam for Story 5-3. In practice Story 5-3 will need `LastConnectedAt` + `ReconnectAttempt` + `Reason` — redesigning the enum to a record and breaking every v0.1 consumer. Shipping the stub was cargo-culting a seam that won't survive contact with real requirements. Story 5-3 designs the contract from scratch with actual producer + consumer present. | Pre-Occam cut saves 1 enum, 2 interface members, 2 DI tests, and a future breaking-change round. Epic 2 scope is stable-connection happy path (epics.md §850) — the service doesn't care about connection state in v0.1. |
| D12 | **Service lifetime is `Scoped`.** Per-circuit in Blazor Server, per-user in Blazor WebAssembly (per architecture.md §397 D7 precedent). On scope dispose, the service disposes all `IObservable` subscriptions, cancels the `PeriodicTimer`, and drops the dictionary. NOT `Singleton` — cross-circuit state leakage would violate tenant isolation (see L03 applied to this story). | Matches `{Command}LifecycleFeature` scope (Story 2-1 Task 1.5 registers per-command Fluxor features as scoped). Circuit teardown must fully release lifecycle state. |
| D13 | **Tenant/user isolation (L03) is INHERITED, not enforced at this service.** `ILifecycleStateService` does NOT read `IUserContextAccessor` and does NOT key on `tenantId`/`userId`. Rationale: the service is already per-circuit scoped (D12); Blazor Server's per-circuit + OIDC session pinning delivers tenant isolation at the outer boundary. Keying lifecycle state on `tenantId` inside a single scope adds no safety and creates a foot-gun if adopter forgets `IUserContextAccessor` wiring. Singleton mis-registration is caught at construction by the D20 runtime guard (added 2026-04-16 post-review); the Roslyn analyzer pre-build variant is deferred to Epic 9. | L03 applies to *persisted* data (LastUsed writes to `IStorageService`). Lifecycle state is ephemeral (D9) and in-memory (D6). Different threat model. |
| D14 | **Transition ordering contract.** `Transition(correlationId, newState, messageId)` MUST be called in this order: `Submitting → Acknowledged → (Syncing →)? Confirmed | Rejected`. `ResetToIdle` resets to `Idle` and clears `MessageId`. Out-of-order calls (e.g. `Confirmed` before `Submitting`) emit HFC2004 and are DROPPED. The state machine transitions table is the single source of truth. See Task 11.2 property test. | AC5 requires state machine invariant. Out-of-order dispatch is a programmer error (bridge bug, reducer misconfiguration); dropping is safer than asserting because it preserves FR30 (exactly one outcome) even when the caller is buggy. |
| D15 | **`CommandLifecycleTransition` record includes `PreviousState` + `NewState` + `CorrelationId` + `MessageId?` + `TimestampUtc` + `LastTransitionAt` + `IdempotencyResolved`.** `IdempotencyResolved=true` when the transition resolved from duplicate-MessageId detection (D10), so observers (FcLifecycleWrapper in 2-4) can surface "already done" messaging instead of celebrating a new success. `LastTransitionAt` is the monotonic timestamp of the *most recent* transition for this correlationId (may equal `TimestampUtc` on fresh transitions, but under reconnect/replay it anchors 2-4's progressive visibility thresholds — 300ms pulse, 2s text, 10s prompt — to real command elapsed time rather than wall-clock-from-subscribe). Sally's Story C (reconnect staleness) — without this, "Still syncing…" timer lies during reconnect storms. **`ResolvedByActor` was evaluated (Sally Story A — "already done by another user" UX) and cut per Pre-mortem PM4 + Occam convergent review 2026-04-16:** no v0.1 producer populates the field (Epic 7 identity wiring is 6 months out), so Story 2-4 would branch on null and deliver no differentiation. Re-add when Epic 7 `CommandResult.ResolvedByActor` lands. | Story 2-4 needs distinct UX for "fresh Confirmed" vs "idempotent Confirmed" (epics §1037 Story 2.5 "IdempotentConfirmed" UX). Story 2-4's timer calibration needs monotonic anchor (Sally Story C). Both are cheap now, breaking post-2-4. Dropping `ResolvedByActor` is not — zero v0.1 consumer justifies it. |
| D16 | **Bridge emitter is hint-name-suffixed `Lifecycle.Bridge.g.cs`** to avoid collision with Story 2-1 `{Command}Actions.g.cs`, Story 2-1 `{Command}Form.g.razor.cs`, Story 2-2 `{Command}Renderer.g.razor.cs` / `{Command}Page.g.razor.cs` / `{Command}LastUsedSubscriber.g.cs`. Full file name: `{CommandTypeName}LifecycleBridge.g.cs`. Namespace matches command's namespace. | L04 collision prevention. Determinism > cosmetic; hint suffix "Lifecycle" unambiguous. |
| D17 | **Test count budget: ~46 new tests** (revised 2026-04-16 after party-mode + advanced-elicitation T1-T20 applies). Break-down: 6 service behavioural tests (+1 `GetActiveCorrelationIds` per T5) + 4 duplicate-detection tests (2 promoted to FsCheck properties per Murat) + 0 terminal-grace tests (cut per T4 / ADR-019) + 4 subscription-contract tests (+1 multi-subscriber AC5 gap) + 5 ULID factory tests (+1 entropy R4 per T16) + 3 bridge emitter tests + 3 bridge emitter snapshot tests (+1 nested-namespace) + 1 HFC1017 generic-command rejection (T14) + 15 FsCheck state-machine properties (+3 concurrency/cache + 2 scope-dedup/re-entrant per T20) + 3 registration/DI tests + 1 non-persistence invariant + 3 end-to-end Counter sample tests. Per L06 defense-in-depth budget: this is a feature story capped at ≤25 decisions (we're at 20 after D19/D20 ✓) and tests scale with decisions, not features. L07 cost-benefit: the 5 concurrency/cache/re-entrant FsCheck properties are the highest-leverage adds — each covers 1000+ scenarios and directly validates FR30/FR36/NFR47 under real runtime conditions. | L06 + L07 applied. Story 2-2 spent 121 tests on 39 decisions (~3.1/decision); 2-3 lands at 46 tests on 20 decisions (~2.3/decision). Tight-but-realistic — the FsCheck property budget (15) absorbs what would be 40+ example-based tests in a less-rigorous story. PruneLoop cut (T4) net-balanced by T11/T14/T16/T20 adds. |
| D18 | **No changes to existing `{Command}LifecycleFeature` reducers.** The per-command Fluxor feature continues to own the per-command state slice. The new bridge ADDS a cross-command index without modifying Story 2-1's emitted reducers. Only actions change: `ResetToIdleAction()` → `ResetToIdleAction(string CorrelationId)`. The marker-interface approach was evaluated (Occam review 2026-04-16) and cut because no consumer reads the marker. | Forward-compatibility guarantee for Story 2-1 (all 229 SourceTools tests pass; 50 Shell tests pass; Counter sample unchanged). ADR-017 framing: index over features, not replacement of features. |
| D19 | **Single-writer invariant — HFC2007 divergence diagnostic + binding consumer contract for Story 2-4.** The bridge is the ONLY writer to `ILifecycleStateService`. The per-command `{Command}LifecycleFeature` reducer is a read-only downstream of the same actions. Because "convention" is not enforcement (Winston review), introduce **HFC2007 (Warning, runtime)**: log-and-continue when `Transition()` observes that an entry exists for the CorrelationId but no prior `Submitted` was seen — indicates a test harness, dev tool, or adopter code dispatched actions directly without going through the bridge. Not a hard failure (tests need flexibility) but loud. Reserved HFC2007 in `DiagnosticDescriptors.cs`; runtime-logged, not analyzer-emitted. **Binding consumer contract added 2026-04-16 per Red Team R1:** Story 2-4 `FcLifecycleWrapper` MUST consume `ILifecycleStateService` via `Subscribe(...)`, NEVER read `{Command}LifecycleFeature` state directly — bypassing the service loses HFC2007 divergence detection and misses the `OutcomeNotifications` FR30 enforcement. Document as a binding contract line in Story 2-4 (reference this D19). | Divergence between per-command feature state and service dictionary is subtle; silent is worse than loud. Consumer contract locks the architectural seam; wrapper that reads features directly defeats the service's purpose. |
| D20 | **Runtime singleton-resolve guard.** `LifecycleStateService` constructor checks a process-static `ConditionalWeakTable<IServiceProvider, LifecycleStateService>` keyed on the resolving scope provider. If the same scope provider already has a live instance, throw `InvalidOperationException` with a Fix-framed message pointing to `services.TryAddScoped<ILifecycleStateService, LifecycleStateService>()` (Winston review — "XML doc is not enforcement"). Five lines of code; closes the foot-gun today. The Roslyn analyzer pre-build guard remains deferred to Epic 9. Note: the guard uses `ConditionalWeakTable` so disposed scopes don't leak references — once the adopter's scope is GC'd, the entry drops. | Adopters who forget `TryAddScoped` and register as Singleton would silently share lifecycle state across circuits (tenant-isolation footgun per L03 spirit, even though lifecycle is ephemeral). |

---

## Architecture Decision Records

### ADR-017: Lifecycle Service as Cross-Command Correlation Index

> **TL;DR (for new adopters):** Per-command Fluxor feature = state slice. `ILifecycleStateService` = correlation-keyed index over all slices.

- **Status:** Accepted
- **Context:** Story 2-1 emits one Fluxor feature per command (`{Command}LifecycleFeature<{Command}LifecycleState>`). State is typed and per-command. Story 2-4 (`FcLifecycleWrapper`) must render a UI for a **single correlated command** regardless of type — the wrapper takes a `CorrelationId` parameter. Consuming Story 2-1's per-command state via reflection (walk all Fluxor features, look for one where `CorrelationId==X`) is AOT-hostile and slow. Alternative: a single unified feature across all commands loses typed payloads.
- **Decision:** Introduce `ILifecycleStateService` as a **hand-written scoped service** that projects the union of per-command Fluxor actions into a correlation-keyed dictionary. Per-command generated bridges (`{Command}LifecycleBridge.g.cs`) subscribe to the typed actions and forward to the service via a type-erased API (`Transition(correlationId, state, messageId)`). Story 2-1's generated features continue to own typed per-command state; `ILifecycleStateService` holds correlation-keyed aggregate state for cross-type consumers (FcLifecycleWrapper, duplicate detection, exactly-one-outcome enforcement).
- **Consequences:** (+) Typed payload preserved for per-command consumers; (+) `FcLifecycleWrapper` consumes a single service instead of reflecting over N features; (+) duplicate detection lives in one place; (+) Story 2-1 emission unchanged except for marker interface application. (-) Two sources of truth for lifecycle state — per-command feature AND service dictionary. Mitigation: the bridge is the ONLY writer to the service (Decision D19 single-writer invariant, HFC2007 divergence diagnostic); per-command feature is read-only input to the bridge. Divergence is detectable via a snapshot test (Task 11.5) AND loud via HFC2007 logs at runtime.

**Scope contract — circuit-locality in v0.1 (Sally review 2026-04-16).** `Subscribe(correlationId, ...)` in v0.1 only sees transitions that flowed through THIS service instance (per-circuit in Blazor Server, per-user in WASM). A user who closes a tab mid-command and reopens lands on a fresh circuit with an empty dictionary — `Subscribe(sameCorrelationId)` will NOT replay the prior transition because the prior circuit's service is gone. This is deliberate: lifecycle state is ephemeral per architecture.md §536 and NOT persisted. Epic 5 (Story 5-4 reconnection reconciliation) backs the same `ILifecycleStateService` interface with a durable lookup seam (server-side event-store query by CorrelationId) WITHOUT changing the interface shape. Adopters consuming `Subscribe()` must assume transitions visible only within the current circuit in v0.1; Epic 5 upgrades this transparently. This contract is pinned so Story 2-4 does not code against "lifecycle is durable" assumptions — it's not, until 5-4.
- **Rejected alternatives:**
  - **Single unified Fluxor feature** — loses typed command payloads; complicates Story 2-1's already-shipped reducers.
  - **`ILifecycleStateService` reads from Fluxor store via `IState<T>` on-demand** — requires reflection over feature list (AOT-hostile, slow); race between action dispatch and service read.
  - **System.Reactive-backed `IObservable<CommandLifecycleTransition>`** — 180 KB + transitive `Microsoft.Bcl.AsyncInterfaces` conflict risk with .NET 10 BCL. The hand-rolled observable would have been ~60 LoC but was superseded by the bespoke callback contract (ADR-018 revision 2026-04-16) which eliminates the Rx-drift incentive altogether.
  - **Emit `ILifecycleStateService` itself per command (per-type typed service)** — combinatorial service explosion; Story 2-4 would need to resolve `ILifecycleStateService<TCommand>` but wrapper is used with CorrelationId, not TCommand.

### ADR-018: Bespoke Callback Subscription (no Rx), NUlid for ULIDs

- **Status:** Accepted (revised 2026-04-16 after Winston review — contract changed from `IObservable<T>` to callback+IDisposable)
- **Context:** The service must push lifecycle transitions to subscribers (FcLifecycleWrapper in 2-4). Three contract shapes considered:
  1. `IObservable<T>` — standard .NET but invites consumers to reach for `System.Reactive` (~180 KB + transitive `Microsoft.Bcl.AsyncInterfaces` 7.x conflict) for composition operators.
  2. `IAsyncEnumerable<T>` — trim-friendly, composes with `await foreach`. But Blazor components cannot cleanly interleave `await foreach` with `OnAfterRenderAsync` re-entrancy and `InvokeAsync(StateHasChanged)` without manual `CancellationTokenSource` plumbing per subscription.
  3. Bespoke `IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition)` — callback invoked on subscribe (replay) + on every subsequent transition; disposable stops further invocations.
- **Decision:** Take option 3 (bespoke callback). Ship a ~50-LoC subscription implementation backed by a per-correlation `List<Subscription>` snapshot-to-array-inside-lock pattern (Amelia review 2026-04-16 — prevents `InvalidOperationException: collection modified` when `Transition` fires while observers subscribe). Take the NUlid dependency for ULID generation, exposed via `IUlidFactory` seam.
- **Consequences:** (+) No Rx invitation — callers that need filtering/throttling do it explicitly inside their callback rather than chaining operators that pull Rx transitively. (+) Blazor-component-idiomatic — `OnInitialized` captures the IDisposable, `Dispose` releases. (+) Trim-safe, AOT-safe. (+) NUlid avoids 30+ LoC of Crockford Base32 + timestamp-hi-lo arithmetic. (-) Not a standard .NET subscription shape — adopters must learn our specific API. Mitigation: XML doc shows usage; FcLifecycleWrapper (2-4) becomes the canonical example. (-) **ImmutableInterlocked CAS storm under rapid sub/dispose (Pre-mortem PM3):** `ImmutableInterlocked.AddOrUpdate` retries on CAS collision. Under heavy subscription churn (e.g., rapid Blazor route changes with many FcLifecycleWrappers mounting/unmounting), CPU can spike on the retry loop. No functional bug, but a performance cliff to watch. Mitigation: if real-world load tests show the cliff, swap `ImmutableList<Subscription>` for a `Channel<T>`-backed subscription queue in a later story. For v0.1 the trade-off is acceptable — FcLifecycleWrapper instances typically number in single digits per page.
- **Rejected alternatives:**
  - **`IObservable<T>`** — leaky abstraction without Rx; invites Rx drift. Breaking-change cost post-2-4 is the reason to pick the right contract now.
  - **`IAsyncEnumerable<T>`** — ergonomic mismatch with Blazor re-entrancy; forces per-subscription `CancellationTokenSource` plumbing.
  - **Take `System.Reactive`** — dependency bloat, transitive conflict risk, full Rx is way more than the contract needs.
  - **Hand-roll ULID from scratch** — Crockford Base32 + monotonic timestamp + 80-bit random is easy to get subtly wrong. NUlid passes the ULID spec's 1000-sample conformance test out-of-the-box.
  - **Use `Guid` as MessageId** — satisfies "unique" but NOT "time-sortable" (ULID canonical property) and NOT "idempotency-key-friendly" per FR36. Guid + tiebreak timestamp loses the lexicographic-sort property for deduplication cursors (relevant for Epic 5 reconciliation).
  - **Ship `ILifecycleStateService` without subscription support (pull-only `GetState`)** — Story 2-4's `FcLifecycleWrapper` needs push-based updates to transition UI state without polling. Polling at 30 Hz across N popovers is visibly wasteful.

### ADR-019: Scope-Lifetime Eviction (no PeriodicTimer, no grace window)

- **Status:** Accepted (revised 2026-04-16 after Hindsight H3 — cut `PeriodicTimer` + time-based grace window entirely; retained terminal entries live for scope lifetime)
- **Context:** Architecture document §536 says `CommandLifecycleState` is **ephemeral** and evicted on terminal state. Epic 5's reconnect path (Story 5-4) will replay SignalR catch-up events, which may re-deliver a Confirmed for a CorrelationId that has already reached terminal. Two competing pressures: (a) FR30 requires exactly-one-outcome even under replay (idempotent absorption), (b) memory footprint should stay bounded. Intermediate drafts landed on 5-min then 60-s grace windows with `PeriodicTimer` pruning, but Hindsight review flagged this as over-engineered for the actual usage profile.
- **Decision:** **No time-based eviction.** Terminal entries live until scope disposal (Blazor circuit teardown, WASM session end). Idempotent replay is detected via the `OutcomeNotifications` counter check inside the entry (same-CorrelationId) and via the bounded `_seenMessageIds` LRU (cross-CorrelationId). Both mechanisms survive without a timer. No `PeriodicTimer`, no `PruneLoopAsync`, no `FakeTimeProvider` test dep, no timer-dispose-order complexity. Memory is bounded by the natural scope lifetime — Blazor Server circuits are typically <1 hour; WASM user sessions are browser-tab-bound. A ~1 MB ceiling for a very chatty circuit (1 cmd/sec × 1 hour × 300 B/entry) is acceptable and matches the per-command Fluxor feature state footprint adopters already tolerate. Epic 5's cross-circuit replay is NOT covered here by design — Story 5-4's durable lookup backs the service interface with a server-side event-store query without changing the interface shape.
- **Consequences:** (+) Simpler service — `LifecycleStateService` shrinks by ~40 LoC. (+) No timer-cancellation edge cases. (+) No `FakeTimeProvider` test seam needed. (+) `LifecycleOptions` reduces to `MessageIdCacheCapacity` only. (+) Scope-teardown is the single eviction mechanism (clear and debuggable). (-) Chatty long-lived circuits could theoretically pressure memory; mitigation is per-circuit self-limitation (Blazor's circuit-lifetime is naturally bounded). If real adopters report pressure, add LRU-on-`_entries` in Epic 9; not speculative v0.1 work. (-) Resubmit-after-scope-teardown creates a fresh lifecycle with the same CorrelationId (Chaos CM5 — documented below as correct semantic).
- **Resubmit-after-scope-teardown semantic (CM5):** If an adopter resubmits the same CorrelationId on a fresh circuit (after the prior circuit was disposed), the service observes two independent lifecycles. This is the correct behavior — the fresh circuit has no memory of the prior one. Epic 5's durable lookup changes this semantic when it lands (prior terminal state becomes visible to the new circuit) without changing the interface.
- **Rejected alternatives:**
  - **60-s grace window with `PeriodicTimer` (prior draft 2026-04-16)** — earned its memory footprint only for within-circuit reconnect replay, which Blazor Server already handles via circuit reconnection (the circuit instance persists across short network drops, so the service persists too). Real cross-circuit replay is Epic 5's durable lookup job.
  - **5-minute grace window (original draft)** — arbitrary; holds thousands of entries; same critique as above, amplified.
  - **No grace window + immediate eviction on terminal** — loses the within-circuit idempotent-replay property (which we ACTUALLY want); breaks FR30 under bridge misfire scenarios.
  - **Persist lifecycle state to `IStorageService`** — architecture §536 explicitly says ephemeral. Persisting creates tenant-isolation attack surface (L03) and doesn't help the cross-circuit case (localStorage is per-device, not per-session) any better than Epic 5's server-side lookup.
  - **LRU-on-`_entries` (bounded-count eviction)** — adds complexity without earning it in v0.1. Reserve for Epic 9 if measured pressure surfaces.

---

## Lifecycle Sequence Diagram (Reference)

Canonical click → Confirmed sequence with ILifecycleStateService observation. Tasks must align.

```
USER    FORM (2-1)    CommandService     LifecycleBridge.g.cs    ILifecycleStateService    Observer (FcLifecycleWrapper 2-4)
 |          |              |                    |                        |                                |
 |-- click->|              |                    |                        |                                |
 |          |-- Dispatch Submitted(CorrId, cmd) |                        |                                |
 |          |              |                    |<-- bridge.OnSubmitted(action) -- [Subscribed via IActionSubscriber]
 |          |              |                    |-- service.Transition(CorrId, Submitting, null) -->       |
 |          |              |                    |                        |-- push to observers [if any] -->|
 |          |              |                    |                        |                                |-- render spinner
 |          |              |                    |                        |                                |
 |          |-- await DispatchAsync(cmd, onLifecycleChange, ct) ------->  |                                |
 |          |              |-- HTTP POST (202 + ULID MessageId) -->       |                                |
 |          |              |<-- CommandResult(MessageId=01HXYZ..., Accepted)                               |
 |          |<-- result -- |                    |                        |                                |
 |          |-- Dispatch Acknowledged(CorrId, MsgId=01HXYZ..) ---------- bridge fwd ---->                  |
 |          |              |                    |                        |-- Transition(Acknowledged, MsgId)
 |          |              |                    |                        |-- dedup: seenMessageIds.add("01HXYZ")
 |          |              |                    |                        |-- push Ack transition to obs -> |
 |          |              |                    |                        |                                |
 |          |              |-- onLifecycleChange(Syncing, CorrId) (stub or SignalR) ---->                  |
 |          |-- Dispatch Syncing(CorrId) -- bridge fwd -- Transition(Syncing, MsgId) -- push --> observer  |
 |          |              |                    |                        |                                |
 |          |              |-- onLifecycleChange(Confirmed, CorrId) ---->                                  |
 |          |-- Dispatch Confirmed(CorrId) -- bridge fwd -- Transition(Confirmed, MsgId) [TERMINAL]        |
 |          |              |                    |                        |-- OutcomeNotifications++ (=1)   |
 |          |              |                    |                        |-- push Confirmed transition ->  |
 |          |              |                    |                        |                                |-- render success
 |          |              |                    |                        |-- keep entry (grace=5min)       |

IDEMPOTENT REPLAY PATH (Epic 5 reconnect, landing Story 5-4 but probed here):
 |          |              |-- [reconnect replays the same Confirmed] -- bridge fwd ->                    |
 |          |              |                    |                        |-- entry exists + same MsgId     |
 |          |              |                    |                        |-- IdempotencyResolved=true     |
 |          |              |                    |                        |-- DROP (no second outcome -- FR30)

DUPLICATE MessageId DIFFERENT CorrelationId (FR36 true duplicate):
 |          |              |-- [new submit, same MsgId, new CorrId] ---- bridge fwd ->                    |
 |          |              |                    |                        |-- seenMessageIds.Contains("01HXYZ") == true
 |          |              |                    |                        |-- CorrelationId new → synthesize terminal entry
 |          |              |                    |                        |-- push Confirmed with IdempotencyResolved=true
 |          |              |                    |                        |-- HFC2005 log (duplicate MsgId)

ERROR PATH:
 |          |              |-- throw CommandRejectedException(reason, resolution) -----> |               |
 |          |-- Dispatch Rejected(CorrId, reason, resolution) -- bridge fwd -- Transition(Rejected, null) [TERMINAL]
 |          |              |                    |                        |-- OutcomeNotifications++ (=1)  |
 |          |              |                    |                        |-- push Rejected -- observer -->|
 |          |              |                    |                        |                                |-- render rejection

OUT-OF-ORDER (PROGRAMMER ERROR):
 |          |-- [bug: dispatch Confirmed without Submitted] ------------ bridge fwd -->                   |
 |          |              |                    |                        |-- entry does NOT exist         |
 |          |              |                    |                        |-- HFC2004 log + DROP (no outcome raised; FR30 still "≤1")

CANCELLATION (form disposed):
 |          | [Dispose]    |                    |                        |                                |
 |          |              |                    | bridge.Dispose() -- unsubscribe                          |
 |          |              |                    |                        |                                | observer.Dispose()
 |          |              |                    |                        |-- observer count decremented    |
```

Task 4 service implementation MUST match this sequence. Task 5 bridge emitter MUST match this sequence. Task 8 Counter sample verification MUST observe this sequence via bUnit.

---

## Acceptance Criteria

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

## Known Gaps (Explicit, Not Bugs)

These are cross-story deferrals intentionally out of scope for Story 2-3. QA should NOT file these as defects.

| Gap | Owning Story | Reason |
|---|---|---|
| Visual lifecycle feedback (spinner, sync pulse 300ms+, "Still syncing..." text 2-10s, action prompt >10s, `aria-live` announcements) | Story 2-4 (FcLifecycleWrapper) | UX rendering is 2-4's scope; 2-3 ships the state source-of-truth the wrapper reads |
| Real `HubConnectionState` observation, reconnection/disconnection handling | Story 5-3 (SignalR connection) | `ConnectionState` returns `Connected` until 5-3 wires real state |
| Reconnection reconciliation (replayed projections, batched catch-up sweep) | Story 5-4 (reconnection reconciliation) | Grace-window idempotency (ADR-019) is the substrate 5-4 builds on; 2-3 does not trigger reconciliation itself |
| Domain-specific rejection message formatting (`[What failed]: [Why]. [What happened to the data]`) | Story 2-5 (rejection + confirmation) | 2-3 propagates Reason+Resolution strings; formatting lives in wrapper/messageBar |
| Destructive command confirmation dialog and form abandonment warning | Story 2-5 | Cross-cutting UX concern |
| Agent-surface two-call lifecycle tool (`lifecycle/subscribe`) | Epic 8 (Story 8-3) | `ILifecycleStateService` is the reusable substrate; agent-surface is a separate transport |
| `IdempotentConfirmed` as a distinct `CommandLifecycleState` enum value | Deferred (v1.x) | For v0.1, idempotent resolution is signalled via `CommandLifecycleTransition.IdempotencyResolved=true` while `NewState=Confirmed`. Promoting `IdempotentConfirmed` to its own enum value requires updating per-command Fluxor features (Story 2-1 reducers) — not worth the blast radius for v0.1 |
| Singleton-registration-detection Roslyn analyzer (guard against adopter mis-registration) | Epic 9 (Story 9.4) | Analyzer diagnostic tooling is 9.4; 2-3 documents the contract in XML doc + sample call site |
| LRU-on-`_entries` for chatty-circuit memory pressure | Epic 9 | ADR-019 revision 2026-04-16 chose scope-lifetime eviction. If real adopter pressure surfaces, add bounded-count LRU on `_entries` alongside the existing `_seenMessageIds` LRU. Not speculative v0.1 work. |
| `ILifecycleStateService` exposure of `IEnumerable<string> ActiveCorrelationIds` for admin/debug surface | Story 9.2 (CLI inspection) | Debug/inspection surface is Epic 9's domain |
| Lifecycle cross-surface mirroring (same lifecycle observed on web + chat agent simultaneously per PRD §523) | Epic 8 (Story 8-4) | 2-3 supports this by being CorrelationId-keyed; 8-4 bridges the MCP surface |
| `Subscribe()` replaying transitions that happened on a PRIOR circuit (closed tab + reopened → fresh circuit → empty state) | Story 5-4 (reconnection reconciliation) | Sally Story B — the `ILifecycleStateService` interface is designed so Epic 5 can back it with durable server-side lookup (event-store query by CorrelationId) WITHOUT changing the interface. v0.1 is circuit-local; v0.3+ silently gains cross-circuit replay. Story 2-4 MUST NOT code against "lifecycle is durable" assumptions — document in 2-4's Dev Notes. |
| MessageId cache exhaustion DoS (`_seenMessageIds` evictable via `Transition()` flood) | Epic 9 (Fluxor middleware rate-limit) | Red Team R3 — attacker bypassing the bridge with crafted `Transition()` calls can flood the LRU cache, evicting legitimate recent MessageIds. Deterministic duplicate detection breaks. Mitigation for v0.1: `LifecycleOptions.MessageIdCacheCapacity` is configurable (default 1024); adopters with high legitimate throughput raise it. Epic 9 adds Fluxor middleware that rate-limits `ICommandLifecycleAction` dispatch per second per scope. |
| Multi-assembly `[Command]` registration (plug-in model) — `AddHexalithDomain<T>` scans only the marker type's assembly | Story 7-2 (tenant context + plug-in isolation) | Pre-mortem PM1 — commands declared in dynamically-loaded NuGet packages won't get their bridge registered. In v0.1 adopters using plug-in patterns must explicitly register each command bridge type. Story 7-2's tenant-aware plug-in loader standardizes the pattern. |
| Bridge emitter analyzer to verify bridge subscribes to EXACTLY the 6 generated action types and nothing more | Epic 9 | Structural analyzer is 9.4 domain; for 2-3, snapshot test (Task 11.5) guards |
| Exported Public API surface review of `ILifecycleStateService` (SemVer discipline) | PublicApiAnalyzer (existing infra) | Shipping public surface auto-triggers existing analyzer; if it flags, update ShippedApi.txt |

---

## Tasks / Subtasks

### Task 0: Prerequisites (AC: all)

- [ ] 0.1: Confirm Story 2-1 + Story 2-2 are merged to main (or at least all `2-1-*.cs` generated-artifact emitters + `CommandFluxorActionsEmitter` are stable). `SourceTools.Tests` + `Shell.Tests` green on HEAD.
- [ ] 0.2: Add `NUlid` package to `Directory.Packages.props` (pin exact `1.7.4`). Reference from `Hexalith.FrontComposer.Shell.csproj` (NOT Contracts — Contracts stays dependency-free per architecture.md §1144).
- [ ] 0.3: Reserve diagnostic IDs HFC1016, HFC1017, HFC2004, HFC2005, HFC2006, HFC2007 in `DiagnosticDescriptors.cs` (SourceTools range 1000-1999 for HFC1016/1017; Shell range 2000-2999 for HFC2004/5/6/7 — architecture.md §648 ID-range table). HFC1016 and HFC1017 are analyzer-emitted (parse-time errors); HFC2004/5/6/7 are runtime `ILogger.LogError`/`LogWarning` with the HFC prefix in the message template. HFC1017 is the Hindsight H9 defense — reject `[Command]` on generic types at parse time. Update `AnalyzerReleases.Unshipped.md` with HFC1016 and HFC1017.
- [ ] 0.4: Confirm `Fluxor.Blazor.Web 6.9.0` is pinned (Story 2-2 Task 0.4 already verified — inherited).
- [ ] 0.5: Verify Story 2-1 `CommandFluxorActionsEmitter` emits the 6 action records `SubmittedAction`, `AcknowledgedAction`, `SyncingAction`, `ConfirmedAction`, `RejectedAction`, `ResetToIdleAction`. If any are missing, HALT and raise cross-story contract issue.

### Task 1: IR Model (AC: 2) — No Changes (See D18)

- [ ] 1.1: Confirm `CommandModel` IR exposes `TypeName`, `Namespace`, `BoundedContext`, `FullyQualifiedName` — required for bridge emitter templating. Expected: all present from Stories 2-1/2-2.
- [ ] 1.2: NO new IR fields added for this story — the bridge is a pure projection of existing IR. If Task 5 discovers a missing field, raise before proceeding.

### Task 2: Contracts API Surface (AC: 1, 2, 3)

- [ ] 2.1: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/ILifecycleStateService.cs`:
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
- [ ] 2.2: **CUT** (Occam review 2026-04-16) — original draft introduced `ICommandLifecycleAction` marker interface to enable cross-type subscription. In practice, the bridge emitter (Task 5.1) subscribes to each concrete `{Command}Actions.*Action` type directly via `IActionSubscriber.SubscribeToAction<T>`; no consumer reads the marker. Creating it would have forced re-approval of Story 2-1 `CommandFluxorActionsEmitter` snapshots with no downstream benefit. If a future story (Epic 8 MCP bridge) needs cross-type action enumeration, introduce the marker then with a real consumer. **Task 2.2 is intentionally empty.**
- [ ] 2.3: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleTransition.cs`:
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
- [ ] 2.4: **CUT** (Occam T2 / Hindsight H2 convergent) — no `ConnectionState` enum or API surface in v0.1. Story 5-3 designs the connection-state contract from scratch with real `LastConnectedAt` / `ReconnectAttempt` / `Reason` requirements present. **Task 2.4 is intentionally empty.**
- [ ] 2.5: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs`:
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
- [ ] 2.6: Verify Contracts `.csproj` still has zero external PackageReferences (architecture.md §1144 — Contracts is dependency-free). Running `dotnet list package` inside Contracts must show only BCL.
- [ ] 2.7: PublicApiAnalyzer: if the project has `PublicApiAnalyzers` wired (check existing `ShippedApi.txt`/`UnshippedApi.txt` in Contracts), append the 6 new public types + members to `UnshippedApi.txt`.
- [ ] 2.8: Create `src/Hexalith.FrontComposer.Contracts/Lifecycle/LifecycleOptions.cs`:
  ```csharp
  public sealed class LifecycleOptions
  {
      /// <summary>Bounded LRU capacity for cross-CorrelationId duplicate MessageId detection. Default 1024. (Decision D10)</summary>
      public int MessageIdCacheCapacity { get; set; } = 1024;
  }
  ```
  `GracePeriod` and `PruneInterval` were evaluated and cut per Hindsight H3 / Occam T4 — see ADR-019. No time-based eviction in v0.1.

### Task 3: ULID Factory & Action Marker Interface Application (AC: 2, 4)

- [ ] 3.1: Create `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`:
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
- [ ] 3.2: Modify `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorActionsEmitter.cs` — **only** extend `ResetToIdleAction()` to `ResetToIdleAction(string CorrelationId)` so the bridge (Task 5.1) can forward CorrelationId when resetting. The other 5 action records already carry CorrelationId from Story 2-1 — leave them untouched (Occam T1 cut the marker interface that would have added a layer of modification). Patch:
  ```csharp
  // Current (Story 2-1 line 47):
  _ = sb.AppendLine("    public sealed record ResetToIdleAction();");
  // New:
  _ = sb.AppendLine("    public sealed record ResetToIdleAction(string CorrelationId);");
  ```
  This is a breaking change to the dispatch site in `CommandFormEmitter.EmitSubmitMethod` — specifically the dispatch inside the `catch (OperationCanceledException)` block at `CommandFormEmitter.cs:301` (verified 2026-04-16; originally estimated "line ~293" but the exact line is 301). There is **only one** dispatch site; the `_submittedCorrelationId` field was already captured on line 255 (`_submittedCorrelationId = correlationId;`) so the fix is mechanical. Update the existing `OnResetToIdle` reducer in `CommandFluxorFeatureEmitter` to accept the new param (it already pattern-matches on action type and returns initial state — adding a param doesn't change behavior; the guard `state.CorrelationId != action.CorrelationId ? state : ...` used by other reducers should NOT be applied here because ResetToIdle is deliberately correlation-resetting). **Blast radius reduced post-Occam:** only 2 snapshot baselines tied to the `ResetToIdleAction` dispatch need re-approval (Task 7.2).
- [ ] 3.3: Modify `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` — line ~246 `var correlationId = Guid.NewGuid().ToString();`: NO change needed for CorrelationId (Decision D2: CorrelationId stays as `Guid.NewGuid().ToString()`, only MessageId switches to ULID). The form does NOT inject `IUlidFactory` — MessageId is generated **server-side** by the command service implementation (stub or real EventStore), not by the form.
- [ ] 3.4: Modify `src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs` line 55 `string messageId = Guid.NewGuid().ToString();`:
  - Inject `IUlidFactory` via constructor (add as required parameter — breaking constructor change within Shell, but no external adopters use `StubCommandService` directly).
  - Replace with `string messageId = _ulidFactory.NewUlid();`
  - Update the 7 existing `StubCommandService` tests in `Services/StubCommandServiceTests.cs` to pass a stub `IUlidFactory`.
- [ ] 3.5: Modify `src/Hexalith.FrontComposer.Shell/Services/DerivedValues/SystemValueProvider.cs` — inject `IUlidFactory` (required). Update the `"MessageId"` case:
  ```csharp
  // From: "MessageId" => new DerivedValueResult(true, Guid.NewGuid().ToString("N")),
  // To:   "MessageId" => new DerivedValueResult(true, _ulidFactory.NewUlid()),
  ```
  Other cases (`CommandId`, `CorrelationId`, `Timestamp`, etc.) are unchanged. **Note:** `SystemValueProvider` handles derived-field pre-fill on command model property whose name is `MessageId` — this is ADDITIONAL to the `StubCommandService` ULID path. Both must converge on `IUlidFactory`.
- [ ] 3.6: Unit tests for `UlidFactory` (exactly 5 tests — +1 for Red Team R4 entropy verification):
  1. `NewUlid_ReturnsValidCrockfordBase32_26Chars` — regex `^[0-9A-HJKMNP-TV-Z]{26}$`
  2. `NewUlid_ReturnsMonotonicStrings_WhenCalledRapidly` — 10 sequential calls all sort lexicographically in ascending order (millisecond-precision timestamp prefix)
  3. `NewUlid_IsThreadSafe` — 100 parallel `NewUlid()` calls yield 100 distinct values
  4. `UlidFactory_ServiceRegistration_ResolvesAsIUlidFactory` — verify DI wiring resolves the right type
  5. `NewUlid_EntropyIsCryptographic_NotPredictableFromPriorOutputs` (Red Team R4) — generate 1000 ULIDs, extract the 80-bit entropy portions, assert statistical distribution is uniform (chi-square at p≥0.01). Ensures NUlid uses `RandomNumberGenerator` not `Random`; catches an accidental swap to a predictable source.

### Task 4: LifecycleStateService Implementation (AC: 1, 2, 3, 4)

- [ ] 4.1: Create `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` as a sealed class implementing `ILifecycleStateService`. Core shape (revised 2026-04-16 for concurrency primitives + options + subscription contract):
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
- [ ] 4.2: Create `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleBridgeRegistry.cs` (mirrors `LastUsedSubscriberRegistry` pattern from Story 2-2 — identical shape):
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
- [ ] 4.3: Modify `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` inside `AddHexalithFrontComposer()` (after the existing Decision D37 block ~line 148):
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
- [ ] 4.4: Modify `AddHexalithDomain<T>()` in the same file. **Verified 2026-04-16** — the existing loop at `ServiceCollectionExtensions.cs:46` already scans for `LastUsedSubscriber`-suffixed types and calls `services.TryAdd(ServiceDescriptor.Scoped(type, type))`. Extend the existing `if (type.Name.EndsWith("LastUsedSubscriber", ...))` block with a parallel branch for `type.Name.EndsWith("LifecycleBridge", StringComparison.Ordinal) && typeof(IDisposable).IsAssignableFrom(type)` that does the same `services.TryAdd(ServiceDescriptor.Scoped(type, type))`. Single-line addition in the loop body. No bootstrap rewrite needed.
- [ ] 4.5: Implement `Transition(correlationId, newState, messageId)` with concurrency primitives (Decision D6, Murat P0):
  - Look up current entry; if absent, `GetOrAdd` creates one with `State=Idle, OutcomeNotifications=0, OriginalTransitionAt=now`.
  - Validate transition against state machine (see AC2 table). Invalid → log HFC2004, return (drop). Validation reads entry.State under the class's intrinsic lock (sealed class; lock on `entry` itself) — atomic snapshot.
  - For duplicate-MessageId detection (Decision D10 revision per T6 / Pre-mortem PM2 — detection-only, never synthesis):
    - If `messageId != null` and `_seenMessageIds.ContainsKey(messageId)` and `correlationId` IS in `_entries`: same-circuit same-CorrelationId replay (Blazor reconnect). The entry's `OutcomeNotifications` counter (D8) handles idempotency — set `IdempotencyResolved=true` on the transition when pushing to subscribers.
    - If `messageId != null` and `_seenMessageIds.ContainsKey(messageId)` and `correlationId` is NEW: cross-CorrelationId MessageId collision. Log HFC2005. **Do NOT synthesize terminal state from cache.** Treat as fresh submission — fresh entry, fresh `OutcomeNotifications=0`, normal state-machine progression. Epic 5 handles the cross-circuit case via durable server-side lookup.
    - Else (MessageId fresh): insert into `_seenMessageIds` + `_seenOrder` under `_seenLock`; evict oldest if over capacity.
  - On entering terminal state, use `Interlocked.CompareExchange(ref entry.OutcomeNotifications, 1, 0)` — if result was 0, this is the first terminal notification; push to subscribers. If result was 1, it's an idempotent replay; still push with `IdempotencyResolved=true` BUT do NOT increment further (counter stays at 1 — FR30 invariant).
  - Record `messageId` in `_seenMessageIds` + `_seenOrder` under `_seenLock`; evict oldest if over `_options.MessageIdCacheCapacity`.
  - Enumerate observers via `_subs.TryGetValue(correlationId, out var list)` → `list` is an `ImmutableList<Subscription>` snapshot (ImmutableInterlocked guarantees stable enumeration even if another thread Subscribes or Disposes concurrently). For each `Subscription sub`: check `if (Volatile.Read(ref sub.Disposed) != 0) continue;` (Red Team R6 defense — closes the race between `Unsubscriber.Dispose` marking disposed and the snapshot-enumeration firing the callback). Invoke `sub.Callback(transition)` **OUTSIDE any entry lock** (Chaos CM4 re-entrancy rule) inside a `try/catch` that logs callback faults to `ILogger` but does not propagate (Red Team R5 defense).
- [ ] 4.6: Implement `Subscribe(correlationId, onTransition)` with the bespoke callback contract (Decision D7 / ADR-018):
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
- [ ] 4.7: **CUT** (ADR-019 revision 2026-04-16) — no `PruneLoopAsync`. Scope-lifetime eviction via `Dispose`/`DisposeAsync` (Task 4.8). **Task 4.7 is intentionally empty.** Helper `IsTerminal` moves inline to Task 4.5 where it is actually used.
- [ ] 4.8: Implement `Dispose` + `DisposeAsync`. Scope-lifetime eviction only — no timer to cancel, no prune loop to await. Both paths converge on the same clear-dictionaries implementation (Pre-mortem PM5: sync Dispose MUST NOT block — no async loop to block on anyway now).
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
- [ ] 4.9: Unit tests for `LifecycleStateService` (exactly 6 behavioural tests — +1 for T5 `GetActiveCorrelationIds`):
  1. `Transition_SubmittingAckSyncingConfirmed_EmitsTransitionsInOrder` — sequential happy path, observer receives 4 transitions in order
  2. `Subscribe_AfterTerminal_ReplaysTerminalOnceOnSubscribe` — subscribe post-Confirmed replays Confirmed exactly once
  3. `Transition_InvalidStateMachineEdge_DropsAndLogsHfc2004` — e.g. Idle → Confirmed rejected
  4. `GetState_UnknownCorrelationId_ReturnsIdle` — default (NOT throw)
  5. `DisposeAsync_WhileSubscriberActive_DoesNotThrow_ClearsAllState` — clean teardown (T4: no timer to cancel, just clear dictionaries)
  6. `GetActiveCorrelationIds_ReturnsSnapshot_LiveAfterTransitions` (T5 / Hindsight H10) — dispatch 3 Transitions across 3 CorrelationIds; assert `GetActiveCorrelationIds()` enumeration contains all 3; then dispose one subscription and assert the CorrelationId's entry still appears (entries outlive subscriptions).
- [ ] 4.10: Duplicate-detection tests (exactly 4 tests — reduced from 5 after Murat promotion of "cross-CorrelationId idempotency" and "LRU eviction" to FsCheck properties #3 and #12):
  1. `Transition_SameMessageIdSameCorrelation_NoReEmit` — second Confirmed with same MsgId absorbed silently
  2. `Transition_SameMessageIdNewCorrelation_LogsHfc2005_TreatsAsFreshEntry` — Decision D10 revision per T6: no terminal synthesis. Fresh entry, `OutcomeNotifications=0`, HFC2005 log. Behavioural check pairs with FsCheck property #13.
  3. `Transition_DuplicateMessageIdPastGraceWindow_LogsHfc2005WarningAndTreatsAsFresh`
  4. `Transition_MessageIdCacheCap_DeterministicLruBoundary` (**Winston review — deterministic, non-FsCheck**) — insert EXACTLY `MessageIdCacheCapacity + 1` distinct MsgIds sequentially; assert the very-first MsgId was evicted and all later N are retained. Paired with property #12 for concurrent coverage.
- [ ] 4.11: **CUT** (ADR-019 revision 2026-04-16) — no `PruneLoop`, no terminal-grace tests. Scope-lifetime eviction is covered by: (a) `DisposeAsync_ClearsAllState` test under Task 4.9 (#5), (b) the 1-command-lifecycle bUnit test under Task 8.2 which disposes the circuit naturally. **Task 4.11 is intentionally empty.** Net saving: −4 tests.
- [ ] 4.12: Subscription-contract tests (exactly 4 tests — added one for Amelia's AC5 gap "multi-subscriber terminal receipt"):
  1. `Subscribe_AfterTransition_ReplaysCurrentStateImmediately_Once` — new subscriber sees exactly one replay transition followed by any further live transitions
  2. `MultipleSubscribersSameCorrelation_AllReceiveTransitions_SetEquality` — two subscribers, three transitions; both callback lists contain the same three transitions. Assert **set-equality, not list-equality** (Murat — CI thread scheduling makes strict ordering flake). Per-subscriber in-order assertion lives in FsCheck property #7.
  3. `Unsubscribe_StopsReceivingTransitions` — `IDisposable.Dispose()` on the subscription removes observer from list; subsequent transitions do not invoke the callback
  4. **`MultipleSubscribers_EachReceiveTerminalExactlyOnce` (Amelia AC5 gap)** — three subscribers on the same CorrelationId, one terminal transition dispatched, assert each subscriber's callback invoked EXACTLY once with the terminal transition (fills the AC5 property #4 gap where the property tests SERVICE state counters not OBSERVER receipts).

### Task 5: Per-Command Lifecycle Bridge Emitter (AC: 2, 3, 4)

- [ ] 5.1: Create `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandLifecycleBridgeEmitter.cs`. Emit `{CommandTypeName}LifecycleBridge.g.cs` per `[Command]`. Template tokens reference `CommandFluxorModel.ActionsWrapperName` (the existing `{Namespace}.{CommandTypeName}Actions` fully-qualified wrapper type computed by `CommandFluxorTransform`). Template (netstandard2.0-safe — generator strings only; substitute `{ActionsWrapperName}` with `model.ActionsWrapperName` at emit time):
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
- [ ] 5.2: Modify `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` — inside the `[Command]`-attribute pipeline (the second `ForAttributeWithMetadataName` pipeline added in Story 2-1 Task 6), add a call to `CommandLifecycleBridgeEmitter.Emit(commandModel)` and register the hint `{CommandTypeName}.LifecycleBridge.g.cs` (note: hint uses dot-separated per Story 2-2 collision pattern — NOT the filename with literal dots).
- [ ] 5.3: Modify `CommandFormEmitter.EmitSubmitMethod` to call `LifecycleBridgeRegistry.Ensure<{CommandTypeName}LifecycleBridge>()` BEFORE the existing `LastUsedSubscriberRegistry.Ensure<...>()` call on line 251. This makes the bridge lazy-activated on first submit (Decision D5). **Injection shape — verify at implementation time (Amelia review 2026-04-16):** the story originally claimed `LastUsedSubscriberRegistry` is resolved via a `[Inject]` field; inspect the current `CommandFormEmitter.EmitClassHeader` / field emission pass to confirm whether the 2-2 pattern uses `[Inject]` property, `[Inject]` field, or `GetService` in-method. Match the existing pattern for `LifecycleBridgeRegistry` — do NOT blindly add `[Inject] private LifecycleBridgeRegistry BridgeRegistry { get; set; } = default!;` without confirming the 2-2 convention. If 2-2 uses `GetService`, use `GetService` here too.
- [ ] 5.4: Bridge emitter tests (exactly 3 tests):
  1. `Emit_CommandWithStandardActions_ProducesBridgeClassWithSixSubscriptions`
  2. `Emit_CommandNamespace_MatchesCommand` — emitted bridge in same namespace as the command
  3. `Emit_DeterministicOutput_RunningTwiceProducesIdenticalSource` — incremental-cache invariant
- [ ] 5.6: **HFC1017 generic-command rejection (Hindsight H9).** In `CommandParser` (already modified by Story 2-2 Task 1), add a parse-time check: if the `[Command]`-annotated type has `Arity > 0` (generic type parameters), report HFC1017 "Command type '{Type}' cannot be generic — specialize or remove the type parameters." Do NOT emit the bridge for generic types. The downstream `CommandFormEmitter` / `CommandFluxorActionsEmitter` / `CommandLifecycleBridgeEmitter` all share this parse-gated pipeline, so rejecting here blocks every emitter uniformly. Add 1 test: `HFC1017_RejectsGenericCommand`. Story 2-2's command sample confirms non-generic commands remain unaffected.

- [ ] 5.5: Bridge emitter snapshot tests (exactly 3 baselines — Murat review 2026-04-16 added the nested-namespace case to prove D16 emitter determinism):
  1. `LifecycleBridgeEmitterTests.Emit_IncrementCommand.verified.txt`
  2. `LifecycleBridgeEmitterTests.Emit_ConfigureCounterCommand.verified.txt` (FullPage density — exercises emission for a command with `[Icon]` attribute to ensure bridge doesn't pick up icon metadata)
  3. `LifecycleBridgeEmitterTests.Emit_NestedNamespace.verified.txt` — a command at `Counter.Domain.Batch.Operations.BulkIncrementCommand` (deeply nested namespace). Proves hint-name generation and `{ActionsWrapperName}` fully-qualified reference work without collision or truncation. Murat: "two vanilla commands prove nothing about edge cases."

### Task 6: Idempotency + Terminal Grace Window (AC: 3, 4)

- [ ] 6.1: Covered by Task 4.10–4.11 tests (5 duplicate-detection + 4 terminal-grace tests). No additional sub-tasks; this is a cross-cutting concern.

### Task 7: Story 2-1 Regression Gate — Marker Interface Application (AC: all)

- [ ] 7.1: Re-run all existing `CommandFluxorActionsEmitter` tests. Expected change: generated action records now have `: global::Hexalith.FrontComposer.Contracts.Lifecycle.ICommandLifecycleAction` after the record parameter list. Tests that snapshot the emitted source need re-approval. Story 2-1 did NOT land `.verified.txt` snapshots for action records (see deferred-work 2026-04-15 entry), so the regression is caught by `CommandFluxorEmitterTests` compile-time parseability checks — these should pass unchanged because adding a marker interface does not break record syntax.
- [ ] 7.2: Re-run all `CommandFormEmitter` tests after the `ResetToIdleAction` signature change (empty → `(string CorrelationId)`). Expected: the two snapshot baselines added in Story 2-2 Session C need re-approval — `CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt` and `CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`. Run `dotnet test`; inspect diff; if only the ResetToIdleAction dispatch site changed (passes correlationId), accept.
- [ ] 7.3: Re-run all `CommandFluxorFeatureEmitter` tests. `OnResetToIdle` reducer signature changed from `(State state, Actions.ResetToIdleAction action)` to same shape — no test change needed since the reducer ignores the new param. If tests break, update to match.
- [ ] 7.4: Regression invariant: **all 229 SourceTools tests + 82 Shell tests + 12 Contracts tests = 323 existing tests continue to pass** (Story 2-2 end state per its Debug Log). If any fail for reasons unrelated to the 3 expected edits above, HALT.

### Task 8: Counter Sample E2E (AC: 1, 2, 3)

> **Scope reduced 2026-04-16 (Winston review):** the original Task 8.2 dev-mode `<FcDiagnosticsPanel>` was scope creep into Story 2-4's domain. Dropped. Task 8 now ships ONLY: (a) an optional debug log in the existing Counter effects, and (b) 3 bUnit e2e tests that prove the service works end-to-end with generated forms.

- [ ] 8.1: **No functional change** to Counter.Web UI per Story 2-4 scope boundary. Counter sample's `CounterProjectionEffects.cs` MAY add a debug-level log reading `ILifecycleStateService.GetState(correlationId)` at `Confirmed` dispatch as a smoke signal. No UI changes. If the log call adds noise in dev runs, drop it — this is optional.
- [ ] 8.2: End-to-end tests (3 tests) — verify lifecycle observability via bUnit:
  1. `CounterPage_IncrementCommandSubmitted_ServiceReachesConfirmed` — submit the 1-field inline popover; assert `GetState(correlationId) == Confirmed` within 2 seconds.
  2. `CounterPage_BatchIncrementSubmitted_SubscribeEmitsFiveTransitions` — inline call `service.Subscribe(correlationId, t => captured.Add(t))` BEFORE submit; count transitions and assert the sequence `[Submitting, Acknowledged, Syncing, Confirmed]` with at most one replay-on-subscribe entry prepended. Use `cut.WaitForAssertion` (Story 2-2 lesson — synchronous Find post-render races).
  3. `CounterPage_Rejected_SubscribeEmitsRejectedAndNoFurtherTransitions` — configure stub to reject via `SimulateRejection=true`; verify subscriber sees Rejected and NO later transitions despite any spurious bridge callbacks.

### Task 9 (Intentionally skipped)

- Reserved for test-project bootstrapping if new test projects are needed. Story 2-3 reuses the existing `Hexalith.FrontComposer.Shell.Tests` and `Hexalith.FrontComposer.SourceTools.Tests` projects — no new project.

### Task 10: Test Fixture Infrastructure (AC: 5)

- [ ] 10.1: Add `TestUlidFactory` in `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/` that emits deterministic ULIDs from a seed — usage example: `new TestUlidFactory(seed: 0)` yields a predictable sequence. Use the NUlid deterministic seed overload `Ulid.NewUlid(DateTimeOffset, byte[])` with incrementing byte arrays.
- [ ] 10.2: **CUT** (ADR-019 revision 2026-04-16) — no `FakeTimeProvider` dependency needed. After cutting `PruneLoop` (T4), there's no time-dependent code in `LifecycleStateService` to test deterministically. `TimeProvider` injection remains in the ctor for forward-compat with Epic 5's durable lookup but unused in v0.1. No package add to `Directory.Packages.props`. **Task 10.2 is intentionally empty.**

### Task 11: FsCheck State Machine Property Tests (AC: 5)

- [ ] 11.1: Add `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/LifecycleStateMachinePropertyTests.cs`.
- [ ] 11.2: Define an FsCheck generator `Arbitrary<LifecycleOperation>` that produces one of: `(correlationId, state, messageId?)` weighted towards valid sequences but includes 10% invalid / out-of-order ops.
- [ ] 11.3: **15 property tests** (each is a `[Property(MaxTest=1000)]` call — running 1000 iterations per property; the three stress/concurrency properties + the re-entrant property use `MaxTest=100` CI / `1000` nightly per architecture.md §1419):
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
- [ ] 11.4: On shrink failure, dump counter-example to `tests/Hexalith.FrontComposer.Shell.Tests/Snapshots/Lifecycle/FsCheckCounterExample_{timestamp}.txt`. Commit to git on CI failure (not pre-emptively).
- [ ] 11.5: Add `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CommandLifecycleBridgeIntegrationTest.cs`:
  - Compile a small synthetic command via `CSharpGeneratorDriver`
  - Assert the emitted bridge source contains exactly 6 `SubscribeToAction<...>` calls
  - Assert none of the 6 subscriptions is a no-op (each has a `.Transition(...)` body)
  - Asserts bridge emitter is structurally correct without having to run bUnit
- [ ] 11.6: Add DI/registration tests (exactly 3):
  1. `AddHexalithFrontComposer_RegistersILifecycleStateService_Scoped`
  2. `AddHexalithFrontComposer_RegistersIUlidFactory_Singleton`
  3. `AddHexalithFrontComposer_RegistersLifecycleBridgeRegistry_Scoped`
- [ ] 11.7: Add **non-persistence invariant** test: `LifecycleStateService_DoesNotWriteToIStorageService` — construct with an `IStorageService` that throws on any Set/GetAsync call, run a full Submitted → Confirmed sequence, assert no throws (service never touched storage).

### Task 12: Axe-core / Accessibility (AC: none — no UI scope)

- [ ] 12.1: **Not applicable.** Story 2-3 does NOT add UI chrome. The `<FcDiagnosticsPanel>` extension in Task 8.2 inherits Story 2-2's axe-compliance (already clean). No new axe-core scans needed.

### Task 13: Automated End-to-End Verification (AC: all)

- [ ] 13.1: Test count rollup check: **~46 new tests** (D17 budget, revised 2026-04-16 after advanced-elicitation T1-T20 apply). CI gate: `dotnet test --list-tests | grep -c "Lifecycle"` ≥ 46. Distribution: 3.6 (5, +1 ULID entropy) + 4.9 (6, +1 GetActiveCorrelationIds) + 4.10 (4) + 4.11 (0, cut per T4) + 4.12 (4) + 5.4 (3) + 5.5 (3) + 5.6 (1, HFC1017 generic) + 8.2 (3) + 11.3 (15, +2 scope-dedup + re-entrant) + 11.5 (1) + 11.6 (3) + 11.7 (1) = 5 + 6 + 4 + 0 + 4 + 3 + 3 + 1 + 3 + 15 + 1 + 3 + 1 = **49**. (3 slack — the elicitation net added properties and a generic-rejection test while the PruneLoop cut dropped 4 grace-window tests.)
- [ ] 13.2: Regression invariant: **existing 323 tests continue to pass** (Task 7.4). Full solution build: `dotnet build -c Release -p:TreatWarningsAsErrors=true` → 0 warnings.
- [ ] 13.3: Automated E2E via Aspire MCP + Claude browser (per user memory preference `feedback_no_manual_validation`):
  - Scenario 1: Increment command → observe Submitting → Acknowledged → Syncing → Confirmed via `<FcDiagnosticsPanel>` reads
  - Scenario 2: Configure command rejection (temporarily configure stub `SimulateRejection=true`) → observe Submitted → Rejected, `IdempotencyResolved=false`
  - Scenario 3: Rapid double-click (race between submit disable and click) → assert only one CorrelationId enters Confirmed, no second outcome
  - Emit `2-3-e2e-results.json` with machine-readable predicates per scenario. Store under `_bmad-output/test-artifacts/`.

---

## Dev Notes

### Service Binding Reference (Task 4.3 registration)

```csharp
// Added to AddHexalithFrontComposer()
services.TryAddSingleton<IUlidFactory, UlidFactory>();
services.TryAddScoped<ILifecycleStateService, LifecycleStateService>();
services.TryAddScoped<LifecycleBridgeRegistry>();
```

### Files Touched Summary

**Contracts/Lifecycle/** (new):
- `ILifecycleStateService.cs`
- `CommandLifecycleTransition.cs`
- `IUlidFactory.cs`
- `LifecycleOptions.cs`
- *(cut per T1: `ICommandLifecycleAction.cs`. cut per T2: `ConnectionState.cs`.)*

**Shell/Services/Lifecycle/** (new):
- `LifecycleStateService.cs`
- `LifecycleBridgeRegistry.cs`
- `UlidFactory.cs`

**SourceTools/Emitters/** (new):
- `CommandLifecycleBridgeEmitter.cs`

**SourceTools/Emitters/** (modified):
- `CommandFluxorActionsEmitter.cs` — `ResetToIdleAction` gains `CorrelationId` parameter (marker interface application cut per T1)
- `CommandFormEmitter.cs` — inject `LifecycleBridgeRegistry`, call `Ensure<T>()` before first submit; `ResetToIdleAction` dispatch passes `_submittedCorrelationId`
- `CommandParser.cs` — reject generic `[Command]` types with HFC1017 (Hindsight H9 per T14)
- (No change needed to `CommandFluxorFeatureEmitter.cs`; the OnResetToIdle reducer already ignores any new param)

**Shell/Services/** (modified):
- `StubCommandService.cs` — inject `IUlidFactory`, replace Guid with ULID for MessageId
- `DerivedValues/SystemValueProvider.cs` — inject `IUlidFactory`, `"MessageId"` branch uses it

**Shell/Extensions/** (modified):
- `ServiceCollectionExtensions.cs` — new registrations + extend `AddHexalithDomain<T>()` to auto-register `*LifecycleBridge` types

**SourceTools/Diagnostics/** (modified):
- `DiagnosticDescriptors.cs` — reserve HFC1016; HFC2004/2005/2006 are runtime-only (logged, not emitted as diagnostics from the generator)

**SourceTools/** (modified):
- `FrontComposerGenerator.cs` — wire bridge emitter into the `[Command]` pipeline
- `AnalyzerReleases.Unshipped.md` — HFC1016 reservation (HFC2004/5/6 are runtime log codes, NOT analyzer-emitted — do NOT add to AnalyzerReleases)

**Directory.Packages.props** (modified):
- Add `NUlid 1.7.4`

**samples/Counter/Counter.Web/** (modified):
- `CounterProjectionEffects.cs` — optional debug log reading `ILifecycleStateService.GetState(correlationId)` at Confirmed dispatch (Task 8.1)
- `Program.cs` — no explicit edit typically required: `AddHexalithFrontComposer()` registers `IUlidFactory` automatically via Task 4.3. If Counter.Web constructs `StubCommandService` directly anywhere (inspect during dev), add explicit `IUlidFactory` passing. Flag here in case dev discovers such a site.

**tests/Hexalith.FrontComposer.Shell.Tests/** (new + modified):
- `Services/Lifecycle/UlidFactoryTests.cs` (new, 4 tests)
- `Services/Lifecycle/LifecycleStateServiceTests.cs` (new, 5+5+4+3 = 17 tests)
- `Services/Lifecycle/LifecycleStateMachinePropertyTests.cs` (new, 10 property tests)
- *(cut per T4 2026-04-16: `Services/Lifecycle/FakeTimeProvider.cs` — no time-dependent code to test)*
- `Services/Lifecycle/TestUlidFactory.cs` (new test helper)
- `Services/Lifecycle/LifecycleRegistrationTests.cs` (new, 3 DI tests)
- `Services/Lifecycle/LifecycleNoPersistenceTests.cs` (new, 1 test)
- `Components/CounterPageLifecycleE2ETests.cs` (new, 3 e2e tests)
- `Services/StubCommandServiceTests.cs` (modified — update constructor calls)

**tests/Hexalith.FrontComposer.SourceTools.Tests/** (new + modified):
- `Emitters/CommandLifecycleBridgeEmitterTests.cs` (new, 3+2 = 5 tests + 2 baselines)
- `Integration/CommandLifecycleBridgeIntegrationTest.cs` (new, 1 test)
- `Snapshots/LifecycleBridgeEmitterTests.Emit_IncrementCommand.verified.txt` (new baseline)
- `Snapshots/LifecycleBridgeEmitterTests.Emit_ConfigureCounterCommand.verified.txt` (new baseline)

### Naming Convention Reference

| Element | Pattern | Example |
|---|---|---|
| Generated lifecycle bridge | `{CommandTypeName}LifecycleBridge.g.cs` (Decision D16) | `IncrementCommandLifecycleBridge.g.cs` |
| Generator hint | `{QualifiedHintPrefix}.LifecycleBridge.g.cs` | `Counter.Domain.IncrementCommand.LifecycleBridge.g.cs` |
| Service implementation | `LifecycleStateService` | — |
| Registry | `LifecycleBridgeRegistry` (mirrors `LastUsedSubscriberRegistry`) | — |
| ULID factory | `UlidFactory` / `IUlidFactory` | — |
| Transition record | `CommandLifecycleTransition` | — |
| Action record (existing, only `ResetToIdleAction` extended) | `{CommandName}Actions.{Verb}Action` carrying `string CorrelationId` | `IncrementCommandActions.ConfirmedAction` |

### Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2, FsCheck.Xunit.v3 (inherited from 2-1/2-2)
- Property tests: 1000 iterations in CI, 10,000 in nightly (per architecture.md §1419)
- `TestContext.Current.CancellationToken` on all `RunGenerators`/`GetDiagnostics`/`ParseText` (xUnit1051)
- `TreatWarningsAsErrors=true` global
- `DiffEngine_Disabled: true` in CI
- **Test count budget (D17):** 42 new tests (11 service + 10 FsCheck + 5 bridge emitter + 4 ULID factory + 3 e2e + 3 DI + others). Cumulative target: ~505 tests.
- **Property test determinism:** each `[Property]` attribute uses a pinned seed (QuietOnSuccess = true; Seed is `Guid("D5F7-...")` derived from property name hash) so a shrunk case can be reproduced

### Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build` (inherited pattern)
- `AnalyzerReleases.Unshipped.md` update for HFC1016 only (2004/5/6 are runtime log codes per architecture.md §648 — code range 2000-2999 is assigned to Shell, and Shell-range diagnostics are runtime logs, not analyzer-emitted; skipping AnalyzerReleases entry is correct — see HFC2001 precedent which is also a runtime diagnostic)
- Roslyn 4.12.0 pinned (inherited)
- `NUlid 1.7.4` added to Directory.Packages.props; referenced from Shell only (Contracts stays dependency-free per architecture.md §1144)

### Previous Story Intelligence

**From Story 2-2 (immediate predecessor):**

- **Patterns that worked:** Hand-written service + per-command emitted subscriber (L05 — `LastUsedValueProvider` + `{Command}LastUsedSubscriber.g.cs`). **Story 2-3 reuses verbatim for the bridge.**
- **Lazy subscriber activation (D35 of 2-2):** idempotent `LastUsedSubscriberRegistry.Ensure<T>()` called at form submit time — reused as `LifecycleBridgeRegistry.Ensure<T>()`.
- **FsCheck property-based testing:** Story 2-2 Task 3.9 used FsCheck for storage-key roundtrip; Story 2-3 extends to state machine. FsCheck.Xunit.v3 package already pinned (inherited).
- **CorrelationId-keyed dict with bounded eviction (D38 of 2-2):** `ConcurrentDictionary<CorrelationId, PendingEntry>` with TTL+MaxInFlight — Story 2-3 reuses the pattern for `_entries` (TTL=5min grace, no MaxInFlight because lifecycle state is user-bounded by circuit scope).
- **Tenant/user fail-closed (D31 of 2-2):** applied to persisted services only; lifecycle state is ephemeral, so the D13 "not at this layer" decision is deliberate (L03 caveat).
- **Snapshot test re-approval pattern (Task 5.3 of 2-2):** regression-gate byte-identical assertion. Story 2-3 applies to `ResetToIdleAction` signature change and marker interface application.

**From Story 2-1:**

- `CommandFluxorActionsEmitter` emits 6 action records — don't break their existing shape, just add marker interface.
- `CommandFormEmitter.EmitSubmitMethod` dispatches 6 actions — augment the `ResetToIdleAction` dispatch with the newly-required CorrelationId parameter.
- Fluxor `IActionSubscriber.SubscribeToAction<TAction>` is the subscription primitive — same one used by Story 2-2's LastUsedSubscriber.
- Stub command service tests (7) use zero delays — Story 2-3 stub changes add `IUlidFactory` constructor param; update stub tests to pass `TestUlidFactory`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.3 — AC source of truth]
- [Source: _bmad-output/planning-artifacts/epics.md#FR23, FR30, FR36 — functional requirements]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR12 — ILifecycleStateService requirements]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR44, NFR45, NFR47 — reliability non-functionals]
- [Source: _bmad-output/planning-artifacts/prd.md#FR23, FR30, FR36 — functional requirements text]
- [Source: _bmad-output/planning-artifacts/architecture.md#141-143 — ULID message IDs + ETag caching]
- [Source: _bmad-output/planning-artifacts/architecture.md#397 — Decision D2 Fluxor lifecycle (Fluxor CommandLifecycleState + FrontComposerLifecycleWrapper)]
- [Source: _bmad-output/planning-artifacts/architecture.md#461-464 — CommandLifecycleState feature per command type; Actions SubmittedAcknowledgedConfirmedRejected; reducer handling state transitions; effect wiring to ProjectionSubscriptionService]
- [Source: _bmad-output/planning-artifacts/architecture.md#536 — CommandLifecycleState is ephemeral; evicted on terminal state; NOT persisted to IStorageService]
- [Source: _bmad-output/planning-artifacts/architecture.md#574 — CorrelationId mismatch in lifecycle: ULID-based correlation from command MessageId; unit test asserting 1:1]
- [Source: _bmad-output/planning-artifacts/architecture.md#589 — CommandLifecycleState + wrapper (D2) implementation sequence]
- [Source: _bmad-output/planning-artifacts/architecture.md#648 — HFC diagnostic ID ranges]
- [Source: _bmad-output/planning-artifacts/architecture.md#741 — Fluxor action payload rule: always include CorrelationId]
- [Source: _bmad-output/planning-artifacts/architecture.md#756 — Structured logging convention (OpenTelemetry, include CommandType + TenantId + CorrelationId)]
- [Source: _bmad-output/planning-artifacts/architecture.md#1144 — Contracts must not reference other packages (dependency-free)]
- [Source: _bmad-output/planning-artifacts/architecture.md#1419 — FsCheck conventions (1000 CI / 10000 nightly)]
- [Source: _bmad-output/planning-artifacts/architecture.md#1436 — Idempotent handling via ULID message IDs with deterministic duplicate detection]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#ADR-010 — ICommandServiceWithLifecycle callback contract]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#Task 4 — Command Fluxor (6 action records + reducer with CorrelationId guard)]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes.md#Decision D28 — hand-written generic service + emitted per-command typed subscriber]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes.md#Decision D35 — idempotent + lazy subscriber registry]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes.md#Decision D38 — ConcurrentDictionary<CorrelationId, PendingEntry> with bounded eviction]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md — known deferrals (IdempotentConfirmed enum, HFC1008 analyzer)]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03 — tenant/user isolation fail-closed]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L04 — generated name collision detection]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L05 — hand-written service + emitted per-type wiring]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L06 — defense-in-depth budget ≤25 decisions for feature story]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L07 — test count matrix scoring]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L09 — ADR rejected-alternatives discipline]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L10 — deferrals name story not epic]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L11 — cheat sheet for large stories]
- [Source: memory/feedback_no_manual_validation.md — automated E2E preference]
- [Source: memory/feedback_cross_story_contracts.md — explicit cross-story contracts (ADR-017 mirrors ADR-016)]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — inherited context for D13 scoping decision]
- [Source: memory/feedback_defense_budget.md — ≤25 binding decisions (we're at 18 ✓)]
- [Source: NUlid 1.7.4 — https://github.com/RobThree/NUlid (MIT license; stable since 2019)]

### Project Structure Notes

- Alignment with architecture blueprint (architecture.md §852):
  - New `Shell/Services/Lifecycle/` folder — consistent with existing `Shell/Services/DerivedValues/` grouping (one service family per folder)
  - New `Contracts/Lifecycle/` additions — existing folder (`CommandLifecycleState.cs`, `ICommandLifecycleTracker.cs`) extended, no new folder
  - NUlid package in Shell only — preserves Contracts dependency-free invariant (architecture.md §1144)
  - No new test project — existing `Hexalith.FrontComposer.Shell.Tests` + `Hexalith.FrontComposer.SourceTools.Tests` absorb new tests
  - No new sample project — Counter.Web gets a dev-mode diagnostic panel in-place
- Detected conflicts or variances:
  - **`ResetToIdleAction` signature change** — Story 2-1 emitted `ResetToIdleAction()` (parameterless); Story 2-3 extends to `ResetToIdleAction(string CorrelationId)`. Blast radius: form emitter dispatch site + 1 reducer + 7 existing tests that construct the action. Each can be updated mechanically. Documented as the expected delta in Task 7.
  - **`StubCommandService` constructor expansion** — adds required `IUlidFactory` param. No external adopter uses `StubCommandService` directly; internal update only. 7 existing tests updated (Task 3.4).
  - **`ICommandLifecycleTracker` coexistence** — the existing provisional `ICommandLifecycleTracker` interface (`Contracts/Lifecycle/`) remains but is now superseded by `ILifecycleStateService` for all new code. Removing `ICommandLifecycleTracker` outright is out of scope for Story 2-3 (no consumer depends on it in-repo; it is a published public API in Contracts so removal requires a SemVer-major bump). Add a Known Gaps entry: "Remove `ICommandLifecycleTracker` — superseded by `ILifecycleStateService` — Story 9-x (deprecation cycle)."

---

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context) — `claude-opus-4-6[1m]`

### Debug Log References

### Completion Notes List

Ultimate context engine analysis completed — comprehensive developer guide created.

### File List

### Change Log

| Date | Change | Reason |
|------|--------|--------|
| 2026-04-16 | Story created via `/bmad-create-story 2-3` | Epic 2 continuation |
| 2026-04-16 | Party-mode review applied (Winston + Amelia + Murat + Sally). 18 decisions → 20 (added D19 HFC2007 single-writer diagnostic, D20 singleton-resolve guard). 42 tests → 47 (added 3 concurrency FsCheck properties, 1 multi-subscriber behavioural for AC5 observer-receipt gap, 1 nested-namespace snapshot; promoted 2 behavioural to properties). Contract change: `Observe() IObservable<T>` → `Subscribe(correlationId, Action<T>) IDisposable` (prevents Rx drift; no-brainer fix-now-vs-break-2-4). Grace window 5 min → 60 s via `LifecycleOptions`. `CommandLifecycleTransition` gained `LastTransitionAt` + `ResolvedByActor`. `PeriodicTimer` dispose order corrected. Added `ConcurrentDictionary<string, byte>` + `ImmutableInterlocked` for thread-safe LRU / observer list / OutcomeNotifications. Swapped hand-rolled FakeTimeProvider → `Microsoft.Extensions.TimeProvider.Testing`. Dropped Task 8.2 dev-mode panel (scope creep into 2-4). Documented `Subscribe()` circuit-locality in ADR-017 + Known Gaps. | Multi-agent review caught: (a) IObservable leaky-abstraction trap, (b) FR30 race on `++` increment, (c) grace window over-provisioned vs 2-4 UX need, (d) missing `ResolvedByActor` for "already done by another user" UX, (e) missing `LastTransitionAt` for 2-4 reconnect-timer honesty, (f) HashSet not thread-safe, (g) single-writer invariant was convention not diagnostic, (h) Singleton mis-registration foot-gun. All items are cheap-now / expensive-post-2-4. |
| 2026-04-16 (later) | Advanced-elicitation T1-T20 applied (Pre-mortem + Red Team + Chaos Monkey + Occam's Razor + Hindsight). Net: 20 decisions stays 20 (Occam didn't cut any D but reshaped D9/D10/D15/D18 significantly). **Cuts:** T1 `ICommandLifecycleAction` marker interface (no consumer), T2 `ConnectionState` enum + property + event (dead stub seam — Story 5-3 redesigns), T3 `ResolvedByActor` field (no v0.1 producer), T4 `PeriodicTimer` + `PruneLoopAsync` + grace window (scope-lifetime eviction replaces), T6 D10 cross-CorrelationId terminal synthesis (Pre-mortem PM2 capability-token leak). **Adds:** T5 `GetActiveCorrelationIds()` debug surface, T7 binding consumer contract for Story 2-4 in D19, T8 `Volatile` disposed-check in subscription invocation (R6 race), T9 try/catch-rollback in `LifecycleBridgeRegistry.Ensure` (CM1), T10 `UlidFactory` NUlid→Guid fallback on `CryptographicException` (CM2), T11 "invoke callbacks outside entry lock" rule + FsCheck property #15 re-entrant-no-deadlock, T13 defensive `AddOptions<LifecycleOptions>`, T14 HFC1017 generic-command rejection (H9), T15 MessageId cache DoS Known Gap, T16 ULID entropy FsCheck test (R4), T17 CAS-storm docs in ADR-018, T18 sync Dispose no-longer-blocks (T4 side-effect), T19 multi-assembly Known Gap, T20 FsCheck properties #14 (scope-dedup) + #15 (re-entrant). Dropped `Microsoft.Extensions.TimeProvider.Testing` add (T4 made it unnecessary). Dropped `FakeTimeProvider` hand-roll (same). Final: 20 decisions, 3 ADRs, 4 Contracts types (was 6), 6 diagnostics reserved, ~46 new tests. | Advanced elicitation is L08 complementary to party-mode — it caught: (a) capability-token leak in cross-CorrelationId synthesis (PM2), (b) `PruneLoop` complexity was over-engineered vs scope-lifetime (H3), (c) `ResolvedByActor` shipped without producer (PM4), (d) `ConnectionState` stub would be redesigned post-5-3 (H2), (e) marker interface shipped without consumer (H1), (f) NUlid can throw under exotic crypto policy (CM2), (g) subscription-disposal race (R6), (h) generic-command hint-name collision in Epic 8 (H9). Every applied item was cheap now, expensive-or-impossible post-2-4/Epic-5/Epic-8. |
