---
title: 'Story 13.5: Preserve Fresh-Row Meaning Across Visual and Data Changes'
type: 'feature'
created: '2026-10-12'
status: 'done'
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

### Review Findings (2026-10-15, production-source chunk 985d3e70..58e2432e)

Chunk: `src/` only (1,237 lines). Tests, snapshots and e2e were not reviewed in this pass. Four layers completed.

- [x] [Review][Reject] The discarded `Ledger.HasResolvedScope()` result does not make the disposal call dead code. Removing the block fails all four `ScopeChangeClearsCueAndClaimsBeforeNewScopeRendersWithoutSpeech` cases (tenant, user, missing tenant, missing user); restoring it passes all four and the identical 534-test focused lane. Disposal observes the intervening scope boundary after the state service removes a generated cue, before any subsequent entry checks occur. Retain the call and document this required side effect. Paired evidence: `/tmp/frontcomposer-13-5-dispose-shell-focused.xml` (530 passed, 4 failed), `/tmp/frontcomposer-13-5-retained-scope-paired.xml` (4 passed), and `/tmp/frontcomposer-13-5-retained-scope-shell-focused.xml` (534 passed); no test expectations changed.

#### Rejected

- Hidden-panel / reactivation silencing applies to all families (AA, EC, BH) — false: already adjudicated as EC-6/EC-11 and D-BH-2; retained inactive panels were never exposed to AT before 13.5, so no spoken outcome is lost.
- Exclusive ownership gated on panel visibility changes 13.4 (AA) — false: Review Triage R2-BH-2 required it, and `ExclusiveOwner` hand-off is covered by RR tests.
- Stale `_localSelectedTabId` after parent resets `ActiveTabId`; selected id matching no tab (BH, EC) — low: the uncontrolled path is only used when `ActiveTabId` was never supplied, and `VisibilityFor` already falls back to the first enabled tab; guards would be extra complexity for a path no caller reaches.
- Reserved `fresh-row:` prefix dispatch on public `Announce`/`Cancel` (AA) — false: R2-BH-3 required default fresh-family dispatch surviving forwarding decorators; the contract is documented in the interface remarks.
- Claim key lacks surface / cross-view dedupe (EC) — false: spec defines a transition as tenant + user + EntityKey + MessageId, independent of ViewKey.
- Group "retired" can be re-announced (EC) — false: each cue mints a fresh GUID group id, never reused.
- Tenant A entry observed after switch to B (EC) — false: producer emits no scope on the entry; origin is bound at first observation by design (BH-1/EC-2 resolution), and claims are cleared on scope change.
- `Clear` nulls `FreshSilentRevision` while a withdrawal is in flight (EC) — false: `PendingFreshSilentRevisions` is deliberately retained and checked at `SurfaceAnnouncementCoordinator.cs:76-77`.
- Cue expiring while hidden is not announced on reveal (EC) — false: expired transitions must stay silent.
- Claim consumed with no mounted owner / `SharedStatusOwner` without a status node (EC) — false: the coordinator retains the group per surface and delivers on bind; hidden panels return before claiming.
- `DateTimeOffset` clock-skew / overflow (EC) — low: carried as R2-BH-4 rejection.
- Public `Dispose()` in PublicAPI (BH, AA) — false: accepted as BH-8/AA-5.
- Service-locator injection, magic `10`, allocation per `VisibilityFor`, markup/indent nits, forced-colors icon, AsyncLocal fragility (BH) — low/false: no demonstrated harm; forced-colors and silence are covered by live and component tests.

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

### Current-checkout review (2026-10-13)

The resumed `bmad-build` review ran all three required layers against `/tmp/frontcomposer-13-5-current-z0mbpsv7/review.diff`, from canonical baseline `985d3e70ac8e9d5492fa857bc3a31208a9543670` through canonical HEAD `c3174dc917b094f3736b098ac78a531f197dbe56`. All layers completed before classification. Blind Hunter's finding-floor calculation was `min(floor(sqrt(313.931) + 1), 10) = 10`; that arithmetic is metadata rather than a finding. New finding IDs below use `N-` to distinguish this continuation.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| N-BH-1 different overlay anchors restore an older load | false | reject | Carried RR fix-delta BH-1: a still-mounted overlay retains its own uncancelled previous owner. The probe restores that owner after restoring the remaining cue; it does not reproduce the separately fixed cancelled-common-anchor case. The existing decision preserves this restoration order. |
| N-BH-2 staggered cross-view state publication | false | reject | Carried R2-BH-5 and C-EC-2: the sequence bypasses the sole built-in publisher's fixed ViewKey and once-per-MessageId decision. Direct entries expire from their original CreatedAt; the component reference explicitly bounds claims to the active ten-second window. No supported generated publication extends the same transition by a second Add. |
| N-BH-3 unmounted entry first observed in another scope | false | reject | Carried resumed BH-1 creation-scope provenance: an entry has no creation-scope field and direct callers bind at the first resolved component observation, as documented. A caller retaining an unobserved record outside the scope-cleared state service cannot supply earlier scope provenance through this contract. |
| N-BH-4 per-cue snapshot and subscription cost | low | reject | The extra snapshots and subscriptions exist, and Snapshot filters/sorts the collection. No everyday rendering failure or measured latency is demonstrated for the transient current-user cue lane; removing them requires shared membership/lifecycle plumbing rather than a direct correction. The proposed optimization adds complexity for an unmeasured large-burst case. |
| N-BH-5 removal wording versus newly exposed pending outcomes | low | reject | The proposed fix edits this build's spec, which the workflow rejects. RR's approved cap/stale fallback regressions deliberately preserve first delivery of unrelated waiting outcomes. The component reference is clarified under N-BH-6 without modifying frozen intent or these tested semantics. |
| N-BH-6 custom coordinator cancellation guarantees | low | patch | The component reference stated silence without the public interface's custom-coordinator qualification. Corrected docs/reference/components/datagrid.md to describe the built-in removed-row guarantee, normal pending-outcome precedence, and the reserved fresh-row family required of custom implementations. No public surface or runtime behavior changes. |
| N-BH-7 missing release migration entry | low | defer | Carried RR migration-note deferral: the target release needs its migration entry; the existing deferred-work entry remains open. Do not append the same deferral again. |
| N-BH-8 live-only browser coverage | low | reject | Carried RR-BH-9: the user's 2026-10-12 live-only decision expressly moves this timing-dependent proof to the Aspire lane. Chromium live acceptance and the accessibility/visual specimen suite pass; reversing that decision requires a different browser proof plan. |
| N-BH-9 direct status-node establishment | maybe-false (medium if true) | defer | Carried original BH-4 and resumed BH-7: generated grids have the persistent node, while batching for a direct entry needs accessibility-tree or supported assistive-technology evidence. The existing direct-path deferral remains open without a duplicate entry. |
| N-BH-10 browser mutation batching for distinct cues | maybe-false (medium if true) | defer | Carried C-BH-8 and resumed BH-9: exposed DOM revisions and bUnit render events do not establish separate spoken utterances when browser batches merge. Story 13.6 retains the required assistive-technology verification; this run claims automated DOM evidence only. |
| N-EC-1 simultaneous surface and visibility rebinding | false | reject | Carried C-BH-3: freshly rebuilding current source and running both exclusive/nonexclusive retained-component cases still passes BecomingVisibleOnAnotherSurfaceKeepsItsOrdinaryReplayPolite. The hypothetical combined cascade/ordinary parameter delivery was not reproduced through supported rendering; no new guard is warranted by the current evidence. |
| N-EC-2 near-maximum direct timestamp | low | reject | Carried R2-BH-4 and C-EC-4: the production clock does not produce this unsupported direct provenance. Timestamp-range branches add complexity for an already-rejected malformed-caller case. |
| N-EC-3 dismissal during external accessor reads | low | reject | Same boundary rationale as C-BH-4: there is no renderer yield between the final eligibility check, claim and publication. Making an adopter getter mutate/dispose the indicator reentrantly, or pinning a concurrent sub-instruction expiry, requires a new lifecycle coordination contract; another check does not make that interval atomic. Hidden/deferred expiry and scope-check intervals pass their existing deterministic regressions. |
| N-EC-4 first cap/stale delivery during withdrawal | false | reject | Carried RR-BH-1/RR-EC-1 resolution: fresh withdrawal must preserve the ordinary Cancel fallback's first-delivery bookkeeping and pending timer retirement. FreshWithdrawalFallbackDeliversCapFirstObservedBehindCancelledFailure and FreshWithdrawalFallbackCompletesPendingStaleIdentityAndCancelsItsOriginalTimer pass and pin this independent pending-outcome delivery; no removed-row message is repeated. |
| N-VG-1 new-surface replay inferred to fail | false | reject | Carried C-BH-3, independently confirmed by the fresh current-source build and both passing surface-rebinding cases in shell-focused.xml. The reviewer's source-only prediction does not occur in the supported retained-component sequence. |

Only N-BH-6 survives as a patch. It is a direct documentation correction and was applied locally because this resumed workflow has no step-03 implementation agent to re-engage. Existing decisions and deferrals are carried; there is no intent/spec loopback or new deferred-work entry.

### Review continuation (2026-10-14)

The three required review layers completed on canonical HEAD `0e151a139d178c1f5d90df50ee3d066ce4d4f35a`, against baseline `985d3e70ac8e9d5492fa857bc3a31208a9543670`, before classification. Complete changes are preserved in `/tmp/frontcomposer-13-5-20261014-fxjmgs_8/all-changes.diff`; the reviewer diff excludes this claims/spec file so only the Edge Case Hunter reads it. Blind Hunter's floor is `min(floor(sqrt(232.356) + 1), 10) = 10`. All seventeen findings have individual verdicts below.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| D-BH-1 different surface treated as reactivation | false | reject | Carried C-BH-3 and N-EC-1/N-VG-1 surface-rebinding decision: the retained-component exclusive/nonexclusive regressions pass after rebuilding current source. Cascade and ordinary parameters are delivered separately; the supported sequence keeps the new surface polite. |
| D-BH-2 ordinary outcome first delivered while hidden | false | reject | Carried original EC-6/EC-11: retained inactive panels were not exposed to assistive technology before this change either. The approved ordinary-panel reactivation regression deliberately retains text silently; it does not promise first-visible replay of hidden ordinary outcomes. Fresh-row first-visible delivery has separate passing coverage. |
| D-BH-3 unsupported direct timestamp | low | reject | Carried R2-BH-4 and original EC-3/EC-10: maximum and future-dated direct provenance are malformed caller inputs. Production timestamps come from the clock; extra timestamp guards are beyond a direct correction for this uncommon case. |
| D-BH-4 unobserved scope round trip | low | reject | Carried original BH-10 and C-BH-5: polling cannot observe an unrendered A-to-B-to-A boundary. The production scope service clears state and the keyed Shell body remounts; adding eager ledger subscriptions would add a contract for uncommon direct retained entries. |
| D-BH-5 direct cue lacks its own scope subscription | low | reject | The direct component checks scope during parameter processing and deferred delivery; the Shell observes ScopeBoundary.Changed, gates rendering with IsCurrent, and keys the body by Generation. An adopter mutating raw context outside that render boundary can retain old markup until a render; another boundary subscription adds coordination beyond a direct correction for that integration. |
| D-BH-6 malformed or synthetic raw scope | low | reject | The ledger and existing indicator state service follow IUserContextAccessor's nonblank resolved-context contract. Production FrontComposerShell additionally validates ScopeBoundary.IsCurrent before rendering its body, and publication is owned by the unchanged resolver. The proposed direct caller supplies context the production boundary rejects; adding a second tenant-routing validation dependency for the visual cue adds complexity for malformed adopter context. |
| D-BH-7 identical transition tuple in another scope | low | reject | Carried C-BH-1: an identical entity/message/timestamp tuple is indistinguishable from the equivalent copies that must retain origin. The built-in resolver keeps its MessageId decision across scopes. Distinguishing a reused direct tuple needs new producer provenance. |
| D-BH-8 independent coordinator cannot supply silence metadata | low | patch | The reserved prefix alone cannot supply internal delivery metadata to FcSurfaceStatus. The built-in coordinator and synchronous forwarding wrappers supply it; correct the component reference to require matching status-rendering support for independent implementations, without adding public surface or changing fallback behavior. |
| D-BH-9 custom state service has inert subscription | low | patch | Entry-aware cues delegate lifecycle to a matching state-service entry. The interface's compatibility Subscribe default is inert, so a custom service must override it for automatic removal/expiry/scope invalidation. Make this integration requirement explicit in the component reference; do not shorten Add-anchored lifetimes with a component fallback timer. |
| D-BH-10 multiple browser revisions versus speech | maybe-false (medium if true) | defer | Carried C-BH-8/N-BH-10: bUnit mutations and browser DOM revisions cannot settle delivery as distinct spoken utterances when Blazor batches merge. Existing Story 13.6 assistive-technology follow-up remains open; no duplicate deferred entry is created. |
| D-BH-11 live-only browser lane | low | reject | Carried RR-BH-9/N-BH-8: the user's recorded 2026-10-12 decision places this timing-dependent composed proof in the Aspire live lane. That required CI lane runs the complete browser test; restoring the stub-server lane would reverse the accepted decision. |
| D-BH-12 validator accepts archived v1 evidence | false | reject | Carried resumed BH-10: archived schema-1/story-9.8 validation is an explicit frozen task. The current producer emits schema-2/story-13.5 and runs all new assertions before validation; archive compatibility does not replace the current live proof. |
| D-EC-1 simultaneous visibility/surface rebinding | false | reject | Carried C-BH-3/N-EC-1: both retained-component cases pass in the fresh current-source Shell run; no supported failure is demonstrated. |
| D-EC-2 near-maximum timestamp | low | reject | Carried R2-BH-4/C-EC-4: unsupported direct provenance near DateTimeOffset.MaxValue requires additional validation branches for a previously rejected malformed input. |
| D-EC-3 first pending cap/stale delivery on withdrawal | false | reject | Carried RR-BH-1/RR-EC-1 and N-EC-4: normal cancellation fallback must preserve first delivery of an independently pending projection outcome. It does not repeat the removed-row message. Both explicit cap/stale fallback regressions pass; the frozen intent is unchanged. |
| D-VG-1 fatal scope-accessor propagation gap | medium | patch | Pre-verified regression gap: recoverable accessor tests and shared classifier/source governance do not execute a fatal getter through this ledger. Add both getter variants asserting that the same fatal exception escapes IsOriginatingScope. No production behavior change is needed. |
| D-VG-2 inferred new-surface replay failure | false | reject | Carried C-BH-3/N-VG-1: current-source exclusive/nonexclusive BecomingVisibleOnAnotherSurfaceKeepsItsOrdinaryReplayPolite both pass in shell-focused.xml. The source-only prediction is not reproduced. |

Surviving entries are one runtime regression-test gap and two direct documentation corrections. This resumed run has no step-03 implementation agent to re-engage, so the parent applies them. No new public surface, production-source patch, intent/spec loopback, or duplicate deferral is required.

### Current build review (2026-10-14, e0476b71)

All three required layers completed before classification. The independent reviewer diff is `/tmp/bmad-13-5-review-a0b79k8_.diff` (238156 bytes), excluding this claims file; complete changes are retained in `/tmp/bmad-13-5-all-changes.diff`. Canonical baseline is `985d3e70ac8e9d5492fa857bc3a31208a9543670`, canonical HEAD is `e0476b71c7623840ba2ab0c276e43800a51fd44a`. Blind Hunter's floor is `min(floor(sqrt(238.156) + 1), 10) = 10`. Verification Gap found no grounded gaps and executed 163 relevant tests without failures or skips.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| E-BH-1 generated remount claim not drained | medium | patch | The generated remount checks speech synchronously before the indicator's Task.Yield continuation, then advances to expiry. Drain its dispatcher before the assertion so expiry cannot conceal a duplicate claim. |
| E-BH-2 expiry observes only row speech | medium | patch | ObserveFreshSpeech filters non-row messages unless its audible list is supplied. Record every non-silent revision around the exact expiry boundary, after earlier pending work is drained. |
| E-BH-3 absolute deferred source path | low | patch | One source_spec embeds this workstation's root, unlike adjacent repository-relative entries. A direct path correction preserves the deferral while allowing another checkout to resolve it. |
| E-BH-4 Add after first direct render | low | patch | Membership is captured only when Entry changes. Clarify that Add must precede first rendering when state-service lifecycle ownership is wanted; retain the previously rejected runtime ownership expansion. |
| E-BH-5 direct timestamp preconditions | low | reject | Carried R2-BH-4 and D-BH-3: future/near-maximum direct timestamps are unsupported handcrafted provenance. Documentation already states expiry from CreatedAt rather than mounting; production uses the publication clock. No new timestamp branches or contract change. |
| E-BH-6 direct-entry registration | low | patch | Entry-aware rendering resolves the Shell's clock, ledger, and coordinator, whereas localization suffices for the legacy cue. Name AddHexalithFrontComposer as the supported registration in the direct-entry documentation. |
| E-BH-7 hidden SVG shapes satisfy icon proof | low | patch | The live detector checks outer geometry and effective opacity but does not test a shape's display/visibility. Add direct effective-visibility checks and negative hidden-shape probes to the existing real-cue proof. |
| E-BH-8 same-color SVG satisfies paint proof | low | patch | Nontransparent fill/stroke may equal the forced-colors Canvas background. Require a painted shape to differ from the system background in this mode proof. |
| E-BH-9 active modes missing from packet | low | patch | The producer requests emulation but records only computed styles. Record and assert both matchMedia results; v2 validation requires them while preserving v1 archive compatibility. |
| E-BH-10 long row identifier overflow | medium | patch | EntityKey allows unbroken strings, which the new visible row name inserts into a block with default overflow-wrap. Add wrapping and a bounded-width scoped-CSS browser assertion to preserve narrow-layout meaning. |
| E-EC-1 cancelled underlying owner revives old load precedence | medium | patch | The reviewer's paired current-assembly probe shows plain cancellation permits New cap, while cancellation under a fresh overlay suppresses it and later clears the node. NonFreshOwner's arbitrary delivered fallback must use ordinary restoration eligibility. This differs from the carried uncancelled current-owner barrier: here the overlay's actual owner was cancelled. |
| E-EC-2 timestamp addition overflow | low | reject | Carried R2-BH-4/C-EC-4/D-EC-2: unsupported near-maximum direct provenance; preserve the existing decision against extra timestamp guards. |
| E-EC-3 unchanged-family claim | medium | patch | Same paired cancellation counterexample as E-EC-1, verified through the public Announce/Cancel path. Group with E-EC-1; retain the mounted underlying owner's existing precedence. |

The parent applies the direct patches because this resumed workflow has no step-03 implementation agent to re-engage. No intent/spec loopback is required, and existing assistive-technology/migration deferrals remain open without new duplicates.

### Resumed build review (2026-10-15)

All three required layers completed before classification against `/tmp/frontcomposer-13-5-resume-0hip2oik/review.diff` (240765 bytes), preserving baseline `985d3e70ac8e9d5492fa857bc3a31208a9543670` and canonical starting HEAD `9a64a112980fd49441689fd0a3c41dd2ab88148d`. Blind Hunter's floor was `min(floor(sqrt(240.765) + 1), 10) = 10`; its arithmetic is reviewer metadata. Verification Gap reported no pre-verified gaps and two Other findings. Every filed finding is recorded individually below.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| F-BH-1 Story 13.8 key corruption | low | defer | The unrelated Story 13.8 key is misspelled. Git blame places the edit in the human's pre-run commit `9a64a112980fd49441689fd0a3c41dd2ab88148d`; preserve that committed change and record the tracking defect separately from Story 13.5 completion. |
| F-BH-2 concatenated metadata comment | low | defer | The header comment joins last_updated and project into one malformed line. Git blame attributes it to the same pre-run human commit; it is independent of this story's runtime and the authoritative YAML scalar remains readable. Preserve it and record the cleanup separately. |
| F-BH-3 simultaneous visibility/surface update | false | reject | Carried C-BH-3, N-EC-1 and D-BH-1: both retained-component exclusive/nonexclusive regressions pass after the final source rebuild. The supported cascading/ordinary parameter sequence delivers them separately and preserves polite new-surface replay. On follow-up, the reviewer withdrew reachability: its reproduction used reflection and direct lifecycle invocation rather than framework parameter delivery; both real regressions pass. The code and prior verdict remain unchanged. |
| F-BH-4 maximum/future timestamp | low | reject | Carried R2-BH-4, C-EC-4 and D-BH-3: production clock provenance cannot reach the unsupported direct MaxValue input. Additional timestamp/timer branches add complexity for the same previously rejected malformed input. |
| F-BH-5 identical tuple in another scope | low | reject | Carried C-BH-1 and D-BH-7: an identical entity/message/timestamp tuple is indistinguishable from an equivalent copy that must retain origin; the sole built-in publisher retains its MessageId decision across scopes. A separate independent-origin contract would expand the direct-caller surface. |
| F-BH-6 observer batches multiple revisions | maybe-false | defer | Carried C-BH-8 and N-BH-10: DOM observations do not prove separate spoken utterances when browser batches merge. If harmful this is medium; supported assistive-technology evidence settles it. Keep the existing Story 13.6 follow-up without another ledger entry. |
| F-BH-7 Chromium-only composed proof | low | reject | Carried RR-BH-9 and D-BH-11: the recorded human decision places the exact-timing composed proof in the Aspire live lane. Changing the browser coverage contract would reverse that decision; the required lane and specimen proof remain intact. |
| F-BH-8 browser observer records only row copy | low | reject | The browser array intentionally measures row-transition counts; generated removal/expiry/scope assertions record every non-silent revision and pass in the final lane, and the browser asserts the shared node's canonical copy. No missed invalid runtime behavior was demonstrated. Adding a broader browser event protocol duplicates existing passing removal assertions and adds complexity. |
| F-BH-9 two-entry browser delivery proof | maybe-false | defer | Carried C-BH-8, N-BH-10 and D-BH-10: all overlapping/same-render and hidden-activation bUnit cases pass, but batching versus user-heard delivery remains unverified (medium if true). The existing supported-assistive-technology follow-up covers this boundary; do not duplicate it. |
| F-BH-10 release migration entry | low | defer | Carried RR migration-note and N-BH-7: the existing deferred entry requires the target release's migration file. Keep that entry open without appending it again. |
| F-EC-1 distinct overlay anchors restore earlier load | false | reject | Carried N-BH-1 and the RR fix-delta BH-1 decision: a still-mounted older overlay retains its own uncancelled previous owner. This differs from the fixed cancelled-common-anchor case. Preserve the recorded restoration order; no new patch. |
| F-EC-2 Story 13.8 key deletion | low | defer | Same verified pre-run committed typo as F-BH-1. Group only this shared defect for one new deferred entry after individual classification. |
| F-EC-3 unrelated pending outcome speaks on withdrawal | false | reject | Carried RR-BH-1, N-EC-4 and D-EC-3: normal cancellation fallback deliberately completes first delivery of an independently pending cap/stale outcome. It does not reannounce the removed row; both explicit fallback regressions pass. |
| F-VG-O1 new-surface replay prediction | false | reject | Carried C-BH-3 and N-VG-1: the reviewer did not execute the test; both freshly rebuilt retained-component cases pass in `/tmp/frontcomposer-13-5-final-shell-focused.xml`. No new supported failing parameter sequence is shown by this finding. |
| F-VG-O2 Story 13.8 key corruption | low | defer | Same verified pre-run committed typo as F-BH-1/F-EC-2. Group for the single tracking-key deferred entry. |

No intent/spec loopback or implementation patch survives triage. Two newly recorded pre-existing tracking defects are deferred; existing manual-assistive-technology and migration deferrals remain open without duplicate entries. The disproved disposal-removal finding remains rejected on its paired failing/passing experiment.

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

### Current-checkout verification and documentation patch (2026-10-13)

Resumed review on canonical HEAD `c3174dc917b094f3736b098ac78a531f197dbe56`. All three required review layers completed. The sole surviving patch clarifies cancellation guarantees for the built-in and custom announcement coordinators in `docs/reference/components/datagrid.md`; production source, dependency pins, governance policy, and the frozen intent are unchanged. The complete execution commands/results are preserved in `/tmp/frontcomposer-13-5-current-z0mbpsv7/verify.py` and `verification-results.json`.

| Check | Result | Evidence under `/tmp/frontcomposer-13-5-current-z0mbpsv7/` |
|---|---|---|
| Debug Shell build | Zero warnings/errors | `shell-build.log` |
| Shell focused regression and Fluent governance lane | 517 passed; zero failed/skipped | `shell-focused.xml` |
| Debug SourceTools build | Zero warnings/errors | `source-build.log` |
| SourceTools emitter/snapshot/identity lane | 41 passed; zero failed/skipped | `source-focused.xml` |
| Serialized Release restore | Exit 0 | `release-restore.log` |
| Full Shell audit | 3664 passed, 3 failed, 1 skipped, zero errors; exit 1 | `shell-full.xml` and `shell-full.log` |
| Existing timing cases, isolated after the full-suite failure | Three consecutive runs, each 3 passed; zero failed/skipped | `timing-isolated-{1,2,3}.xml` and `timing-results.json` |
| Artifact validator | 108 passed; zero failed/skipped | `artifact-validator.log` |
| TypeScript | Exit 0 | `typescript.log` |
| Chromium accessibility and visual specimens | 22 passed; zero failed/skipped; existing baselines retained | `specimen-junit.xml` and `specimen.log` |
| Fresh Aspire source-graph and live browser proof | Source build zero warnings/errors; 1 passed; artifact validation passed; owned AppHost stopped | `live-proof/` and `live-proof.log` |
| Documentation and guarded identity checks after the documentation patch | Docs exit 0; 7 identity-contract tests passed; zero failed/skipped | `postpatch-docs.log` and `postpatch-doc-contract.xml` |
| Whitespace | `git diff --check` exit 0 | Final workspace check |

The full audit passes `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background` and `AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate`, including the canonical and forced-Recommended Release solution-build assertions. Both exclusive/nonexclusive `BecomingVisibleOnAnotherSurfaceKeepsItsOrdinaryReplayPolite` cases pass after a fresh current-source rebuild; the source-only review prediction is not reproduced.

The required live command was:

```bash
FC_EPIC9_ARTIFACT_ROOT=/tmp/frontcomposer-13-5-current-z0mbpsv7/live-proof ./eng/run-epic9-live-proof.sh
```

Its schema-2/story-13.5 packet measures the generated projection's own node: one shared polite node, zero hidden announcements, one first-activation announcement, zero tab-switch/duplicate repeats, and three eligible transition announcements. The real cue has a painted visible icon and system border, with background image and animation absent and transition duration `0s` under forced colors/reduced motion. This remains development evidence; it does not replace archived final evidence or manual assistive-technology acceptance. Final `aspire ps --format Json --non-interactive --nologo` returns `[]`.

The blocking broad command and result are:

```bash
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -result-xml /tmp/frontcomposer-13-5-current-z0mbpsv7/shell-full.xml
```

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
|---|---|---|---|---|---|
| Full Shell audit | Exact built-assembly command above | Exit 1: 3664 passed, 3 failed, 1 skipped | Inside test bodies | Focused Shell 517/517; isolated timing cases 3/3 on each of three runs | Required |

Failures are `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` (the committed catalog selects Verify 33.3.3 while semantic policy requires 33.2.0), `ToolchainPins_MatchApprovedDotnetAndAspireVersions` (the selected **Aspire.Hosting package**, not the CLI, is 13.6.1 while the approved hosting pin is 13.6.0), and `HeldQueryPreservesCallerCancellationAndFatalPropagation(fatal: False)` (timeout). The same timing cases pass all three isolated runs; this does not make the failed broad gate green. `LiveTwoTenantProductionAdapterTests.ProvisionedTenants_QueryCountAndSubscribe_StayIsolated` is skipped because its endpoint, identities and bearer tokens are not configured. The direct semantic-policy command also exits 1 for the committed Verify mismatch:

```bash
python3 eng/dependency_graph.py --root . validate --commit c3174dc917b094f3736b098ac78a531f197dbe56
```

Parent matrix audit: executed covering tests still prove Appear/Re-view copy, icon, one node, view/remount dedupe and hidden-first delivery; Data change preserves first provenance/TTL and silent removed-row behavior through actual filter, requery, paging, materialization and dismissal; Scope covers resolved/missing/throwing getters and deferred invalidation; Expiry covers direct/state-owned exact and hidden/deferred boundaries; Modes passes both compiled-CSS and real-cue browser checks. Existing standalone, coordinator precedence and public tab regressions pass. OI-16, G-4, FLUENT-APP-1, Product approval and existing review deferrals remain open.

The required Hexalith baseline says, "All existing and new tests must pass before a story is complete." The full-suite failure prevents the workflow's `done`/commit transition. Status remains `in-review`, sprint status remains `in-progress`, and no files were staged, committed or pushed. The remaining catalog-policy reconciliation and existing timing failure are recorded exactly; no gate was relaxed to declare completion.

### Final continuation verification (2026-10-14)

Resumed `bmad-build 13.5` on canonical HEAD `0e151a139d178c1f5d90df50ee3d066ce4d4f35a`. All three review layers completed and seventeen findings were individually triaged before patches. Three patches are complete:

- [x] `FreshRowAnnouncementLedgerTests.cs`: both tenant/user getter variants prove the same fatal exception escapes eligibility unchanged. The fixture uses the existing `RuntimeHelpers.GetUninitializedObject` technique, introducing no pragma, suppression, or taxonomy change.
- [x] `docs/reference/components/datagrid.md`: distinguish the built-in coordinator's delivery metadata and synchronous forwarding wrappers from independent coordinators, which need matching status-rendering support.
- [x] `docs/reference/components/datagrid.md`: state that custom indicator services must override the inert compatibility subscription to notify mounted cues of removal, expiry, and scope clearing.

Runtime, generator, resolver, indicator-state-service, public API, dependency pins, and governance-policy sources are unchanged in this continuation. The frozen intent remains unchanged after EOL normalization. No new deferred-work entry was created. All three edited files retain CRLF.

Execution scripts and complete results are preserved under `/tmp/frontcomposer-13-5-20261014-fxjmgs_8/`: `verify.py`, `final-verify.py`, `verification-results.json`, `final-results.json`, and `timing-verify.py`/`timing-results.json`.

| Check | Result | Evidence under that directory |
|---|---|---|
| Debug Shell and SourceTools test-project builds | Zero warnings/errors | `final-shell-build.log`, `source-build.log` |
| Final focused Shell regression/Fluent lane | 519 passed; zero failed/skipped | `final-shell-focused.xml` |
| SourceTools emitter/snapshot/identity lane | 41 passed; zero failed/skipped | `source-focused.xml` |
| Analyzer contract, identifier seal, and solution-argument checks | 3 passed; zero failed/skipped | `final-analyzer-contract.xml` |
| Artifact validator | 108 passed; zero failed/skipped | `artifact-validator.log` |
| TypeScript | Exit 0 | `typescript.log` |
| Chromium accessibility/visual specimens | 22 passed; zero failed/skipped | `specimen-junit.xml` |
| Aspire source graph/live proof | Zero build warnings/errors; 1 browser test passed; artifact validation passed; owned AppHost stopped | `live-proof/`, `live-proof.log` |
| Final documentation and guarded identity checks | Docs exit 0; 7 contract tests passed | `final-docs.log`, `final-doc-contract.xml` |
| Configured Shell fallback | Exit 1: 3658 passed, 3 failed, zero skipped/errors | `final-shell-configured-fallback.xml` |
| Existing timing cases in isolation | Three runs, each 4 passed; zero failed/skipped | `timing-isolated-{1,2,3}.xml` |
| Whitespace and frozen intent | Passed | `final-whitespace.log`, `frozen-before.txt` |

The live packet emits schema 2/story 13.5 and measures the generated projection's own node: one shared polite node, zero hidden announcements, one first-activation announcement, zero tab-switch/duplicate repeats, and three eligible announcements. The actual cue has a painted visible icon and system border with no background image, animation, or transition under forced colors/reduced motion. Runtime/generated source did not change after this packet; subsequent patches change only tests/docs. Existing visual baselines and archived final evidence are preserved. Final `aspire ps --format Json --non-interactive --nologo` returns `[]`.

The unfiltered audit command was:

```bash
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -result-xml /tmp/frontcomposer-13-5-20261014-fxjmgs_8/shell-full.xml
```

It recorded the two policy failures and the unconfigured live two-tenant skip, then stalled without output or CPU-time progress. The owned runner was cancelled after 774.2 seconds (exit -2/SIGINT); it produced no complete XML summary. It is not passing evidence. Ownership/cancellation diagnostics are in `full-audit-cancellation.json` and `shell-full.log`. No unrelated process was stopped. The final fallback preserves the configured five trait exclusions and adds long-running diagnostics:

```bash
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -trait- 'Category=GovernanceBuild' -trait- 'Category=Performance' -trait- 'Category=e2e-palette' -trait- 'Category=NightlyProperty' -trait- 'Category=Quarantined' -longRunning 30 -result-xml /tmp/frontcomposer-13-5-20261014-fxjmgs_8/final-shell-configured-fallback.xml
```

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
|---|---|---|---|---|---|
| Unfiltered Shell audit | Exact unfiltered command above | Cancelled after stall; no complete result | Inside test execution | Completed configured Shell fallback and final focused lane | Required |
| Configured Shell fallback | Exact trait-filtered command above | Exit 1: 3658 passed, 3 failed, zero skipped | Inside test bodies | Story-focused Shell 519/519; three isolated timing runs each 4/4 | Required |

The final configured failures are:

- `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds`: selected Builds catalog has Verify 33.3.3, while the approved semantic policy requires 33.2.0.
- `ToolchainPins_MatchApprovedDotnetAndAspireVersions`: selected Aspire.Hosting package is 13.6.1, while its approved hosting pin is 13.6.0. This is not the CLI version; the installed CLI reports 13.6.0.
- `ReadyReplayBehindNewlyStalledReplayStillStartsWithinBudget`: observed one read instead of two; this unchanged scheduler timing case is already deferred. It passes all three isolated reruns alongside both `HeldQueryPreservesCallerCancellationAndFatalPropagation` cases and `Resolve_IndicatorAdd_ReleasesResolverGateBeforePublication`.

The direct semantic-policy command also exits 1 for the Verify mismatch:

```bash
python3 eng/dependency_graph.py --root . validate --commit 0e151a139d178c1f5d90df50ee3d066ce4d4f35a
```

Intermediate validation exposed two corrected/retained facts: the initial synthetic fixture's scoped CA2201 pragma tripped the sealed analyzer inventory, so the pragma was removed and all three contract/seal checks now pass without changing the ledger; one intermediate 519-test focused run timed out on the unchanged resolver-gate probe, while the final 519-test run and all three isolated timing runs pass. Intermediate failed logs/results remain preserved. Reruns do not make the failed configured lane green.

The final configured audit passes the required `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background` and the analyzer contract/seal checks. Its exclusions omit the two GovernanceBuild whole-solution proofs; the cancelled unfiltered audit supplies no complete current-run result for those proofs. No fresh full-Release success is claimed here.

Parent matrix audit: Appear/Re-view, Data change, Scope, Expiry, and Modes retain executed passing covering assertions with no story-test skips. Legacy standalone compatibility, public coordinator/tab behavior, scope failure/recovery, and direct/state-owned expiry regressions pass. OI-16, G-4, FLUENT-APP-1, Product approval, and existing assistive-technology/migration deferrals remain open.

The required Hexalith baseline says, "All existing and new tests must pass before a story is complete." The rendered build review also requires halting when verification fails and cannot be fixed within the authorized scope. The failed configured lane prevents the `done`/commit transition. Spec status remains `in-review`, sprint status remains `in-progress`; no files were staged, committed, or pushed. Completion still requires dependency-policy reconciliation and a green configured verification lane.

### Current build patch verification (2026-10-14, e0476b71)

All three review layers completed, and all thirteen findings have individual verdicts in the current-build triage table. The parent applied the surviving patches:

- The coordinator uses ordinary cancellation candidates for precedence when a fresh overlay's underlying owner was cancelled. The paired public-operation regression passes without an overlay and failed with an overlay before the production correction (one pass, one failure in `cancellation-red.xml`); both cases now pass. A still-mounted underlying owner retains its existing precedence.
- Generated remount checks drain deferred renderer work before testing dedupe; exact expiry observes every non-silent revision after earlier work is drained.
- Long unbroken identifiers wrap within a bounded cue width. The real-cue mode proof rejects hidden/invisible SVG shapes, requires paint distinct from Canvas, records active forced-colors/reduced-motion media, and validates those v2 fields. Archived schema-1 acceptance remains covered and unchanged.
- Direct-entry docs name Shell registration and the required Add-before-render ordering. One deferred source reference is now repository-relative; its meaning and open status are unchanged.

Evidence is preserved under `/tmp/bmad-13-5-current-verification/`; postpatch command argv, exit codes, logs, and XML summaries are in `postpatch/verify.py`, `postpatch/results.json`, and the lane files listed below.

| Check | Current result | Evidence |
|---|---|---|
| Serialized cached Debug Shell build | Zero warnings/errors | `patched-shell-build.log` |
| Focused Shell behavior/Fluent lane | 521 passed; zero failed/skipped | `postpatch/shell-focused.xml` |
| SourceTools build and emitter/snapshot/identity lane | Zero build warnings/errors; 41 passed | `postpatch/source-build.log`, `postpatch/source-focused.xml` |
| Analyzer contract and identifier seal | 3 passed | `postpatch/analyzer-contract.xml` |
| Artifact validator | 114 passed; zero failed/skipped | `patched-validator.log` |
| TypeScript | Exit 0 | `patched-typescript.log` |
| Chromium accessibility/visual specimens, including long unbroken identifier | 22 passed; zero failed/skipped | `postpatch/specimen-junit.xml` |
| Aspire complete source-graph build and live proof | Zero build warnings/errors; 1 browser test and packet validation passed; owned AppHost stopped | `postpatch/live-proof/`, `postpatch/live-proof.log` |
| Documentation validation | Exit 0 | `patched-docs.log` |
| Configured Shell lane | Exit 1: 3661 passed, 2 failed, zero skipped/errors | `postpatch/shell-configured.xml` |
| Candidate dependency policy, temporary override only | Exit 0; seven selectors validated | `candidate-policy-validation.json` |
| Whitespace, frozen intent, CRLF | Passed | Parent checks; all eleven changed files retain CRLF |

The current live packet is schema 2/story 13.5: one shared polite node, three eligible row announcements, zero duplicate/tab-switch repeats, and true active-media and hidden-shape rejection flags. The narrow-width scoped-CSS proof uses an unbroken identifier rather than hyphen-separated words. Existing visual baselines and archived final packets are unchanged. Final Aspire process discovery returns `[]`.

The original verification build command `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug` stalled at `Determining projects to restore...` before compilation. The owned process was cancelled (the tool reported exit 0, but the log contains cancellation and no successful build summary). The narrower command completed successfully:

```bash
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-restore -m:1 -p:NuGetAudit=false
```

The exact configured lane command was:

```bash
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -trait- 'Category=GovernanceBuild' -trait- 'Category=Performance' -trait- 'Category=e2e-palette' -trait- 'Category=NightlyProperty' -trait- 'Category=Quarantined' -longRunning 30 -result-xml /tmp/bmad-13-5-current-verification/postpatch/shell-configured.xml
```

Its only failures are `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` (Verify 33.3.3 selected versus approved 33.2.0) and `ToolchainPins_MatchApprovedDotnetAndAspireVersions` (Aspire.Hosting 13.6.1 selected versus approved 13.6.0). Previously intermittent scheduler/resolver cases pass in this current completed lane. No fresh unfiltered or full Release success is claimed; the configured exclusions retain their existing CI authority.

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
|---|---|---|---|---|---|
| Shell default build | Exact default build above | Cancelled during stalled restore; no compilation signal | Before test execution | Cached serialized Debug build passed | Required |
| Configured Shell | Exact configured command above | Exit 1: two approved-pin mismatches | Inside test bodies | 3661 other configured tests and 521 focused tests pass | Required |

A concrete, unapplied candidate is `/tmp/bmad-13-5-current-verification/proposed-policy-reconciliation.patch`. It changes exactly three approved literals: Verify and Verify.XunitV3 33.2.0 to 33.3.3 in `eng/dependency-graph-policy.json`, and expected Aspire.Hosting 13.6.0 to 13.6.1 in `CiGovernanceTests.cs`. It does not update dependencies, gitlinks, SDK/CLI pins, release execution identity, or repository policy yet. The temporary JSON candidate validates the current exact graph with seven selectors:

```bash
python3 eng/dependency_graph.py --root . --policy /tmp/bmad-13-5-current-verification/candidate-dependency-policy.json validate --commit e0476b71c7623840ba2ab0c276e43800a51fd44a
```

That is candidate evidence, not a passing authoritative gate. Applying approved-policy changes requires the human's decision; the actual policy and both pin gates remain unchanged. The build review step requires `HALT and escalate to the human` when verification fails and cannot be resolved within the current story authorization. The Hexalith baseline requires all tests to pass before completion. Spec remains `in-review`, sprint remains `in-progress`; nothing is staged, committed, or pushed.

Parent matrix audit: Appear/Re-view, Data change, Scope, Expiry, and Modes have current passing covering assertions, including the cancellation correction and deferred-remount check. Standalone behavior and unchanged resolver/state-service/tab/public contracts retain passing evidence. OI-16, G-4, FLUENT-APP-1, Product approval, and existing assistive-technology/migration deferrals remain open.

### Approved policy reconciliation and completion (2026-10-14)

The human approved the prepared three-literal policy patch in this conversation. Verify/Verify.XunitV3 now require 33.3.3, and the hosting test requires Aspire.Hosting 13.6.1. SDK, Aspire CLI, dependency gitlinks, release execution identity, and enforcement strength are unchanged.

The catalog gate deliberately loads the policy from Git HEAD. Therefore the approved patch was recorded separately in local commit `43cdbb11b56fb62a6e499938abc5aca1199fdbac` before the authoritative gate could pass; only its two approved files were staged. The owning pinned commitlint CLI 21.2.2 accepted the exact full message with zero problems/warnings; evidence is `policy-commitlint.log` under `/tmp/bmad-13-5-current-verification/`.

Current evidence after that commit is in `approved/` under the same directory:

| Check | Result | Evidence |
|---|---|---|
| Debug Shell build with cached assets | Zero warnings/errors | `approved-policy-build.log` in the parent directory |
| Dependency graph fixtures | 102 passed | `approved-policy-fixtures.log` in the parent directory |
| Authoritative committed-policy validation | Exit 0; seven selectors validated | `approved/dependency-policy.json` |
| Both former pin-gate failures | 2 passed; zero failed/skipped | `approved/policy-gates.xml`, `approved/policy-gates-retry.xml` |
| Existing scheduler case in isolation | 1 passed; zero failed/skipped | `approved/scheduler-isolated.xml` |
| Final configured Shell run | 3663 passed; zero failed/skipped/errors | `approved/shell-configured-retry.xml`, `approved/retry-results.json` |

The initial approved-policy configured run hit the previously recorded scheduler race (`ReadyReplayBehindNewlyStalledReplayStillStartsWithinBudget`: readsB expected 2, observed 1), then stalled. The bounded runner stopped its owned process at 300 seconds and produced no complete XML; it is not passing evidence. `approved/shell-configured.log` and `approved/first-configured-timeout.json` preserve that failure. The isolated check passed, and one fresh run of the identical configured lane completed green. No test was excluded, quarantined, rewritten, or weakened to obtain that result. The existing scheduler deferral remains open; the successful final run does not establish that the timing defect is resolved.

The authoritative graph command was:

```bash
python3 eng/dependency_graph.py --root . validate --commit 43cdbb11b56fb62a6e499938abc5aca1199fdbac
```

The final configured command was:

```bash
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -trait- 'Category=GovernanceBuild' -trait- 'Category=Performance' -trait- 'Category=e2e-palette' -trait- 'Category=NightlyProperty' -trait- 'Category=Quarantined' -longRunning 30 -result-xml /tmp/bmad-13-5-current-verification/approved/shell-configured-retry.xml
```

The already executed postpatch generator (41), artifact validator (114), TypeScript, docs, Chromium specimens (22), and Aspire live-proof (1) results remain applicable: this approval changed policy and one governance expectation only. No runtime/browser/generator source changed after those passing proofs. The existing configured exclusions and full Release/unfiltered CI authority remain recorded above; no fresh full Release or unfiltered success is claimed.

All story acceptance rows retain executed passing assertions. Frozen intent is unchanged, and OI-16, G-4, FLUENT-APP-1, Product approval, assistive-technology sign-off, migration notes, and the existing scheduler deferral remain open. The implementation spec is now `done`; the rendered workflow synchronizes its sprint entry to `review` and records the remaining story changes in a local commit. Nothing is pushed.


### Resumed implementation and parent matrix audit (2026-10-15)

Resumed on canonical HEAD `9a64a112980fd49441689fd0a3c41dd2ab88148d`, preserving baseline `985d3e70ac8e9d5492fa857bc3a31208a9543670`. The sole unchecked removal finding was disproved by a paired experiment: removal fails all four generated scope-boundary cases; restoration passes them and the same 534-test lane. The final production change is only an explanatory comment on the required disposal reconciliation. No test expectation, runtime behavior, dependency, or frozen requirement changed.

Final command: `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-restore -m:1 -p:NuGetAudit=false`. Result: zero warnings/errors (`/tmp/frontcomposer-13-5-final-shell-build.log`). The final 19-class Shell lane passes 534/534, zero failed/skipped/errors; the parent parsed `/tmp/frontcomposer-13-5-final-shell-focused.xml`. It includes standalone/lane, ledger, fresh-row, generated composition, resolver, coordinator, surface-status, page-tabs, slow/cap notice, localization/API, type/fatal-guard and Fluent conformance tests, including `Shell_chrome_styles_never_use_accent_as_surface_background`.

Parent matrix audit: Appear/Re-view, Data change, Scope and Expiry are covered by those executed passing component/generated/ledger assertions. The unchanged Modes path retains the inspected passing specimen JUnit (22 tests) and live JUnit (one test) under `/tmp/bmad-13-5-current-verification/postpatch/`. The parent also parsed its schema-2/story-13.5 JSON: one shared polite node, zero hidden speech, one first-activation announcement, zero tab/duplicate repeats, three eligible announcements, visible icon/system border, active forced-colors/reduced-motion, hidden/invisible-shape rejection, and no image/animation/transition. These are retained prior browser results, not a new browser run. All twelve execution tasks remain satisfied; the former unchecked item is now rejected with paired evidence.

The current exact-target `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` stalled before registration. Exact-target describe/stop found no running AppHost; only the owned startup command was cancelled. Final discovery returned `[]`. No new known-good runtime baseline is claimed. Product, OI-16, G-4, FLUENT-APP-1 and existing assistive-technology/migration/scheduler follow-ups remain open.


### Completion (2026-10-15)

The three required review layers are complete, and all fifteen findings have individual verdicts. No new runtime fix survives; the sole source change documents the required disposal scope reconciliation. Two pre-existing tracking defects are recorded separately in deferred-work; existing assistive-technology, migration and scheduler follow-ups remain open. The final freshly rebuilt focused lane is 534 passed with zero failed/skipped/errors. Retained prior default-lane evidence is 3663 passed with zero failed/skipped/errors; the comment-only source change does not alter that code. No new full-suite, Release, live-browser or assistive-technology success is claimed.

All implementation tasks and matrix assertions retain passing evidence; frozen intent, resolver and indicator-state-service source are unchanged. This workflow marks the implementation spec done and synchronizes only Story 13.5 to review. Product, OI-16, G-4, FLUENT-APP-1 and owner acceptance remain open. The resulting local commit contains only the source comment and this run's workflow records; nothing is pushed.
