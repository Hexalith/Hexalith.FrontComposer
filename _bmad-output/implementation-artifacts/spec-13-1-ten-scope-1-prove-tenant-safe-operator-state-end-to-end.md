---
title: 'Story 13.1: [I · TEN-SCOPE-1] Prove Tenant-Safe Operator State End to End'
type: 'feature'
created: '2026-09-23'
status: 'in-progress'
route: 'dispatch'
story_id: '13.1'
baseline_commit: '3008cf194f859219e2fb64c67bc04fc1441c5e87'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FR-30 is only proven lazily and per service. Nothing in the Shell reacts when the resolved tenant/user changes inside a live circuit, so prior-scope badge counts, loaded pages, preferences (`StorageReady` never resets), pending-command snapshots, FC-CNC admission, SignalR detail payloads, and fresh query responses can surface under the next scope. A missing tenant renders as a generic failure or empty-looking data instead of an explicit blocking state, and no test runs the production adapters for two tenants against a real EventStore.

**Approach:** Add one Shell scope-boundary owner that detects a tenant/user change or loss and clears or blocks every scoped store before the next scope renders. Close the specific fail-open paths below. Surface missing or stale scope as an explicit, support-safe blocking state. Prove it with the existing focused tests plus one two-tenant production-seam scenario.

## Boundaries & Constraints

**Always:** Resolve scope through `IFrontComposerTenantContextAccessor` before every query, subscription, count, preference read/write, pending-state read, and fresh-row render. There is no default tenant. The scope-change clear completes before any next-scope render. Storage keys keep the `StorageKeys` shape, or persistence is skipped with HFC2105. Blocked scope output, logs, and evidence contain no tenant/user values, tokens, JWTs, payloads, stack traces, or PII. Reuse the existing focused tests and add only missing assertions. Fluent UI V5 components only. Update PublicAPI baselines for any intentional public change.

**Never:** Implement AM-26 focus/speech or SS-05/SS-06 announcement semantics (Story 13.4 owns them); this story renders the blocking meaning only. No MCP scope changes (Story 14.2). Don't consolidate `FrontComposerStorageKey` into `StorageKeys` (FR29.4 keeps the single-builder coverage as-is). Don't edit `ci.yml`, `release.yml`, or `release-evidence.yml` (pinned release chain). No new CI lane or report wrapper. Don't change the FC-NIP resolver ownership or the DW-679 non-goal.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Two tenants | Tenants A and B with distinguishable projection rows and counts | Each sees only its own query rows, SignalR nudges/details, counts, and preferences | N/A |
| Missing tenant | Tenant or user absent/blank | No query/subscription/count/persist runs; explicit blocking state, never empty grid or `0`-looking home | `TenantContextException` or typed blocked result; HFC2105 for storage |
| Invalid/mismatched | Malformed, synthetic, or mismatched tenant | Same as missing; HFC2015–2019 logged without raw values | Fail closed before cache, token, or HTTP |
| Live A→B switch | Auth state changes while grid, home, badges, pending command, and fresh row are live | A's pages, counts, pending snapshot, admission block, fresh rows, and preferences clear; A's groups are left/Blocked; B hydrates fresh | In-flight A responses are discarded, not rendered |
| Late A response | A query 200 or SignalR detail arrives after the switch | Dropped; never reaches B's state | Revalidate snapshot before returning or forwarding |
| Scope loss | A→none | Everything from A clears; blocking state shows | Same as missing |

**Decisions (2026-09-23):**
- *Blocking surface:* 13.1 renders a support-safe blocking region (heading and message replacing home, projection, and badge content) with no focus/announcement semantics. Story 13.4 later adds AM-26 to the same component.
- *Production-seam vehicle:* an opt-in, env-gated xUnit class in Shell.Tests drives `EventStoreQueryClient`, `ProjectionSubscriptionService`, and `EventStoreActionQueueCountReader` against the live AppHost EventStore for two provisioned tenants. It skips explicitly when no endpoint is configured, and it never edits the hash-bound Gate 2c smoke script or the pact files.
- *Scope:* keep the full spec as one story (user override of the 1600-token guideline).

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/Tenancy/FrontComposerTenantContextAccessor.cs` -- `Resolve`/`Revalidate`; HFC2015–2019 matrix. Reuse; do not weaken.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` -- fresh 200 path (≈l.238–285) returns without `RevalidateSnapshot`; the cache path already revalidates (l.194, 364).
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs` -- l.26 status GET never resolves tenant.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` -- `GroupKey` l.923; `IsGroupContextCurrent` l.809 (fails open with no accessor, l.795/810); detail forward l.366 unchecked; Blocked groups never left.
- `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` -- `_counts` keyed by `Type` only; lane tenant captured once (l.298); keeps the stale count on failure (l.233).
- `src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeReadinessGate.cs`, `ScopeFlipObserverEffect.cs`, `NavigationReducers.cs:165` -- `StorageReady` is one-way; no reset/rehydrate on scope change.
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageState.cs` -- keyed `(ViewKey, Skip)`, never scope-cleared.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs` -- `EnforceScopeBoundary` l.633 only on Register/ResolveTerminal; `GetByMessageId` l.223 and `Snapshot` l.242 leak; unscoped `Register` survives (l.670).
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionGate.cs` -- l.27 FC-CNC uses the unscoped snapshot.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs` -- already fails closed (l.300). This is the reference pattern.
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:720` -- `PaletteScopeChangedAction` exists; nothing dispatches it.
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor.cs:56-80` -- reads counts/user once; no auth subscription.
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs:58` -- existing `AuthenticationStateChanged` pattern to mirror.
- Tests to reuse (Shell.Tests unless noted): `Infrastructure/Tenancy/TenantContextValidationMatrixTests.cs`, `Infrastructure/EventStore/QueryAndCacheTenantIsolationTests.cs`, `Infrastructure/EventStore/CommandTenantIsolationTests.cs`, `Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs:495,524`, `Badges/EventStoreActionQueueCountReaderTests.cs:35`, `State/StorageKeysTests.cs`, `Services/StorageScopeResolverTests.cs`, `State/Navigation/ScopeReadinessGateTests.cs`, `State/PendingCommands/PendingCommandStateServiceTests.cs:435,457`, `Components/DataGrid/FcNewItemIndicatorTests.cs:402-454`, Contracts.Tests `Rendering/ReturnPathValidatorTests.cs`; redaction via `src/Hexalith.FrontComposer.Testing/Assertions.cs:90` (`AssertRedacted`).

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Shell/State/Navigation/` (new scope-boundary effect/service) -- subscribe to `AuthenticationStateChanged`, compare the resolved (tenant, user) with the last snapshot, and on change or loss dispatch one scope-changed action (and the existing `PaletteScopeChangedAction`) before re-render -- single owner of the in-circuit boundary.
- [x] `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationReducers.cs`, `ScopeReadinessGate.cs` -- reset `StorageReady` on scope change and re-arm hydration under the new scope; never persist old state under the new key -- closes the preference leak.
- [x] `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/` reducers, `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` -- clear loaded pages and counts, and re-register the reconciliation lane for the new tenant -- closes the page/count leak.
- [x] `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs`, `CommandExecutionAdmissionGate.cs` -- enforce the boundary on every read, and reject `Register` without scope -- closes the pending/FC-CNC leak.
- [x] `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`, `ProjectionSubscriptionService.cs`, `EventStorePendingCommandStatusQuery.cs` -- revalidate before returning a fresh 200 or forwarding a detail; leave Blocked groups; fail closed when no accessor is registered; scope the status query -- closes the adapter leaks.
- [x] `src/Hexalith.FrontComposer.Shell/Components/` (new Fluent V5 blocking-state component, wired into `Components/Home/FcHomeDirectory.razor`, the generated projection host, and badge surfaces) -- render the explicit support-safe blocked meaning when scope is missing or stale; no live region or programmatic focus (13.4 adds AM-26) -- closes the empty-looking-data path.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/` (new live two-tenant class) -- env-gated; provision tenants A and B with distinguishable data; assert query, subscription, and count isolation through the production adapters; skip explicitly when no endpoint is set; run `AssertRedacted` on the output. Use an existing trait convention, or register any new category in the governance trait inventory; keep it out of the default lane -- production-seam proof for AC 1.
- [x] Shell.Tests -- add only the missing assertions: live A→B switch per store, late-response drop, `GroupHealth.Blocked`, no-accessor fail-closed, and blocking-state render. Run `AssertRedacted` on the new evidence.

**Acceptance Criteria:**
- Given tenants A and B with distinguishable data, when each uses the production query, subscription, count, and storage adapters, then each receives only its own state, proven by the production-seam scenario rather than unit substitutes.
- Given absent, invalid, mismatched, or stale identity, when any scoped operation would run, then it fails closed before prior-scope data can render and the operator sees an explicit blocking state.
- Given a prior-scope SignalR group or persisted preference, when tenant or user changes, then the group is left or Blocked and old state is cleared before the next scope renders; storage uses `StorageKeys` or is skipped with HFC2105.
- Given the existing focused tests, when this story is verified, then they pass unchanged plus the minimum new scenarios, and no output contains tenant payloads, tokens, JWTs, stack traces, or PII.

### Review Findings

Code review 2026-09-26 (pass 2; diff `fc680685` + `85acdf6d` on the same paths; layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor).

- [ ] [Review][Patch] Generated projection `{Type}State.Items` survives an A→B switch (high; decision resolved 2026-09-26: reset only). Emit an `On{Type}ScopeChanged` reducer on `ScopeChangedAction` returning `new(IsLoading: false, Items: null, Error: null)`. Do not auto-dispatch `LoadRequested`: adopters own loading, and an unhandled request would leave the view loading forever. Grids reload through the D2 remount [src/Hexalith.FrontComposer.SourceTools/Emitters/FluxorActionsEmitter.cs:80]
- [ ] [Review][Patch] Component-local prior-scope state (the `FcProjectionSubtitle` count, connection and pending summaries, adopter components) survives A→B because the shell never unmounts `ChildContent` (medium; decision resolved 2026-09-26: `@key` remount). Add an `internal int Generation` to `ScopeBoundaryService`, incremented on every transition, and wrap the current-scope branch in a markup-free internal component keyed by it, so the subtree is rebuilt in the same render. No `IBadgeCountService` contract change [src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:152]
- [ ] [Review][Patch] RazorEmitter Verify snapshots were not refreshed for the new tenant-accessor inject and scope guard, so 6/17 `RazorEmitterTests` and 8/16 `RoleSpecificProjectionApprovalTests` fail (re-run locally by the Verification Gap layer; no `*.verified.txt` contains `TenantContextAccessor`). This is currently masked because main's Quality run fails earlier at Gate 2b [src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:116]
- [ ] [Review][Patch] Theme preference stays visually applied after a scope change: `ReduceScopeChanged` resets the state to Light, but the theme is pushed to `IThemeService` only on the shell's first render and in `HandleThemeChanged`. On A→none, or A→B where B has no stored theme, A's Dark theme remains on screen. Add an effect on `ScopeChangedAction` that applies the reset theme [src/Hexalith.FrontComposer.Shell/State/Theme/ThemeReducers.cs:10]
- [ ] [Review][Patch] The live proof's SignalR isolation assertions can't catch a wrong-group join: the handler increments `_nudgeCount` only when `observedTenant == _tenant`, so an A-scoped service that joined B's group raises `ProjectionChangedForTenant(…, B)` and is silently dropped. Count every `counter-projection` nudge and assert its tenant separately [tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/LiveTwoTenantProductionAdapterTests.cs:143]
- [ ] [Review][Patch] Generated projection scope guard has no behavioral test: `GeneratedComponentTestBase` always registers a succeeding `TestTenantContextAccessor`. Add missing-scope and `RenderContext`-user-mismatch cases that assert `fc-scope-blocked` and no rows [tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:142]
- [ ] [Review][Patch] The `ScopeBoundaryService` orchestration never runs against real collaborators: the tests use substitutes and an empty provider, so the `is ScopeReadinessGate` / `is CommandExecutionAdmissionGate` / `is BadgeCountService` / `GetService<ProjectionSubscriptionService>` branches are unexercised. Add a DI-resolved A→B case that asserts `StorageReadyAction` is re-dispatched, admission is released, counts reset and re-seed, and the A group is blocked or left [src/Hexalith.FrontComposer.Shell/Services/ScopeBoundaryService.cs:83]
- [ ] [Review][Patch] Shell re-render on an in-circuit scope change is untested: only the always-null first render is covered. Add a valid→cleared + `AuthenticationStateChanged` bUnit case asserting `ChildContent` is replaced by `fc-scope-blocked` [src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:475]
- [ ] [Review][Patch] Late-hydration scope re-checks in the Density, Navigation, CommandPalette, CapabilityDiscovery, and DataGridNavigation effects are untested: no test switches scope during `GetAsync`. Mirror `ThemeEffectsScopeTests.HandleThemeChanged_PriorHydrationScope_DoesNotApplyOrPersistUnderB` [src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:131]
- [ ] [Review][Patch] `ScopeChangedAction` reducers for Density, CapabilityDiscovery, and DataGridNavigation have no tests. Add reducer cases or extend `ScopeBoundaryFluxorDispatchTests` [src/Hexalith.FrontComposer.Shell/State/Density/DensityReducers.cs:14]
- [ ] [Review][Patch] `BlockStaleGroupsAsync` promotes a matching Pending or Degraded group to Active via `IsGroupContextCurrent`, so a group never joined on the wire makes later `SubscribeAsync` calls short-circuit. Check `wasPending` and handle the Pending case before calling `IsGroupContextCurrent` [src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:134]
- [ ] [Review][Patch] `BadgeCountService.ResetScope` keeps `_initializeTask`/`_initializeScope`, so after B→none→B with B's first fetch still running, `InitializeAsync` returns the stale task and the disposed fallback lanes are never re-registered. Clear both under `_initializeGate` [src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:122]
- [ ] [Review][Patch] Generated forms silently drop an accepted command whose `AssociateAccepted` returns `ScopeUnavailable`: it matches neither the registered branch nor the `LogPendingRegistrationSkipped` list [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:1285]
- [ ] [Review][Patch] A stale comment contradicts the fail-closed behavior: the catch says an accepted command "can still be registered", but `EnforceScopeBoundary` now returns false and `Register` returns `ScopeUnavailable` [src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:689]
- [ ] [Review][Patch] `FcHomeDirectory.OnInitializedAsync` is now an empty `base` call that still carries the CA2007 suppression justifying logic that moved to `OnParametersSetAsync`. Delete the override [src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor.cs:61]
- [x] [Review][Defer] `ScopeBoundaryService` has no recovery if `Start()` captures a null scope or the scope changes without an auth event (the shell stays blocked until reload) [src/Hexalith.FrontComposer.Shell/Services/ScopeBoundaryService.cs:55] — deferred: maybe-false, medium if true. Pass 1 rejected the auth-event variant for the production `ServerCircuitUserContextAccessor`. To settle it, check whether the interactive circuit's first `OnInitialized` can observe an unresolved principal (`CircuitServicesAccessor.Services` unset or a pending auth task) in a real Blazor Server host.
- [x] [Review][Defer] The `BadgeCountService.Counts`/`TotalActionableItems` getters call `ResetScope()` on a null read, disposing fallback lanes that nothing re-registers when the same scope returns [src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:134] — deferred: maybe-false, medium if true. To settle it, show a production path where `TryGetContext` returns null transiently while the circuit scope is unchanged (for example a getter read outside the circuit's AsyncLocal flow).

**Rejected (pass 2):**
- false — the generated-form "existing handlers keyed on full CLR name" deletion claim: the implementation notes record that the gateway rejects CLR names in `projectionType`, so the kebab route is the fix.
- false — `CounterFixture` breaking NuGet-mode builds: `EventStorePath` comes from `deps.local.props`, and the AppHost, a Debug-only source project, already references `$(EventStorePath)` the same way.
- false — `LoadedPageReducers.ReduceScopeChanged` purity: `TrySetCanceled` inside reducers is the existing pattern in the same file (lines 69, 232, 250).
- false — missing PublicAPI baseline: the Shell has only `PublicAPI.FcTbl.Shipped.txt`, and binary compatibility was restored by `85acdf6d`. The `FcScopeBlocked` summary and Quickstart identity docs are already deferred (deferred-work, 12.1 round 2).
- false — Density `EffectiveDensity` leak on A→B: B's hydrate always dispatches `DensityHydratedAction(stored, resolvedEffective)`. A→none shows the blocked surface.
- false — `85acdf6d` contradicting "no submodule pointer or approval evidence was changed for this story": that note covers `fc680685`, and `85acdf6d` is a separate commit.
- false — `aria-live`/focus missing from `FcScopeBlocked`: the frozen spec assigns AM-26 to Story 13.4.
- false — the live test not being a CI gate: already deferred in pass 1.
- low — concrete-type resets skipped for adopter-replaced gate, badge, or admission services: a rare replacement, and the fix adds interface surface.
- low — fire-and-forget tasks and the unfiltered `async void` catch: the inner calls catch their own faults, and a second `SynchronizeCore(null)` is a no-op once `_snapshot` is null.
- low — fallback paths using `new FcShellOptions()`: reached only when `IFrontComposerTenantContextAccessor` is not registered, and production DI registers it.
- low — compatibility constructors that fail closed ("binary compat" but inert): deliberate fail-closed defaults for direct 4.4 construction, which is rare.
- low — kebab `projectionType` collisions for same-named or generic types within one domain.
- low — the live test's `Category=Performance` trait, integer-only `AssertRedacted` payload, and storage key-only proof: they match the frozen vehicle decision, and the storage key proof was accepted in pass 1.
- low — test assertion weaknesses (`DispatchCount` checked right after `Submit`, case-insensitive `Contact support`, the vacuous `tenant-a` check, first-render-only surface tests).
- low — raw `<h1>` inside `FluentCard` and a possible double h1 on a projection-level block.
- low — realm grants to `tenant-b-user`, fixture `TenantId "*"`, and the AppHost-to-tests `ProjectReference`: local dev topology only.
- low — `BlockStaleGroupsAsync` duplicating the sync pass, the wrong log event, the `_gate` wait without a token, and disposal-time `ObjectDisposedException` (unobserved, harmless).
- low — the dead `context?.TenantId ?? tenantId`, per-key HFC2105 noise, the literal `"hydrate"`, the magic reason string, and `using` ordering.
- low — the `RegisterReconciliationLane` TOCTOU and `ResetScope` disposing while holding `_scopeGate`: a sub-microsecond window, and the lane refresh re-reads the current scope anyway.
- low — the `Register` TOCTOU with `EnforceScopeBoundary` and `_scopeSnapshot` returned outside the lock: the next enforcement flushes a mis-scoped entry.
- low — the same-tenant user switch with a failed leave stranding a Blocked group: needs a leave failure during the switch.
- low — a blocked generated view still registering a fallback lane: the query client rejects it downstream.
- low — scope denial rendered with `CommandWarningKind.Pending`: pass 1 already patched the text to the blocking message.
- low — `TenantContextBlocked` logged on every blocked render: few renders happen while blocked.
- low — the Epic9 release-gate harness and the `FatalExceptionGuard` count 45→48 without itemization.
- rejected (spec edit) — AC4 "existing tests pass unchanged" contradicted by the Register-without-scope inversion that task 4 mandates.

## Implementation Notes

- The in-circuit owner is `Services/ScopeBoundaryService.cs`; scoped reducers, pending state, badge lanes, and subscription groups clear or block on an A→B or A→none transition. The rendering guard stays closed during the clear. Focused tests cover the switch, late response, blocked group, readiness re-arm, and blocking component.
- The opt-in production-adapter test passed against the local AppHost with separate `tenant-a-user` and `tenant-b-user` Keycloak identities. The new opt-in Counter fixture receives genuine EventStore commands, projects distinct A/B rows, and publishes the canonical `counter-projection` SignalR change. The test seeds its own rows and verifies query, count, and notification isolation, plus distinct production storage scope/key resolution for A/B. The fixture's query snapshot is ephemeral and rebuilt from the test's commands; it is not a deployment resource. The production count reader now sends a kebab-case EventStore projection route while preserving the CLR query type, because the gateway rejects CLR names in `projectionType`.
- Review fixes keep canonical rejection closed across pending state, storage, and readiness; bind pending status polls to their registration scope; revalidate cacheless 304 responses; remove stale pending SignalR groups; and version theme hydration actions so late A actions cannot alter B. A real Fluxor dispatch test checks page clearing and pending-provider cancellation. The generated form uses the blocking-state text for a scope-unavailable admission.
- The full Shell suite has one unrelated baseline governance failure: `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval` expects EventStore gitlink `66cb4edaa2b474090f2b4e375d481a4bbcd70a08`; current HEAD `d0481fb005d1e4ca81564b5daf8f0a9d53215f4e` records `04e74fc0a443d538cd1af4473bd8d1760e8f59ba`. No submodule pointer or approval evidence was changed for this story.

## Spec Change Log

## Review Triage Log

| Finding | Verdict / route | Evidence |
|---|---|---|
| Blind 1: scope change without auth event | false / reject | The production `ServerCircuitUserContextAccessor` reads the circuit `AuthenticationStateProvider`; its principal changes through the auth-state event observed by `ScopeBoundaryService`. No independent production mutation path was found. |
| Blind 2: pending validated-scope fallback | high / patch | `EnforceScopeBoundary` uses raw nonblank values when `IValidatedPendingScope.Current()` rejects them, so `Register` and `Snapshot` can accept a malformed scope. |
| Blind 3: storage resolver trusts raw values | high / patch | `StorageScopeResolver.TryResolveScope` tests only whitespace; preference effects can act on an identity that canonical tenant validation rejects. |
| Blind 4: readiness gate trusts raw values | medium / patch | `ScopeReadinessGate.EvaluateAsync` tests only whitespace and can dispatch `StorageReadyAction` on an invalid but nonblank identity after reset. |
| Blind 5: cacheless 304 is not revalidated | medium / patch | The `NotModified` branch returns to a caller-managed cache without calling `RevalidateSnapshot`, unlike the cached 304 and fresh 200 paths. |
| Blind 6: status poll loses registration scope | high / patch | `PendingCommandEntry` has no mandatory origin scope; after `GetByMessageId` returns an A entry, a switch to B before `QueryAsync` can authorize a status request for A's message under B. |
| Blind 7: scope denial says command in progress | low / patch | The generated warning maps every denial except `PendingCommandAlreadyExists` to an admission-in-progress message, including new `ScopeUnavailable`. |
| Blind 8: registration scope loss lacks terminal UI | false / reject | The accepted command remains unconfirmed, while `ScopeBoundaryService` blocks and removes the old generated form on scope loss. Creating a terminal success or failure from an unresolved accepted command would be inaccurate. |
| Blind 9: hydrated theme action crosses scopes | medium / patch | Hydration dispatches an unscoped `ThemeChangedAction`; its persistence effect resolves the later current scope, so an A preference can be written under B. |
| Blind 10: live proof omits storage and switch | medium / patch | The live class exercises query, count, and SignalR adapters but no production storage scope/key path. Focused boundary tests cover the in-circuit switch; the frozen production-seam decision requires only those three EventStore adapters live. |
| Verification gap 1: reducer registration untested | medium / patch | Tests call `LoadedPageReducers.ReduceScopeChanged` directly and mock the dispatcher; removing `[ReducerMethod]` would pass them while leaving the real store uncleared. |
| Verification gap 2: live proof is not a CI gate | medium / defer | The test is opt-in and excluded from the blocking lane by the frozen production-seam decision. A maintained two-tenant CI environment and blocking lane are separate work. |
| Verification other: pending validated-scope fallback | high / patch | Same bypass as Blind 2; the new missing-tenant test has raw nulls and does not exercise nonblank malformed identity. |
| Edge 1: pending validated-scope fallback | high / patch | Same reachable bypass as Blind 2. |
| Edge 2: pending SignalR group stranded | medium / patch | `BlockStaleGroupsAsync` marks pending groups Blocked before checking for Pending, then a failed leave retains a Blocked key that `SubscribeAsync` treats as already subscribed. |
| Edge 3: scope change without auth event | false / reject | Same production principal/event path as Blind 1; custom mutable accessors are outside the observed production circuit path. |
| Edge 4: throwing scope accessor crashes blocking surface | medium / patch | `ScopeBoundaryService.Start` and `IsCurrent` call `TryGetContext` without a nonfatal catch; an accessor exception reaches rendering instead of the blocked view. |
| Edge 5: live storage proof missing | medium / patch | Same omission as Blind 10; the live scenario has fixed A/B EventStore identities and no storage scope/key assertion. |

## Design Notes

`NewItemIndicatorStateService.ApplyScopeBoundaryLocked` is the golden pattern: check scope before every read and write, and clear everything on change or loss. The new owner adds the missing eager trigger. The lazy per-service checks stay as defense in depth, because the auth event can race an in-flight response.

## Verification

**Executed (2026-09-23):**
- `dotnet build Hexalith.FrontComposer.slnx -c Debug --no-restore -m:1 -v:q` — passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -notrait Category=Quarantined -notrait Category=Performance` — final broad run: 2,776 total, 2 failed. One is the pre-existing EventStore gitlink assertion above. The other was a test identifier inventory hash after the final readiness test rename; the ledger was resealed to 3,388 declarations and SHA-256 `4411e16313abd622201eb3c53902fbdd7038a636b4c8142d6f8945e994e6a99b`, and its targeted governance test then passed 1/1. No other Shell test failed.
- `TenantScopeFixture__Enabled=true aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive`; `aspire wait security`, `aspire wait eventstore`, and `aspire wait counter-scope-fixture` — all healthy. The AppHost was stopped after verification.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -class '*LiveTwoTenantProductionAdapterTests'` with local A/B user tokens supplied in process — final run 1 passed, 0 skipped, 0 failed. Test data was seeded through EventStore commands; no token file was written. The AppHost was stopped afterward.
- Focused `EventStoreActionQueueCountReaderTests` (3), `StorageScopeResolverTests` (11), `ScopeReadinessGateTests` (8), and `StorageKeysTests` (17) — all passed.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -method '*AnalyzerPolicy_IdentifierInventory_MatchesSeal'` — passed after the final reseal; `-method '*AnalyzerPolicy_GovernanceContract_FailsClosed'` — passed earlier after staging the new test files and updating the pragma ledger.
- Edited focused Shell suites passed: status query 26, subscriptions 39, query isolation 5, pending state 40, storage resolver 13, readiness 10, scope owner 4, Fluxor dispatch 1, and theme effect/scope/reducer 17. `ShellLayeringTests` passed after the readiness gate was changed to consume the State-level validated-scope seam. `CommandFormEmitterTests` passed 49/49 after updating two affected snapshots. `ReturnPathValidatorTests` passed 73/73.
- `python3 eng/validate-story-artifacts.py --skip-sentinel` — exits 1 only for the pre-existing E11R-AI-1 `implementation_story` omission.
- `git -c core.safecrlf=false diff --cached --check` — passed.

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx -c Debug` -- expected: 0 warnings, 0 errors (TreatWarningsAsErrors).
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -notrait Category=Quarantined -notrait Category=Performance` -- expected: all pass, including the new scope-switch classes.
- `python3 eng/validate-story-artifacts.py --skip-sentinel` -- expected: no new findings beyond pre-existing E11R-AI-1.
- `AnalyzerPolicy_IdentifierInventory_MatchesSeal` -- expected: resealed by this story because it adds tests.
