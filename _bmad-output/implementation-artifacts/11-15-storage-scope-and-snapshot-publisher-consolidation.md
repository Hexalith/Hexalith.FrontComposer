# Story 11.15: Storage scope and snapshot publisher consolidation

Status: ready-for-dev

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

- [ ] **Task 1 — Consolidate the six scope resolvers into one Scoped `StorageScopeResolver` (AC: #1)**
  - [ ] Add `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs` — **`internal sealed`** class + `internal interface IStorageScopeResolver` (namespace `Hexalith.FrontComposer.Shell.Services`; keep internal to avoid public-API growth — see Dev Notes ▸ Previous-story intelligence). Signature mirrors today's `private bool TryResolveScope(out string tenantId, out string userId, string direction)`.
  - [ ] Adopt **`CommandPaletteEffects` as the canonical body** (`State/CommandPalette/CommandPaletteEffects.cs:956-991`): resolve the accessor lazily/null-tolerantly, wrap the accessor property reads in `try/catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)`, and on a throwing getter log `HFC2105` with `Reason=AccessorThrew` + the exception. This **fixes the latent throw-propagation bug in the other five** resolvers (they read `accessor.TenantId`/`UserId` on a non-null field with no try/catch).
  - [ ] Preserve the exact fail-closed contract: on missing/blank/whitespace tenant OR user → set both `out` to `string.Empty`, return `false`, log `FcDiagnosticIds.HFC2105_StoragePersistenceSkipped` at **Information** with the `direction` placeholder; **never log raw tenant/user values** (PII). On success → assign **raw** (un-escaped) identities and return `true`.
  - [ ] Register `TryAddScoped<IStorageScopeResolver, StorageScopeResolver>()` in `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` (co-locate with the other scoped storage services ~`:358-378`). Add the parity registration in `Extensions/EventStoreServiceExtensions.cs` only if resolution there requires it (mirror the `IUserContextAccessor` dual-registration pattern at `ServiceCollectionExtensions.cs:254` / `EventStoreServiceExtensions.cs:58`).
  - [ ] Migrate the six effect classes to inject/consume the resolver and **remove all six local `TryResolveScope` definitions**:
    - `State/Theme/ThemeEffects.cs` (def `132-148`; invocations `66,117`)
    - `State/Density/DensityEffects.cs` (def `215-231`; invocations `90,196`)
    - `State/Navigation/NavigationEffects.cs` (def `376-392`; invocations `151` [`out _, out _` — preserve the discard], `201,344`)
    - `State/DataGridNavigation/DataGridNavigationEffects.cs` (def `540-556`; invocations `153,188` [mid-method re-resolve into existing vars — preserve], `238,278,327`; **preserve DataGrid's pre/post-debounce re-resolution**)
    - `State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs` (def `236-252`; invocations `132,166`)
    - `State/CommandPalette/CommandPaletteEffects.cs` (def `956-991`; invocations `163,622`)
  - [ ] Keep each effect's **feature-specific** fallback/no-scope continuation and its own `direction` argument style intact (Theme/Density/CapabilityDiscovery/CommandPalette use `DirectionHydrate`/`DirectionPersist` consts; Navigation/DataGridNavigation use `"hydrate"`/`"persist"` literals — pass the feature/direction through the resolver call, do not normalize the string shape unless behavior is byte-identical).
  - [ ] Continue passing **raw** identities into `StorageKeys.BuildKey` / `FrontComposerStorageKey.Build` (canonicalization stays centralized there — do NOT pre-escape). Do not add any storage write site (NFR17 tripwire; spec Never-list).
  - [ ] **Do not** collapse the distinction between "scope missing" (logged by the resolver) and "storage threw" (logged by each effect's own persist/hydrate `catch` block, which also uses HFC2105, e.g. `DataGridNavigationEffects.cs:208/253/347/377/403`, `CommandPaletteEffects.cs:656/671`). The resolver owns only the former.
  - [ ] **Out of scope (documented exclusions):** `Services/DerivedValues/LastUsedValueProvider.cs` (`TryResolveTenantAndUser` def `122-144`, D31 one-per-circuit `TenantGuardTripped` + `IDiagnosticSink` + Warning) and `State/Navigation/ScopeReadinessGate.cs` (`EvaluateAsync` inline check `61-84`, Interlocked exactly-once transition dispatch). Leave both untouched; record the rationale in Change Log.

- [ ] **Task 2 — Introduce `SnapshotPublisher<T>` and reuse it in the two replayable twins (AC: #2)**
  - [ ] Add `src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher<T>.cs` — **`internal sealed`** generic primitive capturing the shared shape (`object _sync` lock + `List<Action<T>> _handlers` + `T Current` + `Subscribe(Action<T> handler, bool replay = true)` that replays `Current` under the lock on `replay:true` + `Publish`/`Apply(T)` fan-out + `InvokeSafe` per-handler try/catch that catches all except `OutOfMemoryException` (and `StackOverflowException`) + nested `Subscription : IDisposable` using `Interlocked.Exchange` for idempotent unsubscribe). Do NOT bake owner-specific transition rules into the primitive.
  - [ ] Refactor `State/ProjectionConnection/ProjectionConnectionState.cs` (`ProjectionConnectionStateService`, `49-304`) to delegate its handler-list/replay/`InvokeSafe`/`Subscription` mechanics to `SnapshotPublisher<ProjectionConnectionSnapshot>`, while the owner **retains**: the `_logBuckets` log-dedup (`:59`), `IDisposable`/`IAsyncDisposable` teardown (`231-269`), reconnect and sensitive-logging policy, and the `Subscribe(handler, replay=true)` default. Interface `IProjectionConnectionState`, the `ProjectionConnectionSnapshot` record, and both Scoped registrations (`ServiceCollectionExtensions.cs:358`, `EventStoreServiceExtensions.cs:78`) stay unchanged.
  - [ ] Refactor `State/ReconnectionReconciliation/ReconnectionReconciliationState.cs` (`ReconnectionReconciliationStateService`, `33-186`) the same way, while the owner **retains**: epoch/`IsLogicalDuplicate` staleness gating, atomic current/replay ordering, and the fact that the service class itself does **not** implement `IDisposable` (do not add one). Interface `IReconnectionReconciliationState`, the snapshot record, and the Scoped registration (`ServiceCollectionExtensions.cs:360`) stay unchanged.
  - [ ] Preserve atomic current-then-replay ordering: a replay must never deliver a snapshot staler than a concurrently-applied fresher one (I/O matrix row 3).
  - [ ] **Out of scope (documented exclusions, do NOT migrate):** `Badges/BadgeCountService.cs` (Rx `Subject<T>`, hot/no-replay — see the deferred M19/Rx follow-up), `Services/Lifecycle/LifecycleStateService.cs` (correlation-**keyed**, interface frozen in **Contracts**, 5-state-machine/LRU/D20 invariants), `Services/Feedback/CommandFeedbackPublisher.cs` (hot/no-replay), and the six plain `.NET event` publishers (ProjectionChangeNotifier, PendingCommandStateService, DevModeOverlayController, LifecycleThresholdTimer, FcContentLabelCoordinator, FcPageLayoutCoordinator).

- [ ] **Task 3 — Guards, tests, and evidence (AC: #3)**
  - [ ] Add a **durable source guard** proving no effect-local `TryResolveScope` recurs: extend `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs` (Roslyn-based; `[Trait("Category","Governance")]`) modeled on `ConcretePollingWorkers_HaveOneDeclaration_AtExactInfrastructurePath` (`:37-68`) — collect `MethodDeclarationSyntax` named `TryResolveScope` under `State/**` and assert **0**, and assert a single resolver declaration at the exact `Services/StorageScopeResolver.cs` path. Include a synthetic forbidden case proving the guard can fail. (Use `LoadSources`/`FindClassDeclarations` helpers already in the file.)
  - [ ] Add `SnapshotPublisher<T>` primitive matrix tests (`tests/.../Services/SnapshotPublisherTests.cs`): replay-enabled vs disabled around a concurrent publish (current coherent, replay never stale), idempotent unsubscribe (dispose once/twice, including near a publish → no later callbacks), and per-subscriber non-fatal fault isolation (one throwing subscriber does not stop healthy ones).
  - [ ] Add direct `StorageScopeResolver` tests (`tests/.../Services/StorageScopeResolverTests.cs`): valid scope → raw identities + `true`; missing accessor → fail-closed + HFC2105; blank/whitespace tenant or user → fail-closed + HFC2105; **throwing accessor getter → fail-closed + HFC2105 `Reason=AccessorThrew`** (the new hardening) — never logs raw values.
  - [ ] Re-point or retain the six `*EffectsScopeTests.cs` fail-closed suites (`State/{Theme,Density,Navigation,DataGridNavigation,CapabilityDiscovery,CommandPalette}/*EffectsScopeTests.cs`) so they still assert persistence-skipped + HFC2105 through the new resolver seam; keep `ScopeReadinessGateTests.cs` / `NavigationReducersStorageReadyTests.cs` untouched (near-variant excluded).
  - [ ] Add Scoped-lifetime pins for `IStorageScopeResolver` and `SnapshotPublisher<T>` owners using the `tests/.../Infrastructure/Storage/IStorageServiceLifetimeTests.cs` pattern (two `CreateScope()` → `ShouldNotBeSameAs`; same-scope → `ShouldBeSameAs`; build with `ValidateScopes = true`).
  - [ ] Keep the existing `ProjectionConnection*`/`ReconnectionReconciliation*` suites green (`ProjectionConnectionTelemetryTests.cs`, `ReconnectionReconciliationCoordinatorTests.cs`, `ReconciliationSweepReducersTests.cs`, `ReconnectReconcileStatusIntegrationTests.cs`, `FaultInjection/ReconnectReconcileSubscriptionIntegrationTests.cs`).
  - [ ] Document before/after counts in the File List / Change Log; append a `deferred-work.md` row for the **M19 full-closure follow-up** (converge BadgeCount off `System.Reactive`, drop the Shell `System.Reactive` package reference, and decide on the hot/keyed idioms) — cross-reference DW-0285 (`_subject.OnCompleted`/`OnNext` disposal race) which that follow-up should resolve.

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

- `HFC2105_StoragePersistenceSkipped` (`Contracts/Diagnostics/FcDiagnosticIds.cs:396`) — **Information** severity, runtime-only (no analyzer emission). Structured template with the diagnostic id as the first placeholder; **never** log tenant/user values. The canonical throwing-getter path adds `Reason=AccessorThrew` + the exception object (from CommandPalette). Preserve the "scope missing (resolver)" vs "storage threw (effect catch)" distinction.

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

### Debug Log References

### Completion Notes List

### File List
