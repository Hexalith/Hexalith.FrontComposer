---
title: 'Story 13.4: Announce Projection and Command State Without Noise'
type: 'feature'
created: '2026-10-04'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 8
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
- [x] `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcSurfaceStatus.razor` (new), its backing coordinator types, `Extensions/ServiceCollectionExtensions.cs`, and `Resources/FcShellResources*.resx` — add disposable scoped ownership, fake time and EN/FR copy.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`, projection/Fluxor emitters above, and Shell `State/DataGridNavigation/LoadPageEffects.cs` — carry load/filter origin and safe outcomes; wire shared speech across visible projection states; show pending SlowQuery; recover hidden-detail focus.
- [x] `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.cs`, connection/reconciliation state above, and `wwwroot/js/fc-connectivity.js` (new) — use actual browser offline evidence, stable epochs, fallback presentation and recovery after successful reconciliation even without changed rows; preserve transport budgets.
- [x] `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs`, lifecycle contracts/service above, command emitters, `State/PendingCommands/PendingCommandPollingCoordinator.cs`, and `Infrastructure/EventStore/EventStoreCommandClient.cs` — preserve distinct outcomes, stable acceptance budget, post-await deadline checks, zero preaccept retries and one postack retry preserving MessageId.
- [x] `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcScopeBlocked.razor`, Home/layout and command-denial consumers — integrate actual loading outcomes and activation-owned AM-26; retain adjacent stories' speech contracts.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/`, `tests/Hexalith.FrontComposer.SourceTools.Tests/`, and `tests/e2e/specs/projection-command-announcements.spec.ts` (new) — verify matrix, copy/channel/count, timing races, focus, redaction and grid regressions; update snapshots.

**Review re-derivation tasks:**
- [x] `src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs` and `Components/Rendering/FcSurfaceStatus.razor` — prevent any older pending group on the same surface from replacing a newer terminal/focused outcome; reject queued callbacks after a surface rebind. Preserve per-operation dedupe.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`, `FluxorActionsEmitter.cs`, Shell `State/DataGridNavigation/LoadedPage*.cs`, and `Components/Rendering/FcProjectionEmptyPlaceholder.razor` — derive speech from completed projection/page and normalized filter identities, cancel abandoned/background Loading, distinguish filtered-zero from initial empty and page failure, and keep only the shared polite status node.
- [x] `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcMaxItemsCapNotice.razor.cs`, `FcExpandedRowHiddenBanner.razor.cs`, `FcSlowQueryNotice.razor.cs`, and `FcExpandInRowDetail.razor*` — use result/limit and row/filter-result group identities for repeat transitions, time the earliest outstanding page request, and retain standalone suppression semantics without double speech in generated grids.
- [x] `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.cs` and connection/reconciliation state — cancel pending reconnect speech on Connected, gate recovery on actual browser online state, and give a new offline episode speech even if it follows recovery inside the same transport epoch; cancel recovery expiry when offline returns.
- [x] `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs` and `Infrastructure/EventStore/EventStoreCommandClient.cs` — rehydrate the acceptance budget when replay starts at Degraded, describe preaccept transport outcomes as unknown where acceptance cannot be proven, and use the one same-MessageId postack retry for a truncated accepted body.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/`, `tests/Hexalith.FrontComposer.SourceTools.Tests/`, and `tests/e2e/` — add fake-time regressions for competing group timers, repeat cap/detail events, offline/recovery races, completed filter/page identities, concurrent slow pages, Degraded replay, malformed accepted-body retry, single live node, stale clearance, and AM-08 speech.

**Review iteration 2 re-derivation tasks:**
- [x] Announcement coordinator and status subscribers: make initial replay and subscription atomic; version surface deliveries so an older unlocked callback cannot replace a newer terminal; preserve repeat identical-copy DOM mutation; reject old lifecycle callbacks after rebinding; dispose late browser-connectivity registrations.
- [x] Projection/page pipeline: compare complete normalized request fields without 32-bit collisions or raw/trim disagreement; clear failed provenance after successful 304; use completed page identity for cap notices; preserve cached rows on offscreen page failure; retry the failed projection/page in place without discarding local state.
- [x] Connection and grid presentation: give each browser offline/recovery episode a fresh speech group even inside one transport epoch; restore current disconnected speech when browser online returns; focus Clear Filter only on the first hidden-detail transition; keep a standalone suppression status mounted empty before its text changes.
- [x] Command lifecycle consumers: use one configured acceptance deadline across wrapper/polling; map every merged terminal state exactly; count and describe Warning/DegradedExhausted honestly in pending summary; allow every terminal state through the next-submit guard; clear the acceptance anchor on Idle.
- [x] Runtime tests and snapshots: exercise all new bridge outcomes through the generated form, repeat hidden-row results at the generated grid boundary, callback/subscribe/disposal races, request identity and 304 recovery, offscreen page failure and in-place retry, browser episode recovery, and command state/reset paths; preserve the passing Story 13.4 and adjacent gates.

**Review iteration 3 re-derivation tasks:**
- [x] Connectivity and stale projection: make browser offline evidence label cached rows immediately; retry transient JS watcher setup safely; clear recovery speech whenever transport disconnects or reconciliation restarts; ignore stale queued connection callbacks. Cover actual browser offline events and repeated recovery.
- [x] Virtualized page provenance: retain current filter/request and page-offset result identities independently; reject older cross-filter completions; retain a failed page’s retry handle when an unrelated page succeeds; use only relevant successful results for cap and hidden-detail speech; clear obsolete cap/hidden announcements when notices disappear.
- [x] Command and failure outcomes: bound reading an accepted response body before pending tracking, returning accepted/unknown correlation when the optional read fails and no retry is configured; preserve classified safe projection failure copy, including section-updating; show one nonredundant Degraded status with its action.
- [x] Accessibility contracts: keep standalone detail suppression visually hidden while its polite node stays mounted; verify first hidden-row focus and no repeat focus; verify the new bridge actions reach the lifecycle service with their exact outcome.
- [x] Regression and gate evidence: add fake-time/browser tests for stale/recovery/cap/hidden cancellation, cross-page completion order and retry provenance, accepted-body stall/read errors, listener setup recovery, classified failure and Degraded display; rerun the matrix and adjacent Shell, SourceTools, Chromium, and accessibility gates.

**Review iteration 4 re-derivation tasks:**
- [x] `Services/Announcements/SurfaceAnnouncementCoordinator.cs` and generated projection surface names — preserve the final eligible A in A→B→A trailing bursts and make non-grid surface ownership include the fully qualified projection identity; retain atomic replay and per-operation dedupe.
- [x] `Services/Lifecycle/LifecycleStateService.cs`, `Components/Lifecycle/FcLifecycleWrapper.razor.cs`, and `SourceTools/Emitters/CommandFormEmitter.cs` — finish idempotent confirmation through the form's success/dirty-state callback, align custom visible idempotent copy with its single polite message, and retain terminal first-wins and accepted budgets.
- [x] `Components/EventStore/FcProjectionConnectionStatus.razor.cs` and connectivity tests — reconcile initial browser online evidence with scoped offline state on remount; schedule safe retry after transient watcher setup failure even if the page stays quiet; preserve disposal and episode ordering.
- [x] `State/DataGridNavigation/LoadedPage*.cs`, fallback refresh, `SourceTools/Emitters/RazorEmitter.cs`, and stale/failure rendering — reject same-text A→B→A stale completions using request generation, clear obsolete last-result and primary provenance, bound result metadata with cached pages without losing current failed offset retry, label cached rows after failed refresh, and localize null-items copy.
- [x] `Components/DataGrid/FcMaxItemsCapNotice.razor.cs`, `FcExpandedRowHiddenBanner.razor.cs`, and `Components/Rendering/FcScopeBlocked.razor.cs` — cancel notice speech on disposal; use focus alone for first hidden-row transition and shared polite speech for later distinct results; retry required focus after transient interop failure on a later render.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/`, `tests/Hexalith.FrontComposer.SourceTools.Tests/`, and `tests/e2e/` — prove filtered/sorted/searched retry criteria and generated stale warning with cached rows, A→B→A races, idempotent form completion, quiet-page watcher recovery, metadata bound, surface isolation, localized failure, focus-only first transition, and remount/cancellation; rerun matrix and adjacent gates.

**Review iteration 5 re-derivation tasks:**
- [x] `Components/Rendering/FcProjectionStaleNotice.razor`, `State/DataGridNavigation/LoadedPageReducers.cs`, and `SourceTools/Emitters/RazorEmitter.cs` — give every offline/failed-refresh episode its own speech identity, cancel an obsolete failure group when 304 or successful result recovers, and retain labelled cached rows without duplicate speech.
- [x] `State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs`, `Components/EventStore/FcProjectionConnectionStatus.razor.cs`, and `Resources/FcShellResources*.resx` — announce successful recovery even with no changed rows, but use copy that does not claim data was refreshed when no lane was read.
- [x] `Infrastructure/EventStore/EventStoreCommandClient.cs` — classify malformed accepted JSON shape or nonstring correlation as unreadable optional body, preserving accepted/unknown MessageId, bounded read, one configured same-MessageId postack retry and safe logs.
- [x] `Components/DataGrid/FcExpandedRowHiddenBanner.razor*`, `wwwroot/js/fc-focus.js`, `Components/Rendering/FcScopeBlocked.razor.cs`, and `Components/EventStore/FcPendingCommandSummary.razor` — make first hidden-row focus convey the visible reason through an accessible description; use the JS boolean outcome to retry failed focus; remove duplicate polite speech for the same terminal command while preserving summary content and blocked-scope heading focus.
- [x] `SourceTools/Emitters/RazorEmitter.cs`, `Components/DataGrid/FcSlowQueryNotice.razor.cs`, and Shell page state — count active search and status-chip values as filters for filtered-zero/cap copy; anchor AM-08 at request registration through remount; marshal state/timer reconciliation onto the renderer; cancel in-place failed-page Retry when its view owner is disposed.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/`, `tests/Hexalith.FrontComposer.SourceTools.Tests/`, and `tests/e2e/specs/` — prove repeated offline/failure episodes, 304 speech clearance, no-lane recovery copy, malformed accepted shapes, contextual hidden focus, single command speech path, search/chip zero results, remount slow budget, background state delivery, retry disposal, and browser active-element assertions for hidden detail and blocked scope. Rerun the matrix and adjacent gates.

**Review acceptance:**
- Given two pending groups share a surface, when a terminal outcome arrives, then no older timer replaces it; when a status component binds another surface, then queued old callbacks stay silent.
- Given a second cap, hidden-detail, filtered-zero, or page-failure result, when its completed identity differs, then exactly one corresponding message speaks; a background result cancels stale Loading, and an initial empty surface has one polite status node.
- Given overlapping page requests, when the earliest outstanding request crosses 1,999/2,000 ms, then AM-08 appears at 2,000 ms and clears after the relevant work settles.
- Given browser offline returns after recovery or during reconciliation, when late recovery and clear timers fire, then offline remains visible and truthful; Connected cancels obsolete reconnect speech.
- Given Degraded replay or an accepted response with a truncated body, when the deadline or retry boundary arrives, then 120-second closure and the one 250-ms same-MessageId retry still occur; an unproven preaccept result never claims non-acceptance.

**Review iteration 2 acceptance:**
- Given a pending timer or status binding races a terminal/rebind, when callbacks arrive in either order, then only the latest surface revision is rendered; a publication between bind and replay is not lost, and late connectivity imports leave no watcher behind.
- Given distinct raw request fields, a successful 304 after failure, a capped or offscreen page result, or Retry, when each settles, then provenance matches the actual request, cached rows remain available, each result speaks once, and local input survives retry.
- Given browser offline/online cycles inside one transport epoch, when recovery or fallback repeats, then each episode speaks truthfully and an online return with disconnected transport restores fallback speech.
- Given Warning, NeedsReview, IdempotentConfirmed, or DegradedExhausted, when generated forms, pending summary, and merged terminals consume them, then each displays its own state, allows the next valid attempt, and uses the same configured deadline as polling.
- Given repeated hidden-row results, when only the completed identity changes, then speech repeats without moving focus; standalone suppression updates an already-mounted polite node.

**Review iteration 3 acceptance:**
- Given an offline browser with cached rows or a transient connectivity-module failure, when transport state has not caught up or the shell rerenders, then rows are labelled stale, offline speech appears, and watcher setup can recover without a leak.
- Given recovery followed by disconnect or a new reconcile, when old timers and messages settle, then no stale recovery speech remains; connected state and later recovery use their own episode identity.
- Given overlapping virtualized offsets and changing filters, when completions arrive out of order, then the current request keeps its speech and a failed page keeps its retry until that page succeeds; unrelated results do not repeat cap/hidden-detail speech, and invisible notices clear their own message.
- Given accepted headers with a stalled, malformed, or unreadable optional body, when its bounded wait or configured retry ends, then pending tracking starts with the original MessageId and an honest unknown correlation; safe section-updating failure copy remains distinct.
- Given Degraded, hidden-detail and standalone suppression, when rendered, then only one useful outcome bar appears, first hidden transition focuses Clear Filter, repeat results leave focus alone, and standalone speech stays visually hidden in a persistent polite node.

**Review iteration 4 acceptance:**
- Given Submitting and a distinct idempotent confirmation, when the generated form receives it, then its success callback runs once, dirty state clears, and the visible custom outcome matches the single polite announcement.
- Given A→B→A within one 250-ms announcement window or one grid/fallback request sequence, when old callbacks settle, then only the final eligible A speaks or updates data; separate projection namespaces cannot cancel each other's speech.
- Given a remount after browser online or a transient watcher setup failure on a quiet page, when setup completes or its safe retry fires, then scoped offline state and stale labels match browser evidence and exactly one watcher remains.
- Given virtualized page eviction, cached rows after a failed refresh, or a null-items response, when rendered, then metadata stays bounded, retained rows are labelled stale, the failed offset keeps in-place retry, and visible/speech failure copy is safe and localized.
- Given a first hidden-detail result or blocked scope, when focus interop succeeds, then focus alone conveys that first event; a transient failure can retry focus, later distinct hidden results speak once, and disposed notices cannot replay old speech.
- Given a filtered, sorted, searched generated grid with cached rows, when failed-page Retry or browser offline is exercised through the generated view, then the loader receives the current criteria and the view shows and speaks the stale warning.

**Review iteration 5 acceptance:**
- Given offline→online→offline or failed-refresh→recovery→failed-refresh in one transport epoch, when each episode settles, then each distinct stale warning speaks once; a successful 304 withdraws prior failure speech.
- Given successful reconciliation with no eligible data lane, when recovery appears, then the operator hears truthful connection copy without a claim that rows were refreshed.
- Given accepted headers and an optional JSON array or nonstring `correlationId`, when body parsing ends, then pending tracking starts with the original MessageId and unknown correlation, with no false dispatch-failure claim.
- Given a first hidden expanded row, when Clear filter receives focus, then its accessible description conveys why; if focus returns false, a later render retries. A terminal command updates only one polite speech path, while summary content remains visible.
- Given search or status-chip filters, a pending query mounted late, or a failed-page retry followed by disposal, when each path runs, then zero rows use filtered-zero copy, cap speech is suppressed, AM-08 keeps its original two-second anchor, background callbacks are renderer-safe, and the abandoned retry is cancelled.
- Given hidden-row and blocked-scope transitions in the generated browser specimen, when focus moves, then the intended button or heading is `document.activeElement` and later hidden results do not refocus.

**Acceptance Criteria:**
- Given Submitting at 0ms and Acknowledged at 100ms, when time reaches 349/350ms then silence becomes AM-11; when Syncing precedes Confirmed within 250ms then the exact sequence is [AM-11, AM-13]. Given connection and load/filter groups, when 249/250ms elapse then equivalent trailing-window assertions pass.
- Given pending work, when time crosses 1,999/2,000, 9,999/10,000 and 119,999/120,000ms then canonical states/messages occur once, status polling stays silent at 1s, exhaustion ends admission/polling, and later evidence requires new correlation. Given postack transient failure, when 249/250ms elapse then exactly one same-identity retry occurs.
- Given reconnect/filter/grid regression scenarios, when exercised then retries cap at 30s, closed restart is within 10s, fallback polls every 15s across at most eight lanes, recovery notice lasts 3s with silent expiry, and resettable debounce, 500-row virtualization, 10,000 cap, labelled detail, column priority above 15 and icon/shape plus text remain valid.

## Implementation Notes

- Iteration 0 code was reverted after independent review. Rebuild all unchecked tasks from this spec; the verification results below describe that prior iteration and must be repeated for the re-derived implementation.
- Implemented scoped, fake-time announcement ownership across generated projections, connection status, command lifecycle, grid notices, and blocked-scope focus. Generated projection failures use safe copy and replace empty state; retries after accepted command responses preserve MessageId.
- Runtime tests cover burst and terminal speech, projection empty/failure/stale/recovery, 2s/10s/120s thresholds, same-identity post-ack retry, and missing/stale scope focus. Existing grid, authorization, transport, and accessibility suites remain in the broad gates.

- Iteration 2 code was reverted after the second independent review. The iteration 1 staged tree was preserved as Git tree `401bf99f2e8aff09ae517b7a1b7fc2013c3345d4` for audit, while the non-frozen tasks below govern re-derivation. All iteration 1 verification is historical until rerun.

- Iteration 3 code was reverted after the third independent review. The iteration 2 staged tree was preserved as Git tree `0cb2f44589fdace46b29728ca533ce90550a72b6` for audit. Verification from iteration 2 is historical until the next implementation passes again.

- Iteration 4 code was reverted after the fourth independent review. The iteration 3 staged tree was preserved as Git tree `1a665e4751f808a976464543ec7f67afc6024e5b` for audit. Rebuild all unchecked tasks; the iteration 3 verification is historical until the new implementation passes again.

- Iteration 5 code was reverted after the fifth independent review. The iteration 4 staged tree was preserved as Git tree `148d2b84aa32852ea86e3fee40c526d8d2af0f91` for audit. Rebuild all unchecked tasks; iteration 4 verification is historical until the next implementation passes again.

## Spec Change Log

- Iteration 1, 2026-10-05: Independent review found operation/result identity and cross-surface ordering gaps, concurrent-page timing, offline/recovery races, replay and accepted-body retry gaps, and duplicate/silent accessibility channels (BH-01, BH-03–13, VG-04–05, EC-05–07/09). Added the review re-derivation tasks and Given/When/Then checks above. The known-bad state is a passing focused suite with stale or missing speech on later results and race boundaries. **KEEP:** scoped fake-time coordinator, canonical EN/FR copy, safe query-failure redaction, distinct terminal lifecycle identities, first-terminal-wins, stable acceptance deadline, one postack same-MessageId retry, browser offline evidence, generated projection integration, and passing broad Shell/SourceTools/browser/accessibility coverage. Do not change the frozen approved intent.

- Iteration 2, 2026-10-05: Review found races in delivery/subscription/disposal, incomplete request and result provenance, repeated recovery identity, offscreen failure replacement, a full-page Retry, and new terminal outcomes missing from form and summary consumers (BH2-01–12, VG2-01–02, EC2-01–06/09). Added the iteration 2 re-derivation tasks and acceptance above. The known-bad state is a passing suite with stale speech, hidden usable rows, or mislabelled terminal commands under specific races and repeat results. **KEEP:** the scoped fake-time coordinator; single persistent polite node and keyed repeated-copy update; canonical EN/FR safe copy; completed filtered-zero/page-failure runtime test; first-terminal-wins; stable acceptance identity and one postack same-MessageId retry; browser offline evidence; generated view integration; and broad Shell, SourceTools, Chromium, and accessibility gates. Preserve the frozen approved intent and the iteration 1 KEEP constraints.

- Iteration 3, 2026-10-05: Review found missing cancellation of obsolete visible speech, browser offline/stale and watcher retry gaps, view-wide virtualized page provenance masking other offsets, unbounded accepted-body reads, lost classified failure copy, and redundant Degraded display (BH3-01–11, EC3-01/03–06/08, VG3-01–03). Added the iteration 3 tasks and acceptance above. The known-bad state is a passing suite where one older page or connectivity event misstates current data, a command can hang after accepted headers, or a notice remains after its condition clears. **KEEP:** atomic versioned status replay; keyed repeat-copy DOM mutation; scoped fake-time coordinator; safe localized EN/FR copy; per-episode browser recovery; collision-free request field encoding; in-place page retry preserving rows; distinct generated command outcomes and configurable acceptance budget; first-terminal-wins and one same-MessageId postack retry; the full Shell/SourceTools/browser/a11y passing gates. Respect all earlier KEEP constraints and preserve the frozen approved intent.

- Iteration 4, 2026-10-05: Review found idempotent form and speech divergence, quiet-page connectivity and remount gaps, trailing A→B→A and grid request-generation errors, unbounded page-result metadata, surface collisions and disposed speech, duplicate hidden-detail focus/live output, stale cached rows after query failure, and untranslated null-items copy (BH4-01–11, VG4-04, EC4-02/06). Added iteration 4 tasks and Given/When/Then checks above. The known-bad state is a passing suite that can speak an older burst, accept an old same-text page, leave a watcher absent, or mislead an operator with silent/stale results. **KEEP:** scoped fake-time coordinator and atomic versioned replay; per-offset page provenance and in-place retry; browser offline evidence and episode cancellation; safe classified EN/FR failure copy; one useful Degraded bar; bounded accepted-body read and original MessageId retry; configurable 10s/120s command budget, first-terminal-wins, and the broad Shell/SourceTools/Chromium/accessibility green gates. Preserve all earlier KEEP constraints and the frozen intent.

- Iteration 5, 2026-10-05: Review found stale episodes suppressed within one epoch, failure speech surviving 304 recovery, no-lane recovery overclaiming refreshed data, malformed accepted-body shapes escaping as dispatch failure, hidden focus without its reason, duplicate terminal live channels, search/chip filters misclassified, SlowQuery remount/race gaps, and ownerless failed-page retry work (BH5-01–08/10, EC5-02/03, VG5-03). Added iteration 5 tasks and Given/When/Then checks above. The known-bad state is a passing suite where an accepted command may appear failed, later stale data may be silent, or operators hear incorrect filter/recovery state. **KEEP:** atomic single-surface status replay and trailing A→B→A behavior; namespace-isolated surfaces; idempotent generated form completion and custom copy; browser watcher remount and quiet retry; generation-safe page/fallback completion, bounded metadata, classified localized failures and cached-row labels; focus-only first hidden transition with unrelated speech preserved; accepted-body deadline, first-terminal-wins, original MessageId retry, and all green Shell/SourceTools/Chromium/accessibility gates. Preserve earlier KEEP constraints and frozen approved intent.

## Review Triage Log

### Review iteration 1 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | medium | bad_spec | A terminal on one surface leaves a different group timer live; its later flush replaces the terminal message. Projection stale/load groups make this reachable. |
| BH-02 | low | reject | Completed groups and unsubscribed surfaces remain in circuit memory, but ordinary use adds small bounded-per-operation objects; pruning needs lifecycle policy beyond a direct correction. |
| BH-03 | medium | bad_spec | The cap group closes permanently and uses the same limit identity for every result; a second visible cap is silent. AM-09 requires result/limit identity. |
| BH-04 | medium | patch | The hidden-detail group closes for the view, so a second row/filter result is silent despite ResultIdentity; adding that existing identity to the group is direct. |
| BH-05 | medium | bad_spec | Browser offline after recovery reuses a closed epoch group and leaves the recovery-clear timer live, suppressing AM-30 and clearing offline status. |
| BH-06 | medium | patch | Refreshed does not check _offline, so a late reconcile announces recovery while the browser is offline; an existing-state guard suffices. |
| BH-07 | medium | bad_spec | Grid-navigation change invokes projection speech with old items and a new filter, so no-matches can be announced before the new result settles. |
| BH-08 | medium | bad_spec | Page failure speech uses projection correlation rather than page request identity; the first terminal failure closes later failures or retry results on the view. |
| BH-09 | medium | bad_spec | SlowQuery selects an arbitrary pending page TCS; with concurrent virtualized pages it can restart or miss the earliest 2s deadline. |
| BH-10 | medium | patch | A wrapper replaying Degraded skips acceptance-anchor and deadline setup, so its own 120s closure can be absent when polling is unavailable; extend the existing replay case. |
| BH-11 | high | patch | A lost response can leave acceptance unknown, but the preaccept warning states non-acceptance and invites a fresh submit; truthful uncertainty copy is a direct correction. |
| BH-12 | medium | bad_spec | A truncated accepted JSON body returns null instead of retrying, so the approved postack same-MessageId retry is skipped. |
| BH-13 | medium | patch | A queued callback from an old Surface can overwrite a rebound FcSurfaceStatus; capture and compare the bound surface before applying it. |
| VG-01 | medium | defer | The caller-owned release provenance test gap is in the pre-existing HEAD release commit, outside Story 13.4; add a candidate publisher provenance fixture separately. |
| VG-02 | low | patch | The stale notice test checks removal of visible copy but not coordinator clearance; deleting Cancel would pass it and leave stale speech. |
| VG-03 | low | patch | SlowQuery tests assert visible notice only; removing AM-08 publication would pass them. Assert shared status at 2s and settlement. |
| VG-04 | medium | patch | Generated views add FcSurfaceStatus while the empty placeholder retains implicit polite role=status, creating two status regions for an empty projection. |
| VG-05 | medium | bad_spec | The fixed cap and hidden-detail terminal groups suppress later distinct results; same verified outcomes as BH-03 and BH-04. |
| EC-01 | medium | defer | Terminal-to-Idle is rejected by LifecycleStateService while Fluxor can reset locally; the same behavior existed for Confirmed/Rejected before this story, and a new attempt needs a new correlation. |
| EC-02 | medium | patch | The old-surface queued callback race is reachable on correlation rebinding; same verified outcome as BH-13. |
| EC-03 | medium | patch | A second hidden-detail transition is silenced by the closed view group; same verified outcome as BH-04. |
| EC-04 | medium | bad_spec | A later cap transition is silenced by the closed view group; same verified outcome as BH-03. |
| EC-05 | medium | patch | When a background load with rows settles before 250ms, the queued Loading message is not cancelled because LastResultOperatorInitiated is false. |
| EC-06 | medium | bad_spec | A filtered grid may have zero page rows while projection Items remains nonempty; the current count misses AM-27 no-matches. |
| EC-07 | medium | patch | Connected with failed reconciliation leaves the pending reconnect timer to speak after connection recovery; cancel it on Connected. |
| EC-08 | low | reject | Circuit-long group retention repeats BH-02; the likely everyday impact is negligible and bounded eviction needs extra policy. |
| EC-09 | medium | bad_spec | FcExpandInRowDetail still exposes SuppressedAnnouncement, but removing its live node makes standalone callers silent; preserve its public suppression semantics without duplicate generated speech. |
| EC-10 | false | reject | The authoritative Story 13.4 AC explicitly focuses the replacement heading when missing/stale scope renders; the one-time focus is the required AM-26 path, including scope loss. |

Grouped root causes: coordinator ordering (BH-01); repeated result identity (BH-03/04, VG-05, EC-03/04); offline/recovery ordering (BH-05/06, EC-07); projection/page provenance (BH-07/08, EC-05/06); concurrent slow pages (BH-09); lifecycle replay (BH-10); transport truth/retry (BH-11/12); rebound status (BH-13, EC-02); duplicate live node (VG-04); standalone detail contract (EC-09). The bad-spec groups trigger re-derivation; lower patch/defer routes are moot until the next review.

### Review iteration 2 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH2-01 | medium | bad_spec | Wrapper hard-codes 120 seconds while pending polling uses configurable `MaxPendingCommandPollingDurationMs`; a nondefault option makes two authorities close at different times. |
| BH2-02 | medium | patch | The callback checks correlation before queuing `InvokeAsync` but applies after a rebind without rechecking, so an old transition can mutate a new surface. |
| BH2-03 | medium | bad_spec | `FcSurfaceStatus` reads `Current` before `Subscribe`; a publication between calls can be missed until another message. Atomic replay needs an ownership contract. |
| BH2-04 | medium | bad_spec | Connectivity module import and watcher registration await without disposal checks; `DisposeAsync` can finish first and leave a later watcher/reference live. |
| BH2-05 | high | bad_spec | `PageRequestIdentity` hashes trimmed fields to 32 bits while the request uses raw fields; distinct loads can collide or be treated as current incorrectly. |
| BH2-06 | medium | bad_spec | Successful 304 removes its TCS but leaves prior failed result provenance; the generated failure replacement remains after a valid cached response. |
| BH2-07 | medium | patch | Cap notice receives projection correlation rather than completed page identity, so later capped pages under one projection result are silent. |
| BH2-08 | medium | patch | A changed hidden-result identity sets `_focusPending` even while the row stays hidden; background refresh steals focus from the operator. |
| BH2-09 | medium | bad_spec | Recovery is terminal in the transport epoch group, so a second browser online recovery within that epoch is suppressed. |
| BH2-10 | medium | bad_spec | Browser online cancels the offline group but does not restore the still-current disconnected warning when transport remains disconnected. |
| BH2-11 | high | bad_spec | Any failed virtualized page causes `EmitFailureShell` to replace the whole projection, hiding usable cached rows after an offscreen page failure. |
| BH2-12 | medium | bad_spec | `FcProjectionFailure.Retry` calls `Navigation.Refresh`, discarding local state and focus for a projection/page failure instead of retrying that request in place. |
| VG2-01 | medium | patch | The generated bridge dispatches five new outcomes, but executable integration tests exercise only Confirmed/Rejected; a wrong new action could pass existing tests. |
| VG2-02 | medium | patch | Component tests cover repeated hidden identities, but the generated grid test checks only the first result; an emitter identity omission could pass. |
| EC2-01 | high | bad_spec | Coordinator flush captures handlers before unlocking; an older delivery can run after a newer terminal delivery and replace its speech. |
| EC2-02 | medium | bad_spec | The status `Current`/`Subscribe` gap is the verified BH2-03 race. |
| EC2-03 | medium | bad_spec | `FcPendingCommandSummary` does not count Warning or DegradedExhausted and defaults their entry text to Confirmed; both statuses are now reachable from the pending service. |
| EC2-04 | high | patch | Generated `SubmitAsync` permits only Idle/Rejected/Confirmed, so new terminal states block the next submission as pending. |
| EC2-05 | high | bad_spec | `MergedTerminal` maps every nonconfirmed status to Rejected and idempotent to Confirmed, losing the newly distinct outcomes before the form displays them. |
| EC2-06 | medium | patch | Wrapper `Idle` leaves `_acceptanceAt` set; an allowed nonterminal reset and later acknowledgement under the same correlation inherits the first deadline. |
| EC2-07 | medium | defer (carried) | Carried from iteration 1 EC-01: terminal-to-Idle divergence predates this story and the same service guard remains; a new attempt uses a fresh correlation. |
| EC2-08 | low | reject (carried) | Carried from iteration 1 BH-02/EC-08: circuit-long group retention remains low impact in ordinary use and bounded eviction needs extra policy. |
| EC2-09 | medium | patch | Standalone detail suppression now inserts a populated live node conditionally; the prior always-mounted empty node was needed for a later text update to speak. |

Grouped root causes: delivery and binding order (BH2-02/03, EC2-01/02), lifecycle budgets and terminal consumers (BH2-01, EC2-03–06, VG2-01), request/page provenance and fallback (BH2-05–08/11/12), connectivity episode and disposal (BH2-04/09/10), and standalone/generated grid speech (VG2-02, EC2-09). The bad-spec entries trigger re-derivation; patch/defer entries are moot for this iteration.

### Review iteration 3 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH3-01 | medium | patch | Transport disconnect cancels recovery expiry but leaves the recovery group’s spoken message active; a brief disconnect can leave “restored” speech after the visible state changes. |
| BH3-02 | medium | bad_spec | Browser offline only changes component-local `_offline`; `FcProjectionStaleNotice` reads transport state, so cached rows can remain unlabelled until transport detects loss. |
| BH3-03 | medium | bad_spec | Connectivity setup is attempted only on first render and JS failures are swallowed; transient import/registration failure disables browser offline evidence for that component lifetime. |
| BH3-04 | high | patch | Accepted response headers establish admission, but an `IOException`/`HttpRequestException` body read escapes when retry count is zero, reporting failure rather than accepted/unknown correlation. |
| BH3-05 | high | bad_spec | `LastResultByKey` is one slot per view; an older offset/request can overwrite the current filter result after its speech and failure decisions. |
| BH3-06 | medium | bad_spec | Any successful page clears view-wide `FailureByKey`; a failed offset loses retry provenance when a different offset settles. Same view-wide ownership flaw as BH3-05. |
| BH3-07 | medium | bad_spec | Hidden-detail identity includes every completed page ID, so an unrelated virtualized page repeats the hidden-row message under unchanged filter and row conditions. |
| BH3-08 | medium | patch | Standalone suppression was restored as an unstyled `div`, whereas its former live node was visually hidden; text now appears as extra page content. |
| BH3-09 | medium | bad_spec | The loader stores safe section-updating copy for shape mismatch, but `FcProjectionFailure` always displays generic query failure, losing the distinct recoverable classification. |
| BH3-10 | medium | patch | A failed page advances the ID passed to cap notice while item count falls back to cached rows, causing a new cap announcement for a failed fetch. |
| BH3-11 | medium | bad_spec | Degraded during ActionPrompt renders two warning bars, and the new outcome bar repeats its title as body; this adds redundant visible status under the story’s no-noise intent. |
| VG3-01 | medium | patch | bUnit mocks the connectivity JS module and calls .NET directly; no browser test exercises the `offline` listener, so removing its registration would pass current tests. |
| VG3-02 | medium | patch | Hidden-row tests cover banner and repeated speech but never assert the JS focus call or stable focus after a repeat result. |
| VG3-03 | medium | patch | Generated-form tests assert new Fluxor actions but not their bridge-to-`ILifecycleStateService` mappings, so a wrong service outcome could pass. |
| EC3-01 | high | bad_spec | `ResponseHeadersRead` completes at accepted headers while optional body read has no bounded deadline; a stalled body prevents pending tracking and its 120-second budget from starting. |
| EC3-02 | medium | defer (carried) | Carried from iteration 1 EC-01 and iteration 2 EC2-07: terminal-to-Idle rejection predates this story; a new attempt uses a fresh correlation. |
| EC3-03 | medium | patch | Cap notice sets `_wasVisible` false without cancelling its active announcement, leaving obsolete limit speech after filters/count hide the notice. |
| EC3-04 | medium | patch | Hidden-row banner similarly does not cancel its active group when the detail becomes visible again. |
| EC3-05 | medium | patch | A queued disconnect callback can overwrite a newer connected snapshot read in `OnParametersSet`; the component does not check the current connection state before reconciling. |
| EC3-06 | medium | patch | Starting reconciliation clears visible recovery and its timer but does not cancel the recovery announcement group, leaving old recovery speech. |
| EC3-07 | low | reject (carried) | Carried from iterations 1–2: group history remains low impact in ordinary use, while bounded eviction would require extra policy. |
| EC3-08 | medium | patch | The standalone suppression styling regression is the same verified outcome as BH3-08. |

Grouped root causes: connectivity status and cancellation (BH3-01–03, EC3-05/06, VG3-01), page and result ownership (BH3-05–07/10, EC3-03/04), accepted-body certainty and timing (BH3-04, EC3-01), classified/visual copy (BH3-08/09/11, EC3-08), and focus/bridge verification (VG3-02/03). Bad-spec entries trigger re-derivation; patch and defer routes are moot until the next review.

### Review iteration 4 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH4-01 | medium | bad_spec | Generated `OnStateChanged` clears dirty state and invokes `OnConfirmed` only for `Confirmed`; the new `IdempotentConfirmed` path skips success navigation and abandonment cleanup. |
| BH4-02 | medium | bad_spec | On mount with browser online, connectivity setup does not clear scoped `BrowserOffline`; an online transition while the prior watcher was unmounted leaves cached rows stale until another event. |
| BH4-03 | medium | bad_spec | Coordinator `Seen` rejects the final A in a pending A→B→A burst, so B wins the trailing window despite A being the latest eligible state. |
| BH4-04 | medium | bad_spec | Changing grid request clears page results but retains last result and successful primary provenance; returning to an earlier request can reuse its old failure, cap, or hidden-detail result. |
| BH4-05 | medium | bad_spec | A fallback refresh lacks a provider completion token and stale guard compares only request text; A→B→A lets the old A response replace the new A result. |
| BH4-06 | medium | bad_spec | Page-cache eviction removes `PagesByKey` but leaves `ResultsByPage` for each visited offset, so virtualized scrolling grows per-circuit result metadata beyond `MaxCachedPages`. |
| BH4-07 | medium | bad_spec | Non-grid generated surfaces use only the simple projection type name; distinct namespaces with the same type name share announcements and clears. |
| BH4-08 | medium | bad_spec | Cap and hidden-detail components do not cancel their owned terminal groups on disposal; a remount on the same surface can replay speech for a removed notice. |
| BH4-09 | high | bad_spec | The first hidden-detail transition both publishes AM-22 to the live status and focuses Clear Filter, giving one event two speech paths against the frozen single-path rule. |
| BH4-10 | medium | bad_spec | A failed refresh retains cached pages and the grid shows them with an inline failure, but stale labelling is limited to disconnected transport, leaving retained rows without an age warning. |
| BH4-11 | medium | bad_spec | Null-items failure provenance stores hard-coded English copy that the generated view displays and speaks directly, bypassing the French AM-30 resource. |
| VG4-01 | medium | patch | The filtered retry runtime test checks only offset, size, and identity; empty query criteria would pass it and fetch unfiltered rows. Assert filter, sort, and search in the dispatched action. |
| VG4-02 | medium | patch | Only the stale-notice component test executes stale copy; removing the generated attachment would pass current generated/browser checks. Test cached rows and shared speech at the generated boundary. |
| VG4-03 | medium | defer (carried) | carried from EC-01/EC2-07/EC3-02: terminal-to-Idle rejection is the same pre-existing service guard; new attempts use a fresh correlation. |
| VG4-04 | medium | bad_spec | Connectivity import/watch failure is swallowed without a scheduled retry; the current test forces a later render, while a quiet page can keep the offline watcher absent. |
| EC4-01 | medium | defer (carried) | carried from EC-01/EC2-07/EC3-02: the terminal-to-Idle guard still rejects reset, as previously classified outside this story. |
| EC4-02 | medium | bad_spec | The unbounded `ResultsByPage` entry retention is the same verified cache-boundedness defect as BH4-06. |
| EC4-03 | low | reject (carried) | carried from BH-02/EC-08/EC2-08/EC3-07: circuit-long group history remains low impact in ordinary use, and pruning requires an ownership policy. |
| EC4-04 | medium | patch | Hidden-detail focus clears `_focusPending` before JS interop; a transient failure prevents a later render from retrying required Clear Filter focus. |
| EC4-05 | medium | patch | Blocked-scope heading marks focus requested before the interop succeeds; a transient failure prevents later focus retry for that activation. |
| EC4-06 | medium | bad_spec | An idempotent outcome speaks generic AM-13 while the visible Info bar can show custom `IdempotentInfoMessage`; speech and visible outcome disagree. |

Grouped root causes: lifecycle idempotent consumers (BH4-01, EC4-06); connectivity initialization (BH4-02, VG4-04); trailing dedupe (BH4-03); grid provenance and cache ownership (BH4-04–06, EC4-02); surface ownership and stale copy (BH4-07/08/10/11); hidden-detail channel and focus (BH4-09, EC4-04/05); generated-boundary verification (VG4-01/02). Bad-spec entries trigger re-derivation; patch entries are moot. Carried defer/reject rows remain recorded without repeating the prior action.

### Review iteration 5 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH5-01 | medium | bad_spec | Stale-notice group uses connection epoch and offline/failed boolean; Cancel keeps Seen, so a second offline episode within the same epoch cannot speak. |
| BH5-02 | medium | bad_spec | Reconciliation completes as Refreshed even when the scheduler has no eligible lane; AM-07 says “Data refreshed” although no data was read. Recovery still needs truthful speech. |
| BH5-03 | high | bad_spec | Accepted headers establish admission, but a JSON array or numeric correlation throws `InvalidOperationException` outside the optional-body catches and reports dispatch failure instead of accepted/unknown MessageId. |
| BH5-04 | medium | bad_spec | First hidden-detail focus lands on a Clear filter button with no accessible description of the hidden expanded row; suppressing AM-22 live text leaves the event unexplained to assistive technology. |
| BH5-05 | medium | bad_spec | Pending summary remains a polite live region while wrapper terminal speech enters the shared status; the same resolved command can update both channels. |
| BH5-06 | medium | bad_spec | Generated `AnyRealFilterActive` ignores reserved `__search` and `__status`, so an active search/chip with zero rows uses initial-empty speech and can leave cap visible. |
| BH5-07 | medium | bad_spec | Slow-query start times live only in the component; remounting while a page request is pending restarts the two-second AM-08 budget, violating the stable request anchor. |
| BH5-08 | medium | bad_spec | Fallback refresh can dispatch page state from a timer/background continuation, while `OnStateChanged` mutates slow-query fields before `InvokeAsync`; timer and render reconciliation can race. |
| BH5-09 | low | reject | The first-N wording at an exact configured cap is a pre-existing cap presentation and is literally true; proving whether more rows exist requires extra query state for negligible daily impact. |
| BH5-10 | medium | bad_spec | Generated failed-page Retry uses `CancellationToken.None`; disposal cancels the completion but cannot cancel the loader request after the view unmounts. |
| BH5-11 | low | reject (carried) | carried from BH-02/EC-08/EC2-08/EC3-07/EC4-03: circuit-long announcement group history remains low impact and pruning needs an ownership policy. |
| BH5-12 | low | reject | Generated filtered-zero, retry, and cached-stale behavior already run in bUnit; the browser-only focus gap has its own VG5-01 row, so duplicating every case in Chromium adds little distinct evidence. |
| VG5-01 | medium | patch | Hidden-row tests verify only the JS interop call; a helper that returns without moving focus would pass. Assert Clear filter is the active element on first result and remains unchanged on repeats in Chromium. |
| VG5-02 | medium | patch | Blocked-scope tests inspect markup but never prove heading focus; removing `FocusAsync` would pass them. Assert active heading after scope loss in a browser gate. |
| VG5-03 | medium | patch | `focusFirstButtonWithin` can return false without throwing, but the caller uses `InvokeVoidAsync` and clears `_focusPending`; later renders cannot retry failed focus. Consume the boolean result. |
| EC5-01 | medium | defer (carried) | carried from EC-01/EC2-07/EC3-02/EC4-01: the terminal-to-Idle service guard predates this story; new attempts use fresh correlation. |
| EC5-02 | medium | bad_spec | A second failed cached refresh in one epoch reuses a closed stale group; same verified episode-dedupe defect as BH5-01. |
| EC5-03 | medium | bad_spec | A successful 304 clears failed page provenance but does not withdraw its terminal failure group, so the shared status can keep speaking failure after recovery. |
| EC5-04 | low | reject (carried) | carried from BH-02/EC-08/EC2-08/EC3-07/EC4-03: group history remains low impact under the existing ownership model. |

Grouped root causes: stale/recovery episode identity (BH5-01/02, EC5-02/03); accepted-body truth (BH5-03); hidden/scope focus semantics and verification (BH5-04, VG5-01–03); duplicate command channel (BH5-05); filter semantics (BH5-06); query anchor and renderer dispatch (BH5-07/08); retry lifetime (BH5-10). Bad-spec entries trigger re-derivation; patch rows are moot this iteration. Carried rows keep prior routes.

### Review iteration 6 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH6-01 | high | patch | A transport failure can leave acceptance unknown, but generated EN/FR fallback resources still state that the command was not accepted and invite another submission; the client’s own warning payload says to check status first. Correcting those resources is direct, though patch work is moot after spec-level findings. |
| BH6-02 | medium | patch | The generated form renders a 250 ms preaccept delay with `TotalSeconds:0` as “Retry after 0 seconds”; `CreatePreAcceptWarning` supplies that hint even though a fresh submit is unsafe when acceptance is unknown. Removing that misleading hint is direct. |
| BH6-03 | medium | patch | Without a custom `IdempotentInfoMessage`, the Info bar says “Already confirmed” while `AnnounceLifecycle` speaks generic AM-13 “Command confirmed”; the default announcement should use the same distinct outcome copy. |
| BH6-04 | medium | patch | Confirmed and idempotent dismissal callbacks set visual state to Idle but leave the coordinator message in the visible persistent status node; the expired confirmation remains on the page. Canceling that correlation’s status when its bar is dismissed is direct. |
| BH6-05 | medium | bad_spec | A generated projection announces terminal failure before rendering `FcProjectionStaleNotice`; the child then queues a new stale group on the same surface, whose 250 ms flush can replace the current failure. The spec needs a consistent precedence rule for simultaneous failure and stale cached rows. |
| BH6-06 | medium | patch | The stale-notice subscription queues `InvokeAsync` without checking disposal inside the callback; a callback queued before `Dispose` can call `Reconcile` afterward and republish stale speech. A disposal guard is direct. |
| BH6-07 | medium | bad_spec | For a non-grid projection, `EmitFailureShell` never passes `state.Error` to `FcProjectionFailure`, so the generated failed action’s message is discarded in favor of generic failure copy. The spec must define a safe classified-message path for these views. |
| BH6-08 | medium | bad_spec | The same non-grid `state.Error` is ignored by `AnnounceProjectionState`, which always speaks generic AM-30; visible and spoken classified failure detail cannot agree. Same non-grid safe-message root cause as BH6-07. |
| BH6-09 | medium | patch | The rejected pending-summary row renders a Fluent message bar without its own `aria-live="off"`; Fluent V5 gives that element an implicit status role, so the outer summary’s off setting does not reliably suppress a second live channel. A direct attribute correction preserves the visible row. |
| BH6-10 | medium | bad_spec | `OnPhaseChangedFromTimer` queues a callback with no timer or correlation generation captured; after a wrapper rebind, an already-queued old ActionPrompt can update the new command’s timer phase. The rebind ownership rule needs to cover timer callbacks. |
| BH6-11 | low | reject (carried) | carried from BH-02/EC-08/EC2-08/EC3-07/EC4-03/BH5-11: coordinator groups and unsubscribed surfaces remain until scope disposal; ordinary-use impact remains low and pruning needs an ownership policy. |
| BH6-12 | medium | patch | `SurfaceAnnouncementCoordinator.Deliver` catches every non-cancellation exception, including exceptions the repository’s `ExceptionGuard` treats as fatal; the callback isolation filter needs the existing fatal guard. |
| EC6-01 | medium | defer (carried) | carried from EC-01/EC2-07/EC3-02/EC4-01/EC5-01: the terminal-to-Idle service guard predates this story, and a new attempt uses a new correlation. |
| EC6-02 | medium | bad_spec | Connection-status disposal leaves the shared surface’s current message and group identities; a remount resets local offline/recovery episode counters, can collide with a closed group, and can replay old speech. The spec needs surface ownership across remounts. |
| EC6-03 | medium | bad_spec | `TrimResultMetadata` evicts the oldest failed offset once more than `MaxCachedPages` failures exist; `CurrentFailedPage` then loses that offset’s in-place retry despite the review acceptance saying it lasts until that page succeeds. Bounded metadata and retry ownership need a coherent rule. |
| EC6-04 | low | reject (carried) | carried from BH-02/EC-08/EC2-08/EC3-07/EC4-03/EC5-04: circuit-long coordinator identity history remains low impact under the existing ownership model. |
| VG6-01 | medium | patch | The slow-query notice is currently emitted outside the full replacement body, but Counter replacement tests do not run a pending query or assert AM-08; moving it inside the skipped body would pass existing checks. Add a generated-view fake-time assertion. |
| VG6-02 | medium | patch | `CounterFullViewReplacement` still has its own `aria-live="polite"` text while the generated envelope adds `FcSurfaceStatus`; its current tests never count status channels. Removing that sample live attribute and asserting one channel is direct. |

Grouped root causes: truthful command warning and idempotent/dismissed status copy (BH6-01–04); stale/failure precedence and callback lifetime (BH6-05/06); non-grid classified failure copy (BH6-07/08); duplicate pending-summary semantics (BH6-09); lifecycle timer ownership (BH6-10); coordinator fatal handling (BH6-12); connection remount ownership (EC6-02); failed-page metadata policy (EC6-03); generated replacement verification (VG6-01/02). Bad-spec entries require a loopback. `review_loop_iteration` is now 6, exceeding the step-04 limit of 5, so this run halts for human escalation; patch and defer work is not processed.

### Review iteration 7 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH7-01 | high | patch (carried) | carried BH6-01: EN/FR fallback still says a transport-uncertain command was not accepted, while the client reports acceptance unknown. |
| BH7-02 | medium | patch (carried) | carried BH6-02: the generated warning still formats its 250 ms hint as “Retry after 0 seconds.” |
| BH7-03 | false | reject | The visible idempotent bar has distinct copy, but the authoritative Story 13.4 AC explicitly requires Confirmed and IdempotentConfirmed to share spoken AM-13. The prior BH6-03 patch route conflicts with that rule. |
| BH7-04 | medium | patch (carried) | carried BH6-04: confirmed/idempotent dismissal changes local visual state but leaves the current status message. |
| BH7-05 | medium | bad_spec (carried) | carried BH6-05: the stale notice can queue after terminal query-failure speech and later replace it on the same surface. |
| BH7-06 | medium | patch (carried) | carried BH6-06: a queued stale-notice callback has no disposal guard inside its renderer callback. |
| BH7-07 | medium | bad_spec (carried) | carried BH6-07 for the visible non-grid failure: EmitFailureShell drops the action's error. Generic spoken AM-30 is required by the canonical epic, so the reviewer's additional speech-detail claim is refuted. |
| BH7-08 | medium | patch (carried) | carried BH6-09: the nested rejected FluentMessageBar still has its implicit live semantics despite the outer summary's aria-live=off. |
| BH7-09 | medium | bad_spec (carried) | carried BH6-10: queued timer-phase work does not capture correlation or timer generation before a wrapper rebind. |
| BH7-10 | medium | patch (carried) | carried BH6-12: coordinator delivery catches all non-cancellation exceptions, including repository-defined fatal exceptions. |
| BH7-11 | medium | bad_spec (carried) | carried EC6-02: connection-status disposal leaves shared-surface state, while remount starts local episode counters again. |
| BH7-12 | false | reject | release-publish.yml is in release_definition_fingerprints; manifest_diagnostics compares that complete set at verification and classify_release_payload blocks drift, so omission from the narrower fallback digest does not permit the claimed stale approval in production. |
| EC7-01 | medium | defer (carried) | carried EC6-01: terminal-to-Idle rejection predates Story 13.4, and a new attempt uses a new correlation. |
| EC7-02 | medium | patch | Canceling another group's queued message clears PendingIdentity but retains its identity in Seen; a still-relevant repeat is suppressed. Removing only that undelivered identity is a direct correction. |
| EC7-03 | medium | patch | Clear removes group objects, but an already queued callback carries only name and version; a new same-name group can reach the same version and accept the old flush. Compare captured group ownership. |
| EC7-04 | medium | patch (carried) | carried BH6-06: a queued stale-notice callback can reconcile after disposal and republish speech. |
| EC7-05 | medium | bad_spec | A non-grid failed refresh with cached Items enters EmitFailureShell and returns before rendering those rows or their stale label, contradicting the story's cached-data feedback. |
| EC7-06 | medium | patch | On a rerender with a changed MaxUnfilteredItems and unchanged ResultIdentity, visible cap text changes but the announcement condition compares only result; include cap in the transition identity. |
| VG7-01 | medium | patch | The verification-gap layer found no fake-time test for a first terminal observation at or after the 120-second budget; deleting ResolveTerminal's new deadline conversion would let late confirmation pass the existing expiry and duplicate tests. |
| VG7-02 | high | bad_spec | At that same first-observation deadline, ResolveTerminal returns DegradedExhausted and converges lifecycle, but generated CommandFormEmitter dispatches the original ConfirmedAction from observation.State; the form can report success after local exhaustion. |

Grouped root causes: carried command warning/dismissal (BH7-01–02, BH7-04), stale and failure ordering (BH7-05–07, EC7-04–05), duplicate live status and timer ownership (BH7-08–09), fatal callback isolation (BH7-10), connection remount ownership (BH7-11), coordinator pending identity and cleared-group callback ownership (EC7-02–03), cap transition identity (EC7-06), and first terminal-at-deadline verification and form-state divergence (VG7-01–02). BH7-03 and BH7-12 are refuted; EC7-01 remains a carried pre-existing issue. The bad-spec groups require a loopback. `review_loop_iteration` is now 7, above the step-04 limit of 5, so this run halts for human escalation; lower patch and defer work is not processed.

### Review iteration 8 — independent layers

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH8-01 | medium | patch (carried) | carried EC7-02: `Cancel` still clears `PendingIdentity` and leaves the identity in `Seen`, so a later announce of that identity returns at `SurfaceAnnouncementCoordinator.Announce`. |
| BH8-02 | medium | patch (carried) | carried EC7-03: `Clear` still drops group objects after bumping version, and `Flush` still accepts a callback whose name and version match a newer group. |
| BH8-03 | medium | patch (carried) | carried BH7-10: `Deliver` still catches every exception except `OperationCanceledException`, including `ExceptionGuard` fatal exceptions. |
| BH8-04 | false | reject | `SubscribeWithReplay` invokes the subscriber under `_gate`, but `FcSurfaceStatus` only queues `InvokeAsync` to store the message and call `StateHasChanged`. That callback does not re-enter the coordinator, so the described deadlock does not occur. |
| BH8-05 | maybe-false | defer | Generated `OnStateChanged` still calls `AnnounceProjectionState` before `InvokeAsync`. A concurrent Fluxor notification could interleave `_pendingLoadingGroup` and `_activeFailureGroup`; whether Fluxor can deliver those notifications concurrently is not settled by this code. Unverified medium. |
| BH8-06 | medium | bad_spec (carried) | carried BH7-05: a terminal query failure and `FcProjectionStaleNotice` still share one surface, and the stale group can flush after the failure. |
| BH8-07 | medium | patch (carried) | carried BH7-06: the stale-notice `InvokeAsync` callback still has no disposal guard before `Reconcile`. |
| BH8-08 | medium | bad_spec (carried) | carried BH7-07: non-grid `EmitFailureShell` still creates `FcProjectionFailure` without the action message. |
| BH8-09 | medium | bad_spec (carried) | carried EC7-05: that same non-grid failure shell still returns before cached rows and `FcProjectionStaleNotice`. |
| BH8-10 | medium | bad_spec (carried) | carried EC6-03: `TrimResultMetadata` still evicts the oldest failed offset once failed results fill `ResultMetadataLimit`, so `CurrentFailedPage` can lose in-place Retry. |
| BH8-11 | medium | patch (carried) | carried EC7-06: `FcMaxItemsCapNotice` still speaks only when `ResultIdentity` changes, so a new `MaxUnfilteredItems` can change the visible sentence without a new announcement. |
| BH8-12 | false | reject | `PendingStartedAtByKey` falls back to `DateTimeOffset.UtcNow` only when `RegisteredAt` is null. Generated `LoadPageAsync` and in-place retry both set `RegisteredAt` from `TimeProvider.GetUtcNow()` before dispatch. |
| BH8-13 | medium | bad_spec (carried) | carried BH7-09: `OnPhaseChangedFromTimer` still applies the live correlation and acceptance anchor inside the queued callback. |
| BH8-14 | medium | bad_spec | Disconnect, or `TimeoutActionThresholdMs` below 10 seconds, enters `ActionPrompt` before the canonical 10-second mark. `OnPhaseChangedFromTimer` samples Degraded only on that callback and only when elapsed time is already at least 10 seconds, and the timer does not emit another phase change, so Degraded never starts. |
| BH8-15 | low | patch | A blank `ViewKey` returns from `ReconcileVisibility` without cancelling `_slowTimer` or the active slow-query group, so a cleared view can still speak AM-08. |
| BH8-16 | medium | patch (carried) | carried BH7-04: confirmed and idempotent dismissal still set local state to Idle and leave the coordinator message in place. |
| BH8-17 | high | patch (carried) | carried BH7-01: generated fallback copy still says the command was not accepted, while `CreatePreAcceptWarning` says acceptance is unknown. |
| BH8-18 | medium | patch (carried) | carried BH7-02: the generated warning still formats the 250 ms hint as “Retry after 0 seconds.” |
| BH8-19 | high | bad_spec (carried) | carried VG7-02: `ResolveTerminal` can store `DegradedExhausted` at the polling budget, while generated `CommandFormEmitter` still dispatches `ConfirmedAction` from `observation.State`. |
| BH8-20 | medium | patch (carried) | carried BH7-08: the rejected `FluentMessageBar` in `FcPendingCommandSummary` still has no `aria-live="off"`. |
| BH8-21 | medium | bad_spec (carried) | carried BH7-11: connection-status disposal still does not clear `projection-connection` groups, and a remount restarts `_offlineEpisode` and `_recoveryEpisode` at zero. |
| BH8-22 | low | patch | `fc-connectivity.js` still calls `invokeMethodAsync` without handling rejection after the .NET reference is gone. |
| BH8-23 | false | reject | `DisposeAsync` waits on `_connectivityGate` only while initialization holds it, and `TryInitializeConnectivityAsync` releases that gate in `finally` after the JS calls return or throw. The wait is the in-flight call, not a stranded lock. |
| BH8-24 | low | reject | `unwatchConnectivity` can throw `InvalidOperationException` during circuit teardown and skip disposing the .NET reference. The page is already going away, and covering that exception adds catch branches beyond a direct correction. |
| BH8-25 | medium | defer | Visible lifecycle bars still use hardcoded English (“Submission acknowledged”, “Submission confirmed”, “Already confirmed”, and the action-prompt body) beside localized speech. Those literals predate this story’s announcement channel. |
| BH8-26 | medium | patch | The defensive `LoadPageFailedAction` still stores “effect exited without terminal dispatch” as `ErrorMessage`, so that internal sentence can be shown and spoken. |
| BH8-27 | false | reject | A null-item failure leaves `LoadedPageResult.ErrorMessage` null and stores the resource name only in `FailureByKey`. Generated failure speech uses `page.ErrorMessage ?? Localizer["Am30QueryFailed"]`, so it does not speak the English TCS exception or the raw resource name. |
| BH8-28 | medium | patch | `ClassifyCommandAsync` still runs before `using (response)`, so a throw from classification skips disposal of the accepted response. |
| BH8-29 | low | reject | A body read that outlives the two-second `WaitAsync` can fault unobserved after the linked token is cancelled. The command path already treats that timeout as unknown correlation, and observing the leftover task is more than a direct correction. |
| BH8-30 | low | reject | The verification record and sprint status are behind this review loop. Correcting them means editing this build’s spec or its tracking note, which this review does not do. |
| EC8-01 | medium | defer (carried) | carried EC7-01: terminal-to-Idle rejection predates Story 13.4, and a new attempt uses a new correlation. |
| EC8-02 | medium | bad_spec (carried) | carried BH8-14: the same early `ActionPrompt` callback is the only Degraded sample, so a disconnected command can skip the 10-second Degraded state until the polling deadline. |
| VG8-01 | medium | patch | Pre-verified gap: no `QueryAsync` test advances fake time across `MaxPendingCommandPollingDurationMs` and returns `Confirmed`. Removing the post-await `IsExpired` branch would still pass the current expiry tests and could store that confirmation. |
| VG8-02 | medium | patch | Pre-verified gap: `HasRealFilter` treats search text and a `__status` chip as real filters, but `HandleLoadPageAsync` tests never send either. Removing those exceptions would still pass the empty-filter clamp tests and could truncate a filtered request. |
| VG8-03 | medium | patch | Pre-verified gap: the safe failure test constructs `LoadPageEffects` without a localizer. Changing the localizer arm back to `ex.Message` would still pass it and could show transport text on a localized host. |
| VG8-04 | medium | patch | Pre-verified gap: hidden-detail tests leave `ResultSettled` at its default true. Deleting `&& ResultSettled` would still pass them and could focus Clear filter before the filter result exists. |

Grouped root causes: carried coordinator identity and cleared-group callbacks (BH8-01/02); fatal callback isolation (BH8-03); stale/failure precedence and non-grid failure rendering (BH8-06/08/09); failed-page metadata (BH8-10); lifecycle timer ownership and the 10-second Degraded sample (BH8-13/14, EC8-02); command truth versus generated form state (BH8-17/19). Those bad-spec groups require a loopback. `review_loop_iteration` is now 8, above the step-04 limit of 5, so this run halts for human escalation. Patch, defer, and reject rows are recorded and are not processed.

## Verification

### Iteration 0 (historical; code was reverted)

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 --no-restore` and the SourceTools.Tests analogue: passed, 0 warnings/errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -maxThreads 1`: 3,126 total, 0 failed, 1 skipped (live two-tenant endpoint/credentials unavailable). The earlier parallel full run collided on shared Release build output; both affected governance methods passed separately.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests`: 1,309/1,309 passed. No `.received.txt` files remain.
- `npm --prefix tests/e2e run typecheck`: passed. Focused Chromium announcement and authorization specs: 8/8 passed. `npm --prefix tests/e2e run test:a11y`: 22/22 passed. `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 --no-restore`: passed; one transient MSB3026 retry warning.
- `git diff --cached --check`: passed. Independent review completed; see the Review Triage Log.

### Iteration 1 (re-derived implementation)

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 --no-restore -p:UseSharedCompilation=false`: passed with 0 warnings/errors. Release Counter.Web build passed with 0 warnings/errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -maxThreads 1`: 3,136 total, 0 failed, 1 skipped (live two-tenant endpoint and credentials unavailable). After the final generated-view assertion and status revision, focused generated/status/stale tests passed 4/4 and Counter view tests 24/24. The tracked identifier seal passed separately at 3,530 declarations and SHA-256 `27dadf8bc055579ee208afced5d10a76778d94710f1ab3df4a3974791b8d2962`.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests`: 1,309/1,309 passed after the final emitter snapshot refresh. `npm --prefix tests/e2e run typecheck` passed. New announcement Chromium E2E passed 1/1; policy-gated command authorization Chromium E2E passed 7/7; specimen accessibility suite passed 22/22.
- Frozen matrix audit: **Burst** is covered by `LifecycleBurstAnnouncesOnlyAcknowledgedThenImmediateTerminal` and `ConnectionAndLoadGroupsUseTrailing250Milliseconds`; **Terminal** by those tests plus mapped-rejection and first-terminal lifecycle tests; **Projection** by `CompletedFilteredPages_AnnounceZeroAndFailureThroughOneLiveNode`, `CachedRowsBecomeVisiblyStaleAndAnnounceOnceAfterTheWindow`, `ConnectedCancelsQueuedReconnectSpeech`, and existing loading/failure/scope tests; **Budgets** by `ConcurrentPagesKeepTheEarliestDeadlineAndClearEachSpokenResult`, `AcceptedCommandBecomesDegradedAtTenSecondsAndClosesAtTwoMinutes`, `DegradedReplayRetainsTheOriginalAcceptanceDeadline`, and accepted-body retry tests; **Scope** by `ScopeBlockedSurfaceTests` and `FcScopeBlockedTests`. All covering Shell tests ran and passed in the full or final focused runs; the live endpoint skip is outside the matrix.
- `git diff --cached --check` passed. No `.received.txt` approval artifacts remain.

### Iteration 2 (re-derived implementation)

- Shell Debug test project and Counter.Web Release build passed with zero warnings/errors. Full serialized Shell assembly: 3,143 total, 0 failed, 1 skipped because the live two-tenant EventStore endpoint and credentials are unavailable. Two further runtime test groups added after the full run passed 74/74; the staged CA1707 inventory seal passed separately at 3,531 declarations and SHA-256 `d7c7d8f65790edfc1548ec1883b9cad2e51c6b7e23aac33465ad90128bd0963f`.
- Full SourceTools assembly passed 1,309/1,309, including refreshed generator approvals. TypeScript typecheck passed. Chromium Story 13.4 announcement E2E passed 1/1, policy-gated command authorization passed 7/7, and specimen accessibility passed 22/22. `git diff --cached --check` passed; no approval `.received.txt` files remain.
- Frozen matrix audit: **Burst** and **Terminal** remain covered by the fake-time coordinator and lifecycle tests, now including revision ordering and generated terminal-action cases. **Projection** is covered by generated filtered-zero, page failure, offscreen failure/retry, 304 recovery, stale, offline, recovery-episode, and status-count tests. **Budgets** is covered by concurrent slow-page, configurable accepted-command deadline, Degraded replay, exhaustion, and same-MessageId retry tests. **Scope** remains covered by blocked-scope focus and isolation tests. Every covering test ran and passed in the full Shell assembly or the final 74 focused tests; the live endpoint skip is outside these rows.

### Iteration 3 (historical; code was reverted)

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 --no-restore -p:UseSharedCompilation=false`: passed with zero warnings/errors. `timeout 360s dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-build --output Normal`: 3,157 passed, 0 failed, 1 skipped for missing live two-tenant endpoint credentials. The staged CA1707 inventory seal passed separately at 3,532 declarations and SHA-256 `ed4455e1904f2a319b32c55a761c8568d7cfc14076485fa18d8dad3a8fe6403d`.
- `timeout 180s dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug --no-restore --output Normal`: 1,309/1,309 passed. `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 --no-restore -p:UseSharedCompilation=false`: passed with zero warnings/errors. `npm --prefix tests/e2e run typecheck` passed; combined Story 13.4 announcement and policy Chromium cases passed 9/9; isolated final Story 13.4 cases passed 2/2; specimen accessibility passed 22/22. No `.received.txt` artifacts remain.
- Frozen matrix audit: **Burst** is covered by `LifecycleBurstAnnouncesOnlyAcknowledgedThenImmediateTerminal` and `ConnectionAndLoadGroupsUseTrailing250Milliseconds`; **Terminal** by those tests, `TerminalCancelsOlderPendingGroupsOnTheSameSurface`, and generated form terminal tests; **Projection** by generated filtered-zero/failure, stale/offline/recovery, page-provenance and retry, and single-status tests; **Budgets** by `ConcurrentPagesKeepTheEarliestDeadlineAndClearEachSpokenResult`, `AcceptedCommandBecomesDegradedAtTenSecondsAndClosesAtTwoMinutes`, Degraded replay, and accepted-body same-MessageId retry tests; **Scope** by blocked-scope focus/isolation tests. These Shell tests ran in the full passing suite; browser cases and accessibility gates passed separately. The credential-dependent skip does not cover a matrix row.
- `git diff --cached --check`: passed after staging.

### Iteration 4 (historical; code was reverted)

- Shell Debug test project and Counter.Web Release build passed with zero warnings/errors. Full serialized Shell assembly, after the staged CA1707 reseal: 3,167 total, 3,166 passed, 0 failed, 1 credential-dependent live two-tenant skip. The staged CA1707 identifier inventory separately passed at 3,532 declarations and SHA-256 `ca36f5ee09c4b1ccf29024d581f97c15f20bcdae95b5f8bc3bd8dd58348e46b7`.
- SourceTools full assembly passed 1,309/1,309 after generator approval refresh. Chromium Story 13.4 plus authorization cases passed 9/9; specimen accessibility 22/22; TypeScript typecheck passed. No `.received.txt` artifacts remain. A final focused hidden-row status-preservation correction was built with zero warnings/errors and `FcExpandedRowHiddenBannerTests` passed 5/5 after that edit.
- Frozen matrix audit: **Burst** is covered by `LifecycleBurstAnnouncesOnlyAcknowledgedThenImmediateTerminal`, `ConnectionAndLoadGroupsUseTrailing250Milliseconds`, and `LastEligibleStateWinsWhenBurstReturnsToItsFirstIdentity`; **Terminal** by the coordinator terminal and generated lifecycle-form tests; **Projection** by generated filtered-zero/failure, cached stale/offline, request-generation, retry, and status-count tests; **Budgets** by concurrent slow-page, configured accepted-command deadline, Degraded replay, exhaustion, and same-MessageId accepted-body retry tests; **Scope** by blocked-scope focus and tenant-isolation tests. All named covering Shell tests ran and passed in the full assembly or the final focused hidden-row rerun. The live endpoint skip is outside the matrix.
- `git diff --cached --check` passed after the final source/test edit.

### Iteration 5 (re-derived implementation)

- Shell Debug test project and Counter.Web Release build passed with zero warnings/errors. Full serialized Shell assembly on the real staged index: 3,179 total, 3,178 passed, 0 failed, 1 skipped for missing live two-tenant endpoint/credentials. The staged CA1707 inventory seal separately passed at 3,534 declarations and SHA-256 `9813c503fbadeed20b7a4838de193fa6e54c999a0fc92a0dbebf349f9122548b`.
- SourceTools full assembly passed 1,309/1,309 after reviewed generator approval updates. Story 13.4 Chromium cases passed 4/4, adjacent authorization Chromium cases passed 7/7, specimen accessibility passed 22/22, and TypeScript typecheck passed. No `.received.txt` artifacts remain. `git diff --cached --check` passed.
- Frozen matrix audit: **Burst** is covered by `LifecycleBurstAnnouncesOnlyAcknowledgedThenImmediateTerminal`, `ConnectionAndLoadGroupsUseTrailing250Milliseconds`, and the final A→B→A fake-time regression; **Terminal** by coordinator and generated lifecycle-form tests, including accepted unknown-correlation shapes; **Projection** by generated filtered-zero/failure, repeated stale/offline, 304 recovery, retry cancellation, and single-status tests; **Budgets** by concurrent/remounted slow-query anchors, configured accepted-command 10s/120s closure, and 250-ms same-MessageId retry tests; **Scope** by blocked-scope and tenant-isolation tests, with active-element browser coverage for the heading. All covering Shell tests ran and passed in the full assembly; browser and accessibility gates passed separately. The live endpoint skip covers no matrix row.
