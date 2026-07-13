---
baseline_commit: e914c615ea395b469c6ca3fd53dafdad159e559e
---
# Story 11.15: Storage scope and snapshot publisher consolidation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Type: refactor (Epic 11 lower-risk consolidation group — closes architecture-review finding M19 subset). -->
<!-- Companion spec: _bmad-output/implementation-artifacts/spec-11-15-storage-scope-and-snapshot-publisher-consolidation.md
     was authored `status: blocked` on two intent-gap decisions. BOTH are now RESOLVED below (see Dev Notes ▸ Scope Decisions);
     a dev/spec flow may lift the spec's `blocked` state using these recorded decisions. -->

## Story

As a FrontComposer maintainer,
I want the six duplicated storage scope-resolution helpers and the two duplicated replayable snapshot-publisher containers consolidated into one shared resolver and one shared publisher primitive,
so that tenant/user fail-closed hardening and subscription/replay/disposal behavior are defined once and applied uniformly instead of drifting across copies.

## Acceptance Criteria

Refines FR13; closes the M19 duplication subset for clusters 4 (scope resolver) and 5 (snapshot pub/sub). Scope is deliberately bounded per the two decisions in **Dev Notes ▸ Scope Decisions** — do not broaden.

1. **(M19 cluster 4 — scope resolver)** Given the six duplicated effect-local `TryResolveScope` implementations, when storage scope resolution is consolidated, then all six Shell persisted-feature effects resolve tenant/user through a single Scoped `Services/StorageScopeResolver`, the resolver fails closed uniformly for missing/blank/**non-fatally throwing** accessors (adopting the strongest existing guard), and every existing successful storage key and hydrate/persist behavior remains **byte-for-byte** unchanged. Tenant/user fail-closed behavior is covered by direct resolver tests.

2. **(M19 cluster 5 — snapshot pub/sub)** Given the two hand-rolled replayable snapshot containers (`ProjectionConnectionStateService`, `ReconnectionReconciliationStateService`), when `SnapshotPublisher<T>` (or an approved equivalent) is introduced, then subscription, current/replay ordering, idempotent unsubscribe, per-subscriber non-fatal fault isolation, and disposal are covered once by the primitive and reused by both former duplicate sites, while each owner **retains its distinct** epoch/dedup/reconnect/logging/telemetry semantics and its unchanged public interface.

3. **(evidence + anti-recurrence)** Given the duplicated call sites are removed, when story validation and governance run, then the before/after call-site reduction is documented in the story File List / Change Log (baseline `6 effect-local resolver definitions / 16 invocations` → `0 effect-local definitions / 16 shared invocations`; `2 hand-rolled replayable snapshot containers` → `1 shared primitive + 2 thin owners`), a **durable source guard** fails if an effect-local `TryResolveScope` is reintroduced under `State/**`, and the explicitly-excluded near-variants and other notification idioms (with the deferred M19/Rx-removal follow-up) are recorded.

## Tasks / Subtasks

- [x] **Task 1 — Consolidate the six scope resolvers into one Scoped `StorageScopeResolver` (AC: #1)**
  - [x] Add `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs` — **`internal sealed`** class + `internal interface IStorageScopeResolver` (namespace Hexalith.FrontComposer.Shell.Services; keep internal to avoid public-API growth — see Dev Notes ▸ Previous-story intelligence). Signature mirrors today's `private bool TryResolveScope(out string tenantId, out string userId, string direction)`.
  - [x] Adopt **`CommandPaletteEffects` as the canonical fail-closed body** (`State/CommandPalette/CommandPaletteEffects.cs:956-991`): resolve the accessor lazily/null-tolerantly, wrap the accessor property reads in `try/catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)`, and on a throwing getter log `HFC2105` with `Reason=AccessorThrew` plus a bounded exception-type category. Do **not** attach the exception object because adopter exception messages/stacks can contain tenant or user data. This **fixes the latent throw-propagation bug in the other five** resolvers (they read `accessor.TenantId`/`UserId` on a non-null field with no try/catch).
  - [x] Preserve the exact fail-closed contract: on missing/blank/whitespace tenant OR user → set both `out` to `string.Empty`, return `false`, log `FcDiagnosticIds.HFC2105_StoragePersistenceSkipped` at **Information** with the `direction` placeholder; **never log raw tenant/user values** (PII). On success → assign **raw** (un-escaped) identities and return `true`.
  - [x] Register `TryAddScoped<IStorageScopeResolver, StorageScopeResolver>()` in `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` (co-locate with the other scoped storage services). Add the parity registration in Extensions/EventStoreServiceExtensions.cs only if resolution there requires it (mirror the existing `IUserContextAccessor` dual-registration pattern).
  - [x] Migrate the six effect classes to inject/consume the resolver and **remove all six local `TryResolveScope` definitions**:
    - `State/Theme/ThemeEffects.cs` (def `132-148`; invocations `66,117`)
    - `State/Density/DensityEffects.cs` (def `215-231`; invocations `90,196`)
    - `State/Navigation/NavigationEffects.cs` (def `376-392`; invocations `151` [`out _, out _` — preserve the discard], `201,344`)
    - `State/DataGridNavigation/DataGridNavigationEffects.cs` (def `540-556`; invocations `153,188` [mid-method re-resolve into existing vars — preserve], `238,278,327`; **preserve DataGrid's pre/post-debounce re-resolution**)
    - `State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs` (def `236-252`; invocations `132,166`)
    - `State/CommandPalette/CommandPaletteEffects.cs` (def `956-991`; invocations `163,622`)
  - [x] Keep each effect's **feature-specific** fallback/no-scope continuation and its own `direction` argument style intact (Theme/Density/CapabilityDiscovery/CommandPalette use `DirectionHydrate`/`DirectionPersist` consts; Navigation/DataGridNavigation use `"hydrate"`/`"persist"` literals — pass the feature/direction through the resolver call, do not normalize the string shape unless behavior is byte-identical).
  - [x] Continue passing **raw** identities into StorageKeys.BuildKey / FrontComposerStorageKey.Build (canonicalization stays centralized there — do NOT pre-escape). Do not add any storage write site (NFR17 tripwire; spec Never-list).
  - [x] **Do not** collapse the distinction between "scope missing" (logged by the resolver) and "storage threw" (logged by each effect's own persist/hydrate `catch` block, which also uses HFC2105, e.g. `DataGridNavigationEffects.cs:208/253/347/377/403`, `CommandPaletteEffects.cs:656/671`). The resolver owns only the former.
  - [x] **Out of scope (documented exclusions):** `Services/DerivedValues/LastUsedValueProvider.cs` (`TryResolveTenantAndUser` def `122-144`, D31 one-per-circuit `TenantGuardTripped` + `IDiagnosticSink` + Warning) and `State/Navigation/ScopeReadinessGate.cs` (`EvaluateAsync` inline check `61-84`, Interlocked exactly-once transition dispatch). Leave both untouched; record the rationale in Change Log.

- [x] **Task 2 — Introduce `SnapshotPublisher<T>` and reuse it in the two replayable twins (AC: #2)**
  - [x] Add `src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs` — **`internal sealed`** generic primitive capturing the shared shape (`object _sync` lock + subscription collection + `T Current` + `Subscribe(Action<T> handler, bool replay = true)` that replays `Current` under the lock + atomic `TryApply`/`ReadCurrent` owner seams + post-transition `Deliver` fan-out). Per-subscription locking makes unsubscribe idempotent and excludes already-captured callbacks after successful disposal; per-handler fault isolation catches all except `OutOfMemoryException` and `StackOverflowException`. Do NOT bake owner-specific transition rules into the primitive.
  - [x] Refactor `State/ProjectionConnection/ProjectionConnectionState.cs` (`ProjectionConnectionStateService`, `49-304`) to delegate its handler-list/replay/`InvokeSafe`/`Subscription` mechanics to `SnapshotPublisher<ProjectionConnectionSnapshot>`, while the owner **retains**: the `_logBuckets` log-dedup (`:59`), `IDisposable`/`IAsyncDisposable` teardown (`231-269`), reconnect and sensitive-logging policy, and the `Subscribe(handler, replay=true)` default. Interface `IProjectionConnectionState`, the `ProjectionConnectionSnapshot` record, and both Scoped registrations (`ServiceCollectionExtensions.cs:358`, `EventStoreServiceExtensions.cs:78`) stay unchanged.
  - [x] Refactor `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationState.cs` (`ReconnectionReconciliationStateService`) the same way, while the owner **retains**: epoch/`IsLogicalDuplicate` staleness gating, atomic current/replay ordering, and the fact that the service class itself does **not** implement `IDisposable` (do not add one). Interface `IReconnectionReconciliationState`, the snapshot record, and its Scoped registration in `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` stay unchanged.
  - [x] Preserve atomic current-then-replay ordering: a replay must never deliver a snapshot staler than a concurrently-applied fresher one (I/O matrix row 3).
  - [x] **Out of scope (documented exclusions, do NOT migrate):** `Badges/BadgeCountService.cs` (Rx `Subject<T>`, hot/no-replay — see the deferred M19/Rx follow-up), `Services/Lifecycle/LifecycleStateService.cs` (correlation-**keyed**, interface frozen in **Contracts**, 5-state-machine/LRU/D20 invariants), `Services/Feedback/CommandFeedbackPublisher.cs` (hot/no-replay), and the six plain `.NET event` publishers (ProjectionChangeNotifier, PendingCommandStateService, DevModeOverlayController, LifecycleThresholdTimer, FcContentLabelCoordinator, FcPageLayoutCoordinator).

- [x] **Task 3 — Guards, tests, and evidence (AC: #3)**
  - [x] Add a **durable source guard** proving no effect-local `TryResolveScope` recurs: extend `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs` (Roslyn-based; `[Trait("Category","Governance")]`) modeled on `ConcretePollingWorkers_HaveOneDeclaration_AtExactInfrastructurePath` — collect `MethodDeclarationSyntax` and local-function declarations named `TryResolveScope` under `State/**` and assert **0**, and assert a single resolver declaration at `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs`. Include synthetic forbidden method and local-function cases proving the guard can fail. (Use the existing `LoadSources`/`FindClassDeclarations` helpers.)
  - [x] Add `SnapshotPublisher<T>` primitive matrix tests in `tests/Hexalith.FrontComposer.Shell.Tests/Services/SnapshotPublisherTests.cs`: replay-enabled vs disabled around a concurrent publish (current coherent, replay never stale), idempotent unsubscribe (dispose once/twice, including near a publish → no later callbacks), and per-subscriber non-fatal fault isolation (one throwing subscriber does not stop healthy subscribers).
  - [x] Add direct `StorageScopeResolver` tests in `tests/Hexalith.FrontComposer.Shell.Tests/Services/StorageScopeResolverTests.cs`: valid scope → raw identities + `true`; missing accessor → fail-closed + HFC2105; blank/whitespace tenant or user → fail-closed + HFC2105; **throwing accessor getter → fail-closed + HFC2105 `Reason=AccessorThrew`** (the new hardening) — never logs raw values.
  - [x] Re-point or retain the six `*EffectsScopeTests.cs` fail-closed suites (`State/{Theme,Density,Navigation,DataGridNavigation,CapabilityDiscovery,CommandPalette}/*EffectsScopeTests.cs`) so they still assert persistence-skipped + HFC2105 through the new resolver seam; keep ScopeReadinessGateTests.cs / NavigationReducersStorageReadyTests.cs untouched (near-variant excluded).
  - [x] Add Scoped-lifetime pins for `IStorageScopeResolver` and `SnapshotPublisher<T>` owners using the existing IStorageServiceLifetimeTests.cs pattern (two `CreateScope()` → `ShouldNotBeSameAs`; same-scope → `ShouldBeSameAs`; build with `ValidateScopes = true`).
  - [x] Keep the existing `ProjectionConnection*`/`ReconnectionReconciliation*` suites green (ProjectionConnectionTelemetryTests.cs, ReconnectionReconciliationCoordinatorTests.cs, ReconciliationSweepReducersTests.cs, ReconnectReconcileStatusIntegrationTests.cs, FaultInjection/ReconnectReconcileSubscriptionIntegrationTests.cs).
  - [x] Document before/after counts in the File List / Change Log; append a `deferred-work.md` row for the **M19 full-closure follow-up** (converge BadgeCount off System.Reactive, drop the Shell System.Reactive package reference, and decide on the hot/keyed idioms) — cross-reference DW-0285 (the Subject completion/publish disposal race) which that follow-up should resolve.

### Review Findings

- [x] [Review][Patch] High — Sanitize accessor-failure logging to a bounded exception category and update the contradictory Task 1 wording [src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs:55]
- [x] [Review][Patch] High — Restore binary-compatible public constructor overloads for the four effects whose optional `IServiceProvider` parameter changed the CLR signature [src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:24]
- [x] [Review][Patch] High — Preserve ProjectionConnection's redacted subscriber-fault logging and the two owners' distinct logging semantics [src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs:132]
- [x] [Review][Patch] High — Remove the unrelated EventStore and Memories submodule upgrades from this bounded refactor [references/Hexalith.EventStore:1]
- [x] [Review][Patch] High — Amend HEAD to a commitlint-valid Conventional Commit subject [HEAD 420afc8e]
- [x] [Review][Patch] Medium — Guarantee that successful unsubscription prevents callbacks captured by an in-flight publish, and replace the sequential near-publish test with a coordinated race [src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs:80]
- [x] [Review][Patch] Medium — Restore atomic projection teardown capture so suppression buckets cannot be logged against a stale connection snapshot [src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs:205]
- [x] [Review][Patch] Medium — Add deterministic concurrent subscribe/publish assertions that observe replay ordering rather than only final `Current` [tests/Hexalith.FrontComposer.Shell.Tests/Services/SnapshotPublisherTests.cs:151]
- [x] [Review][Patch] Medium — Prove production-resolved effects consume the registered scoped `IStorageScopeResolver`, not their local test fallback [tests/Hexalith.FrontComposer.Shell.Tests/Services/StorageScopeResolverLifetimeTests.cs:20]
- [x] [Review][Patch] Medium — Extend the anti-recurrence guard and synthetic negative case to detect local functions named `TryResolveScope` [tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs:367]
- [x] [Review][Patch] Low — Document the new `serviceProvider` constructor parameters and Theme's existing undocumented `state` parameter [src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:19]
- [x] [Review][Defer] Medium — Serialize concurrent state advance and fan-out so subscribers cannot observe a newer snapshot followed by an older one [src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs:104] — deferred, pre-existing

### Review Findings — 2026-07-13 (bmad-code-review, round 2)

Adversarial 4-layer review (Blind Hunter · Edge Case Hunter · Verification Gap · Acceptance Auditor) over `e914c615..HEAD`. Outcome: 2 decision-needed, 2 patch, 3 defer (1 already tracked), 7 dismissed.

- [x] [Review][Patch] High (resolved from decision — user ACCEPTED the upgrades, 2026-07-13) — Correct the story record: the `references/Hexalith.EventStore` (`58761c50`) and `references/Hexalith.Memories` (`8fc57cf4`) gitlinks at HEAD are intentionally KEPT at the post-merge commits, NOT restored to baseline. Remove/amend the false "restored to baseline (`1f3d1b3e` / `a077fd09`)" statements in the Dev Agent Record, File List, and Change Log so the record matches the tree. (Original finding: commit `5560bc53` restored both to baseline, then HEAD commit `daa667f8` re-applied the upgrades despite its "restore subproject references" message.) [references/Hexalith.EventStore:1, references/Hexalith.Memories:1]
- [x] [Review][Patch] Medium (resolved from decision — user chose RESTORE attribution, 2026-07-13) — Restore per-feature attribution to the HFC2105 fail-closed skip diagnostic: thread a feature/caller discriminator through `StorageScopeResolver.TryResolveScope` so the skip log names Theme/Density/Navigation/DataGrid/Capability/Palette again (message and/or a structured `Feature` field), and pin it with a test. Diagnostic ID, Information severity, and no-PII must stay preserved. (The six removed helpers each logged feature-specific text under the effect's own logger category; production now logs generic `"Storage {Direction} skipped"` under a single `ILogger<StorageScopeResolver>` category.) [src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs:56]
- [x] [Review][Patch] Medium — The replay-ordering guard test cannot catch its own regression: it triggers the concurrent `Publish(2)` from inside the replay handler (which already holds the publisher + per-subscription locks), forcing `[1,2]` even if replay were moved outside the publisher lock. Rewrite to race the publish against `Subscribe` on an independent thread and assert the joining subscriber never observes fresh-then-stale. [tests/Hexalith.FrontComposer.Shell.Tests/Services/SnapshotPublisherTests.cs:164]
- [x] [Review][Patch] Low — `SnapshotPublisher<T>.InvokeSafe` invokes the owner `_subscriberFaultHandler(ex)` outside any guard, so a throwing fault handler (e.g. a faulting logger) escapes the catch, aborts the remaining fan-out, and escalates into `Apply`/`Publish`/`Deliver` — defeating the per-subscriber isolation the primitive exists to provide. Wrap the fault-handler call to swallow non-fatal exceptions. [src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs:150]
- [x] [Review][Defer] Medium — Advance-vs-advance fan-out race (fresh-then-stale delivery); re-confirms the round-1 deferred item, already tracked in deferred-work.md. [src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs:103] — deferred, pre-existing
- [x] [Review][Defer] Low — `ProjectionConnectionStateService.Apply` has no `_disposed` guard: a late transition arriving after `Dispose()` rebuilds suppression buckets under `_logSync` that the completed one-shot F07 flush never emits, silently losing suppressed-transition counts. Predates 11.15. [src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs:89] — deferred, pre-existing
- [x] [Review][Defer] Low — The Roslyn recurrence guard is name-coupled to the exact identifier `TryResolveScope` under `State/**`; re-inlined tenant/user resolution under a different method name, or placed outside `State/**`, passes undetected. Has a real synthetic negative, so not vacuous. [tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs:367] — deferred, pre-existing

**Dismissed as noise (7):** per-subscription lock deadlock (theoretical only — no concrete failure in the owners' single-dispatcher usage; the Dispose-blocking is the intended "no callback after Dispose returns" guarantee); TryApply atomicity single-thread-only coverage (behavior is correct under the lock); AccessorThrew stack-trace detail dropped (deliberate PII hardening — an improvement); post-dispose delivery-semantics change (benign, matches new tests); non-atomic `??=` lazy resolver (harmless — stateless resolver, production pre-assigns, atomic ref write); redundant `Interlocked.Exchange` inside the lock (not a defect); per-effect throwing-accessor path not separately tested (adequately covered by the resolver test + Roslyn guard + scope tests).

## Dev Notes

### Scope Decisions (RESOLVED — the two intent-gaps that blocked spec-11-15)

- **Decision A — StorageScopeResolver inventory = the six duplicated effects ONLY.** The two near-variants (`LastUsedValueProvider` D31 one-per-circuit; `ScopeReadinessGate` exactly-once transition) are **excluded** and left as-is with documented rationale. They are distinct concerns (different diagnostic channel/severity, `null`-vs-`string.Empty` outs, and transition-not-fallback semantics), not clones of the six. This matches the M19 finding's "6 Fluxor effects" target and minimizes regression risk. *(Confirmed by user, 2026-07-12.)*
- **Decision B — SnapshotPublisher inventory = the two replayable twins ONLY** (`ProjectionConnectionStateService` + `ReconnectionReconciliationStateService`). **Excluded:** `BadgeCountService` (Rx), `LifecycleStateService` (keyed + Contracts-frozen), `CommandFeedbackPublisher` (hot), plain events. Full M19 closure (drop `System.Reactive`) is deferred to a follow-up story. This matches the epics AC wording ("hand-rolled snapshot pub/sub containers") and the spec Never-list ("don't add replay to hot warning/badge streams"; "don't change `IBadgeCountService`/`ICommandFeedbackPublisher`/`ILifecycleStateService`"). *(Confirmed by user, 2026-07-12.)*
- Note on Rx: dropping `System.Reactive` later is **not** a Contracts-API break — `IBadgeCountService.CountChanged` returns `System.IObservable<T>` (a BCL type), and `System.Reactive` is only an internal Shell implementation dependency whose sole consumer is `BadgeCountService.cs`. That makes the deferred follow-up an internal swap + package-reference removal.

### Architecture & layering constraints (must follow)

- **Placement:** both new types live under `src/Hexalith.FrontComposer.Shell/Services/` with namespace `Hexalith.FrontComposer.Shell.Services` (namespace = folder path, enforced un-spoofably by `ShellLayeringTests`). The **`Services` layer has no forbidden outbound edges**, and **`State → Services` is explicitly allowed**, so the six effects legally depend on `StorageScopeResolver` and the two State snapshot owners legally depend on `SnapshotPublisher<T>`. `Routing → Services` is forbidden — irrelevant here. Keep the new types dependency-inward (depend only on `IUserContextAccessor` + `ILogger`); do not let them reference `State`/`Components` (avoids a cycle the guard does not itself prevent). [Source: architecture.md#Shell-sublayers; `tests/.../Architecture/ShellLayeringTests.cs:215-247`]
- **Keep both new types `internal`.** Story 11.9's review explicitly made a relocated helper `internal` to avoid adding an unnecessary package API, and reclassified public relocations as breaking `v3.0` moves needing `CompatibilitySuppressions.xml` + CP0001 evidence. Internal types avoid all of that. The consolidated interfaces (`IProjectionConnectionState`, `IReconnectionReconciliationState`) already exist and are unchanged, so **no** public-API baseline, `PublicAPI.FcTbl.Shipped.txt`, `ShellOwnershipIdentityTests`, or `CompatibilitySuppressions` change is expected. If the dev makes anything public, that reintroduces the 11.9 compatibility-evidence burden — avoid it.
- **Scoped-lifetime discipline (ADR-030):** every accessor and every snapshot owner is Scoped (per-circuit); the new resolver and primitive-owning services must be Scoped too. Never capture them in singletons. All relevant accessors (`IUserContextAccessor`, `IFrontComposerTenantContextAccessor`) are Scoped. [Source: project-context.md ▸ Blazor Shell & Fluxor Rules]
- **Fluxor single-writer discipline (ADR-007)** is unaffected — this is a helper-extraction refactor; effects keep their dispatch/persistence ownership. Do not move persistence into the resolver (spec Never: "add storage write sites").

### Storage-key invariants (must not change)

- `StorageKeys.BuildKey` → `{tenant}:{user}:{feature}[:{discriminator}]`; identity canonicalization (trim → NFC → `Uri.EscapeDataString`; user lowercased only when email-shaped) happens **inside** `StorageKeys`/`FrontComposerStorageKey`. Callers pass **raw** identities today and must continue to. `FrontComposerStorageKey.Build` → `frontcomposer:lastused:{tenantCanon}:{userCanon}:{fqn}:{prop}`. Changing where escaping happens would change stored-key bytes and orphan persisted state. [Source: `State/StorageKeys.cs`, `Services/FrontComposerStorageKey.cs`]

### Diagnostics & support-safe logging

- `HFC2105_StoragePersistenceSkipped` (`Contracts/Diagnostics/FcDiagnosticIds.cs:396`) — **Information** severity, runtime-only (no analyzer emission). Structured template with the diagnostic id as the first placeholder; **never** log tenant/user values. The throwing-getter path adds `Reason=AccessorThrew` plus a bounded exception-type category without attaching the exception object. Preserve the "scope missing (resolver)" vs "storage threw (effect catch)" distinction.

### Testing standards (this project)

- xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`) + **NSubstitute**; test files plural `{Class}Tests.cs`; methods `Subject_Scenario_Expectation`. [Source: project-context.md ▸ Testing Rules]
- **Run with `DiffEngine_Disabled=true`** or Verify hangs. Project runs **solution-level with trait filters** (not per-project) as the rule; for focused local iteration use the Shell test csproj directly. A `[Trait("Category","Governance")]` guard runs in CI Gate 2b; an untagged source guard runs in the Gate 3a default lane. `CiGovernanceTests` pins the exact CI filter strings — do not change lane taxonomy.
- Scoped-lifetime test template: `tests/.../Infrastructure/Storage/IStorageServiceLifetimeTests.cs` (build provider with `ValidateScopes = true`, two scopes → not-same, same scope → same).
- New-guard template: `ShellLayeringTests.ConcretePollingWorkers_HaveOneDeclaration_AtExactInfrastructurePath` (`:37-68`) already asserts "type has exactly one declaration at an exact path AND the old path is absent" — reuse its `LoadSources`/`FindClassDeclarations` helpers. **Story 11.9 review lesson:** the reviewer replaced a lexical/regex architecture scan with **Roslyn syntax+semantic** analysis to defeat comment/alias/FQN/trivia spoofing — prefer the Roslyn approach for the new `TryResolveScope` guard rather than a bare text grep.

### Previous-story intelligence (Story 11.9 — the immediate sibling in this consolidation group)

- 11.9 declared/enforced the Shell layering that this story's placement relies on, and added `ShellLayeringTests.cs`. Its review applied 9 patches; the durable lessons that apply here: (1) keep relocated/new helpers **internal**; (2) use **Roslyn semantic** guards, not lexical; (3) add **production-DI lifecycle tests** (scoped registration, exact injection, cross-scope isolation, deterministic teardown) — `tests/.../Extensions/RelocatedInfrastructureRegistrationTests.cs` is the pattern; (4) preserve behavior **byte-for-byte** (whitespace, exception-parameter names, trailing separators were all flagged); (5) a follow-up review was recommended because the guard/DI evidence changed — budget for one.
- 11.9 explicitly scoped OUT "storage/snapshot consolidation (11.15)" in its Never-list, so no overlap/conflict — this story is the intended successor.

### Git intelligence

- Recent relevant commits: `171b0803 docs: specify storage scope consolidation` (added this story's `spec-11-15…` + the 11.16 blocking note), `0b3fab3a refactor(shell): clarify layering boundaries` (11.9). **Commit convention:** this is a refactor → use `refactor(shell): …`, **never `feat`** (a false `feat` triggers a minor bump + NuGet publish). Branch `refactor/<desc>`; no direct commits to `main`; PR targets `main`. [Source: project-context.md ▸ Development Workflow Rules]

### Excluded notification idioms (record in Change Log for M19 traceability)

Full Shell notification inventory (for the deferred M19 follow-up, do NOT touch in this story): 4 hand-rolled containers (ProjectionConnection ✅in-scope, Reconciliation ✅in-scope, LifecycleStateService keyed/Contracts-frozen ❌, CommandFeedbackPublisher hot ❌), 1 Rx (BadgeCountService ❌ — sole `System.Reactive` consumer), 6 plain `.NET event` publishers ❌ (ProjectionChangeNotifier, PendingCommandStateService, DevModeOverlayController, LifecycleThresholdTimer, FcContentLabelCoordinator, FcPageLayoutCoordinator). Adjacent non-pub/sub containers (do NOT mistake for targets): `NewItemIndicatorStateService`, `InlinePopoverRegistry`, `LastUsedSubscriberRegistry`, `LifecycleBridgeRegistry`.

### I/O & Edge-Case Matrix (from spec-11-15)

| Scenario | Input / State | Expected | Error handling |
|----------|---------------|----------|----------------|
| Valid persisted scope | current nonblank tenant + user | existing storage key + hydrate/persist behavior byte-for-byte equivalent | none |
| Invalid persisted scope | missing accessor / blank identity / **non-fatal accessor throw** | no storage read/write/remove/enumeration; feature-specific fallback still occurs | sanitized HFC2105 fail-closed; **fatal exceptions propagate** |
| Snapshot subscription | subscribe (replay on/off) around concurrent publish | current snapshot coherent; replay never stale after a fresher update | subscriber faults do not stop healthy subscribers |
| Subscription disposal | token disposed once/repeatedly, incl. near publish | no callbacks after successful removal; idempotent | none |

### Verification (must pass before Done)

```bash
# from repo root
dotnet restore Hexalith.FrontComposer.slnx -p:NuGetAudit=false
dotnet build   Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0   # 0 warnings/errors (TWAE)

# focused lane (new + affected suites), direct Shell test project:
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 \
  --filter "FullyQualifiedName~StorageScopeResolverTests|FullyQualifiedName~SnapshotPublisherTests|FullyQualifiedName~ShellLayeringTests|FullyQualifiedName~EffectsScopeTests|FullyQualifiedName~ProjectionConnection|FullyQualifiedName~ReconnectionReconciliation"

# governance lane (guard runs here) + full Shell default lane:
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "Category=Governance"
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"

git diff --check   # no whitespace/format errors
```

> Test-runner caveat (memory `shell-tests-direct-xunit-runner`): if VSTest sockets are blocked in this environment, run the built `Hexalith.FrontComposer.Shell.Tests` executable directly with `-class`/`-notrait` instead of `dotnet test`.

### Project Structure Notes

- New files: `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs`, `src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs` (or `SnapshotPublisher{T}.cs`), and matching `tests/Hexalith.FrontComposer.Shell.Tests/Services/StorageScopeResolverTests.cs` + `SnapshotPublisherTests.cs`. Namespace must equal folder path.
- Modified: the six `State/**/*Effects.cs`, both `State/**/*State.cs` snapshot owners, `Extensions/ServiceCollectionExtensions.cs` (+ possibly `EventStoreServiceExtensions.cs`), `Architecture/ShellLayeringTests.cs`, the six `*EffectsScopeTests.cs`, and `deferred-work.md`.
- No `.csproj`/`Directory.Packages.props` change expected (no new packages; `System.Reactive` **stays** — its removal is the deferred follow-up). No generated-code, schema, CLI/MCP wire, or public-API baseline change.
- No conflict with the unified structure; this tightens it (removes 6 duplicate private methods, converges 2 near-identical containers).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-11.15] — authoritative AC (M19 clusters 4 & 5).
- [Source: _bmad-output/implementation-artifacts/spec-11-15-storage-scope-and-snapshot-publisher-consolidation.md] — intent contract, Boundaries/Never-list, I/O matrix, and the two now-resolved intent-gap decisions.
- [Source: _bmad-output/project-docs/architecture-quality-review-2026-07-04.md#M19] — "Four change-notification idioms … converge on one non-Fluxor primitive; drop System.Reactive"; duplication table rows 4 & 5.
- [Source: _bmad-output/planning-artifacts/architecture.md#Shell-sublayers] — Services/State/Routing layering; Telemetry cross-cutting.
- [Source: _bmad-output/project-context.md] — .NET 10, TWAE, `.slnx`-only, ConfigureAwait(false), sealed-by-default, ULIDs, scoped-lifetime discipline, testing stack, commit conventions.
- [Source: _bmad-output/implementation-artifacts/spec-11-9-shell-layering-declaration-and-route-label-relocation.md] — sibling refactor pattern, Roslyn guard, internal-helper + DI-lifecycle-test lessons.
- Code anchors: `State/{Theme:132,Density:215,Navigation:376,DataGridNavigation:540,CapabilityDiscovery:236,CommandPalette:956}/…Effects.cs`; `State/ProjectionConnection/ProjectionConnectionState.cs:49-304`; `State/ReconnectionReconciliation/ReconnectionReconciliationState.cs:33-186`; `Contracts/Diagnostics/FcDiagnosticIds.cs:396`; `Extensions/ServiceCollectionExtensions.cs:254,358-378`; `tests/.../Architecture/ShellLayeringTests.cs:37-68,215-247`; `tests/.../Infrastructure/Storage/IStorageServiceLifetimeTests.cs`.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (Claude Opus 4.8) — /bmad-dev-story.

### Debug Log References

- Completion revalidation (2026-07-13): `dotnet restore Hexalith.FrontComposer.slnx -p:NuGetAudit=false` passed; `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` passed with **0 warnings / 0 errors**.
- Completion revalidation (2026-07-13), `DiffEngine_Disabled=true`: full solution default lane passed **4,053 / 4,053** (Contracts 203, Contracts.UI 10, CLI 67, MCP 372, Shell 2,281, SourceTools 1,063, Testing 57); solution Governance lane passed **258 / 258** (Shell 126, SourceTools 132).
- Completion reconciliation found that squash merge `8b8e002d` retained unrelated EventStore and Memories pointer upgrades despite the review record stating they were removed. Commit `5560bc53` restored both root gitlinks to their `baseline_commit` values (`EventStore` `1f3d1b3e`; `Memories` `a077fd09`), but the subsequent HEAD commit `daa667f8` re-applied the upgrades. **Code review (round 2, 2026-07-13) confirmed the tree still carries the upgraded pointers (`EventStore` `58761c50`; `Memories` `8fc57cf4`); the user ACCEPTED these upgrades as-is, so they are kept intentionally rather than restored to baseline.** No submodule content edits; `git diff --check` is clean.
- Build (Release, TWAE) Shell: `dotnet build src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` → **0 warnings / 0 errors**.
- Build (Release, TWAE) Shell.Tests → **0 warnings / 0 errors** (after fixing two xUnit analyzer findings — xUnit1031/xUnit1051 — in the new concurrency test).
- Tests run via the built xUnit v3 executable directly (VSTest sockets are environment-blocked — memory `shell-tests-direct-xunit-runner`), `DiffEngine_Disabled=true`:
  - New + affected focused set (StorageScopeResolverTests, SnapshotPublisherTests, StorageScopeResolverLifetimeTests, ShellLayeringTests, all six `*EffectsScopeTests`): **88 passed / 0 failed**.
  - Non-scope effect suites + both snapshot owners + integration + DI-registration (ThemeEffectsTests, DataGridNavigationEffectsTests, NavigationEffectsLastActiveRouteTests, CommandPaletteEffectsTests, ProjectionConnectionTelemetryTests, ReconnectionReconciliationCoordinatorTests, ReconnectReconcileStatusIntegrationTests, ReconnectReconcileSubscriptionIntegrationTests, NudgeToSchedulerLaneRefreshIntegrationTests, FcProjectionConnectionStatusTests, RelocatedInfrastructureRegistrationTests): **80 passed / 0 failed**.
  - Review-focused set (SnapshotPublisherTests, SnapshotPublisherOwnerLoggingTests, StorageScopeResolverTests, StorageScopeResolverLifetimeTests, ShellLayeringTests): **35 passed / 0 failed / 0 errors**.
  - **Full Shell default lane** (`-notrait Category=Performance -notrait Category=e2e-palette -notrait Category=NightlyProperty -notrait Category=Quarantined`): **2281 passed / 0 failed / 0 errors**.
  - **Governance lane** (`-trait Category=Governance`, where the new source guard runs — CI Gate 2b): **126 passed / 0 failed**.
- `git diff --check` → clean; all changed `.cs` files normalized to CRLF per `.gitattributes`/`.editorconfig`.

### Completion Notes List

- **AC1 (M19 cluster 4).** The six duplicated effect-local `TryResolveScope` methods are removed and replaced by a single Scoped `Services/StorageScopeResolver` (internal `sealed`, with an internal `IStorageScopeResolver` interface in its own file per the one-type-per-file rule). The resolver adopts CommandPalette's canonical fail-closed guard, with review hardening that sanitizes accessor failures to a bounded exception-type category, so the accessor getters are read inside a non-fatal `try/catch`, **fixing the latent throw-propagation bug the other five copies shared** (they read `accessor.TenantId`/`UserId` with no guard). Fail-closed contract is byte-for-byte: missing/blank/whitespace tenant OR user → both `out` set to `string.Empty`, return `false`, HFC2105 at Information with the `direction` placeholder (never logging raw tenant/user values); on success the raw identities are returned and `StorageKeys`/`FrontComposerStorageKey` keep ownership of canonicalization. The throwing-getter path logs `Reason=AccessorThrew` plus the bounded exception type without attaching the exception object.
- **Consumption seam (review-hardened for binary compatibility).** The persisted-feature effects are **public** classes, while `IStorageScopeResolver` must remain internal to avoid public-API growth. `NavigationEffects` and `CommandPaletteEffects` retain their existing `IServiceProvider` seam. For Theme, Density, DataGridNavigation, and CapabilityDiscovery, review restored the exact pre-11.15 public CLR constructor signatures and added assembly-internal resolver-aware constructors; production Fluxor descriptors are replaced with scoped factories that call those internal constructors with the exact registered `IStorageScopeResolver`. Directly constructed existing adopters/tests keep the old constructor and lazily create the same resolver implementation. `StorageScopeResolverLifetimeTests` pins both the four published six-parameter signatures and all six production-resolved effects' reference identity with the registered scoped resolver.
- **AC2 (M19 cluster 5).** New Scoped `Services/SnapshotPublisher<T>` owns the shared handler-list / `Current` + replay-under-lock / `InvokeSafe` fault-isolation / `Interlocked` idempotent `Subscription` mechanics. `ProjectionConnectionStateService` and `ReconnectionReconciliationStateService` delegate those mechanics but **retain their distinct semantics** via `TryApply(Func<T,T?>)` (owner decision runs atomically under the lock) + `Deliver` (fan-out): ProjectionConnection keeps reconnect-attempt accumulation, logical-state dedup, the 30-second rate-limited log buckets + F16 eviction, telemetry, `IDisposable`/`IAsyncDisposable` F07 flush; Reconciliation keeps epoch+status staleness gating, logical-duplicate dedup, atomic current-then-replay, and stays non-`IDisposable`. Both public interfaces, snapshot records, and all Scoped registrations are unchanged. No owner-specific rule is baked into the primitive. The `TryApply`+`Deliver` split preserves each owner's original "mutate-under-lock → log/telemetry → fan-out" ordering.
- **AC3 (evidence + anti-recurrence).** New Roslyn (syntax-level, spoof-proof) governance guard in `ShellLayeringTests`: asserts **0** `TryResolveScope` method declarations under `State/**` and exactly one `StorageScopeResolver` class at `Services/StorageScopeResolver.cs`, plus a synthetic forbidden case proving the guard can fail. **Before/after counts:** scope resolver `6 effect-local definitions / 16 invocations` → `0 effect-local definitions / 16 shared invocations`; snapshot pub/sub `2 hand-rolled replayable containers` → `1 shared primitive + 2 thin owners`. Excluded near-variants left untouched with rationale (below); deferred M19/Rx-removal follow-up appended to `deferred-work.md` (cross-referencing DW-0285).
- **Documented exclusions (unchanged).** `Services/DerivedValues/LastUsedValueProvider.cs` (`TryResolveTenantAndUser`, D31 one-per-circuit + `IDiagnosticSink` Warning) and `State/Navigation/ScopeReadinessGate.cs` (Interlocked exactly-once transition) are distinct concerns, not clones of the six — left as-is (Decision A). `BadgeCountService` (Rx `Subject`), `LifecycleStateService` (correlation-keyed, Contracts-frozen), `CommandFeedbackPublisher` (hot), and the six plain `.NET event` publishers are excluded from the SnapshotPublisher migration (Decision B); full M19 closure (drop `System.Reactive`) is the deferred follow-up.
- **Scope-test disposition.** The six `*EffectsScopeTests.cs` are **retained unchanged** — they assert persistence-skipped + HFC2105 fail-closed behavior which now flows through the new resolver seam (via the test-fixture fallback), so they pass byte-for-byte with no edits. Direct resolver coverage (incl. the new throwing-accessor hardening) lives in the new `StorageScopeResolverTests`.
- No `.csproj`/`Directory.Packages.props` change (no new packages; `System.Reactive` stays). No generated-code, schema, CLI/MCP wire, or public-API baseline change (the effect-ctor optional-param additions are not in any tracked baseline; Release build clean under TWAE + the full Governance/boundary lane green confirms it).
- **Completion reconciliation & code-review correction.** The squash merge reintroduced the EventStore/Memories pointer upgrades; commit `5560bc53` restored them to baseline but HEAD commit `daa667f8` re-applied them. Code review (round 2) surfaced the discrepancy and the user ACCEPTED the upgrades, so the root gitlinks are intentionally KEPT at `EventStore` `58761c50` / `Memories` `8fc57cf4` (NOT baseline `1f3d1b3e` / `a077fd09`). Full Release/default/Governance validation remains green.

### File List

**New:**

- `src/Hexalith.FrontComposer.Shell/Services/IStorageScopeResolver.cs`
- `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs`
- `src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/StorageScopeResolverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/SnapshotPublisherTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/SnapshotPublisherOwnerLoggingTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/StorageScopeResolverLifetimeTests.cs`

**Modified:**

- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` (register `IStorageScopeResolver` Scoped and replace four Fluxor effect descriptors with internal-constructor factories)
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs`
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationState.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs` (recurrence guard + `FindMethodDeclarations` helper)
- `references/Hexalith.EventStore` (root gitlink at `58761c50` — squash-merge upgrade ACCEPTED as-is in code review 2026-07-13; NOT restored to baseline `1f3d1b3e`)
- `references/Hexalith.Memories` (root gitlink at `8fc57cf4` — squash-merge upgrade ACCEPTED as-is in code review 2026-07-13; NOT restored to baseline `a077fd09`)
- `_bmad-output/implementation-artifacts/deferred-work.md` (M19 full-closure follow-up row)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status tracking)
- `_bmad-output/implementation-artifacts/11-15-storage-scope-and-snapshot-publisher-consolidation.md` (this story: `baseline_commit`, status, task checkboxes, Dev Agent Record, File List, Change Log)

**Note:** the six `State/**/*EffectsScopeTests.cs` were **not** modified — the story's projected File List anticipated re-pointing them, but the production-factory/direct-construction compatibility seam keeps them green unchanged (they exercise the resolver behavior transitively). Recorded per Task 3's "re-point **or retain**" option.

## Change Log

- 2026-07-13 — Code review (round 2) applied 4 patches: (1) corrected this record — the EventStore/Memories root gitlinks are intentionally KEPT at the post-merge commits `58761c50` / `8fc57cf4` (user accepted the upgrades), NOT restored to baseline `1f3d1b3e` / `a077fd09` as the prior reconciliation entry claimed (commit `5560bc53` restored them, HEAD `daa667f8` re-applied the upgrade at `341ed48e`, then commit `8af6e141` tracked EventStore forward to `58761c50`); (2) restored per-feature attribution to the HFC2105 fail-closed skip diagnostic — threaded a `feature` discriminator through `StorageScopeResolver.TryResolveScope` and all 16 call sites, with a new attribution test; (3) rewrote the replay-ordering test to race `Publish` against `Subscribe` on independent threads (the previous version triggered the publish from inside the replay handler and could not catch a regression); (4) guarded `SnapshotPublisher.InvokeSafe`'s owner fault-handler call so a throwing fault handler cannot abort the fan-out. Re-validation results appended after the run.
- 2026-07-13 — (SUPERSEDED re: submodule pointers by the round-2 correction above) Completion reconciliation restored the EventStore and Memories root gitlinks to the captured baseline after squash merge `8b8e002d` reintroduced unrelated upgrades that review had declared removed. Revalidated the complete result: restore passed; Release solution build 0 warnings / 0 errors; full default lane 4,053/4,053; Governance 258/258; `git diff --check` clean. Story and sprint status returned to `review`.
- 2026-07-13 — Adversarial code review applied all 11 patches: bounded accessor-failure categories without exception attachment; exact public constructor compatibility plus production resolver identity pins; owner-specific subscriber-fault logging; post-capture unsubscribe safety; atomic projection teardown capture; deterministic replay-race and local-function governance coverage; documentation cleanup; unrelated submodule bumps removed; Conventional Commit metadata repaired. Review verification: Release solution build 0/0, focused 35/35, full Shell default 2281/2281, Governance 126/126. One verified medium, pre-existing concurrent delivery-ordering issue remains deferred, so status returns to `in-progress`.
- 2026-07-13 — Story 11.15 implemented (refactor; M19 clusters 4 & 5). Consolidated six effect-local `TryResolveScope` resolvers into one Scoped `Services/StorageScopeResolver` (adopting CommandPalette's throwing-accessor guard, fixing the latent throw-propagation bug in the other five) — `6 definitions / 16 invocations` → `0 definitions / 16 shared invocations`. Introduced Scoped `Services/SnapshotPublisher<T>` and reused it across `ProjectionConnectionStateService` + `ReconnectionReconciliationStateService` (each retaining its distinct epoch/dedup/reconnect/logging/telemetry semantics and unchanged public interface) — `2 hand-rolled containers` → `1 primitive + 2 thin owners`. Added a Roslyn governance guard (0 effect-local `TryResolveScope` under `State/**`; single resolver at the exact path; synthetic negative case), direct resolver tests (incl. new throwing-accessor `Reason=AccessorThrew` hardening), `SnapshotPublisher<T>` matrix tests, and Scoped-lifetime pins. Excluded `LastUsedValueProvider`/`ScopeReadinessGate` (Decision A) and `BadgeCountService`/`LifecycleStateService`/`CommandFeedbackPublisher`/plain events (Decision B); appended the deferred M19/Rx-removal follow-up (cross-referencing DW-0285). Release build 0/0 (TWAE); full Shell default lane 2277/0, Governance lane 126/0. No package, generated-code, schema, wire, or public-API baseline change.
