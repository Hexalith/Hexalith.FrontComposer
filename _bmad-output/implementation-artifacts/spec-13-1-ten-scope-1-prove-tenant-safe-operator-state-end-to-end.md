---
title: 'Story 13.1: [I · TEN-SCOPE-1] Prove Tenant-Safe Operator State End to End'
type: 'feature'
created: '2026-09-23'
status: 'ready-for-dev'
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
- [ ] `src/Hexalith.FrontComposer.Shell/State/Navigation/` (new scope-boundary effect/service) -- subscribe to `AuthenticationStateChanged`, compare the resolved (tenant, user) with the last snapshot, and on change or loss dispatch one scope-changed action (and the existing `PaletteScopeChangedAction`) before re-render -- single owner of the in-circuit boundary.
- [ ] `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationReducers.cs`, `ScopeReadinessGate.cs` -- reset `StorageReady` on scope change and re-arm hydration under the new scope; never persist old state under the new key -- closes the preference leak.
- [ ] `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/` reducers, `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` -- clear loaded pages and counts, and re-register the reconciliation lane for the new tenant -- closes the page/count leak.
- [ ] `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs`, `CommandExecutionAdmissionGate.cs` -- enforce the boundary on every read, and reject `Register` without scope -- closes the pending/FC-CNC leak.
- [ ] `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`, `ProjectionSubscriptionService.cs`, `EventStorePendingCommandStatusQuery.cs` -- revalidate before returning a fresh 200 or forwarding a detail; leave Blocked groups; fail closed when no accessor is registered; scope the status query -- closes the adapter leaks.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/` (new Fluent V5 blocking-state component, wired into `Components/Home/FcHomeDirectory.razor`, the generated projection host, and badge surfaces) -- render the explicit support-safe blocked meaning when scope is missing or stale; no live region or programmatic focus (13.4 adds AM-26) -- closes the empty-looking-data path.
- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/` (new live two-tenant class) -- env-gated; provision tenants A and B with distinguishable data; assert query, subscription, and count isolation through the production adapters; skip explicitly when no endpoint is set; run `AssertRedacted` on the output. Use an existing trait convention, or register any new category in the governance trait inventory; keep it out of the default lane -- production-seam proof for AC 1.
- [ ] Shell.Tests -- add only the missing assertions: live A→B switch per store, late-response drop, `GroupHealth.Blocked`, no-accessor fail-closed, and blocking-state render. Run `AssertRedacted` on the new evidence.

**Acceptance Criteria:**
- Given tenants A and B with distinguishable data, when each uses the production query, subscription, count, and storage adapters, then each receives only its own state, proven by the production-seam scenario rather than unit substitutes.
- Given absent, invalid, mismatched, or stale identity, when any scoped operation would run, then it fails closed before prior-scope data can render and the operator sees an explicit blocking state.
- Given a prior-scope SignalR group or persisted preference, when tenant or user changes, then the group is left or Blocked and old state is cleared before the next scope renders; storage uses `StorageKeys` or is skipped with HFC2105.
- Given the existing focused tests, when this story is verified, then they pass unchanged plus the minimum new scenarios, and no output contains tenant payloads, tokens, JWTs, stack traces, or PII.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

`NewItemIndicatorStateService.ApplyScopeBoundaryLocked` is the golden pattern: check scope before every read and write, and clear everything on change or loss. The new owner adds the missing eager trigger. The lazy per-service checks stay as defense in depth, because the auth event can race an in-flight response.

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx -c Debug` -- expected: 0 warnings, 0 errors (TreatWarningsAsErrors).
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -notrait Category=Quarantined -notrait Category=Performance` -- expected: all pass, including the new scope-switch classes.
- `python3 eng/validate-story-artifacts.py --skip-sentinel` -- expected: no new findings beyond pre-existing E11R-AI-1.
- `AnalyzerPolicy_IdentifierInventory_MatchesSeal` -- expected: resealed by this story because it adds tests.
