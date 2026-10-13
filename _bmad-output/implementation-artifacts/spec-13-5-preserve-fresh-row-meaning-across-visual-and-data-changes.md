---
title: 'Story 13.5: Preserve Fresh-Row Meaning Across Visual and Data Changes'
type: 'feature'
created: '2026-10-12'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '985d3e70ac8e9d5492fa857bc3a31208a9543670'
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
- `src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs` -- dedupe is per surface/group only, hence a separate ledger. Existing `Announce`/`Cancel` operations and their precedence must retain their behavior. Add internal fresh-row transient operations: immediate speech, silent cancellation of its own group, and selective group retirement. Do not close a terminal group or cancel unrelated projection timers. Public coordinator contract and existing-family semantics remain unchanged. Avoid group prefixes `page:`, `scope:`, `limit:`, `stale:`, `lifecycle:`, `slow-query:`, `blocked:`.
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` (~263, ~441) -- scoped coordinator and indicator registrations.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` -- `RenderNewItemIndicators` (~1739) emits bare keyed indicators; surface `"projection:" + _viewKey` (~1074) and its `FcSurfaceStatus` (~2022); subscription → `StateHasChanged` (~1262) already avoids waiting for unrelated renders; dismissal paths (~1724, ~1782) unchanged. Row names use `_itemKeyAccessor(item).ToString()` (`ProjectionRoleBodyEmitter.cs` ~609).
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources{,.fr}.resx` -- `AmNN` key style (`Am08SlowQuery`); FR spaces before ':'.
- `src/Hexalith.FrontComposer.Shell/PublicAPI.FcTbl.Shipped.txt` -- 13.4 recorded new notice parameters here.
- Shell tests: `Components/DataGrid/FcNewItemIndicatorTests.cs`, `FcNewItemIndicatorLaneIntegrationTests.cs` (standalone; must stay green unchanged); `Generated/Epic9CompositionTests.cs` (`ConfigureFakeTime`, `RenderGrid`), `CounterStoryVerificationTests.cs` (~561) assert old generated copy; `Generated/GeneratedProjectionAnnouncementTests.cs` shows the one-polite-node count.
- `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts`, `tests/e2e/scripts/validate-epic9-artifacts.mjs` (+`.test.mjs`) -- pin role, aria-live, label and copy at browser evidence `schemaVersion` 1/story `9.8`; CI `epic9-live-acceptance` runs them. `tests/e2e/specs/specimen-accessibility.spec.ts` (~294–428) -- scoped-CSS proof injection and forced-colors/reduced-motion context on the `type` specimen (screenshot baselines exist; add no visible specimen content).
- Governance: `ShellTypeOrganizationGovernanceTests`, `FcShellResourcesTests`, `AnalyzerPolicyGovernanceTests` (CA1707 seal), `FcTblPackageBoundaryTests`.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Shell/Services/Announcements/FreshRowAnnouncementLedger.cs` (new, internal, scoped) and `ServiceCollectionExtensions.cs` -- `TryClaim(entry)` is true once per tenant + user + EntityKey + MessageId, false for blank scope, pruning claims older than ten seconds -- ViewKey-independent dedupe without public contract changes.
- [x] `FcNewItemIndicator.razor{,.cs,.css}`, `PublicAPI.FcTbl.Shipped.txt` -- add documented `Entry` and `SharedStatusOwner`. With `Entry`: decorative `FcFluentIcons.InfoCircle16()`, text from `Text` or `Am21UpdatedRow`, no live role or label; bind the entry to its resolved originating scope, suppress blank provenance before claiming, defer claims while a containing page tab is hidden, announce AM-21 immediately when claimed through the fresh-row transient path, and silently retire its group on removal/dispose; for direct entries, enforce a ten-second lifecycle without shortening the state service lifetime of generated entries; own `FcSurfaceStatus` only when not shared. Without `Entry`: legacy semantics. CSS: forced-colors `CanvasText` inline-start border, no `forced-color-adjust: none`, reduced motion unchanged.
- [x] `FcShellResources.resx`, `FcShellResources.fr.resx` -- add `Am21UpdatedRow`.
- [x] `RazorEmitter.cs` -- pass `Entry` and `SharedStatusOwner=true`; refresh affected `.verified.txt` snapshots and emitter assertions.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/` -- ledger and component tests; update generated-copy assertions; cover every I/O row with fake time, polite-node and announcement counts, two views, unrelated-render absence, and suppressed Unknown/NoOp/Delete/Rejected/NeedsReview/server-allocated outcomes.
- [x] `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts`, `tests/e2e/scripts/validate-epic9-artifacts{,.test}.mjs` -- assert visible AM-21, no cue live role, one shared node, no tab-switch repeat; emit evidence v2/story 13.5; validator keeps v1 pins and accepts v2.
- [x] `tests/e2e/specs/specimen-accessibility.spec.ts` -- scoped indicator proof: border survives, no background image, animation or transition.
- [x] `docs/reference/components/datagrid.md` -- append the new parameters and shared-channel behavior; keep fixture-guarded sentences.

- [x] `SurfaceAnnouncementCoordinator.cs` -- add internal fresh-row transient publish/withdraw helpers, leaving existing public operations and all other announcement families behaviorally unchanged. Immediate transient publication must preserve unrelated pending timers; withdrawing a fresh group restores status text silently and removes only that group/history. Cover pending stale/limit/page outcomes and repeated fresh-row cycles.
- [x] `Components/Layout/FcPageTabs.razor` and a new internal panel-visibility context if needed -- cascade effective active-panel exposure (including nested inactive ancestors), without changing retained panel content or tab/public contracts. `FcNewItemIndicator` uses this to leave hidden transitions unclaimed until first visible delivery; visibility toggles after delivery do not repeat speech.
- [x] `GeneratedFreshRowAnnouncementTests.cs` -- complete a real page result and drive the generated filter/requery action; assert original provenance/TTL and silent cue removal. `FcFreshRowIndicatorTests.cs` -- add direct resolved-scope swaps, invalid identity, direct expiry/remount, hidden-first-visible delivery and pending-status cancellation regressions.
- [x] `epic-9-fresh-row-acceptance.spec.ts` -- count AM-21 only when the shared status node is exposed (no hidden/inert/aria-hidden ancestor and visible layout); prove hidden grid receives zero speech before activation and exactly one on first activation; inspect the actual cue text/icon/system border under forced colors/reduced motion without adding specimen content.

**Acceptance Criteria:**
- Given a standalone `FcNewItemIndicator` without `Entry`, when rendered, then its role, live, label and copy are unchanged.
- Given this story is complete, when readiness is evaluated, then OI-16, G-4, FLUENT-APP-1 and Product approval remain open.

### Review Findings

Code review of `985d3e70..99d31eda` (2026-10-12): Blind Hunter (BH), Edge Case Hunter (EC), Verification Gap (VG) and Acceptance Auditor (AA) layers; no layer failed.

- [x] [Review][Patch] The Epic 9 browser spec fails in CI's default browser job; resolved 2026-10-12 to run it only in the Aspire live-proof lane and skip it under the stub server — high (VG-O2 + BH-15) [tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts:9]. `browser-regressions-linux` runs `npx playwright test --workers 4` over every spec against the stub server, whose confirm delay defaults to 6500 ms (`playwright.config.ts:5`). Under that delay, the second overlapping update settles after the first cue materializes and becomes its own eligible transition. `expect.poll(speechCount).toBe(3)` (`tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts:204`) then receives 4. The reviewer reproduced this on Chromium, and the spec's own diagnostic run recorded the same failure. Only the Aspire live proof passes the exact count. Choose one: run this spec only in the live-proof lane; derive the expected count from the transitions actually observed; or make the overlap deterministic in the stub configuration.
- [x] [Review][Patch] Ownership transfer now silences the transferred status node for every announcement family, and a Story 13.4 regression test fails at HEAD — high (VG-O1 + AA-3) [src/Hexalith.FrontComposer.Shell/Components/Rendering/FcSurfaceStatus.razor:95,108]. `FcSlowQueryNoticeTests.DirectSlowAndCapNoticesKeepTerminalThenNewRequestUsesOnePersistentStatus` expects 1 polite node and finds 0; it failed on a re-run during review. The transfer callback forces `_suppressSpeech = true`, and non-owner delivery captures silence. Neither is limited to the fresh-row family, so the 13.4 one-node contract regresses and Gate 3a would fail. The story's verification lane never ran the `FcSurfaceStatus` consumer classes. Restore the 13.4 expectation unchanged while keeping the 13.5 tests green. If both cannot hold, escalate as a decision.
- [x] [Review][Patch] A repeated, already-delivered non-terminal identity is spoken again while a fresh-row overlay is current — medium (EC-4 + AA-2) [src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs:205-209]. The delivered-identity early return still compares the raw `CurrentGroup` (now the fresh group). A re-rendered `load:` identity therefore re-queues. `Flush` then finds that same group through `NonFreshOwner`, so no barrier applies, and it re-speaks "Loading…" over AM-21. Also accept `NonFreshOwner(owner)` key/identity as current in that guard.
- [x] [Review][Patch] Fresh-row withdrawal can restore a group that `Cancel` would never restore — medium (EC-5 + EC-9) [src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs:361-368]. When `PreviousGroup` was cancelled under the overlay, the fallback picks any `IsRestorable` group by `DeliveryOrder`. That includes old page/load groups, which `Cancel` restores only through its lifecycle/failure/stale/cap selection. Outdated text and an outdated precedence owner become current. Reuse `Cancel`'s restoration selection for the fallback.
- [x] [Review][Patch] Changing `Entry` while a claim is pending loses the new entry's AM-21 — medium (BH-1 + EC-1) [src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor.cs:150-156]. `OnParametersSet` keeps `_pendingAnnouncement` false while `_announcementScheduled` is set. The continuation then returns on `Entry != captured` without re-arming. This is reachable through the public direct `Entry` parameter. Re-arm and re-render in that early-return branch.
- [x] [Review][Patch] The cross-view/remount and panel toggle-back dedupe assertions run before the deferred claims they guard — medium (AA-8a/b) [tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcFreshRowIndicatorTests.cs:112-116]. Claims run after `Task.Yield()`, but the count is asserted immediately after `Render`. The test can pass whether or not a second claim happens. Flush the renderer dispatcher before asserting, in both this test and `HiddenPanelDefersFirstSpeechIncludingNestedInactiveAncestors`.
- [x] [Review][Patch] No test covers silence for non-fresh messages on panel reactivation or ownership transfer — medium (VG-1 + BH-18) [src/Hexalith.FrontComposer.Shell/Components/Rendering/FcSurfaceStatus.razor:107]. Every reactivation test replays a fresh-row message, which is already silenced by `IsFreshRow`. Deleting `panelReactivated ||` fails nothing. Add a route-backed `FcPageTabs` case: a terminal non-fresh message is spoken while visible, then switch away and back, and assert retained text with `aria-live="off"`.
- [x] [Review][Patch] Uncontrolled `FcPageTabs` selection now drives panel exposure, and no test covers it — medium (VG-2 + BH-9 + AA-4) [src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageTabs.razor.cs:107,382]. `_localSelectedTabId` feeds `FluentTabs.ActiveTabId`, deferred rendering and `PagePanelVisibility`. Every tabs test sets `ActiveTabId` or `ModuleRoute`. Add an uncontrolled case: select a later tab and assert that its panel is exposed and that its deferred content renders.
- [x] [Review][Patch] The generated removal-silence observer only counts AM-21 copy — low (AA-8c) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedFreshRowAnnouncementTests.cs:289-294]. `ObserveFreshSpeech` keeps only messages starting with "Updated row:". A non-silent restoration, or deletion wording during materialization, filter or dismissal, cannot fail these tests. Record every non-silent revision in the removal assertions.
- [x] [Review][Patch] The reserved `fresh-row:` group semantics are undocumented on the public coordinator interface — low (BH-2 + AA-1) [src/Hexalith.FrontComposer.Shell/Services/Announcements/ISurfaceAnnouncementCoordinator.cs:18-22]. For `fresh-row:` groups, `Announce` ignores `terminal`/`immediate` and normal dedupe, and `Cancel` silently withdraws already-spoken text. The interface still says `Cancel` "cancels an unspoken intermediate message". Document the reserved family on `Announce` and `Cancel`.
- [x] [Review][Patch] The `datagrid.md` additions are in the wrong section and incomplete — low (BH-12 + AA-6 + BH-13) [docs/reference/components/datagrid.md:139-140,166-168]. Both paragraphs follow the `Related` link list instead of Parameters/Accessibility. The docs also omit four points: with `Entry`, `Text` changes only the visible copy while speech stays canonical; `AriaLabelOverride` is ignored; hidden-panel deferral applies only inside `FcPageTabs`; and generated cues no longer carry `role`/`aria-live`/`aria-label` or the old copy. Keep fixture-guarded sentences.
- [x] [Review][Patch] Two Shell governance tests fail at HEAD; found while verifying the fixes above — high [src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor:23, tests/Hexalith.FrontComposer.Shell.Tests/Architecture/FatalExceptionGuardGovernanceTests.cs:11]. The entry-aware cue's `FluentIcon` declares no `Color`, which the Fluent UI v5 conformance test requires, and the ledger's fail-closed scope read adds a 79th shared fatal catch filter without raising the ratchet from 78. The icon now uses `Color.Primary`, matching the cue's brand stroke, and the ratchet is 79.
- [x] [Review][Defer] The direct entry-aware path inserts its exclusive live region in the same render as the cue [src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor:16-21] — deferred: maybe-false, medium if true (BH-4). `FcSurfaceStatus` renders only once `_scopeResolved` is true, and AM-21 follows one yield later. Blazor Server may apply both batches together, contradicting the repository's AM-31 "region exists before its text arrives" rule. Generated grids are unaffected. To settle it, run an assistive-technology or accessibility-tree browser check of a direct (`SharedStatusOwner=false`) cue.
- [x] [Review][Defer] An unrelated scheduler test is flaky [tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs:1036] — deferred: pre-existing (VG-O3). `ReadyReplayBehindNewlyStalledReplayStillStartsWithinBudget` failed once in a full default-lane run and passed 3/3 in isolation. This diff does not touch it.

Rejected:

- BH-3 (false): AM-21 over a mounted failure is not a 13.4 precedence reversal. Failure-over-limit/stale guards still resolve the underlying owner through `NonFreshOwner`; the transient output was an explicit R2-BH-6 decision.
- BH-5 (low): only `FcPageTabs` cascades exposure. Generated grids in other retained-hidden containers are uncommon, and the fix needs a new public visibility contract.
- BH-6 (low): the per-render cascade notification costs a few cheap `OnParametersSet` calls. The proposed cached instances would not change Blazor change detection.
- BH-7 (maybe-false, low): fire-and-forget `InvokeAsync` matches the Shell idiom, and no failing re-entrant path was shown.
- BH-8 + AA-5 (low): the added `Dispose()` is the necessary cleanup surface and is intentionally baselined. A virtual dispose pattern would add public surface for no demonstrated need.
- BH-10 (low): an A→B→A switch with no indicator render in B leaves an A-valid cue in A for less than ten seconds. A scope subscription would add complexity.
- EC-8 + EC-12 (false): `FrontComposerShell` keys its body by `ScopeBoundary.Generation`, so every descendant remounts before a new scope renders.
- BH-11 (reject, spec edit): the frozen intent fixes `{0}` = EntityKey and the canonical "Updated row" copy.
- BH-14 (low): hidden consumption is detected indirectly, because the first-activation poll would stay at 0. The field alias is redundancy, not error.
- BH-16 (false): each negative case changes one field of a passing v2 fixture, so the rejection's cause is pinned.
- BH-17 (low): reflective internal reads in tests are a maintenance cost with no demonstrated defect.
- BH-19 (low): only a hypothetical deferring adopter decorator loses the AsyncLocal marker.
- BH-20 + AA-7 (reject, spec edit): the spec status, ephemeral `/tmp` evidence and workflow notes would require editing this spec. Sprint status already reads `review`.
- EC-2 (low): same as the earlier R2-BH-4 rejection (malformed `DateTimeOffset.MaxValue` input).
- EC-3 + EC-10 (low): only future-dated direct `CreatedAt` input outlives the claim window. Production uses the clock.
- EC-6 + EC-11 (false): retained inactive panels were never exposed to assistive technology before 13.5 either, so no previously spoken outcome is lost.
- EC-7 (low): an unknown or disabled selected id is an adopter error or a route fallback transient.
- AA-8d (low): the cue makes no focus calls, and the component test already pins zero JS focus invocations.
- AA-8e (low): the forced-colors-only transition is non-essential, and meaning never depends on it.
- AA-9 (reject, spec edit): the frozen FR literal uses U+0020 before ':'. Switching to U+00A0 would renegotiate frozen copy.

Re-review of the fix delta (`8ee70a64`, 2026-10-12): Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor layers (IDs prefixed `RR-`); no layer failed.

- [x] [Review][Patch] Withdrawal restores `Cancel`'s fallback silently and skips its delivery bookkeeping — medium (RR-BH-1 + RR-EC-1 + RR-VG-O1 + RR-AA-2) [src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs:371-387]. Scenario: a cap first observed behind a page failure, the failure cancelled while a fresh-row overlay is current, then the overlay withdrawn. `Cancel` would speak the cap; withdrawal shows it silently and never marks it delivered. A restored stale candidate also keeps its pending timer, and its `CurrentIdentity` differs from `Cancel`'s `"stale"`. Share one restore helper (selection, identity and speech) between both paths.
- [x] [Review][Patch] Stacked overlays record a fallback underlying owner and restore an outdated page message — medium (RR-EC-4 + RR-AA-1) [src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs:329,677-679]. Sequence: terminal `load:first` and `load:second`, overlay A, `Cancel(load:second)`, overlay B, then withdraw A and B. The status ends on "Loaded first page.", while a plain `Cancel(load:second)` clears it. B takes its `UnderlyingGroup` from `NonFreshOwner`'s any-restorable fallback; it should inherit the current overlay's underlying group.
- [x] [Review][Patch] The withdrawal fallback order is unpinned for stacked overlays — medium (RR-VG-2 + RR-BH-2) [tests/Hexalith.FrontComposer.Shell.Tests/Services/Announcements/SurfaceAnnouncementCoordinatorTests.cs:83-94]. Deleting the underlying-group step, or moving it ahead of the remaining overlays, passes every test. Add two facts: overlays stacked over a blocked terminal and withdrawn oldest-first (a later cap stays silent), and three overlays over a terminal load withdrawn middle then newest ("Updated row: A.").
- [x] [Review][Patch] An A→B→A return under an overlay is unpinned — medium (RR-VG-3) [src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs:210-213]. Dropping the `LastIdentity == identity` comparison passes every test, swallows the return to "Loading." and later restores the outdated "Refreshing.". Add the fact.
- [x] [Review][Patch] No test transfers ownership while a fresh-row message is current — medium (RR-VG-1 + RR-AA-4 + RR-EC-6 + RR-BH-4a) [src/Hexalith.FrontComposer.Shell/Components/Rendering/FcSurfaceStatus.razor:107]. The non-owner fresh-row clause is the only guard against exposing AM-21 a second time after a handoff. Add a case where AM-21 is delivered live to a non-owner, ownership then transfers, and the promoted node shows the text with `aria-live="off"`.
- [x] [Review][Patch] The materialization removal test reads its baseline before that iteration's deferred claim lands — medium (RR-BH-6 + RR-EC-2) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedFreshRowAnnouncementTests.cs:171-172]. Wait for the iteration's AM-21 before capturing `before`.
- [x] [Review][Patch] The reactivation test never checks that the node speaks again — low (RR-BH-5) [tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcFreshRowIndicatorTests.cs:279-300]. Announce a new message after reactivation and assert it is polite.
- [x] [Review][Patch] The coordinator interface remarks overstate the contract — low (RR-BH-3 + RR-AA-3) [src/Hexalith.FrontComposer.Shell/Services/Announcements/ISurfaceAnnouncementCoordinator.cs:18-34]. Three problems:
  - "Once" holds only while the group exists.
  - Withdrawal does not always restore "the status it covered".
  - Adopter coordinators keep ordinary behaviour.

  Reserve the prefix on the interface and attribute the overlay behaviour to the built-in coordinator.
- [x] [Review][Patch] `datagrid.md` contradicts itself and exposes internal IDs — low (RR-BH-10 + RR-BH-11 + RR-BH-12c + RR-BH-13 + RR-AA-7) [docs/reference/components/datagrid.md:119-128,150-160]. Fixes:
  - Limit the "render status regions" bullet to cues without `Entry`.
  - Replace "AM-21" with plain wording.
  - Name `aria-live` and the `data-testid` hook.
  - Say that `SharedStatusOwner=true` needs a status node the page already provides.
- [x] [Review][Defer] After any silent revision, the `aria-live` change from off to polite and the new text land in one update [src/Hexalith.FrontComposer.Shell/Components/Rendering/FcSurfaceStatus.razor:9,124] — deferred: maybe-false, medium if true. This is the pre-existing silent-revision design, not specific to this delta. Settle it with NVDA, JAWS and VoiceOver on a silent restore followed by a new message.
- [x] [Review][Defer] No migration note tells adopters that generated cues dropped `role`, `aria-live`, `aria-label` and the "New item" copy [docs/migrations/] — deferred: pre-existing (introduced by `99d31eda`); it needs the target release's migration file.

Rejected (re-review):

- RR-EC-3 (false): a status node inserted with its text already present is not announced (AM-31). Story 13.4's one-node test requires a polite node after transfer, and 13.4 had no transfer-time silencing.
- RR-EC-5 (false): restoring a still-mounted cue's text is the pinned intent (`InvalidatedOldCapIsNeverRestoredThroughFreshPreviousOrFallback`). The displaced outcome was cancelled, and `Cancel` never restores it either.
- RR-BH-7 (low): the untested re-arm variants (hidden panel, another view, `null`, A→B→A) reuse the existing deferral and equality paths, and no failing case was shown.
- RR-BH-8 + RR-EC-7 + RR-AA-6 (low): CI gating is correct; the default job skips the spec, and the live lane runs it and requires `skipped="0"`. Only manual stub-host runs of this spec still hit the count, and a precise flag would change the live-proof script contract.
- RR-BH-9 (reject, decided): dropping the spec from the stub job, Firefox and WebKit included, follows the user's live-only decision of 2026-10-12.
- RR-BH-12a/b (low): the front-matter `reviewed` date marks a whole-page review, and the validator passes. The direct-caller warning would document an unverified deferred item.
- RR-BH-14 (low): the ledger's free-text evidence chain predates this change, and restructuring it is out of scope.
- RR-BH-15 (low): pinning fatal-filter sites instead of a count is a governance redesign with no demonstrated miss.
- RR-BH-16 (low): the uncontrolled-tabs test fails on both regressions it targets. Asserting the previous panel's exposure would need a second status node.
- RR-AA-5 (low): the reactivation guard is pinned through controlled selection, and `RouteBackedHiddenPanelDefersFirstDeliveryAndRouteReactivationIsSilent` already exercises route-driven visibility.
- RR-AA-note (no action): the live proof that predates `Color.Primary` re-runs in CI on the pushed commit, and its check asserts only that the icon is visible.


### Continuation review patches (2026-10-13)

- [x] [Review][Patch] C-BH-2: remove a direct cue when the deferred eligibility check or claim rejects its originating scope; keep an eligible duplicate cue visible.
- [x] [Review][Patch] C-BH-7: count fresh-row revisions only on the generated projection's status node, and assert its canonical text.
- [x] [Review][Patch] C-BH-10: prove direct/state-owned expiry while hidden and expiry before the deferred first claim with deterministic time and no speech.
- [x] [Review][Patch] C-BH-11: document the active ten-second claim window and the resolver's independent once-per-MessageId guard.
- [x] [Review][Patch] C-VG-1: prove both scope getter failures suppress the direct cue and speech, reject retained entries after recovery, and permit a new recovered transition.

C-BH-3 was disproved by retained-component regressions for exclusive and nonexclusive owners; both pass without changing `FcSurfaceStatus`. Existing manual-assistive-technology deferrals remain unchanged. All three required review layers completed; every finding has an individual verdict in the triage log below.

## Implementation Notes

This is a step-03 implementation handoff inside an active bmad-build workflow. The parent owns orchestration and review; implement the spec directly without starting or rendering another build workflow.

## Spec Change Log

- Review iteration 1: corrected the non-frozen plan for originating-scope binding, direct-entry lifecycle/validation, panel exposure, silent transient withdrawal, bounded fresh-group history and real filter/page completion coverage. Avoid the demonstrated old-scope leak, hidden-only first delivery, pending-stale speech on removal and retained fresh history. KEEP canonical EN/FR copy, icon, grid-adjacent placement, legacy standalone localization-only compatibility, scoped view-independent claim identity, unchanged resolver/state service/public coordinator behavior, forced-colors/reduced-motion CSS, generator wiring and snapshots, v1 evidence compatibility, and all existing passing regression tests. Implement the transient support as internal operations; retain all existing public coordinator semantics and regression expectations.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| BH-1 | high | bad_spec | A resolved tenant/user swap keeps an unchanged direct Entry visible; OnParametersSet checks only scope validity and entry equality, not originating scope. |
| BH-2 | medium | bad_spec | Cancel restores an undelivered stale candidate as a non-silent revision; the existing restoration test exercises only an already-spoken limit. |
| BH-3 | medium | patch | Add validates view/entity only; blank MessageId reaches Announce identity validation after TryClaim, so an accepted entry can break rendering. |
| BH-4 | medium | bad_spec | Direct Entry callers have no state-owned expiry and can remount an expired transition after claim pruning. |
| BH-5 | high | bad_spec | FcPageTab retains inactive rendered content. The component claims a transition while the status node is hidden, consuming its only accessible delivery. |
| BH-6 | medium | bad_spec | The browser observer counts polite nodes in hidden panels, so hidden delivery is accepted as speech. |
| BH-7 | false | reject | Immediate terminal-outcome speech intentionally cancels superseded intermediate page speech under epic-13 canonical coalescing. A pending Page ready message need not follow its newer row outcome; cancellation alone is not a defect. |
| BH-8 | medium | bad_spec | A new GUID group is retained after every cue cancellation. The new fresh-row family needs bounded retirement without changing other families or public operations. |
| BH-9 | false | reject | The scoped CSS probe exercises the compiled indicator selector and fails if it is absent. The actual cue/icon and scoped attributes are covered by component rendering; no missing or invisible production icon was demonstrated. The new visible live proof will additionally inspect the real icon under modes. |
| BH-10 | medium | bad_spec | The new test calls dismissal directly and scrolls without completing a page load, leaving the fresh-row behavior across real generated filter/requery and page completion unproved. |
| EC-1 | medium | patch | Same confirmed blank-message rendering failure as BH-3; the state service accepts this reachable input. |
| EC-2 | high | bad_spec | Same confirmed originating-scope omission as BH-1; validity remains true after a resolved scope swap. |
| EC-3 | medium | bad_spec | Same confirmed retained fresh-group history as BH-8; the proof counted 100 cancelled groups. |
| EC-4 | medium | bad_spec | Same confirmed pending-stale restoration as BH-2; the proof observed a non-silent revision during cue cancellation. |

Verification-gap reviewer: no grounded verification gaps reported. Findings were classified individually before grouping. Shared roots: BH-1/EC-2; BH-2/EC-4; BH-3/EC-1; BH-8/EC-3. Plan gaps require re-derivation, so lower patch entries are carried into the amended implementation tasks.

Second review: every finding was individually classified after all layers returned. The code at each affected location was checked; no unchanged-location finding is carried from the first implementation.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| R2-BH-1 | high | patch | Equivalent record copies bypass reference-only origin binding. Preserve the demonstrated origin across equal transition copies/remounts with bounded metadata, without changing producer semantics. |
| R2-BH-2 | high | patch | Exclusive status ownership ignores panel exposure, so a visible same-view cue publishes into the hidden first owner. Status leases must follow the existing visibility context. |
| R2-BH-3 | medium | patch | A forwarding decorator takes the public immediate path and cancels unrelated timers; default fresh-family dispatch and delivered silence must survive forwarding while retaining other-family behavior. |
| R2-BH-4 | low | reject | DateTimeOffset.MaxValue cannot support a ten-second expiry and throws for this unsupported direct timestamp. Production provenance is the clock timestamp; extra timestamp/timer guards for this rare malformed caller input exceed a direct correction. |
| R2-BH-5 | false | reject | The probe publishes the same MessageId into a second view nine seconds later, bypassing the sole eligible publisher. PendingCommandOutcomeResolver fixes target.ViewKey, records a once-per-MessageId decision before Add, and validates observation provenance against the current clock; valid production publication cannot extend the same transition into another Add lifetime. Direct alternate-view entries expire by the original CreatedAt. |
| R2-BH-6 | medium | patch | The transient CurrentGroup hides the prior terminal failure from Announce/Flush guards. Resolve the underlying owner for precedence while retaining the transient message as current output. |
| R2-BH-7 | medium | patch | WithdrawFreshRow accepts an invalidated cap through both PreviousGroup and Delivered fallback. Check each prior candidate against existing family restoration eligibility. |
| R2-BH-8 | medium | patch | Concurrent or reentrant withdrawal delivery can supersede the sole silent marker before its subscriber runs. Preserve silence for revisions still being delivered with bounded lifetime. |
| R2-BH-9 | medium | patch | Hidden revisions are skipped before WeakMap tracking, so visibility alone can count an old message. Track every observed revision and count only exposed fresh mutations. |
| R2-BH-10 | medium | patch | All new generated fresh-row cases have one active entity. Add actual shared-node coverage for multiple fresh entries, including a same-render burst and both withdrawal orders; each row must produce a status mutation. |
| R2-EC-1 | high | patch | Same demonstrated hidden exclusive owner as R2-BH-2; PanelVisibility alone checks the cue rather than the selected status owner. |
| R2-EC-2 | low | reject | Same unsupported DateTimeOffset overflow as R2-BH-4; no production clock/producer creates this timestamp, and additional rare-input guards are not justified. |
| R2-EC-3 | medium | patch | Same demonstrated underlying terminal barrier omission as R2-BH-6, with a limit becoming spoken behind a still-mounted failure. |
| R2-EC-4 | high | patch | Same verified hidden-owner path as R2-BH-2/EC-1; it violates first-visible delivery despite a visible cue. |
| R2-EC-5 | medium | patch | Same verified precedence omission as R2-BH-6/EC-3; other-family guards must see the underlying owner. |
| R2-VG-1 | medium | patch | Pre-verified broken-verification gap: hidden unchanged revisions count on exposure, and the component cases omit ModuleRoute. Add route-backed coverage and record hidden revisions. |
| R2-VG-2 | medium | patch | Pre-verified regression gap: no simultaneous A/B ownership case guards the noncurrent withdrawal return. Remove the older cue and assert the newer status remains with no extra speech. |

Grouped shared roots: origin copies; exposed status leases; forwarding decorators; terminal precedence (BH-6/EC-3/EC-5); restoration eligibility; in-flight silence; browser revision tracking (BH-9/VG-1); overlapping generated entries (BH-10/VG-2). All surviving fixes add no public surface and guard the demonstrated state, so they route to patch. Unsupported timestamp guards are rejected as rare low-impact complexity, and the duplicate-publication probe is rejected on the sole-producer contract.

### 2026-10-13 resumed build review triage

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| BH-1 creation-scope provenance | false | reject | A direct entry has no creation-scope field; its documented origin is the first resolved component observation. Creation in another scope cannot be inferred from this input. |
| BH-2 unobserved A-to-B-to-A | low | reject | Carried prior BH-10: no render in B means no observed boundary; the cue lasts under ten seconds and a scope subscription adds a new contract. |
| BH-3 other hidden containers | low | reject | Carried prior BH-5: generated retained grids use FcPageTabs exposure; arbitrary hidden hosts need a new public visibility contract. |
| BH-4 late raw Subscribe callback | low | reject | Callback delivery outside the lock predates this story for every family. Production FcSurfaceStatus uses versioned callbacks and ignores older revisions; a complete ordering change is disproportionate for a rare external raw listener race. |
| BH-5 failed claim leaves delivered set | false | reject | After scope loss, origin generation makes the entry ineligible; retrying it on return would breach the boundary. Duplicate claims also must not reannounce. |
| BH-6 maximum direct timestamp | low | reject | Carried R2-BH-4: MaxValue is unsupported provenance that the production clock cannot produce; defensive branches add complexity for malformed direct input. |
| BH-7 direct live-region timing | maybe-false (medium if true) | defer | Carried earlier BH-4: an accessibility-tree or assistive-technology check of SharedStatusOwner=false would settle whether the empty node and first text are batched. |
| BH-8 speech after live-region re-enable | maybe-false (medium if true) | defer | Carried earlier RR-BH-4b: the existing silent-revision design needs NVDA, JAWS, and VoiceOver verification after a silent restore. |
| BH-9 DOM mutation versus actual speech | maybe-false (medium if true) | defer | The browser observer measures exposed DOM revisions. Manual assistive-technology sign-off is the open Story 13.6 gate; no manual result is claimed here. |
| BH-10 v1 acceptance | false | reject | The frozen task retains v1/story 9.8 validation for archived packets; this story's live producer emits v2/story 13.5 and runs v2 assertions. |
| VG-1 forced-colors icon paint | medium | patch | Pre-verified: the live check accepts an opacity-zero SVG because it inspects only geometry and visibility. Assert opacity and painted SVG color. |
| EC-1 older terminal blocks cap | false | reject | Carried resumed review BH-2: an uncancelled terminal intentionally keeps its barrier in NonFreshOwner; the behavior predates this story. |
| EC-2 Clear erases in-flight silence | medium | patch | Clear removes PendingFreshSilentRevisions before a concurrent withdrawal Deliver reads it, making a prior-scope restored message audible. Preserve pending markers until delivery finishes. |
| EC-3 scope changes between checks | high | patch | IsEligibleEntry validates origin, but TryClaim rechecks only nonblank scope. A resolved switch between the calls can announce the old entry in the new scope. Recheck origin inside TryClaim. |
| EC-4 maximum direct timestamp | low | reject | Carried R2-BH-4; this is the same unsupported malformed timestamp as BH-6. |


### 2026-10-13 build continuation (workflow 41530feb5cde4fc47546)

Blind Hunter (BH), Edge Case Hunter (EC), and Verification Gap (VG) all completed before classification. The review diff is `/tmp/frontcomposer-13-5-review-mcva18kt.diff`, against canonical baseline `985d3e70ac8e9d5492fa857bc3a31208a9543670`. Every filed finding is classified individually below; the finding-floor arithmetic is reviewer metadata, not a defect.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| C-BH-1 same-timestamp new-scope publication | low | reject | Reusing the same entity, MessageId and CreatedAt across scopes is indistinguishable from the equivalent copies that must retain origin. The built-in resolver retains its MessageId decision across scopes. Distinguishing an uncommon direct caller's reused transition needs a new provenance contract, beyond a direct correction. |
| C-BH-2 invalid scope during deferred delivery retains cue | high | patch | The deferred continuation can reject eligibility or the claim while leaving `_scopeResolved` true. The already-rendered direct cue remains until another parameter update or expiry. Clear visibility and rerender on demonstrated ineligibility, while keeping a valid duplicate cue visible. |
| C-BH-3 different surface treated as reactivation | false | reject | The retained-component regression passes for exclusive and nonexclusive owners before any production change. Blazor applies the exposure cascade and ordinary surface rebinding as separate parameter deliveries; the new surface keeps polite replay. The same component instance is retained. No failing supported rebinding sequence was demonstrated, so no extra guard is added. |
| C-BH-4 scope switch after successful claim | low | reject | This hypothesizes an external mutation between synchronous renderer instructions after the final claim check. No normal producer yields in this interval; coordinating arbitrary concurrent accessor mutation and publication requires a new boundary contract rather than another effective check. |
| C-BH-5 unobserved scope round trip | low | reject | Carried prior BH-10 and resumed BH-2: the ledger cannot observe a boundary without a read. Production ScopeBoundaryService clears generated indicator state and remounts the keyed shell subtree; adding eager ledger subscriptions would introduce a contract for an uncommon direct retained entry. |
| C-BH-6 Add after direct render | low | reject | Generated publication always calls Add before rendering its entries. An adopter rendering a direct entry and later converting it to state ownership with an equal value is an uncommon unsupported ordering; dynamic ownership reconciliation adds branches and subscriptions for that ordering. |
| C-BH-7 browser observer accepts another surface | medium | patch | The global observer can count the canonical row copy on another page status node while the generated grid's node remains empty. Restrict observation to this projection's existing parent/status node and assert that node's canonical text. |
| C-BH-8 mutation batches versus speech | maybe-false (medium if true) | defer | Carried resumed BH-9: a DOM observer sees committed exposed revisions and cannot establish whether intermediate mutations in one browser batch become separate spoken utterances. Supported assistive-technology checks in Story 13.6 settle this existing deferred question; do not create a duplicate deferral. |
| C-BH-9 generated focus comparison | low | reject | Carried prior AA-8d: the component test pins zero JS focus invocations and the cue/status rendering has no focus operation. Additional browser focus capture was already rejected as redundant; no focus movement was demonstrated. |
| C-BH-10 hidden/deferred expiry coverage | low | patch | Existing tests cover expiry and hidden-first delivery separately. Add deterministic combinations for direct and state-owned hidden expiry, and expiry before the deferred first claim, requiring absent cue and speech. |
| C-BH-11 direct-caller dedupe documentation | low | patch | The component docs state an unqualified once-per-transition guarantee, while the scoped claim ledger is intentionally pruned after ten seconds. Clarify the active window and the resolver's separate once-per-MessageId publication guard without changing behavior. |
| C-EC-1 obsolete terminal cap barrier | false | reject | Carried RR-BH-2 and resumed EC-1: an older uncancelled terminal retains the required NonFreshOwner barrier while an overlay remains. Its suppression was explicitly preserved by prior review; the code still has that behavior. |
| C-EC-2 delayed duplicate view publication | false | reject | Carried R2-BH-5: the sole built-in publisher fixes target ViewKey and records the MessageId decision before Add, for the circuit lifetime. The proposed second Add bypasses that contract; valid direct alternate-view entries expire from their original CreatedAt. |
| C-EC-3 cap-barrier claim | false | reject | Carried the same recorded barrier decision as C-EC-1. This claim restates that finding's outcome, not a separate reachable regression. |
| C-EC-4 delayed-view dedupe claim | false | reject | Carried the same sole-publisher decision as C-EC-2. The reviewer confirms that the built-in resolver cannot produce the required second publication. |
| C-VG-1 throwing scope accessor | medium | patch | Pre-verified regression gap: existing throwing-accessor tests exercise other state/tenant services, not this ledger's exception path. Add direct-component tenant/user getter-failure cases proving cue and speech suppression, retained-origin rejection after recovery, and eligibility for a new recovered transition. |

Surviving entries are five independent patches (deferred scope visibility, projection-local browser observation, hidden/deferred expiry tests, direct-caller docs, and throwing-accessor tests). The new surface-rebinding regression passes unchanged and is retained as verification evidence. Each smallest correction adds no public surface and guards the demonstrated state. Existing deferrals are carried without being appended again; no intent or spec loopback is required.

## Design Notes

Pruning claims after ten seconds cannot re-announce: a claim happens after `Add`, expiry counts from `Add`, and the resolver decides once per MessageId, so no active entry outlives its claim.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug`, then run the built test assembly with `-class` per touched class -- expected: pass.
- Same pattern for `tests/Hexalith.FrontComposer.SourceTools.Tests` snapshot and emitter classes -- expected: pass.
- `cd tests/e2e && node --test scripts/validate-epic9-artifacts.test.mjs` -- expected: pass.
- `cd tests/e2e && npx playwright test specs/specimen-accessibility.spec.ts --project=chromium` -- expected: pass, or record the exact environment blocker.
- From the repository root, `./eng/run-epic9-live-proof.sh` -- expected: pass, or record the exact environment blocker.

### Implementation evidence (2026-10-12)

The implementation preserves FC-NIP publication/state/coordinator semantics. Generated cues now name `EntityKey`, include the decorative info icon, and claim AM-21 through a scoped view-independent ledger. Standalone callers without `Entry` retain their original localization-only dependency, role, live label and copy. Browser evidence now emits schema version 2/story 13.5 while the validator retains the archived version 1/story 9.8 pins. OI-16, G-4, FLUENT-APP-1 and Product approval remain open.

Executed build commands:

```bash
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false
```

Both completed successfully with `0 Warning(s)` and `0 Error(s)`. Logs: `/tmp/frontcomposer-story13-5-shell-build.log` and `/tmp/frontcomposer-story13-5-source-build.log`.

Executed Shell test command:

```bash
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FreshRowAnnouncementLedgerTests' -class '*FcFreshRowIndicatorTests' -class '*FcNewItemIndicatorTests' -class '*FcNewItemIndicatorLaneIntegrationTests' -class '*GeneratedFreshRowAnnouncementTests' -class '*Epic9CompositionTests' -class '*CounterStoryVerificationTests' -class '*GeneratedProjectionAnnouncementTests' -class '*ShellTypeOrganizationGovernanceTests' -class '*FcShellResourcesTests' -class '*FcTblPackageBoundaryTests' -class '*PendingCommandOutcomeResolverTests' -xml /tmp/frontcomposer-story13-5-shell-results.xml
```

Result: `Total: 328, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0`. The runner reported that `-xml` is deprecated in favor of `-result-xml`; the requested XML was produced. Output: `/tmp/frontcomposer-story13-5-shell-focused.log`.

| Covering class | Passed | Evidence coverage |
|---|---:|---|
| FreshRowAnnouncementLedgerTests | 9 | View-independent transition claims, distinct entity/message identity, 9.999/10-second claim pruning, missing/changed tenant and user |
| FcFreshRowIndicatorTests | 7 | Exact EN/FR AM-21, decorative icon, no cue live semantics, one node, no focus calls, visible override with canonical speech, cross-view/remount dedupe, silent restoration, standalone dependency compatibility |
| GeneratedFreshRowAnnouncementTests | 13 | Already-rendered grid reacts without unrelated render; paging, later message and remount keep first provenance/TTL; cue present at 9.999 seconds and absent at 10; silent materialization/filter/requery/dismiss/scope removal; suppressed Unknown identity/materiality, NoOp, Delete, Rejected, NeedsReview and server-allocated outcomes |
| FcNewItemIndicatorTests | 19 | Unchanged standalone copy/ARIA and indicator-state mutation, provenance and expiry behavior |
| FcNewItemIndicatorLaneIntegrationTests | 6 | Unchanged standalone lane composition and automatic disappearance |
| Epic9CompositionTests | 2 | Production generated-command publication, callback/polling, first-wins provenance and removal paths |
| CounterStoryVerificationTests | 28 | Existing generated Counter behavior and new generated cue copy |
| GeneratedProjectionAnnouncementTests | 60 | Story 13.4 one-node, cancellation, co-mounted ownership and precedence regressions |
| PendingCommandOutcomeResolverTests | 65 | Unchanged resolver eligibility and once-per-message publication decisions |
| ShellTypeOrganizationGovernanceTests | 30 | Shell type and layer organization |
| FcShellResourcesTests | 88 | Localization parity and resource contracts |
| FcTblPackageBoundaryTests | 1 | Intentional public API baseline |

Executed SourceTools test command:

```bash
dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class '*RazorEmitterTests' -class '*RoleSpecificProjectionApprovalTests' -class '*FcNipCommandTargetIdentityContractTests' -xml /tmp/frontcomposer-story13-5-source-results.xml
```

Result: `Total: 41, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`RazorEmitterTests`: 18; `RoleSpecificProjectionApprovalTests`: 16; `FcNipCommandTargetIdentityContractTests`: 7). Output: `/tmp/frontcomposer-story13-5-source-tests.log`. All affected generated snapshots pass, including the emitter's normalized sequence literals for `Entry` and `SharedStatusOwner`.

Executed focused analyzer governance command:

```bash
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*AnalyzerPolicy_GovernanceContract_FailsClosed' -method '*AnalyzerPolicy_IdentifierInventory_MatchesSeal' -method '*AnalyzerPolicy_SolutionBuildArguments_ForceRecommendedWithoutWeakeningWarnings'
```

Result: `Total: 3, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0`. Output: `/tmp/frontcomposer-story13-5-analyzer-tests.log`. No CA1707 seal changes were needed.

Executed browser and artifact checks from `tests/e2e`:

```bash
node --test scripts/validate-epic9-artifacts.test.mjs
npx tsc --noEmit
npx playwright test specs/specimen-accessibility.spec.ts --project=chromium
```

Results: artifact validator `101 passed, 0 failed`; TypeScript exit 0; Chromium specimen suite `22 passed`, including the scoped fresh-row forced-colors/reduced-motion proof (system inline-start border, no background image, no animation, no transition, no forced-color opt-out). Existing visual baselines remained unchanged. All browser assertions in this suite ran. Browser results: `tests/e2e/test-results/junit.xml` and `tests/e2e/playwright-report/`; the forced-colors/reduced-motion testcase passed.

Executed live proof from the repository root:

```bash
./eng/run-epic9-live-proof.sh
FC_EPIC9_ARTIFACT_ROOT=/tmp/frontcomposer-story13-5-live-proof-final ./eng/run-epic9-live-proof.sh
```

The default command exited 2 before browser assertions: `Epic 9 artifact directory must be empty: /home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9`. Existing artifacts were preserved. The fresh-directory development run completed successfully: source-graph build `0 Warning(s), 0 Error(s)`; browser `1 passed`; artifact validator `Validated Epic 9 proof artifacts`; `Epic 9 live proof passed: /tmp/frontcomposer-story13-5-live-proof-final`. Aspire started, discovered the endpoint, and stopped successfully. Browser v2 evidence measured one shared polite node, one create announcement, zero tab-switch repeats, zero duplicate announcements, and three eligible transition announcements overall. This dirty-tree development packet does not replace archived final evidence or close Product acceptance.

Broad analyzer-gate limitation: the earlier combined Shell command included `-class '*AnalyzerPolicyGovernanceTests'` together with the twelve Shell class filters above. `AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate` failed because its zero-warning assertion observed `8 Warning(s), 0 Error(s)` from MSB3277 conflicts between `Microsoft.IdentityModel.Protocols.OpenIdConnect`/`Microsoft.IdentityModel.Tokens` versions 8.19.2 and 8.23.0 in the existing Release dependency graph. Its nested command was `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended`. The combined run was interrupted after exposing the blocker, so its hidden-control Release probe also reported cancellation. The focused checks and final Debug builds above all passed; the broad Release gate was not weakened. Diagnostic output: `/tmp/frontcomposer-story13-5-shell-tests.log`.

The initial `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` returned without a running AppHost; `aspire describe` reported no running AppHost. A foreground `aspire run --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` remained at `Building AppHost` and was stopped through its own session. The completed source-graph live proof above supplies the runtime verification.

An additional direct-server diagnostic was attempted from `tests/e2e`:

```bash
FC_E2E_OUTPUT_DIR=/tmp/frontcomposer-story13-5-browser-results FC_E2E_HTML_REPORT_DIR=/tmp/frontcomposer-story13-5-browser-report FC_E2E_CANDIDATE_COMMIT=985d3e70ac8e9d5492fa857bc3a31208a9543670 npx playwright test specs/epic-9-fresh-row-acceptance.spec.ts --project=chromium
```

It ran browser assertions and failed at the final AM-21 count (`Expected: 3; Received: 4`). This optional server configuration defaults to a 6.5-second command-confirm delay against the sample's 5-second projection catch-up window, allowing the second update to settle after the first cue materializes. The required Aspire run above passed the overlap scenario and all speech counts. A diagnostic retry with `FC_E2E_STORY_3_6_CONFIRM_DELAY_MS=500` and output roots `/tmp/frontcomposer-story13-5-browser-pinned-results` and `/tmp/frontcomposer-story13-5-browser-pinned-report` was stopped at the parent workflow's instruction (exit 130); it supplies no acceptance evidence. The green required packet was preserved.

### Parent matrix audit (initial implementation, before review iteration 1, 2026-10-12)

Read the unified tracked and untracked diff against the recorded baseline and checked the actual xUnit/JUnit results. All tasks are complete. Appear and Re-view pass in `FcFreshRowIndicatorTests` and `GeneratedFreshRowAnnouncementTests`; Data change, Scope and Expiry pass in `GeneratedFreshRowAnnouncementTests`; Modes passes in the Chromium forced-colors/reduced-motion testcase. Every covering test ran without skips. Standalone compatibility passes. No readiness or Product gate was closed.

### Review iteration 1 implementation evidence (2026-10-12)

All amended execution tasks are complete. The default coordinator now has internal fresh-row publication and withdrawal operations; its public operations and other families keep their existing behavior. Immediate AM-21 preserves unrelated timers; withdrawing the cue restores status silently and retires that fresh group and its identity history. Status owners capture silence synchronously before queued renderer work. Adopter-supplied coordinators retain the existing interface `Announce`/`Cancel` fallback.

Entry provenance binds to the first observed tenant/user scope generation through weak entry ownership. Unresolved or invalid provenance never renders or claims speech; a resolved scope swap and a later return cannot reuse an old entry. Direct entries expire at `CreatedAt + 10 seconds`; state-owned entries retain the state service's Add-anchored window and observe its silent removals. Retained page panels cascade effective exposure, including nested inactive ancestors, so hidden cues wait for first visible delivery. Existing retained content and tab contracts remain intact.

Final commands and results:

```bash
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FreshRowAnnouncementLedgerTests' -class '*FcFreshRowIndicatorTests' -class '*FcNewItemIndicatorTests' -class '*FcNewItemIndicatorLaneIntegrationTests' -class '*GeneratedFreshRowAnnouncementTests' -class '*Epic9CompositionTests' -class '*CounterStoryVerificationTests' -class '*GeneratedProjectionAnnouncementTests' -class '*FcPageTabsTests' -class '*SurfaceAnnouncementCoordinatorTests' -class '*ShellTypeOrganizationGovernanceTests' -class '*FcShellResourcesTests' -class '*FcTblPackageBoundaryTests' -class '*PendingCommandOutcomeResolverTests' -result-xml /tmp/frontcomposer-story13-5-iteration1-shell-results.xml

dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false
dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class '*RazorEmitterTests' -class '*RoleSpecificProjectionApprovalTests' -class '*FcNipCommandTargetIdentityContractTests' -result-xml /tmp/frontcomposer-story13-5-iteration1-source-results.xml
```

Both Debug builds passed with `0 Warning(s), 0 Error(s)`. Shell: `410 passed, 0 failed, 0 skipped`; SourceTools: `41 passed, 0 failed, 0 skipped`. Logs: `/tmp/frontcomposer-story13-5-iteration1-shell-build.log`, `/tmp/frontcomposer-story13-5-iteration1-shell-tests.log`, `/tmp/frontcomposer-story13-5-iteration1-source-build.log`, `/tmp/frontcomposer-story13-5-iteration1-source-tests.log`.

The Shell lane includes 22 `FcFreshRowIndicatorTests`, 14 `GeneratedFreshRowAnnouncementTests`, 9 ledger tests, 27 page-tab tests, 39 existing coordinator tests, 60 Story 13.4 generated-announcement tests, and all prior standalone/lane/resolver/localization/API/layering checks listed in the command. New regressions cover direct resolved-scope swaps and returns, malformed identity, direct expiry/remount, Add-anchored generated expiry, hidden-first delivery and nested ancestors, pending stale/limit/page delivery after silent withdrawal, 100 bounded retirement cycles, and a custom-coordinator fallback. Generated tests complete actual page loads, assert cached page end-state and original provenance/TTL, dispatch the actual column-filter action, and complete the resulting provider requery without fresh-row removal speech or deletion wording.

From `tests/e2e`:

```bash
node --test scripts/validate-epic9-artifacts.test.mjs
npx tsc --noEmit
npx playwright test specs/specimen-accessibility.spec.ts --project=chromium
```

Artifact validator: `108 passed, 0 failed, 0 skipped`; TypeScript: exit 0; Chromium specimens: `22 passed`, including scoped fresh-row forced-colors/reduced-motion proof. Existing visual baselines were unchanged. Logs: `/tmp/frontcomposer-story13-5-iteration1-artifact-tests.log`, `/tmp/frontcomposer-story13-5-iteration1-typecheck.log`, `/tmp/frontcomposer-story13-5-iteration1-specimen-tests.log`. Final specimen JUnit: `tests/e2e/test-results/junit.xml`.

From the repository root:

```bash
./eng/run-epic9-live-proof.sh
FC_EPIC9_ARTIFACT_ROOT=/tmp/frontcomposer-story13-5-iteration1-live-proof ./eng/run-epic9-live-proof.sh
```

The default command exited 2 before browser assertions: `Epic 9 artifact directory must be empty: /home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9`. Existing artifacts were preserved. The fresh-directory development run passed: complete Debug source-graph build `0 Warning(s), 0 Error(s)`, Chromium live browser `1 passed`, artifact validation passed, and Aspire stopped successfully. Log: `/tmp/frontcomposer-story13-5-iteration1-live-proof.log`; complete packet: `/tmp/frontcomposer-story13-5-iteration1-live-proof`.

The v2 browser evidence counts only exposed status nodes, excluding hidden, inert, aria-hidden and non-visible layout ancestors. It records one shared polite node, zero AM-21 before first activation, exactly one on first activation, zero tab-switch/duplicate repeats, and three eligible announcements overall. The actual cue in forced colors/reduced motion has a visible icon, a solid system-color inline-start border, background image `none`, animation `none`, and transition duration `0s`. Validator v1/story 9.8 compatibility remains covered.

The earlier Release analyzer blocker was rechecked without changing any package pins or warning policy:

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -m:1 -p:NuGetAudit=false
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate' -method '*AnalyzerPolicy_GovernanceContract_FailsClosed' -method '*AnalyzerPolicy_IdentifierInventory_MatchesSeal' -method '*AnalyzerPolicy_SolutionBuildArguments_ForceRecommendedWithoutWeakeningWarnings' -result-xml /tmp/frontcomposer-story13-5-iteration1-analyzer-results.xml
```

Restore succeeded. Analyzer lane: `4 passed, 0 failed, 0 skipped`; both the exact canonical and forced-Recommended Release/NuGet solution builds satisfied their zero-warning/zero-error assertions. The earlier MSB3277 conflict did not reproduce after the serialized Release restore. Logs: `/tmp/frontcomposer-story13-5-iteration1-release-restore.log` and `/tmp/frontcomposer-story13-5-iteration1-analyzer-tests.log`.

`git diff --check` passed. OI-16, G-4, FLUENT-APP-1 and Product approval remain open; this development packet does not replace archived final evidence or owner acceptance.

### Parent matrix audit after re-derivation (2026-10-12)

Read the final unified diff and parsed the actual Shell/SourceTools/analyzer xUnit results, specimen JUnit and v2 live evidence. All 12 execution tasks are complete. All matrix rows are covered by executed passing tests with no skips: Appear/Re-view (entry/component/generated/visible live tests), Data change (actual filter action/requery/page completion and materialization/dismissal), Scope (direct and generated scope swaps/clears), Expiry (direct and state-owned exact boundaries), Modes (compiled CSS and actual live icon/border/motion). Legacy standalone and existing public coordinator/tab regressions pass. The approved frozen intent is unchanged after normal Git EOL normalization; resolver and indicator state service source are unchanged. The prior Release warning is resolved by a proper Release restore and the exact gate now passes.

### Final review patches and parent verification (2026-10-12)

All eight surviving second-review root causes were patched. Equivalent transition copies retain their originating scope and original expiry; exclusive status leases follow effective panel exposure; forwarding coordinators preserve fresh-family dispatch and delivery silence; terminal guards resolve their underlying owner; restoration respects existing family eligibility; silent metadata survives each in-flight delivery; browser tracking records hidden revisions; and overlapping generated cues each mutate the view's own shared node once. No findings were deferred. The exact cap-retirement regression uses supported nonterminal immediate caps; terminal cap guards remain intact.

The deferred render continuation rechecks entry eligibility before claiming. A focused equivalent-copy regression pins original direct expiry. The standalone EN/FR tests inspect the actual child node and freshly parsed renderer markup because bUnit's parent query cache can predate a deferred child render; assertions still require canonical text and exactly one polite node. Ordinary nonexclusive initial replay retains coordinator speech policy.

Parent full verification ran after patch completion, using the exact commands preserved in `/tmp/frontcomposer-story13-5-final-verify.py` and their corresponding `/tmp/frontcomposer-story13-5-final-*.log` files:

| Check | Result | Evidence |
|---|---|---|
| Debug Shell build and all touched/regression class lanes | 0 warnings/errors; 428 passed, 0 failed/skipped | `/tmp/frontcomposer-story13-5-final-shell-results.xml` |
| Debug SourceTools build, emitter/snapshot/identity lanes | 0 warnings/errors; 41 passed, 0 failed/skipped | `/tmp/frontcomposer-story13-5-final-source-results.xml` |
| Serialized Release restore and four exact analyzer gates | 4 passed, 0 failed/skipped; canonical and forced Recommended builds satisfy zero-warning/error policy | `/tmp/frontcomposer-story13-5-final-analyzer-results.xml` |
| Artifact validator | 108 passed, 0 failed/skipped | `/tmp/frontcomposer-story13-5-final-artifact-tests.log` |
| TypeScript | `npx tsc --noEmit` exit 0 | `/tmp/frontcomposer-story13-5-final-typecheck.log` |
| Chromium accessibility/visual specimens | 22 passed, 0 failed/skipped | `tests/e2e/test-results/junit.xml` |
| Fresh source Aspire/live acceptance proof and artifact validation | 1 passed, 0 failed/skipped; source graph 0 warnings/errors; owned AppHost stopped | `/tmp/frontcomposer-story13-5-final-live-proof/junit.xml` and v2 command evidence |
| Whitespace | `git diff --check` exit 0 | `/tmp/frontcomposer-story13-5-final-diff-check.log` |
| Exact full local commit message | Pinned commitlint 21.2.2: 0 errors, 0 warnings | `/tmp/frontcomposer-story13-5-final-commitlint.log` |

The live proof command was `FC_EPIC9_ARTIFACT_ROOT=/tmp/frontcomposer-story13-5-final-live-proof ./eng/run-epic9-live-proof.sh`; the default artifact directory remains preserved. Its schema 2/story 13.5 evidence records zero announcements before hidden-panel activation, one on first activation, one shared polite node, zero tab-switch/duplicate repeats, and three eligible fresh transitions overall. The real cue has a visible icon and system border, with no background image, animation, or transition under the required modes. This is development evidence from the dirty working tree, not archived final Product acceptance.

Final parent matrix audit: every row has executed passing assertions and no skips. Appear/Re-view cover canonical EN/FR copy, icon, shared node, remount/view-independent dedupe and hidden-first delivery; Data change covers actual generated filter/requery/page completion, first-wins overlap, both withdrawal orders and silent removal; Scope covers direct/generated tenant/user changes and equivalent copies; Expiry covers exact 9.999/10-second direct and Add-anchored state lifetimes; Modes cover compiled CSS and the actual live cue. Parsed xUnit/JUnit and live JSON support these results. All 12 execution tasks remain checked. The approved frozen intent is unchanged after Git EOL normalization, and resolver/state-service sources are unchanged. OI-16, G-4, FLUENT-APP-1 and Product approval remain open.

### RR patch verification and fix-delta review (2026-10-13)

All nine RR-prefixed patch items are checked off. Existing deferred items, rejected lists, frozen intent, and user tracking changes are preserved. No files were staged or committed; all six edited implementation/docs files retain CRLF.

- Debug Shell and SourceTools builds passed with zero warnings/errors using `-c Debug -m:1 -p:NuGetAudit=false`.
- The requested 18-class focused Shell lane passed three consecutive runs: 486 passed, zero failed/skipped per run. Evidence: `/tmp/frontcomposer-13-5-rr-13b4bbm7/focused-{1,2,3}.xml` and `.log`.
- Old-coordinator proof: backed up the fixed file, replaced only the coordinator with `git show HEAD:src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs`, rebuilt, and ran all coordinator tests. Four of 54 failed: cancelled stacked underlying owner (case 3c), cap first restoration (3d), stale fallback delivery, and queued A→B→A return (3e). Restored the fixed CRLF source, refreshed its timestamp so MSBuild rebuilt the fixed binary, and confirmed `git diff --stat`. Evidence: `/tmp/frontcomposer-13-5-rr-13b4bbm7/old-coordinator-tests.xml` and `.log`.
- Full Shell command: `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -result-xml /tmp/frontcomposer-13-5-rr-13b4bbm7/full-shell.xml`. Result: 3653 passed, two known pin failures (`CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds`, `ToolchainPins_MatchApprovedDotnetAndAspireVersions`), and one existing live two-tenant test skipped because its endpoint, identities and bearer tokens are not configured. No other failures; the deferred scheduler tests passed.
- CA1707 identifier seal and fatal-guard governance passed unchanged; new public test names contain no underscores and no fatal catch filters were added.
- `pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` passed; `FcNipCommandTargetIdentityContractTests` passed all seven tests. `git diff --check` passed.
- Repository baseline runtime limitation: `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` stalled before registration; exact-target `aspire describe` and `aspire stop` reported no running AppHost. The owned startup command was interrupted; the unrelated platform AppHost was left untouched. Logs: `/tmp/frontcomposer-13-5-rr-13b4bbm7/aspire-{start,describe,stop}.log`.

BMAD code review ran on `git diff HEAD -- src tests docs _bmad-output/contracts`: six files, 285 additions and 76 deletions. The spec and tracking files were excluded; the verbatim Review Findings section was supplied only as the Edge Case Hunter's claims file. Blind Hunter, Edge Case Hunter, Verification Gap, and Acceptance Auditor all completed. No new actionable findings survived triage.

New review triage (existing rejected lists above remain unchanged):

| Finding | Verdict | Evidence |
|---|---|---|
| BH-1: different overlay anchors restore older load | false, rejected | A still-mounted cue retains its own uncancelled previous owner; the requested restoration order deliberately returns it. RR-EC-4's cancelled common anchor is covered separately. |
| BH-2: older terminal suppresses cap before withdrawal | false, rejected | The older group remains uncancelled. This is the existing `NonFreshOwner` terminal barrier, which the user expressly requires preserving; the same suppression exists at HEAD. |
| BH-3: queued A→B→A under overlay speaks A | false, rejected | This is the user's exact required case 3e. Ordinary cancellation-without-replay remains unchanged and passes. |
| BH-4: stale fallback speaks before original deadline | false, rejected | The user expressly requires first fallback delivery and pending timer retirement. Retained-owner restoration still leaves unrelated pending work intact. |
| BH-5: noncanonical stale identity can repeat | low, rejected | Ordinary `Cancel` already uses the canonical literal at HEAD; framework stale producers use it, and the user requires it. Supporting custom stale identities would add a separate identity policy. |
| BH-6: direct disposal leaves old bUnit DOM | false, rejected | The test follows the requested `InvokeAsync` disposal and exercises the real lease-release/promotion callback, requiring the promoted cue node to remain silent. |

Full triage evidence: `/tmp/frontcomposer-13-5-rr-13b4bbm7/review-triage.txt`. Review result: zero decisions, zero patches, zero new deferrals; six rejected. Story and sprint statuses remain `in-progress`; Product and owner acceptance gates remain open.

### Resumed review fixes and verification (2026-10-13)

The resumed review patched three findings: the fresh-row claim now rechecks its originating tenant/user at the claim point; scope clearing retains in-flight silent withdrawal markers until delivery finishes; and the live forced-colors check requires an opaque, painted SVG icon. New regressions cover a scope switch between eligibility and claim, clearing during withdrawal delivery, and a fresh entry after a scope round trip. The other findings are individually triaged above; BH-9 remains a Story 13.6 manual assistive-technology follow-up.

- Debug Shell and SourceTools test-project builds each passed with zero warnings and zero errors. The 16-class Shell lane passed 467/467 with no skips (`/tmp/frontcomposer-13-5-postpatch-shell-final.xml`); SourceTools emitter/snapshot/identity lanes passed 41/41 (`/tmp/frontcomposer-13-5-postpatch-source.xml`).
- The artifact validator passed 108/108; `npx tsc --noEmit` passed; Chromium accessibility/visual specimens passed 22/22 with no skips (`tests/e2e/test-results/junit.xml`).
- `FC_EPIC9_ARTIFACT_ROOT=/tmp/frontcomposer-story13-5-review-live-proof-20261013 ./eng/run-epic9-live-proof.sh` passed: one Chromium live test, artifact validation, and owned Aspire shutdown. The v2/story 13.5 packet records one shared polite node, zero hidden announcements, one first-exposure announcement, zero repeats on tab switches or duplicate outcomes, three eligible announcements, and a painted icon/system border with no image or motion. Evidence: `/tmp/frontcomposer-story13-5-review-live-proof-20261013/junit.xml` and `playwright-results/epic-9-fresh-row-acceptanc-b89a4-to-an-already-rendered-grid-chromium/epic-9-command-evidence.json` under that packet.
- The full Shell command `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -result-xml /tmp/frontcomposer-13-5-postpatch-full-shell.xml` exited 1: 3654 passed, 4 failed, 1 skipped. The pin gates expect Verify 33.2.0 but the selected Builds catalog has 33.3.3, and expect Aspire 13.6.0 but the selected CLI is 13.6.1. A fallback-refresh scheduler test observed one read instead of two, and a pending-command cancellation/fatal-propagation test timed out. Those two timing cases passed 3/3 when rerun in isolation (`/tmp/frontcomposer-13-5-postpatch-flaky-focused.xml`). The skipped live two-tenant EventStore case lacks configured endpoint, identities, and bearer tokens.
- `git diff --check` passed. The frozen intent, resolver, state service, OI-16, G-4, FLUENT-APP-1, Product approval, and archived final evidence remain unchanged. The broad Shell gate is not green, so this spec remains `in-review` and sprint status remains `in-progress`; no local commit was created.


### Build continuation verification (2026-10-13, workflow 41530feb5cde4fc47546)

Review and implementation resumed from `in-review` on canonical HEAD `b3aafdc5b89f2ccc637c8e0845687b2e3b692a9d`. The only production change in this continuation clears and rerenders an ineligible direct cue after its deferred eligibility check or claim. Nine new regression cases cover both scope-check intervals, both throwing getters, direct/state-owned hidden expiry, expiry before first claim, and exclusive/nonexclusive surface rebinding. The surface-rebinding cases passed before the fix and prove the proposed production guard is unnecessary. The two deferred-scope regressions failed before the fix with retained prior-scope markup and pass afterward: `/tmp/frontcomposer-13-5-build-88mp16gj/regression-red.xml`.

Final verification commands are preserved in `/tmp/frontcomposer-13-5-build-88mp16gj/verify.py`. Both test-project builds use `-c Debug -m:1 -p:NuGetAudit=false` and pass with zero warnings/errors. The 18-class Shell lane passes 478/478 with no skips (`shell-focused.xml`); SourceTools emitter/snapshot/identity lanes pass 41/41 (`source-focused.xml`). The full Shell run also passes the Shell Fluent governance lane, including `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background`, and the exact canonical/forced-Recommended Release analyzer build gate.

Post-patch browser and documentation commands:

```bash
# From tests/e2e:
node --test scripts/validate-epic9-artifacts.test.mjs
npx tsc --noEmit
FC_E2E_OUTPUT_DIR=/tmp/frontcomposer-13-5-build-88mp16gj/postpatch-specimen-results FC_E2E_HTML_REPORT_DIR=/tmp/frontcomposer-13-5-build-88mp16gj/postpatch-specimen-report FC_E2E_JUNIT_PATH=/tmp/frontcomposer-13-5-build-88mp16gj/postpatch-specimen-junit.xml npx playwright test specs/specimen-accessibility.spec.ts --project=chromium

# From the repository root:
FC_EPIC9_ARTIFACT_ROOT=/tmp/frontcomposer-13-5-build-88mp16gj/postpatch-live-proof ./eng/run-epic9-live-proof.sh
pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild
git diff --check
```

Artifact validator: 108 passed, zero failed/skipped. TypeScript: exit 0. Chromium specimens: 22 passed, zero failed/skipped. Aspire live proof: one passed, artifact validation passed, source-graph build zero warnings/errors, owned AppHost stopped; final `aspire ps --format Json` returns `[]`. Docs and whitespace checks pass. Logs, xUnit/JUnit, and development packets are under `/tmp/frontcomposer-13-5-build-88mp16gj/`.

The post-patch v2/story 13.5 live packet measures the projection's own status node: one shared polite node, zero hidden announcements, one first-activation announcement, zero tab-switch/duplicate repeats, and three eligible announcements. The actual cue's icon is painted and visible, its inline-start border uses system color, and background image/animation/transition are absent under the required modes. Existing specimen visual baselines were unchanged. This dirty-tree development packet does not replace archived final evidence or manual assistive-technology acceptance.

The broad gate remains failed:

```bash
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -result-xml /tmp/frontcomposer-13-5-build-88mp16gj/shell-full.xml
```

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
|---|---|---|---|---|---|
| Full Shell audit | Exact built-assembly command above | Exit 1: 3664 passed, 3 failed, 1 skipped, 0 errors | Inside test bodies | Story-focused Shell 478/478; isolated timing cases pass three consecutive runs | Required |

Failures: `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` rejects the selected Builds catalog's Verify 33.3.3 against policy 33.2.0; `ToolchainPins_MatchApprovedDotnetAndAspireVersions` rejects Aspire 13.6.1 against its approved 13.6.0 pin; `HeldQueryPreservesCallerCancellationAndFatalPropagation(fatal: False)` times out. The live two-tenant EventStore test is skipped because endpoint, identities and bearer tokens are not configured. The before-patch audit also failed the same two pin gates and `ReadyReplayBehindNewlyStalledReplayStillStartsWithinBudget` (one read instead of two), with 3655 passed, 3 failed and 1 skipped; its results are preserved as `before-patch-shell-full.xml` and `.log`.

Both existing timing cases pass three consecutive isolated runs, using:

```bash
# Run for i = 1, 2, 3, preserving a separate result file per run:
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*ReadyReplayBehindNewlyStalledReplayStillStartsWithinBudget' -method '*HeldQueryPreservesCallerCancellationAndFatalPropagation' -result-xml /tmp/frontcomposer-13-5-build-88mp16gj/timing-isolated-{i}.xml
```

Each isolated run reports 3 passed, zero failed/skipped. These reruns do not make the failed broad gate green. No package pins, toolchain policy, resolver, indicator state service, or coordinator source were changed in this continuation.

Parent matrix audit: Appear/Re-view retain canonical copy, one node and view/remount dedupe; Data change retains silent real filter/requery/page/materialization/dismissal behavior; Scope now also covers deferred invalidation and throwing accessors; Expiry includes direct and state-owned hidden/deferred boundaries; Modes passes compiled CSS and real live cue checks. All covering story assertions pass without skips. The frozen intent is byte-equivalent after EOL normalization. OI-16, G-4, FLUENT-APP-1, Product approval, archived final evidence, and existing deferrals remain unchanged.

The required Hexalith baseline says, "All existing and new tests must pass before a story is complete." This prevents the workflow's Step 5 `done`/commit transition while the broad gate is failed. Spec status stays `in-review`; sprint status stays `in-progress`. No local commit or push was created.
