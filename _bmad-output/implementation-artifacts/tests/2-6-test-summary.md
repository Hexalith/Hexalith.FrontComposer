# Test Automation Summary — Story 2.6 (Live projection updates with reconnect & reconciliation)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Role:** QA automation engineer (test generation only).
**Date:** 2026-06-04 · **Baseline commit:** `ea23187` · **Branch:** `test/story-2-6-live-projection-updates`
**Framework (existing, reused):** xUnit v3 + Shouldly + bUnit (`JSInterop.Mode = Loose`) + NSubstitute + `FakeTimeProvider`.
**Net `src/` change: ZERO** — every gap closed at the integration-test layer with production types.

## Discovered gaps (auto-applied)

The dev-story left two end-to-end seams covered only in isolation. Both were flagged but not filled by its own
pins; this QA pass closes them:

| # | Gap (what was NOT pinned end-to-end) | Why it was a gap | New pin |
|---|---|---|---|
| 1 | **AC2** — reconnect driven through the **real** `ProjectionSubscriptionService.OnConnectionStateChangedAsync` → real `ReconnectionReconciliationCoordinator` → real `ProjectionFallbackRefreshScheduler` → real `FcProjectionConnectionStatus`. | Fault suites drive the service but with a **stub** scheduler and **no** reconciliation coordinator; the dev's `ReconnectReconcileStatusIntegrationTests` starts at `coordinator.ReconcileAsync(...)` directly, **bypassing** the service's reconnect trigger. | `ReconnectReconcileSubscriptionIntegrationTests` (2) |
| 2 | **AC1(a)** — a live nudge driving the **real** scheduler's registered lane refresh through the real subscription service. | Fault suites assert only that a **stub** `TriggerNudgeRefreshAsync` is called; the scheduler suite exercises lanes directly. Nothing connected the two halves (nudge → grid lane actually re-queries). | `NudgeToSchedulerLaneRefreshIntegrationTests` (2) |

## Generated tests

### E2E / integration tests (UI + service composition)
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/ReconnectReconcileSubscriptionIntegrationTests.cs` — AC2 full chain (bUnit + fault harness):
  - `ReconnectThroughSubscriptionService_WithChangedReconcile_RejoinsSurfacesConfirmationAndSweepMarker` — drop → "Reconnecting…" → reconnect: group rejoins (Join hit count 1→2), the real reconcile refreshes the registered lane, a `MarkReconciliationSweepAction` is dispatched for the changed `ViewKey`, the mounted `FcProjectionConnectionStatus` surfaces "Reconnected -- data refreshed", then auto-clears after `ProjectionReconnectedNoticeDurationMs` (via `FakeTimeProvider`).
  - `ReconnectThroughSubscriptionService_WithUnchangedReconcile_RejoinsButStaysSilent` — no-change reconcile: rejoin still happens, status returns silently to `Idle`, **no** confirmation copy, **no** sweep marker.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/NudgeToSchedulerLaneRefreshIntegrationTests.cs` — AC1(a) nudge seam (fault harness + real scheduler):
  - `LiveNudge_RefreshesTheRegisteredLane_ThroughTheRealScheduler` — a SignalR nudge through the real service refreshes the lane the generated view registers via `RegisterLane(...)` (refresh count = 1).
  - `LiveNudge_RoutesToMatchingLaneOnly_LeavesOtherProjectionUntouched` — projection-type/tenant routing isolation: nudging `orders:acme` refreshes only that lane, never `billing:acme`.

### API tests
- N/A — Story 2.6 is a Blazor/.NET shell read-path feature; there are no new HTTP endpoints. The "API" surface
  (SignalR projection hub + scheduler) is exercised through the fault-injection harness against the real service.

## Coverage

- **AC1(a)** live nudge → grid lane refresh: ✅ now pinned end-to-end (service → real scheduler → real lane).
- **AC1(b)** new-item indicator marks fresh rows: dev-story option-3 **deferral** (producer lives in the
  command-lifecycle path, Epic 3/5 / Story 5-5) — unchanged here; producer→consumer contract remains pinned by
  `FcNewItemIndicatorLaneIntegrationTests`. **Still requires PO/review sign-off** (not a QA-resolvable gap).
- **AC2** reconnect → status + reconcile missed changes: ✅ now pinned end-to-end through the real subscription
  service (previously only isolated / coordinator-first).

## Validation results

- **Build:** `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/...csproj -c Release -m:1 /nr:false` → **0 Warning(s) / 0 Error(s)** (TWAE green).
- **New pins:** 4/4 pass (`ReconnectReconcileSubscriptionIntegrationTests` ×2, `NudgeToSchedulerLaneRefreshIntegrationTests` ×2).
- **Full filtered lane** (`-notrait Category=Performance|e2e-palette|NightlyProperty|Quarantined`, `DiffEngine_Disabled=true`, xUnit v3 in-process runner — solution-level VSTest is the CI gate; it `SocketException`s in this sandbox per Stories 2.3/2.4/2.5):
  - `Hexalith.FrontComposer.Shell.Tests`: **1747 → 1751 total** (+4 new, all green); **8 failed — unchanged**.
- **Standing failure baseline re-proved** (same documented pre-existing/environmental cluster, none new, none mine):
  - `PendingStatusReopenGovernanceTests` ×4 (deferred-work file-IO)
  - `NavigationEffectsLastActiveRouteTests.HandleAppInitialized_StoredRoute_DispatchesHydratedActions` ×1 (hydration)
  - `CounterStoryVerificationTests` ×2 (Verify snapshot drift)
  - `CommandRendererFullPageTests.Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty` ×1 (query-fallback)
- **`SourceTools.Tests`:** NOT touched (no `RazorEmitter.cs` change). Approval baseline / `.verified.txt`: **zero edits**.
- **Sentinel scan (retro AI-2):** new files clean — no `</content>` / `<invoke` / `<parameter` / `antml:` tags.

## Checklist (`checklist.md`)

- [x] E2E/integration tests generated (UI exists) · [x] API tests N/A (no endpoints)
- [x] Standard framework APIs (xUnit v3 / bUnit / Shouldly) · [x] Happy path · [x] Critical error / no-change cases
- [x] All generated tests pass · [x] Semantic/accessible locators (`role`/`aria-live`, `data-testid`) · [x] Clear `Subject_Scenario_Expectation` names
- [x] No hardcoded waits/sleeps (`FakeTimeProvider` + `WaitForAssertion`) · [x] Independent tests (own harness/SUT per test)
- [x] Summary created · [x] Tests in mirror dirs · [x] Summary includes coverage metrics

## Next steps

- Run the new pins in CI via the **solution-level** `dotnet test` gate (`Hexalith.FrontComposer.slnx`).
- **PO/review:** the AC1(b) "new-item marking" deferral (dev-story option 3) is a product-acceptance decision,
  outside QA scope — confirm acceptance or carve-out before closing Story 2.6.
