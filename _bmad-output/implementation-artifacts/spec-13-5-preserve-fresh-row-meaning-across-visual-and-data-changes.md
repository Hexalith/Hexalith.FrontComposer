---
title: 'Story 13.5: Preserve Fresh-Row Meaning Across Visual and Data Changes'
type: 'feature'
created: '2026-10-12'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Generated grids show a generic "New item…" cue with its own live region, so operators cannot tell which row their command changed, remounting a view can repeat speech, and forced-colors meaning leans on authored color.

**Approach:** Keep FC-NIP publication unchanged; make each generated cue name its row, speak AM-21 once per tenant + user + transition through the shared polite node, keep every removal silent, and prove scope, data-change, forced-colors, reduced-motion and ten-second behavior deterministically.

## Boundaries & Constraints

**Always:** Apply `epics.md` Story 13.5 and `ux-design.md` AM-21, SS-48, AE-07/AE-08, plus VC-06 and RM-1 fresh-row rows. AM-21 EN is exactly "Updated row: {0}." and FR "Ligne mise à jour : {0}.", where `{0}` is the row identifier generated grids already use for row names (the EntityKey). Speak on surface `projection:{ViewKey}` with one group per transition and no focus move. A transition is tenant + user + EntityKey + MessageId, independent of ViewKey or view mount. Preserve resolver eligibility, the once-per-MessageId decision, first-wins provenance and TTL, scope clear-before-render, DW-679, Story 13.4 one-node and precedence behavior, and standalone `FcNewItemIndicator` behavior when no entry is supplied.

**Decision (human, 2026-10-12):** Keep the delivered grid-adjacent cue, one per entry above the grid, with its existing dismissal on materialization, filter change, expiry and scope change. Its visible text becomes the canonical AM-21 copy with an info icon; SS-48 "row decoration" is met by naming the row, not by in-row placement. The full-size spec is kept.

**Never:** Change `PendingCommandOutcomeResolver`, `INewItemIndicatorStateService`, or coordinator semantics; add a second live region inside generated grids; announce on duplicate, remount, expiry, dismissal, filter, paging or scope removal; make color, background image or animation the only carrier; edit archived `artifacts/epic-9-final-*` bundles or `9-8-live-acceptance.md`; close OI-16, G-4, FLUENT-APP-1 or Product approval.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Appear | Eligible Material entry, grid already rendered | Cue with icon and row-naming text; one AM-21 in the shared node; focus unchanged | Missing tenant/user ⇒ no cue, no speech |
| Re-view | Same transition remounted or under another ViewKey | Cue renders; no second AM-21 | Claims pruned only after the ten-second window |
| Data change | Filter, page, requery, dismiss, later message | First entry kept; TTL not extended; removal silent; no deletion wording | N/A |
| Scope | Tenant/user cleared or changed | Cue and claim absent before the new scope renders; silent in both scopes | N/A |
| Expiry | Fake time 9.999 s then 10 s | Present, then removed silently | N/A |
| Modes | Forced colors, reduced motion | Text, icon and system border remain; no transition, animation or background image | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor{,.cs,.css}` -- public FcTbl component: div with its own `role=status`/`aria-live=polite`, `Text`/`AriaLabelOverride`, keys `NewItemIndicatorText`/`NewItemIndicatorAriaLabel`; CSS brand border plus `color-mix` background, 200 ms transition disabled under reduced motion; no forced-colors rule or icon. Shared-mode precedent: `FcSlowQueryNotice` (`SharedStatusOwner`; renders `FcSurfaceStatus Surface="projection:"+ViewKey ExclusiveOwner` only when standalone).
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`, `NewItemIndicatorEntry.cs` -- first-wins `(ViewKey, EntityKey)`, `TimeProvider` ten-second timer, scope clearing in `Add`/`Snapshot`; entry has ViewKey, EntityKey, MessageId, CreatedAt. Do not change.
- `src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs` -- dedupe is per surface/group only, hence a separate ledger. Use `immediate: true` (not `terminal`: closed groups suppress later `limit:`/`stale:` speech). `Cancel` silently restores already-spoken states. Avoid group prefixes `page:`, `scope:`, `limit:`, `stale:`, `lifecycle:`, `slow-query:`, `blocked:`.
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` (~263, ~441) -- scoped coordinator and indicator registrations.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` -- `RenderNewItemIndicators` (~1739) emits bare keyed indicators; surface `"projection:" + _viewKey` (~1074) and its `FcSurfaceStatus` (~2022); subscription → `StateHasChanged` (~1262) already avoids waiting for unrelated renders; dismissal paths (~1724, ~1782) unchanged. Row names use `_itemKeyAccessor(item).ToString()` (`ProjectionRoleBodyEmitter.cs` ~609).
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources{,.fr}.resx` -- `AmNN` key style (`Am08SlowQuery`); FR spaces before ':'.
- `src/Hexalith.FrontComposer.Shell/PublicAPI.FcTbl.Shipped.txt` -- 13.4 recorded new notice parameters here.
- Shell tests: `Components/DataGrid/FcNewItemIndicatorTests.cs`, `FcNewItemIndicatorLaneIntegrationTests.cs` (standalone; must stay green unchanged); `Generated/Epic9CompositionTests.cs` (`ConfigureFakeTime`, `RenderGrid`), `CounterStoryVerificationTests.cs` (~561) assert old generated copy; `Generated/GeneratedProjectionAnnouncementTests.cs` shows the one-polite-node count.
- `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts`, `tests/e2e/scripts/validate-epic9-artifacts.mjs` (+`.test.mjs`) -- pin role, aria-live, label and copy at browser evidence `schemaVersion` 1/story `9.8`; CI `epic9-live-acceptance` runs them. `tests/e2e/specs/specimen-accessibility.spec.ts` (~294–428) -- scoped-CSS proof injection and forced-colors/reduced-motion context on the `type` specimen (screenshot baselines exist; add no visible specimen content).
- Governance: `ShellTypeOrganizationGovernanceTests`, `FcShellResourcesTests`, `AnalyzerPolicyGovernanceTests` (CA1707 seal), `FcTblPackageBoundaryTests`.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.FrontComposer.Shell/Services/Announcements/FreshRowAnnouncementLedger.cs` (new, internal, scoped) and `ServiceCollectionExtensions.cs` -- `TryClaim(entry)` is true once per tenant + user + EntityKey + MessageId, false for blank scope, pruning claims older than ten seconds -- ViewKey-independent dedupe without public contract changes.
- [ ] `FcNewItemIndicator.razor{,.cs,.css}`, `PublicAPI.FcTbl.Shipped.txt` -- add documented `Entry` and `SharedStatusOwner`. With `Entry`: decorative `FcFluentIcons.InfoCircle16()`, text from `Text` or `Am21UpdatedRow`, no live role or label; after rendering a new MessageId, announce AM-21 immediately when claimed; cancel its group on dispose; own `FcSurfaceStatus` only when not shared. Without `Entry`: legacy semantics. CSS: forced-colors `CanvasText` inline-start border, no `forced-color-adjust: none`, reduced motion unchanged.
- [ ] `FcShellResources.resx`, `FcShellResources.fr.resx` -- add `Am21UpdatedRow`.
- [ ] `RazorEmitter.cs` -- pass `Entry` and `SharedStatusOwner=true`; refresh affected `.verified.txt` snapshots and emitter assertions.
- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/` -- ledger and component tests; update generated-copy assertions; cover every I/O row with fake time, polite-node and announcement counts, two views, unrelated-render absence, and suppressed Unknown/NoOp/Delete/Rejected/NeedsReview/server-allocated outcomes.
- [ ] `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts`, `tests/e2e/scripts/validate-epic9-artifacts{,.test}.mjs` -- assert visible AM-21, no cue live role, one shared node, no tab-switch repeat; emit evidence v2/story 13.5; validator keeps v1 pins and accepts v2.
- [ ] `tests/e2e/specs/specimen-accessibility.spec.ts` -- scoped indicator proof: border survives, no background image, animation or transition.
- [ ] `docs/reference/components/datagrid.md` -- append the new parameters and shared-channel behavior; keep fixture-guarded sentences.

**Acceptance Criteria:**
- Given a standalone `FcNewItemIndicator` without `Entry`, when rendered, then its role, live, label and copy are unchanged.
- Given this story is complete, when readiness is evaluated, then OI-16, G-4, FLUENT-APP-1 and Product approval remain open.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Pruning claims after ten seconds cannot re-announce: a claim happens after `Add`, expiry counts from `Add`, and the resolver decides once per MessageId, so no active entry outlives its claim.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug`, then run the built test assembly with `-class` per touched class -- expected: pass.
- Same pattern for `tests/Hexalith.FrontComposer.SourceTools.Tests` snapshot and emitter classes -- expected: pass.
- `cd tests/e2e && node --test scripts/validate-epic9-artifacts.test.mjs` -- expected: pass.
- `cd tests/e2e && npx playwright test specs/specimen-accessibility.spec.ts --project=chromium` -- expected: pass, or record the exact environment blocker.
- From the repository root, `./eng/run-epic9-live-proof.sh` -- expected: pass, or record the exact environment blocker.
