# Dev Agent Cheat Sheet (Read First — 2 pages)

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
