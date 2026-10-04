---
title: 'Story 13.4: Announce Projection and Command State Without Noise'
type: 'feature'
created: '2026-10-04'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '7dd5f7cf07122ac25e33732c02f31b47564518a1'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projection errors, lifecycle deadlines and duplicate speech mislead operators.

**Approach:** Complete canonical speech, coalescing, projection feedback and command closure.

## Boundaries & Constraints

**Always:** Apply `_bmad-output/planning-artifacts/epics.md` Story 13.4 and `ux-design.md` UX-AM-1: AM-01–17/22/24/26/27/30 and assigned SS-05–24/26/34/35/49. Use one persistent polite status node; AM-26 uses its exact visible heading and focus alone. Preserve tenant/user isolation, authorization, input, mapped-rejection focus, first-terminal-wins, Fluent V5 styling, grid contracts, and existing enum values. Inherit Fluent semantics; visual decisions remain owner-owned. Adjacent rows retain semantics when sharing the channel.

**Never:** Use assertive alerts, announce the same event through focus and live text, expose sensitive values, treat acceptance as success, mutate an exhausted lifecycle, or broaden fresh-row/adopter helpers. OI-16, G-4, FLUENT-APP-1, and Product approval remain open.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Output/recovery | Guard |
| --- | --- | --- | --- |
| Burst | Same operation/epoch/load, different intermediate states | Trailing 250ms; last eligible message wins | Dedupe; discard superseded results |
| Terminal | Outcome or mapped focused summary | Cancel pending speech; immediate terminal or focus-only summary | Mapped AM-14 and duplicates silent |
| Projection | Loading, empty, filtered zero, failure, offline, stale, recovery | Distinct canonical state, safe actions and labelled cached data | No empty substitution or poll/retry speech |
| Budgets | Pending query/accepted command | Slow at 2s; Degraded active at 10s; terminal exhausted at 120s | Stable anchors; settlement clears slow; late responses cannot reopen |
| Scope | Missing/stale tenant or denied activation | Replacement heading focused once per activation/outcome | Hidden entries and repeated renders silent |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Emitters/` — `RazorEmitter`, `ProjectionRoleBodyEmitter`, `FluxorFeatureEmitter`, `FluxorActionsEmitter`: error-before-empty rendering, provenance, stale-result guards and grid envelope; command emitters consume terminal identities.
- `src/Hexalith.FrontComposer.Shell/` — `Components/Rendering`, `DataGrid`, `EventStore`, `Lifecycle`, `Home`, `Layout`: reuse `DataGridFocusScope`, `fc-focus.js`, resources and disposal guards; slow notice currently times completed queries.
- Shell `State/DataGridNavigation`, `ProjectionConnection`, `ReconnectionReconciliation`, `PendingCommands` — query provenance, epochs and authoritative outcome/deadline coordination. Preserve Infrastructure workers and pure State/Routing.
- `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleState.cs` and Shell `Services/Lifecycle/LifecycleStateService.cs` — retain identities; update consumers and API/snapshot evidence.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcSurfaceStatus.razor` (new), its backing coordinator types, `Extensions/ServiceCollectionExtensions.cs`, and `Resources/FcShellResources*.resx` — add disposable scoped ownership, fake time and EN/FR copy.
- [ ] `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`, projection/Fluxor emitters above, and Shell `State/DataGridNavigation/LoadPageEffects.cs` — carry load/filter origin and safe outcomes; wire shared speech across visible projection states; show pending SlowQuery; recover hidden-detail focus.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.cs`, connection/reconciliation state above, and `wwwroot/js/fc-connectivity.js` (new) — use actual browser offline evidence, stable epochs, fallback presentation and recovery after successful reconciliation even without changed rows; preserve transport budgets.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs`, lifecycle contracts/service above, command emitters, `State/PendingCommands/PendingCommandPollingCoordinator.cs`, and `Infrastructure/EventStore/EventStoreCommandClient.cs` — preserve distinct outcomes, stable acceptance budget, post-await deadline checks, zero preaccept retries and one postack retry preserving MessageId.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcScopeBlocked.razor`, Home/layout and command-denial consumers — integrate actual loading outcomes and activation-owned AM-26; retain adjacent stories' speech contracts.
- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/`, `tests/Hexalith.FrontComposer.SourceTools.Tests/`, and `tests/e2e/specs/projection-command-announcements.spec.ts` (new) — verify matrix, copy/channel/count, timing races, focus, redaction and grid regressions; update snapshots.

**Acceptance Criteria:**
- Given Submitting at 0ms and Acknowledged at 100ms, when time reaches 349/350ms then silence becomes AM-11; when Syncing precedes Confirmed within 250ms then the exact sequence is [AM-11, AM-13]. Given connection and load/filter groups, when 249/250ms elapse then equivalent trailing-window assertions pass.
- Given pending work, when time crosses 1,999/2,000, 9,999/10,000 and 119,999/120,000ms then canonical states/messages occur once, status polling stays silent at 1s, exhaustion ends admission/polling, and later evidence requires new correlation. Given postack transient failure, when 249/250ms elapse then exactly one same-identity retry occurs.
- Given reconnect/filter/grid regression scenarios, when exercised then retries cap at 30s, closed restart is within 10s, fallback polls every 15s across at most eight lanes, recovery notice lasts 3s with silent expiry, and resettable debounce, 500-row virtualization, 10,000 cap, labelled detail, column priority above 15 and icon/shape plus text remain valid.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Individually build Shell.Tests and SourceTools.Tests with `dotnet build <project.csproj> -c Debug -m:1`; run built xUnit v3 assemblies with `DiffEngine_Disabled=true` and `-class` for affected classes. Run `npm --prefix tests/e2e run typecheck`, focused Chromium announcement/grid/lifecycle/authorization specs and `test:a11y`. Record exact blockers/fallbacks; complete independent review before done.
