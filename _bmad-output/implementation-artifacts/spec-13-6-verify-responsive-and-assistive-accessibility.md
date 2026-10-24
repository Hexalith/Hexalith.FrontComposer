---
title: 'Story 13.6: Verify Responsive and Assistive Accessibility'
type: 'feature'
created: '2026-10-15'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '3c8679628ebdb8995afc09f712a04fcde623695c'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Existing specimen checks do not establish AE-01–AE-09 across changed operator surfaces. The axe defaults omit WCAG 2.2, simulated zoom permits page overflow, and manual assistive/device evidence is missing.

**Approach:** Extend existing browser/component lanes, correct demonstrated accessibility failures, and assemble one candidate-bound result referencing reused and new evidence for shell, Home, navigation, tabs, toolbar, projection/detail, form, lifecycle, palette and dialogs.

## Boundaries & Constraints

**Always:** Follow canonical UX-AE-1/UX-VC-1/UX-RM-1. Preserve Fluent V5 ownership, nine typography mappings/version 3.1.0, default 32px compact rows, truthful virtualization, 72px/48px rails and 75rem constrained measure. Rows may grow under spacing/reflow. Preserve tenant isolation and 13.2–13.5 focus, lifecycle, speech and silent-expiry behavior. Missing human evidence leaves this story incomplete.

**Never:** Add independent palettes, type ramps, invented breakpoints, broad axe exclusions or duplicate workflows. Treat viewport resizing as actual browser zoom, DOM mutations as heard speech, or automation as manual evidence. Infer OI-16, G-4, FLUENT-APP-1 or Product approval.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior | Failure handling |
|---|---|---|---|
| AE-01–03 | 320px; actual 400% zoom from 1280px; WCAG text spacing | No loss/clipping/page overflow; only labelled grid/tab regions scroll horizontally | Record exact failing surface; fix and rerun |
| AE-04–05 | Every pointer target and keyboard stop, including active overlays | 24×24px or one evidenced permitted exception; whole target/focus ring visible and unobscured | Reject missing inventories, invalid spacing circles/equivalents |
| AE-06–08 | Both themes, forced colors, reduced motion | Required contrast; immediate text/icon/shape/focus; no essential meaning lost | Retain computed-style/state evidence |
| AE-09 | Semantics, keyboard and speech sequences | WCAG 2.2 tags; no critical findings, duplicate speech or hover-only actions | Retain scoped findings and dispositions |
| Manual | Supported AT/device journeys | Dated human observations tied to candidate and versions | Missing runs remain unperformed/blocked |

## Decisions (2026-10-15)

The user approved both recommendations and requested application.

- Missing item: retain neutral unavailable-item copy. Focus its non-live explanation only when removal of the focused detail loses focus; otherwise preserve focus and announce once through the shared polite owner. Suppress duplicate speech and repeated refreshes; cancel stale recovery after scope/navigation/operator focus changes. Reuse focus-before-replacement; AM-22 remains filter-only.
- Zero-field failure: show compact Fluent feedback beside its trigger, wrapping below when needed. Carry the existing safe outcome from hidden form to renderer; keep trigger focus and one speech owner. Retain feedback until next attempt/navigation/scope change. Accepted commands continue through the existing pending summary. No automatic retry or new dialog/service.

</frozen-after-approval>

## Code Map

- Reuse journey specs under `tests/e2e/specs/`; preserve the fresh-row live-only lane.
- `docs/accessibility-verification/README.md` and `manual-log-template.md`: human NVDA/Firefox, JAWS/Chrome, VoiceOver/Safari and tablet/phone evidence.

## Tasks & Acceptance

**Execution:**

- [x] `tests/e2e/helpers/a11y.ts`, `tests/e2e/specs/a11y-helper.spec.ts` — add and prove default `wcag22aa` coverage in both helpers without weakening existing checks.
- [ ] `tests/e2e/helpers/responsive-accessibility.ts` (new), `tests/e2e/specs/responsive-accessibility.spec.ts` (new) — exercise the matrix with spacing, target exceptions, full focus/occlusion geometry, contrast and media assertions; test geometry failures with synthetic fixtures. Reuse existing journeys/artifacts.
- [x] `tests/e2e/specs/specimen-accessibility.spec.ts` — distinguish reflow from real zoom; remove overflow allowance; reuse visual/contrast proof.
- [x] `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor{,.css}`, `FcCommandPalette.razor.css`, `FcPaletteResultList.razor.css`, `FcPageToolbar.razor` in that directory — fix clipping and Fluent token ownership; preserve defaults.
- [x] `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css`, `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`, `RazorEmitter.cs` in that directory — support lossless text spacing and bounded grid scrolling with truthful row sizing; update affected generator approvals and focused density/rendering tests.
- [x] `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandedRowHiddenBanner.razor{,.cs}`, `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`, `CommandFormEmitter.cs` in that directory, `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js` — implement decisions and focused regressions.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` (new) — bind full candidate identity and sanitized references to every AE/surface; record manual journeys/outcomes, unresolved gates and opt-in UX review or authorized skip; disposition every Critical/High finding.

### Resumed implementation tasks — 2026-10-17

- [x] B1 — `tests/e2e/helpers/responsive-accessibility.ts`, `tests/e2e/specs/responsive-accessibility.spec.ts`: check text across composed ancestors/open shadow roots, detect demonstrated native-value vertical clipping, and reject or measure explicit text clips. Preserve nested scrollport and top-layer positives; add each reproduced negative and a valid positive.
- [x] B2 — same helper/spec: prevent images, positioned opaque pseudo paint and unrelated paint underneath exterior focus indicators from certifying ancestor-only contrast. Prefer a precise computed measurement where supported, otherwise fail explicitly for required pixel evidence. Preserve real Fluent controls, transparent slot/caret ownership, clipped visible paint and existing SVG/shadow negatives.
- [x] B3 — same helper/spec: inventory visible pointer-active menu checkbox/radio roles and aria-hidden controls; base minimum/spacing/equivalent checks on reachable clipped geometry. Retain all existing exception provenance and nonempty-inventory checks. Do not silently omit a clipped essential control.
- [x] B4 — same helper/spec: scale the actual indicator envelope or fail closed for demonstrated nontrivial transforms; retain normal translated Fluent controls and existing pseudo-focus clipping positives.
- [x] B5 — `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js`, responsive spec, existing missing-detail tests if needed: cancel old captures when query navigation changes the document identity. Trace duplicate ViewKey instances through the real component and group owner. Fix a reproduced physical-instance error with existing internal identity/DOM seams and no new public C# API; otherwise record the exact refutation/remaining uncertainty. Preserve co-mounted single-speech/focus ownership, handoff, cancellation and scope disposal.
- [x] B6 — `tests/e2e/scripts/a11y-state-evidence.test.mjs`: use node:path basename and node:url fileURLToPath for platform-native paths; retain the existing retry/CLI behavioral assertions and run all twelve Node tests. Do not claim Windows execution on Linux.
- [ ] Evidence — update this spec's implementation/review notes and `13-6-accessibility-result.md` with current candidate hashes, exact commands/results, every regression disposition and remaining manual/platform/owner/visual/catalog gates. Reuse the existing evidence lane at `evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/`; preserve all historical packets.

Given these reproduced counterexamples, when the helper regressions execute on all three engines, then invalid evidence fails and corresponding valid cases pass without weakening thresholds or approved snapshots. Given component-level reproduction/refutation, when duplicate recovery is assessed, then its disposition cites that proof. These bounded checks supplement the original acceptance criteria; required manual observations and catalog/visual gates stay open.

**Acceptance Criteria:**

- Given each changed surface, when AE-01–AE-09 journeys run, then the matrix passes, including 1.5× line height, 2× paragraph spacing, 0.12em letter spacing, 0.16em word spacing; 4.5:1 normal text, 3:1 large text/non-text/focus; and individually justified Inline/Spacing/Equivalent/User Agent Control/Essential target exceptions.
- Given automated and dated human evidence, when readiness is assessed, then each requirement cites candidate-valid proof; actual 400% zoom, heard speech (including direct cues and silent-restore/new-message sequences) and real-device outcomes are never inferred, and missing evidence prevents completion.

### Review Findings

Code review 2026-10-16, pass 1 of 2 (`src/`, `samples/`, `.github/`, `docs/` of `3c867962..HEAD`; `tests/` pass pending).

- [x] [Review][Decision→Patch, applied] Home route title now neutral `HomePageTitle` ("Home"/"Accueil") [src/Hexalith.FrontComposer.Shell/Components/Pages/FcHomeRouteView.razor:11]
- [x] [Review][Decision→Defer] No code change; verify that a tenant switch recreates the renderer (hidden zero-field feedback scope-change clearing) although Decision 2 says "retain until next attempt/navigation/scope change"; confirm whether renderer survives scope change before adding a clear [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs]
- [x] [Review][Patch] Hard-coded English "Retry after N seconds." appended to localized hidden warning; add a resx key (en + fr) [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:~1752]
- [x] [Review][Patch] `_serverWarning.Detail ?? _serverWarning.Title` at three call sites passes a whitespace Detail through, so no bar/announcement; reuse the first-non-blank chain with `CommandWarningFallback` [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:~436,~1385,~1430]
- [x] [Review][Patch] `containDialogFocus` Tab handler: `!tabbables.includes(active)` still fires when focus is on a light-DOM child of fluent-dropdown/combobox, so Tab jumps to the first control; treat owned children as in-list [src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js:~291]
- [x] [Review][Defer] `await Task.Run(cancellation.Dispose)` is still awaited, so a blocked provider callback may still delay completion — deferred: maybe-false; settle by checking whether `EventStoreClientTests` blocking-cleanup tests (:1246, :1361) assert the returned task completes before dispose finishes
- [x] [Review][Defer] `observeRowMetrics` MutationObserver on `documentElement` forces row re-measure on any style/class change — deferred: maybe-false/perf; settle with a profile of a multi-grid page
- [x] [Review][Defer] Detail-focus tracker eviction at 32 and per-render JS import in `FcExpandedRowHiddenBanner` — deferred: unlikely at realistic counts
- [x] [Review][Defer] `AccessibilityScenarioControls.SetConnection` can await a cancelled/stale `_pageCompletion` — deferred: Counter specimen fixture only
- [x] [Review][Defer] Forced-colors `--colorNeutralForeground*` override on `.fc-shell-root` vs "no Fluent theme redefinition" rule — deferred: maybe-false; scoped to forced-colors, owner to confirm the exception

#### Rejected

- Thread-safety of `AccessibilitySpecimenState`, hard-coded specimen deny reason, `FcStatusIcon` bare `span` selector, global `fluent-tablist`/`[data-fc-datagrid]` CSS, disabled-option opacity removal, 40rem specimen breakpoint, rationale-doc evidence gaps, duplicate-sequence in renderer lambda, missing-episode re-announce, manual-evidence docs not updated, focus-return rework, unlabelled scroll: low/speculative or refuted by cited tests/forced-colors affordance; no demonstrated harm.

#### Pass 2 (`tests/`)

- [x] [Review][Decision→Patch, applied A: run id shared via quality.yml env] Evidence validator not bound to the current run: `validate-a11y-artifacts.mjs:60` passes `expectedEngine` but never `expectedRun`, so a complete stale run in the artifact directory can satisfy the gate while the current run is partial (AC2 "candidate-valid"); `FC_A11Y_EVIDENCE_RUN_ID` is a per-process UUID, so the validator process cannot see it without a handoff [tests/e2e/scripts/validate-a11y-artifacts.mjs:60]
- [x] [Review][Decision→Patch, applied A: AddAccessibilitySpecimenSeams + theory test] Only the command-service seam of the Counter Test-host replacements (authorization evaluator, page loader, admission gate, auth-state provider in `Program.cs:102-114`) has a gating test; moving them out of the `IsEnvironment("Test")` block fails nothing [samples/Counter/Counter.Web/Program.cs:102]
- [x] [Review][Patch] `a11y-state-evidence.test.mjs` splits light/dark with hard-coded `i = 58`; derive from the state list [tests/e2e/scripts/a11y-state-evidence.test.mjs:49-54]
- [x] [Review][Patch] `MissingWithoutFilterEvidenceShowsNeutralReasonAndDoesNotAnnounceAm22` now asserts the reason is spoken once; rename (confirm `AnalyzerPolicy_IdentifierInventory_MatchesSeal` after) [tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandedRowHiddenBannerTests.cs:~64]
- [x] [Review][Defer] AE-01..03 actual 400% browser zoom is not exercised (only viewport reflow) — deferred: manual/owner evidence lane, must stay "unperformed" in the result file
- [x] [Review][Defer] "Only labelled grid/tab regions scroll horizontally" is never asserted (only page overflow) — deferred: new inner-scroller inventory helper, own story
- [x] [Review][Defer] Keyboard-cycle proof covers only palette and settings; destructive alertdialog, abandonment warning, filter popover, view menu lack it; `verifyKeyboardCycle` resumes via programmatic focus at boundaries — deferred
- [x] [Review][Defer] 320px reflow loop checks text clipping only with text spacing on — deferred
- [x] [Review][Defer] `Essential` target-exception kind accepted on non-empty strings alone; stale exceptions not flagged when the target already conforms (`used.add` on match) — deferred
- [x] [Review][Defer] Decision 1/2 scope-change cancellation and clearing untested (no tenant-switch case) — deferred with pass 1 scope-change item
- [x] [Review][Defer] No fr-culture assertion for `RetryAfterSeconds`; Test-only seam registrations lack negative-environment test if D2 resolves to rename — deferred

##### Rejected (pass 2)

- `AnalyzerPolicy_IdentifierInventory_MatchesSeal`: refuted, run green locally against the current tree.
- Reflection/`closeAfterJoin!`/`VerifyInvoke` diagnostics, hard-coded evidence literals (`heardSpeech: not observed` is explicit), icon-sampler shape/alpha corners, multi-dialog selection, `settlePaint` no-throw, route-rewrite pattern guard, `expectTextContrast` colour parsing, behaviour-change notes, trait addition, style nits: speculative or no demonstrated harm.

#### Pass 3 (`src/` + `samples/Counter`, re-review 2026-10-18)

- [x] [Review][Patch] (applied: clear moved after the admission check; 2 snapshots updated, CommandFormEmitterTests 68/68) Hidden zero-field `SubmitAsync` awaits `PresentHiddenOutcomeAsync(null)` (an `InvokeAsync` into the parent) before the synchronous idle/terminal state guard, so two rapid activations can both clear feedback and both read an idle lifecycle; run the state guard first, then clear the outcome [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:~1314]
- [x] [Review][Defer] Zero-field inline branch emits the button/form/bar inside the new `FluentStack` `ChildContent` lambda using the outer `seq++`, so sequence numbers may differ per child re-render — deferred: maybe-false (same pattern as existing popover/card lambdas); settle with a bUnit/Playwright re-render that asserts the trigger button keeps focus and identity across a hidden-outcome update [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs:981-1034]
- [x] [Review][Defer] `HandleRowMetricsAsync` silently drops a height when `defaultHeight` mismatches the current density, yet `fc-datagrid.js` marks it `acknowledged` on promise resolution, so JS will not resend until the height changes — deferred: maybe-false (transient during density switch); settle by switching density with a wrapping-row grid and asserting `data-fc-grid-item-size` converges [src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:~2507, src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-datagrid.js:~1746]
- [x] [Review][Defer] Hidden-form rejection paths call `PresentHiddenOutcomeAsync(..., announce:false)` into an `aria-live="off"` bar — deferred: maybe-false; confirm the lifecycle wrapper speaks exactly once for a rejected zero-field command (no silence, no double speech) [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:~2230,2257]
- [x] [Review][Defer] `FcExpandedRowHiddenBanner` Dispose can leave `_missingFocusModule` set while an in-flight `OnAfterRender` uses it (possible `ObjectDisposedException`) and a stale `focusReplacementHeading` with the same `_recoveryKey` can delete a newer episode's captured focus — deferred: maybe-false; settle with a bUnit case that disposes mid-import and a Playwright case with two consecutive missing episodes

##### Rejected (pass 3)

- Already dispositioned in pass 1 (Task.Run dispose, `observeRowMetrics` global observer, tracker eviction at 32, `_pageCompletion`, forced-colors tokens, status-icon `span` selector, global tablist/datagrid CSS, retry-after resx): duplicates.
- 480px lifecycle-bar breakpoint: spec note cites Fluent's multiline message-bar breakpoint; not an invented layout tier.
- Always-focusable grid region (`tabindex="0"`): intended for scrollable-region accessibility; low.
- Retry text "0/1 seconds": pre-existing wording, only moved to resx; low and non-trivial fix.
- Radio-group Tab boundary in `containDialogFocus`: radios are never last in the only radio-bearing dialog (settings has trailing action buttons); latent.
- Specimen/Test-only seams (`Action<bool>` resolution, fake-auth replacement, `Select` throwing, cascading RenderContext, scenario toggle query), `PresentHiddenOutcomeAsync` claim race, and palette 11px→12px token change: Test-host or specimen fixtures, or negligible.
- Missing tests in this diff: refuted, tests are in the `tests/` group (pass 2) and Verification Gap found no gaps.

#### Pass 4 (`tests/` since pass 2 + `src/` fix delta, 2026-10-23)

Scope: `fc0cfaeb..386ec94f` over the 25 Story 13.6 test files plus `02626ead..386ec94f` for `CommandFormEmitter.cs` and the specimen CSS (27 files, +3,877/−319). Evidence packets, spec/ledger narrative and unrelated sync files are excluded; `FrontComposerShellTests.cs` lines 128–153 belong to Story 15.2 (`e08a8ab4`). Four layers returned 41 raw findings (`R4-A/V/B/E#`); each verdict is in the Review Triage Log.

- [x] [Review][Decision→Patch, chosen 2026-10-23: clear on blocked attempts too; applied] Blocked activation leaves the earlier hidden outcome visible — since `9265b09f` the null clear runs only after admission, so a state-guard or admission denial (`PendingCommandAlreadyExists`, `AdmissionAlreadyInProgress`, policy) announces AM-20 through `_blockedOutcome` while the renderer's visible bar keeps the previous attempt's text (for example "Retry after 17 seconds."). Decision 2 retains feedback "until next attempt"; the emitter itself calls a blocked submission an attempt. `EarlierHiddenOutcomeIsClearedOnlyByAnAdmittedAttempt` pins the current behavior. Decide whether a denied activation clears the prior hidden outcome; clearing inside the denial branch does not reintroduce the pass-3 race (R4-A1) [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:1333]
- [x] [Review][Patch] (applied) The ordinary warning bar repeats `CommandWarningFallback` as both title and body when Title, Detail and EntityLabel are all blank, so the sentence shows twice and the implicit status region speaks it twice; omit the body sentence when the title already carries the fallback, keep the RetryAfter line, and update `OrdinaryBlankWarningUsesSafeLocalizedFallbackAndRetryHint` plus the two CommandForm snapshots (R4-A2, R4-B11) [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:2050]
- [x] [Review][Patch] (applied) The reduced-motion fixture can pass vacuously: it asserts only an absent transition/animation under `reduce`, so an unloaded stylesheet or renamed class still passes. First assert motion under `no-preference`, then its removal under `reduce`, and assert that the `.fc-reconciliation-sweep` outline still paints (R4-A4, R4-B7, R4-E15) [tests/e2e/specs/responsive-accessibility.spec.ts:598]
- [x] [Review][Patch] (applied) The AppHost Test branch omits `Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000` although it claims the browser lane's timings, while state evidence records `fixtureOptions` from Playwright config metadata; add the variable and pin every `webServer.env` timing in `AppHostCounterAccessibilityOverridesAreConfinedToExplicitLocalTestRuns` (R4-A5, R4-B2, R4-E14) [src/Hexalith.FrontComposer.AppHost/Program.cs:171]
- [x] [Review][Patch] (applied) Prove the admission lease is released when the parent clear callback disposes the form: add the existing settlement probe to the `disposeOnClear: true` case (R4-V1) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:87]
- [x] [Review][Patch] (applied) `visibility:hidden` on any ancestor excludes a painted `visibility:visible` descendant from the target, clipping and competing-paint inventories (a probe's visible 10px button was omitted). Test the element's own inherited computed visibility and keep the ancestor walk for display/opacity/inert (R4-E3) [tests/e2e/helpers/responsive-accessibility.ts:90]
- [x] [Review][Patch] (applied) `clippedPaint(content=true)` clips absolute/fixed text by overflow ancestors that are not its containing block, so `sample()` returns early (a probe's visible 1.2:1 escaped label produced no sample). Resolve the containing block for out-of-flow content, or fail for pixel evidence (R4-E4) [tests/e2e/helpers/responsive-accessibility.ts:1171]
- [x] [Review][Patch] (applied) `validate-a11y-artifacts.mjs` parses the report, each state JSON and the capability attachment without guards: a truncated file crashes with a stack trace, and zero or multiple capability attachments surface as "Missing actual forced palette paint". Report each as a validation error (R4-B3, R4-E13) [tests/e2e/scripts/validate-a11y-artifacts.mjs:64]
- [x] [Review][Patch] (applied) `glyphPaint` runs before the overlap test, so any unrelated `text-shadow` or text stroke on the page makes every text contrast measurement throw (reproduced by probe). Compute glyph boxes first and call `glyphPaint` only for intersecting text (R4-E1) [tests/e2e/helpers/responsive-accessibility.ts:1329]
- [x] [Review][Patch] (applied) Assert the `#fc-main-content h1` focus move (`toBeFocused`) before the `zero-accepted` evidence (R4-B8) [tests/e2e/specs/accessibility-state-matrix.spec.ts:207]
- [x] [Review][Patch] (applied) The target inventory overwrites an earlier exception failure with the limitation/clipping reason; retain every reason (R4-B9) [tests/e2e/helpers/responsive-accessibility.ts:259]
- [x] [Review][Patch] (applied) `RecordingHiddenDetailFocusModule.FocusCalls` merges `focusFirstButtonWithin` and `focusReplacementHeading`; count each identifier and assert the replacement-heading call in the co-mounted missing test (R4-B14) [tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/RecordingHiddenDetailFocusModule.cs:36]
- [x] [Review][Defer] Consecutive missing episodes with an identical `ResultIdentity` share one episode group, so a stale `focusReplacementHeading` completion could pass `IsCurrentRecovery` (R4-E17) [src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandedRowHiddenBanner.razor.cs:246] — deferred: maybe-false (medium if true), carried by pass 3's "two consecutive missing episodes" ledger entry, so no new entry; settle by showing that a generated view can repeat page `Identity`/`LastResultCorrelationId` across two missing episodes, then add a bUnit case.

Patches applied 2026-10-23, uncommitted (12 entries; R4-B1/R4-E12 reclassified as rejected before any change). Verification:
- SourceTools: CommandForm/CommandRenderer emitter classes 103/103; the two CommandForm approvals were updated with only the intended three hunks.
- Shell Debug: build clean; the five affected classes 214/214 (ZeroFieldFailureAccessibility, CommandRendererWrapperIntegration, FcExpandedRowHiddenBanner, FrontComposerUiAppHost, CommandTargetGeneratedForm); `AnalyzerPolicy_IdentifierInventory_MatchesSeal` passes.
- Node evidence suite 26/26. The two new CLI cases fail against the HEAD validator.
- Browser regressions fail against the HEAD helper and pass after the fix: the three new R4 synthetic tests plus the updated offscreen-target test on Chromium, Firefox and WebKit (12 red, 9 + 3 green).
- Complete Story 13.6 browser selection against a Playwright-started Release Test host: 891 cases, 875 passed / 12 failed / 4 skipped, no retries. All 12 failures are Firefox/WebKit specimen visual-baseline height mismatches, the visual gate the result already records as open; they were not re-run on an unmodified HEAD build. TypeScript typecheck passes.
- Blocked: `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -c Debug` fails with MSB3030 file locks held by another session's running Aspire AppHost, plus CS1704 duplicate `Hexalith.Memories.*` (source vs NuGet 2.27.1). No error is in `Program.cs`, and Release is not a valid AppHost configuration. The one added `.WithEnvironment` call is pinned by `AppHostCounterAccessibilityOverridesAreConfinedToExplicitLocalTestRuns`.

##### Rejected (pass 4)

- R4-A3 (low): the 320px page Tab cycle includes the offscreen skip link and runs `expectFocusVisible` on it once revealed (AE-05). The unmeasured size and contrast are low-risk (8px/12px padding, Fluent foreground/background tokens), and the skip link is not a 13.6-changed surface.
- R4-A5 sub-claim (false): the shipped 3,000ms notice is covered by `FcProjectionConnectionStatusTests`; only the AppHost lockstep survives, as the patch above.
- R4-A6 (false): the remount assertion (`ReferenceEquals` false) proves a fresh renderer whose `_hiddenOutcome` is a per-instance field, so the carried BH02 premise holds; the redundant empty-feedback check does not weaken it.
- R4-A7 (low): no aria-hidden horizontally scrolling surface exists; removing the exclusion needs a composed walk and a review of why it was added.
- R4-B1/R4-E12 (low; reclassified from patch on 2026-10-23 before any change): a two-project probe with the pinned Playwright 1.63.0 shows that the JSON reporter merges specs across projects only when the working directory is the `testDir` itself. Its merge compares an already-relative `spec.file` with `relative(rootDir, spec.file)` resolved from the working directory. Every documented invocation runs from `tests/e2e` or the repository root, so each project keeps its own spec and `spec.id` equals that project's `info.testId`; earlier three-engine replays accepted 116 rows per engine. A run launched from `tests/e2e/specs` fails closed with "Unbound" errors. Recheck on a Playwright upgrade.
- R4-B4 (false): snapshot bytes must equal the final passed attempt's own `image/png` attachment. The PNG-integrity spec records that element screenshots are not viewport-sized and scopes the contract to decodability.
- R4-B5 (low): reusing the pinned Playwright decoder without dependency changes was a recorded decision of the PNG-integrity spec. `./lib/utilsBundle` is in playwright-core 1.63.0's exports map, and an upgrade fails loudly in the Node suite.
- R4-B6 (low): a failing normal-mode geometry or contrast measurement fails its test, and the validator accepts only final passed attempts. Only the removal of an opt-in goes unnoticed, and guarding it would mean new validator rules and fixtures.
- R4-B10/R4-E7 (low): fails closed. Current exception records each match one element (density radio by value, the single abandonment breadcrumb link), and per-record staleness would also need a validator change.
- R4-B12 (low; upper-bound claim false): leaving out a cap was deliberate (R-G01: JS acknowledges every positive finite measured height), and the generated guards are pinned by approved RazorEmitter snapshots.
- R4-B13 (low): reflection is the established pattern in these suites and fails loudly; `InternalsVisibleTo` hooks would add test-only surface.
- R4-B15 (low): the seams register after quickstart/domain (`Program.cs:176`), `Replace` makes them resolve, and the Test-host browser lane exercises each one; the test's purpose is environment gating.
- R4-B16 (false): neither Shell nor Fluent 5.0.1 sets `color-scheme` on html/body (only on `.fluent-datepicker`), so both probes resolve the same Canvas; as `color` and as `background-color` it resolves to the same system color.
- R4-E2 (low): fails closed, and no current page triggers it (lanes green). A correct prefilter needs pseudo-envelope geometry, because flow pseudos can overflow their host.
- R4-E5 (low): real Fluent focus strokes against elevation layers of at most 0.28 alpha keep more than a 10:1 margin; the fix would restructure the conservative shadow bound.
- R4-E6 (low): the shell's single modal reservation prevents stacked modals, and no current surface stacks them.
- R4-E8 (low): reproduced as a fail-closed false failure, but no current surface triggers it, and a correct inline-host envelope proof is not a direct fix.
- R4-E9 (low): the evidence intentionally binds to the currently painted 320×900 state, matching its PNG. Scrolling would disturb the root-scroll/transient-state protocol, and the boundary paint uses shared Fluent tokens.
- R4-E10 (false): `ready()` applies text spacing (`accessibility-state-matrix.spec.ts:13-22`).
- R4-E11 (low): fails closed, every current page cycle passes, and deep shadow active-element tracking is nontrivial.
- R4-E16 (false): `ResolveHiddenWarningText` receives only Shell-built warnings with localized non-blank Title/Detail and no EntityLabel/RetryAfter; server warnings take the catch path.

#### Pass 5 (pass 4 fix delta, 2026-10-23)

Scope: the uncommitted pass 4 fix delta `git diff HEAD -- src tests` (15 files, +247/−54); spec and sprint narrative excluded. Four layers returned 40 raw findings (`R5-A/V/B/E#`); each verdict is in the Review Triage Log. Several entries correct the pass 4 patches themselves.

- [x] [Review][Patch] (applied) `background()` composites a `visibility:hidden` ancestor's background, which never paints, under the visible descendants that R4-E3 now samples. Two probes certified 13.08:1 and 21:1 for text whose real backdrop is white. Skip hidden layers there and in the opaque-plane proof, and extend the R4 fixture with a background-bearing hidden ancestor (R5-V3, R5-A1, R5-E4) [tests/e2e/helpers/responsive-accessibility.ts:1053]
- [x] [Review][Patch] (applied) While an out-of-flow escape is in progress, a nested absolute ancestor replaces the fixed element's viewport containing block, so a positioned overflow box re-clips the fixed text and drops its sample (probe-confirmed). Assign `outOfFlowBlock` only when not already escaped (R5-V6, R5-A2, R5-E6) [tests/e2e/helpers/responsive-accessibility.ts:1193]
- [x] [Review][Patch] (applied) Escaped out-of-flow text is composited against intermediate ancestors' backgrounds even where those boxes are not behind it. Include such a layer only when its box contains the sampled boxes; otherwise skip it when disjoint, or fail for pixel evidence on partial overlap (R5-E5) [tests/e2e/helpers/responsive-accessibility.ts:1053]
- [x] [Review][Patch] (applied) `expectNoTextClipping` still returns at a `visibility:hidden` element, so the visible descendants R4-E3 meant to include are never visited. Descend through own-visibility-hidden elements while keeping the other exclusions (R5-E14) [tests/e2e/helpers/responsive-accessibility.ts:990]
- [x] [Review][Patch] (applied) Add visibility fixtures for `focusGeometry` (painted descendant of a hidden ancestor covering the ring), `expectNoTextClipping` (clipped visible text in a hidden ancestor), competing paint, and a contrast negative control (inherited-hidden text yields no sample) (R5-V4, R5-B3) [tests/e2e/specs/responsive-accessibility.spec.ts:2138]
- [x] [Review][Patch] (applied) Overlap is now tested on glyph boxes only, so unrelated text whose shadow or stroke (not its letters) reaches the sample is skipped instead of requiring pixel evidence. Expand the glyph boxes by the shadow offset/blur and stroke extent before the overlap test (R5-B1) [tests/e2e/helpers/responsive-accessibility.ts:1364]
- [x] [Review][Patch] (applied) No test disposes the form during a blocked attempt's hidden-outcome clear; removing the new `_disposed` return would still publish "Command not run" and invoke `OnBlockedSubmission`. Add blocked (admission-busy and in-flight) disposal cases asserting nothing is published, and pin the guard in the emitter test (R5-V1, R5-B9) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:88]
- [x] [Review][Patch] (applied) No rendered test keeps `CommandWarningFallback` in the body when the server title is non-blank and Detail/EntityLabel are blank; add en/fr cases asserting title, body fallback and retry hint (R5-V2, R5-B14) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs:41]
- [x] [Review][Patch] (applied) R4-A1 is proven only at the form callback. Add a renderer-level case: an earlier RateLimited outcome is visible, a blocked click removes `fc-zero-field-outcome`, AM-20 is presented once, and focus stays on the trigger with nothing dispatched (R5-A4, R5-B9) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:124]
- [x] [Review][Patch] (applied) Validator robustness: a report, state file or capability that parses to `null` or a non-object still crashes or is silently ignored; zero capability attachments still surface only as per-file "Missing actual forced palette paint" while evidence declares media-only forced colors; the unreadable-capability branch is untested (R5-A3, R5-B6, R5-E10, R5-E11, R5-E12) [tests/e2e/scripts/validate-a11y-artifacts.mjs:62]
- [x] [Review][Patch] (applied) `containingBlock` misses `will-change: translate|scale|rotate|contain`, `content-visibility` other than `visible`, and `transform-style: preserve-3d`, and accepts a positioned `display: contents` ancestor; add those direct cases. Applying `contain: paint` clipping in `clippedPaint` is pre-existing and stays out of scope (R5-E7, R5-B5) [tests/e2e/helpers/responsive-accessibility.ts:1142]
- [x] [Review][Patch] (applied) The warning bar still repeats itself when a non-blank Detail or EntityLabel equals the server Title; skip a body candidate equal to the title (R5-E2) [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:2055]
- [x] [Review][Patch] (applied) The lease-release probe lacks a negative control; assert inside the clearing callback that the gate denies a probe (the lease is held) before disposing (R5-B10) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:101]
- [x] [Review][Patch] (applied) Other banner focus tests still assert the merged `FocusCalls`; assert the identifier-specific counters where the path is known (R5-B11) [tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandedRowHiddenBannerTests.cs:376]
- [x] [Review][Patch] (applied) The reduced-motion sweep-outline assertion runs under forced colors and ignores color; check a non-transparent outline with forced colors off as well (R5-B15) [tests/e2e/specs/responsive-accessibility.spec.ts:634]
- [x] [Review][Patch] (applied) `visible(element, ownVisibility = true)` has one caller, which passes `false`; drop the dead parameter and the duplicate `getComputedStyle` in `visit` (R5-B16) [tests/e2e/helpers/responsive-accessibility.ts:1067]
- [x] [Review][Defer] The AppHost Test-run timing is pinned only as source text; nothing proves that the AppHost-launched host binds 30000 ms or that a reused/manual host matches the recorded `fixtureOptions` (R5-V5, R5-B12) [src/Hexalith.FrontComposer.AppHost/Program.cs:177] — deferred: carried by the existing R-VG02 ledger entry (effective Counter resource configuration needs an AppHost launch harness); no new entry.

Patches applied 2026-10-23, uncommitted (16 entries). Red/green and broad checks:
- The four new R5 browser tests fail against the Pass 4 helper (12 failures across three engines) and pass after the fix.
- The two new validator CLI cases fail against the Pass 4 scripts; the Node evidence suite passes 28/28.
- The blocked-attempt disposal theory fails with the new `_disposed` guard temporarily removed, then passes after it is restored and rebuilt.
- SourceTools emitter classes 103/103: the two CommandForm approvals changed only in the R5-E2 body hunk.
- Affected Shell classes 220/220; `AnalyzerPolicy_IdentifierInventory_MatchesSeal` passes.
- Complete Story 13.6 browser selection against a rebuilt Release Test host: 903 cases, 887 passed / 12 failed / 4 skipped. Failures are the same twelve Firefox/WebKit specimen visual-baseline height mismatches, with no non-visual failure. TypeScript typecheck passes.

##### Rejected (pass 5)

- R5-E1/R5-B8 (low): for a newer attempt to be admitted during the blocked clear, the blocking condition must clear within one parent render, and the pre-existing `InvokeAsync` hop before `OnBlockedSubmission` already allows the same interleaving. An attempt sequence would add state for an undemonstrated window.
- R5-E3/R5-B2 (low): a `visibility:visible` pseudo-element inside a hidden host was excluded before this delta as well; sampling it needs the visit body restructured per sample kind.
- R5-E8 (low): propagated html/body overflow leaves offscreen escaped boxes unclipped, which only adds conservative samples.
- R5-E9 (low): `visibility: collapse` follows the helpers' existing `hidden`-only pattern; collapsed table tracks are not a current surface.
- R5-E13 (low): `FC_E2E_STORY_3_6_CONFIRM_DELAY_MS` is an explicit developer override of the lane default; the AppHost pins the default.
- R5-B4 (low): the target and clipping helpers clipped every overflow ancestor before this delta and fail closed; unifying the containing-block walk across helpers is a separate refactor.
- R5-B7 (false): Decision 2 covers zero-field feedback only; ordinary-form warning lifetime is Story 13.3 behavior and unchanged.
- R5-B13 (low): the reachable area of an unreachable target really is below 24px, so both retained reasons are accurate.
- R5-B14 fallback-chain sub-claim (false, carried R4-E16): `ResolveHiddenWarningText` receives only Shell-built warnings.

#### Pass 6 (pass 5 fix delta, 2026-10-23)

Scope: the pass 5 fix delta (pass 4 working state → current, 12 files, 610 diff lines). Four layers returned 50 raw findings (`R6-A/V/B/E#`); most are coverage gaps on the pass 5 helper branches. Verdicts are in the Review Triage Log.

- [x] [Review][Decision→Patch, chosen 2026-10-23: supplementary cue; rationale corrected in pass 7, R7-A1] Disposition: the sweep is a transient 700ms (`SweepTtl`) reinforcement, dispatched in the same render cycle as the "Reconnected -- data refreshed" notice for the same changed lanes; the text carries the meaning, so WCAG 1.4.11 does not apply. It is subtle by Story 5.4 design in both motion modes (45%/35% brand outline). This is an engineering classification, not a UX approval; UX may later raise it to a ≥3:1 Fluent stroke token as a Story 5.4 follow-up. Patch: reword the test to assert that a static, non-moving marker remains, without claiming preserved meaning. Original finding: reduced-motion reconciliation sweep contrast — under `prefers-reduced-motion` the sweep keeps only `outline: 1px solid` brand at 35% alpha (about 1.6:1 on white; at most about 2.4:1 at 35% alpha), below AC1's 3:1 non-text threshold. The pass 4 test asserts only alpha > 0 yet comments "no essential meaning is lost". Decide whether the sweep is supplementary to the Reconnected notice (record the disposition and reword the assertion) or essential (raise it to ≥3:1 through a Fluent stroke token, a Story 5.4 visual change needing owner approval) (R6-A2, R6-B15, R6-E16) [src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css:151]
- [x] [Review][Patch] Pin the pass 5 helper branches with three-engine fixtures. Mutation probes passed all 146 synthetic fixtures for each of: the `display:contents` skip and new containing-block conditions; `background()` `paints = inside` and its nested-absolute guard; the opaque-plane visibility guard; shadow blur reach, stroke-only reach, and a shadow that stops short (R6-V1, R6-V2, R6-V3, R6-V4, R6-A4, R6-A5, R6-B16) [tests/e2e/specs/responsive-accessibility.spec.ts:2185]
- [x] [Review][Patch] The `will-change` containing-block regex treats `contain-intrinsic-size` as `contain`; match comma-separated tokens exactly (R6-E12) [tests/e2e/helpers/responsive-accessibility.ts:1174]
- [x] [Review][Patch] Outside samples (focus rings, borders) start `background()` at the parent, so an absolute/fixed focused element never seeds its containing-block escape; seed it from the element itself (R6-A7, R6-B11, R6-E6) [tests/e2e/helpers/responsive-accessibility.ts:1426]
- [x] [Review][Patch] A zero-area escaped ancestor fill that the sample boxes straddle throws a spurious partial-background error; treat zero-area fills as not painting (R6-E8) [tests/e2e/helpers/responsive-accessibility.ts:1073]
- [x] [Review][Patch] `expectTextContrast` still composites `visibility:hidden` ancestors' backgrounds; skip them as `background()` now does (R6-A8, R6-B13) [tests/e2e/helpers/responsive-accessibility.ts:905]
- [x] [Review][Patch] The renderer-level blocked test omits the speech-once and trigger-focus assertions that R5-A4 named; subscribe to the renderer surface and verify no focus move (R6-A1, R6-B7) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:175]
- [x] [Review][Patch] The blocked-disposal theory cannot distinguish the blocked path from the admitted path; probe inside the callback that this attempt holds no lease (R6-A9, R6-B4, R6-E15) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:136]
- [x] [Review][Patch] The scope-unavailable blocked branch calls `FocusAttemptedControlAsync` after a parent callback that may dispose the form; add the `_disposed` return and a scope-unavailable disposal arm (R6-B5, R6-E5) [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:562]
- [x] [Review][Patch] The lease negative control accepts any denial; require `AdmissionAlreadyInProgress` (R6-E4) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:111]
- [x] [Review][Patch] The warning-dedupe theory lacks padded-equal, EntityLabel-equal and Detail-equal-with-different-EntityLabel rows (R6-B3, R6-E3, R6-V5) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs:73]
- [x] [Review][Patch] Filter-path banner tests no longer rule out a stray `focusReplacementHeading` call, and `CoMountedHeldFocusImportHandsCurrentEpisodeToSurvivingOwner` still uses the merged counter (R6-A3, R6-B6, R6-E14) [tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandedRowHiddenBannerTests.cs:316]
- [x] [Review][Patch] The validator still throws `TypeError` on nested bad shapes (`{"suites":[null]}`, non-array `results`/`attachments`) and on `{"suites":{}}`, which is untested; the capability loop in the new test is order-dependent (R6-A10, R6-B8, R6-B9, R6-E13, R6-V6) [tests/e2e/scripts/validate-a11y-artifacts.mjs:65]
- [x] [Review][Patch] The media-only status literal is duplicated in three files; export one constant (R6-B10) [tests/e2e/scripts/a11y-state-evidence.mjs:109]
- [x] [Review][Patch] The competing-paint visibility fixture has no valid counterpart (inherited-hidden cover must pass) (R6-A6) [tests/e2e/specs/responsive-accessibility.spec.ts:2197]
- [x] [Review][Defer] An absolute box placed outside its containing block (negative offsets, `top:100%` menus) still composites that block's fill and every ancestor above it; pre-existing compositing scope (R6-B12, R6-E7) [tests/e2e/helpers/responsive-accessibility.ts:1064] — deferred: pre-existing, medium; settle with a non-top-layer menu fixture outside a filled containing block.
- [x] [Review][Defer] Pass 4/5 change contrast/clipping semantics but no candidate digest binds them and `13-6-accessibility-result.md` predates them (R6-A11) — deferred: covered by the open Evidence task; record after the fixes are committed.

##### Rejected (pass 6)

- R6-B1/R6-E1/R6-E2 (low): normalizing case/punctuation or comparing against the localized fallback adds locale heuristics; the demonstrated duplicate was an exact title match.
- R6-B2 (low): the hidden zero-field outcome is intentionally one sentence; a server title is meaningful content there.
- R6-B14/R6-E11/R6-V-Other (low, carried R5-E3): a `visibility:visible` pseudo-element in a hidden host is now probe-confirmed in a synthetic page, but no current surface uses it; sampling it needs per-kind restructuring.
- R6-E9 (low): multi-line inline/border/background-clip fill geometry is an approximation outside any current surface.
- R6-E10 (low, carried R5-E9): `visibility: collapse`.

#### Pass 7 (pass 6 fix delta, 2026-10-23)

Scope: the pass 6 fix delta `git diff 4a4051c2..HEAD -- src tests` (code-only commit `6f6eb898`; 14 files, +241/−39, 625 diff lines); spec, result and receipt narrative excluded. Four layers returned 33 raw findings (`R7-A/V/B/E#`): Blind Hunter 18, Edge Case Hunter 9, Verification Gap 2 (one pre-verified gap plus one probe-confirmed other finding) and Acceptance Auditor 4. Each verdict is in the Review Triage Log. A three-engine triage probe confirmed that `will-change: backdrop-filter|transform-style|offset-path` and the `backdrop-filter` property create containing blocks; the Acceptance Auditor's probe confirmed the in-flow zero-height false pass.

- [x] [Review][Decision→Patch, chosen 2026-10-24: keep supplementary on a corrected rationale] The sweep stays an engineering-classified supplementary cue, independent of the Reconnected notice: it is a transient 700ms (`SweepTtl`) marker on rows whose refreshed values are themselves the content, and it is never the only carrier of information. It is not guaranteed to accompany the notice, because a partial reconnect paints it with no notice. This replaces the pass 6 "same render cycle" rationale; it is still not a UX approval, and the ≥3:1 Fluent stroke token stays an optional Story 5.4 follow-up. The patch below rewords the test comment. Original finding: Sweep disposition premise is false for partial reconnects — The pass 6 decision classified the reduced-motion reconciliation sweep (35% brand outline, about 1.6:1) as supplementary because it is "dispatched in the same render cycle as the Reconnected notice". `ReconnectionReconciliationCoordinator` dispatches `MarkReconciliationSweepAction` whenever `ChangedViewKeys.Count > 0` (`:228`), but completes `succeeded: false` when any lane failed, was skipped or read incompletely; `Complete` maps that to `Idle`, and `FcProjectionConnectionStatus` shows and announces Reconnected only for `Refreshed` (and also suppresses it offline or after an epoch change). A partial reconnect therefore paints the sub-3:1 sweep with no notice, and the new test comment (`responsive-accessibility.spec.ts:623-624`) states the false premise. Options: (a) keep the supplementary classification on a corrected rationale (the transient 700ms marker highlights changed data that itself carries the content; no notice dependency) and reword the comment; (b) dispatch the sweep only for a fully successful pass, coupling it to the notice (Story 5.4/5-5 behavior change); (c) raise the reduced-motion marker to a ≥3:1 Fluent stroke token (Story 5.4 visual change needing owner approval) (R7-A1, R7-B18)
- [x] [Review][Patch] (applied) Reword the sweep test comment to the corrected rationale: a supplementary transient marker on changed rows whose refreshed values carry the content, with no dependency on the Reconnected notice, and no contrast claim (R7-A1 resolution, R7-B18) [tests/e2e/specs/responsive-accessibility.spec.ts:623]
- [x] [Review][Patch] (applied) `containingBlock` misses demonstrated containing-block triggers: the R6-E12 exact-token rewrite dropped `will-change: backdrop-filter` and `transform-style` (the old regex matched them across hyphen word boundaries), and neither version handles `will-change: offset-path`, the `backdrop-filter` property, or WebKit's `-webkit-backdrop-filter` (property and `will-change` token). A fixed label inside such an `overflow:hidden` ancestor is really clipped in all three engines but is still sampled as escaped. Add the tokens and property checks, and extend the R6 containing-block fixture loop with those rows (R7-V2, R7-B12, R7-E9; the bare `offset-path` property is not demonstrated and stays out) [tests/e2e/helpers/responsive-accessibility.ts:1177]
- [x] [Review][Patch] (applied) No test proves that a non-disposed scope-unavailable attempt still focuses its attempted control: `ScopeUnavailableIsVisibleWithoutDispatchOrHiddenFocusAndEachAttemptSpeaksOnce` never asserts `focusAttemptedControl`, so an always-true `_disposed` guard would pass every runtime test, and the disposed row's `VerifyNotInvoke` has no positive control. Assert one `focusAttemptedControl(<form>-submit, false)` per click there (R7-V1, R7-B3) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:301]
- [x] [Review][Patch] (applied) The blocked-disposal theory was not updated coherently for its scope row: the comment and `noLeaseDuringClear` still say "clear" although the scope row disposes during its warning; the tenant restore is outside `finally`; and the in-callback `probe.IsAdmitted.ShouldBeTrue()` duplicates the post-callback assertion while throwing inside the component (R7-B4) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:140]
- [x] [Review][Patch] (applied) The warning-dedupe theory has no row with a non-blank Detail distinct from the title, so Detail-over-EntityLabel precedence is unverified, and the `"Affected item"` row never asserts the localized fallback is absent ("one body"); rename `fallback` to `expectedBody` (R7-B5, R7-E4 distinct-Detail part) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs:79]
- [x] [Review][Patch] (applied) The emitter ordering test throws `ArgumentOutOfRangeException` instead of a named assertion when the scope-presentation line is missing (`IndexOf(..., -1, ...)`), and checks the guard against `scopeBranchEnd` before asserting that branch exists; assert both anchors first (R7-B8, R7-E3) [tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs:1327]
- [x] [Review][Patch] (applied) The negative half of "R6 simple contrast ignores hidden ancestor fills" uses an unconstrained `rejects.toThrow()`; any locator or "needs pixel evidence" error satisfies it. Match the contrast assertion failure (R7-B14, R7-A4) [tests/e2e/specs/responsive-accessibility.spec.ts:2280]
- [x] [Review][Patch] (applied) The validator CLI prints the prefix twice ("Invalid authoritative Playwright report <path>: Invalid authoritative Playwright report: …"), and the now-redundant `isRecord` pre-check claims a required suites array although `suites` is optional (R7-B15) [tests/e2e/scripts/validate-a11y-artifacts.mjs:65]
- [x] [Review][Defer] `background()` composites every non-escaped ancestor fill without geometry. Beyond the pass 6 containing-block case (R6-B12), in-flow text overflowing a zero-height filled ancestor is a probe-confirmed false pass (15.46:1 against black for text actually on white, about 1.4:1) [tests/e2e/helpers/responsive-accessibility.ts:1058] — deferred: pre-existing, medium; extends the R6-B12 entry. Settle with an in-flow overflow fixture plus the non-top-layer menu fixture, then apply the inside/disjoint/empty fill test to in-flow and containing-block layers (R7-A3, R7-B10, R7-E8).
- [x] [Review][Defer] `13-6-accessibility-result.md` does not record the sweep's sub-3:1 non-text classification or the UX follow-up it permits [_bmad-output/implementation-artifacts/13-6-accessibility-result.md] — deferred: covered by the open Evidence task; record after the R7-A1 decision (R7-A2).

##### Rejected (pass 7)

- R7-B1/R7-E1 (false): after disposal, `PresentScopeUnavailableAsync` and the authorization-denied branches only queue `StateHasChanged` and log. The renderer ignores a render queued for a disposed component, and no speech, focus or dispatch follows.
- R7-E2 (false): `_formDomId` is instance-unique (`Interlocked` sequence), so a disposed form's control is disconnected and `focusAttemptedControl` returns false without moving focus.
- R7-B7 (false): the whole-test `VerifyNotInvoke` checks are stricter than a windowed check, and `restoreOverlayOrigin` is emitted only on the overlay renderer path (`CommandRendererEmitter.cs:642`), not the inline zero-field renderer.
- R7-B16 (false): no TypeScript consumer imports the module's other exports; a future one fails loudly at typecheck.
- R7-B17 (false): the single exported constant (R6-B10) changes writer and validator together; re-validating older evidence after a change fails loudly. `'painted system palette'` is informational; validity is gated on `forcedColorsPaint`.
- R7-B2 (low): the post-admission scope exits return immediately and the `finally` releases the lease; there is no disposal-sensitive continuation to test, and the code is unchanged by this delta.
- R7-B6/R7-E4 case part (low, carried R6-B1/E1/E2): case-insensitive dedupe adds locale heuristics.
- R7-B9/R7-E5 (low, carried R5-E9/R6-E10): `visibility: collapse`.
- R7-B11/R7-E7 (low): positioned exterior pseudo paint on a static host is pre-existing with no current surface; fixing it means threading pseudo position through `sample()` and `background()`.
- R7-B13 (low): skipped `content-visibility:hidden` contents only add conservative samples; no current surface; needs a new `visible()` branch.
- R7-E6 (low): a `visibility:hidden` target was measured before this delta as well; callers target visible text, and failing closed needs a new guard.

#### Pass 8 (pass 7 fix delta, 2026-10-24)

Scope: the pass 7 fix delta `git diff 6f6eb898 HEAD` over seven test files (the source/test part of `482206ca` plus `5b8bae10`; +64/−20, 252 diff lines). The spec, ledger, gitlinks, `adc0dbe2`, `aee73e58` and the evidence-simplification merges are excluded. Four layers returned 30 raw findings (`R8-A/V/B/E#`): Blind Hunter 12, Edge Case Hunter 11, Verification Gap 2 (no gaps; two other findings) and Acceptance Auditor 5. Each verdict is in the Review Triage Log. A three-engine triage probe (Chromium 153, Firefox 155, WebKit 26.6) confirmed that computed `will-change` keeps the authored case (`TRANSFORM`) although every engine contains, that `offset-path: ray(0deg)` contains in all three while Chromium and Firefox keep `transform: none`, and that `path("M0 0")` computes to `none`.

- [x] [Review][Decision→Defer, chosen 2026-10-24: option (b)] Scope-unavailable attempted-control focus targets the hidden form's submit — The new positive control in `ScopeUnavailableIsVisibleWithoutDispatchOrHiddenFocusAndEachAttemptSpeaksOnce` pins `focusAttemptedControl(fc-command-form-…-submit, false)`. In the zero-field inline renderer that button sits inside `<div style="display:none">` (`CommandRendererEmitter.cs:1005-1006`), so `focusTarget` cannot focus it and returns false. Every other blocked reason in the same renderer uses the visible trigger (`FcCommandBlockedOutcome` `AttemptedControlId = _triggerButtonId`, `:998`; asserted at `ZeroFieldFailureAccessibilityTests.cs:239-241`), and Decision 2 says "keep trigger focus". The scope branch (`CommandFormEmitter.cs:567`, Story 13.3 `b3744b3e`) is pre-existing; the pass 7 test now cements its hidden target. Keyboard activation and Chromium/Firefox pointer activation already leave focus on the trigger, so the visible gap is pointer activation that does not focus the button (Safari/WebKit); no browser test covers scope-unavailable focus. Options: (a) patch: generated forms take the host's attempted-control id, the zero-field renderer passes `_triggerButtonId`, the scope branch focuses it and the test asserts `triggerId` (adds a generated `[Parameter]` and approval updates); (b) defer as pre-existing 13.3 behavior, settled by a WebKit pointer-activation fixture, and reword the new assertion to say it pins the current hidden-form target rather than the AM-20 contract; (c) accept the current behavior (R8-B1, R8-V2) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:330] — deferred: pre-existing Story 13.3 behavior (`b3744b3e`); only Safari-style pointer activation is affected, so settle it with a WebKit pointer-activation fixture before changing the generated form contract.
- [ ] [Review][Patch] Reword the scope-unavailable positive control so it pins the current hidden-form `-submit` target as a known limitation (deferred R8-B1), not as the AM-20 attempted-control contract; keep the once-per-attempt count (R8-B1 option (b), R8-V2) [tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs:325]
- [ ] [Review][Patch] `containingBlock` ignores a computed `offset-path`: `ray(0deg)` contains a fixed label in Chromium and Firefox while computed `transform` stays `none` (WebKit reports an identity matrix the transform check already catches). Add a computed `offset-path` ≠ `none` check and an R6 `offset-path:ray(0deg)` row (R8-B2, R8-E3) [tests/e2e/helpers/responsive-accessibility.ts:1177]
- [ ] [Review][Patch] `will-change` token matching is case-sensitive: computed `willChange` keeps `TRANSFORM` as authored and all three engines contain, yet `containingHints.includes` misses it. Lower-case each token and add a mixed-case R6 row (R8-E1) [tests/e2e/helpers/responsive-accessibility.ts:1181]
- [ ] [Review][Patch] The computed `backdrop-filter` clause in `containingBlock` is unobservable: `background()` (`:1083`) and `clippedPaint()` (`:1192`) throw "Filtered or blended paint needs pixel evidence" on every backdrop-filter ancestor they walk, escaped or not; two layers deleted the line and every R5–R7 fixture still passed in all three engines. Delete the clause and retitle/re-comment R7 as the fail-closed filtered-paint pin it is; it does not prove containment (R8-B4, R8-A2, R8-E4, R8-V1) [tests/e2e/helpers/responsive-accessibility.ts:1178]
- [ ] [Review][Patch] The prefixed R6/R7 rows never pin which engine contains: their `contained` result can flip silently (it also relies on the `<p>` offsetting the block from the viewport top), and the R7 comment "fails closed in every engine" is false for `-webkit-backdrop-filter` in Chromium and Firefox, which take the escaped branch. Assert that `contained` equals `browserName === 'webkit'` for prefixed rows and correct the comment (R8-A3, R8-B5) [tests/e2e/specs/responsive-accessibility.spec.ts:2241]
- [ ] [Review][Patch] The tightened CLI regexes match the report path with `\S+`, so a temp path containing a space (a Windows profile such as `C:\Users\John Doe\…`) fails a correct message. Match the escaped `join(root, 'playwright.json')` path instead (R8-B6, R8-A4, R8-E9) [tests/e2e/scripts/a11y-state-evidence.test.mjs:440]
- [ ] [Review][Patch] The emitter ordering test still slices `source[present..presentEnd]` and searches from `present` before asserting that either anchor exists, so a missing method header throws `ArgumentOutOfRangeException` (the R7-B8 defect one level up), and the new comment calls `scopeBranchEnd` a search start. Assert `present` and `presentEnd` first, and reword the comment to cover every anchor that bounds a slice, search or comparison (R8-E6, R8-B8 comment part) [tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs:1315]
- [ ] [Review][Patch] The corrected R7-A1 test comment says the sweep "briefly marks changed rows", but `GridHostClass()` adds `fc-reconciliation-sweep` to the whole grid host for a changed view key (`RazorEmitter.cs:1816-1820`), not to individual rows. Reword it to a refreshed grid (R8-B11 wording part, R8-E11) [tests/e2e/specs/responsive-accessibility.spec.ts:623]

##### Rejected (pass 8)

- R8-B3/R8-E2 (low): prefixed or alias `will-change` hints (`-webkit-transform|filter|perspective|transform-style`, `offset`, `-moz-transform`) contain in some or all engines, but no surface uses them, and complete coverage needs a per-engine table that `CSS.supports` cannot derive (WebKit supports `offset` yet does not contain; `-webkit-transform-style` rejects `none`).
- R8-B7 (low): the duplicated prefix literal drifts loudly, because the CLI regexes at `:440` and `:487` fail on any mismatch; a shared export adds module and declaration surface.
- R8-B8 return part (false): removing the scope branch's `return;` withdraws the outcome and presents `FcCommandBlockedOutcome`, whose second `focusAttemptedControl(trigger)` breaks `ShouldHaveSingleItem` in the runtime scope test, which also waits for the withdrawn "workspace identity" outcome.
- R8-B9 (low): busy and in-flight rows never focus inside the form by design (the renderer owns that focus); their `VerifyNotInvoke` still catches a focus call added ahead of the disposed guard, and the scope row has its R7-V1 positive control.
- R8-B10 (low): the remaining suggested rows exercise no new branch; the case-variant part is carried R6-B1/E1/E2 and R7-B6.
- R8-B11 contrast part: decided by R7-A1 (supplementary on partial reconnects, chosen 2026-10-24).
- R8-B12 (false): the positive half certifies 21:1 only against white, and the negative half can fail only on the ratio; a composited black fill would pass rather than throw.
- R8-A1 (low): the pass 7 negative-control record undercounts: 10 rows fail, not 7, because Firefox fails `backdrop-filter`, `transform-style` and `offset-path` like Chromium (two layers reproduced it). Correcting it edits the spec under review, so it is recorded here instead.
- R8-A5: carried R7-A2 (Evidence task); no new ledger entry.
- R8-E5 (maybe-false, low): no fc-focus interop is pending once the count reaches two, and a late call would break the exact count anyway.
- R8-E7 (false): same as the R8-B8 return part.
- R8-E8 (low): carried R6-B1/E1/E2 and R7-B6.
- R8-E10 (false): the validator writes one `console.error` line, and the nested-shape regex fails on an inline duplicate prefix.

## Implementation Notes

### Completion follow-up, 2026-10-16

The current completion candidate `sha256:f8d9a2b2a0608d5ed0bc461f433734e153b2836fa1237658539cc638bb2c03cf` executes real generated zero-field outcomes, real generated-detail removal and 116 themed state rows per engine. The final three-spec nonvisual run is 361 passed / 0 failed / 2 skipped; sixteen Build-review findings are patched. Latest configured Shell remains failed at3680 passed / 1 failed on its separately attributed source; the preceding `16abc` run retains 12 visual failures and8 existing skips. Unsupported WebKit actual palette paint, Windows and all manual/owner gates remain open. [Current result and human checklist](13-6-accessibility-result.md) gives candidate-bound proof; no completion or release approval is inferred. Frozen decisions are unchanged.

### Historical implementation record, 2026-10-15

The user requested both recommendations be applied and asked for a human checklist with manual checks open. Implementation and executed focused lanes pass; Story 13.6 remains incomplete. Automated matrix expansion and the candidate/checklist are available, with full remaining surface/state/theme coverage, missing browser integrations, manual observations and owner gates explicitly open in `13-6-accessibility-result.md`. The unchecked tasks retain those limitations. No completion or release approval is inferred.

Evidence inspected: 201 focused Shell/integration tests, 1,313 serialized SourceTools tests, 28 new/helper Chromium cases and 22 existing specimen cases (six ordinary visual comparisons included). Earlier broad Shell ran 3,678 tests with three subsequently corrected pins and one environment skip; no settled full-suite pass is claimed. Candidate implementation hashes were checked independently and match. Isolated generator p95 is 333.403ms. Final real-grid spacing/ItemSize is 32→47; enlarged-user-text stress reaches 113, then Roomy plus override removal restores 56→Compact 32. The final 42 route/toolbar/announcement journeys and the real-banner fixture case also pass.

The review resolved 18 patch groups, including the Settings accordion boundary and final fixture isolation. BH12 was rejected as a low-severity unused exception path; no deferred-work entry was created. Spec and sprint remain in review, with manual checks and remaining matrix coverage open as requested.

## Spec Change Log

### Resumed review correction — 2026-10-17

Fourteen individual findings were classified after all three independent review layers returned. The frozen intent is sufficient; the implementation tasks did not specify composed-tree clipping, actual painted contrast/target envelopes, Windows fixture paths or query navigation identity. Re-derive those non-frozen tasks below. No code from this run exists to revert; preserve all previously committed work. KEEP: the current production rendering, contrast thresholds, Fluent ownership, all 24 approved image bytes, typography/density contracts, final-attempt evidence authority, single logical speech owner, and prior passing positive/negative regressions. No dependency/policy/submodule update, snapshot regeneration, staging, commit or push.

## Review Triage Log

| Finding | Verdict | Evidence | Route |
|---|---|---|---|
| BH01 | medium | The detail cancellation handler uses the first composed-path HTMLElement and closest(), which cannot cross a Fluent shadow boundary; the reviewer reproduced cancellation on an in-detail activation. | patch G01 |
| BH02 | medium | observeRowMetrics initializes height to the density floor, suppressing its first floor-valued callback. Returning from a different density can leave the generated cached ItemSize above the actual CSS row height. | patch G02 |
| BH03 | medium | The cell measurement takes the largest immediate child, not the complete vertical extent. Two stacked 30px blocks reproduce a 60px content extent reported as 32px. | patch G03 |
| BH04 | medium | ShowValidationSummaryAsync forwards only form-level errors for hidden forms; allowlisted field-only validation lives in EditContext and is omitted from the visible outcome. | patch G04 |
| BH05 | medium | The normal returned Rejected branch dispatches RejectedAction but never forwards its safe copy, while the hidden lifecycle card remains invisible. The exception path does forward. | patch G05 |
| BH06 | medium | The warning callback uses a null-coalescing chain without a nonblank fallback; NotFound/RateLimited with empty or whitespace problem text can clear feedback and speech. | patch G06 |
| BH07 | medium | Fluent 5.0.0 AllowDismiss defaults to true (verified with package API documentation and dotnet-inspect). The new bar omits the parameter, permitting feedback to disappear before the agreed next-attempt lifetime. | patch G07 |
| BH08 | medium | applyTextSpacing visits only roots present at invocation. A subsequently rendered open shadow root receives none of the four overrides, invalidating spacing evidence after remount. | patch G08 |
| BH09 | medium | focusGeometry accepts a focused target with zero outline and no box shadow; no assertion requires any visible focus indication. The negative fixture reproduced an empty failures array. | patch G09 |
| BH10 | medium | focusGeometry excludes pointer-events:none boxes and relies on hit testing. An opaque visually covering overlay with that property is ignored, although it obscures the target. | patch G10 |
| BH11 | medium | verifyKeyboardCycle succeeds on the first repeated active element after any one stop; it does not prove the expected controls were reached or forward/reverse modal boundaries. | patch G11 |
| BH12 | low | The User Agent Control exception validates native tags but not CSS ownership; an authored 8px native input can therefore be accepted if a caller supplies that exception. Current real inventories use only Spacing, with no User Agent Control claims. Establishing CSS-origin provenance adds machinery for an unused path. | reject: unlikely current use; nontrivial provenance checks |
| EC01 | medium | Independently confirmed the blank NotFound/RateLimited outcome described in BH06; the null/whitespace chain leaves no local recovery message. | patch G06 |
| EC02 | medium | The hidden warning callback omits RetryAfter, although the existing ordinary warning presents it; a rate-limited zero-field action loses its recovery guidance. | patch G12 |
| EC03 | medium | The generated grid key includes measured row height, so spacing changes remove the focused grid subtree. The observer has no capture/recovery for that demonstrated remount. | patch G13 |
| EC04 | medium | The same no-indicator helper defect as BH09 was independently reproduced with outline:none and box-shadow:none. | patch G09 |
| VG01 | medium | Pre-verified gap: component tests mock replacement focus and the browser fixture invokes tracking manually. Removing the real banner trackDetailFocus registration is not detected by those tests. | patch G14 |
| VG02 | medium | Pre-verified gap: the viewport failure/retry test uses the 44px floor and never accepts measured metrics. Reverting the measured-height divisor is undetected; an 88px row changes the required failure/retry page. | patch G15 |
| VG03 | medium | Pre-verified gap: modal keyboard coverage can pass after a short repeated cycle without the expected settings controls or reverse boundary. Existing body-escape checks do not establish those claims. | patch G11 |
| VG04 | medium | Verified Other finding: the measured-height grid key remounts focused descendants, with no existing focus preservation around the row-metric update. | patch G13 |
| P01 | medium | PresentHiddenOutcomeAsync awaits an external callback then can claim speech after disposal; SubmitAsync also continues after the awaited clear callback. A callback can synchronously dispose its component before returning, so both continuations need the existing disposed guard. | patch G16 |
| P02 | high | The stronger Settings cycle reproduced a premature wrap at Theme: containDialogFocus omitted fluent-accordion-item, so an internal heading was treated as an unknown boundary and Preview, Reset and Done were skipped. Adding that existing host to the selector is the direct correction; forward/reverse inventory detects its removal. | patch G17 |
| P03 | medium | The settled reused-journey run failed because the new missing-detail fixture was nested inside the old announcement fixture, exposing two fc-surface-status nodes to its uniquely scoped assertion. Move the new fixture beside the old section within the existing query gate; each then owns its existing status boundary. | patch G18 |

All findings were classified individually before grouping. G01–G18 are direct corrections in existing implementation/test seams, add no production public surface, and cover the demonstrated states above. No intent change or spec amendment is needed. BH12 is rejected under the low-severity rule; no current result claims that unused exception.

### Review continuation — 2026-10-17

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| R-BH01 | false | A new result identity while the same detail stays missing is a repeated refresh; the approved decision requires silence. `MissingWithoutFilterEvidenceShowsNeutralReasonAndAnnouncesItOnce` covers the identity change. | reject |
| R-BH02 | medium | JavaScript acknowledges a measured height above 4096 after the generated C# handler returns without applying it, leaving virtualizer spacing below the rendered row height. | patch R-G01 |
| R-BH03 | maybe-false | `observeContent` traverses the document on scheduled measurements; user-visible cost has not been measured on a multi-grid page. A representative profile would settle this. | defer |
| R-BH04 | medium | `CancelMissingFocusAsync` skips module disposal when `cancelReplacementFocus` throws a handled interop exception. | patch R-G02 |
| R-BH05 | false | AE-09 requires zero critical findings; the established scanner intentionally blocks serious and critical while recording minor and moderate findings. | reject |
| R-BH06 | medium | The hardcoded `bmad-loop` executable path is not portable, but it is an agent-context hook unrelated to this story; the baseline already contained absolute hook paths. | defer |
| R-BH07 | false | The pre-existing 50 MiB raw-artifact cap correctly fails the retained oversized run. The result explicitly records this open gate, and splitting or weakening the root check would not make the visual failures pass. | reject |
| R-BH08 | false | CI supplies `FC_A11Y_EVIDENCE_RUN_ID` to both run and validator. Omitting it is an intentional local replay mode for an existing packet, not a current-run claim. | reject |
| R-BH09 | false | Eighteen visual failures are explicitly recorded as open; regenerating approved baselines without human visual approval violates the frozen constraints. | reject |
| R-BH10 | false | Windows, WebKit paint, real zoom/devices and human AT observations are already marked unperformed, and the story remains in-review. | reject |
| R-EC01 | medium | Any `dialog[open]`, including a nonmodal dialog, currently excludes visible outside controls from target evidence. | patch R-G03 |
| R-EC02 | medium | The text-clipping check inspects host/ancestor overflow but not the generated pseudo's own hidden overflow, so its clipped label can pass. | patch R-G04 |
| R-EC03 | medium | Independent confirmation of R-BH02's generated handler ceiling versus JavaScript acknowledgement. | patch R-G01 |
| R-VG01 | medium | StatusOverview uses the measured grid height but has only generated-source assertions; no rendered test updates its virtualizer after a measurement. | patch R-G05 |

All fourteen findings were classified individually before grouping. R-G01 joins R-BH02/R-EC03; the other patch groups each have one root. R-BH03 and R-BH06 are deferred under the review rules; no frozen intent change is needed.

### Current Build review — 2026-10-17

The blind, edge-case and verification-gap layers reviewed the baseline-to-current textual diff before any classification. The 453 MB diff includes retained evidence; the source/test findings were verified against current callers, guards and earlier triage. The route below records every finding individually before grouping.

| Finding | Verdict | Verified evidence | Route |
| --- | --- | --- | --- |
| BH01 | medium | `13-6-accessibility-result.md` describes the prior committed `12e8b8e1` review patch as currently uncommitted at `0618c110`. Candidate identity is stale. | patch D1 |
| BH02 | false | `FrontComposerShell.razor` keys the renderer subtree by `ScopeBoundary.Generation`; `ShellScopeRemountClearsFeedbackAndCancelsHeldPriorScopeDispatch` proves tenant and user remount, feedback removal and cancellation. | reject |
| BH03 | maybe-false | The row observer measures materialized rows and remeasures as rows enter view. A mixed-height virtualized scroll trace is needed to establish a user-visible spacer error for unseen rows. | defer |
| BH04 | maybe-false | Carried R-BH03: document traversal cost has no representative multi-grid profile, and the observer still traverses on measurement. | carried defer; no new ledger entry |
| BH05 | low | Carried C2-BH10: a bare `addEventListener` target is not discoverable from DOM semantics, and no current changed consumer uses one. A general listener registry is disproportionate. | carried reject |
| BH06 | medium | `expectNoPageOverflow` resolved `aria-labelledby` against `ownerDocument`, which cannot find an ID in the same open shadow root. The three-engine fixture reproduces the missed valid grid label. | patch D2 |
| BH07 | medium | Existing cycle journeys cover palette and Settings, while destructive confirmation, abandonment, filter and view overlays lack the same full forward/reverse keyboard inventory. No new modal behavior is introduced by this review patch. | defer |
| BH08 | medium | The previous `motion.every` could pass on an empty list. The new three-engine fixture asserts all three CSS cue classes have reduced-motion paint; a live reconciliation-marker transition remains unproved here. | defer live transition; scoped CSS assertion added |
| BH09 | false | Actual 400% zoom is explicitly `not performed` in the result and blocks completion; neither the spec nor current result claims AE-02 closure. | reject |
| BH10 | false | NVDA, JAWS, VoiceOver and heard speech are explicitly unperformed and blocking, with the human checklist retained. | reject |
| BH11 | false | Physical devices, Windows execution and WebKit actual palette paint are explicitly open gates; no platform completion is claimed. | reject |
| BH12 | false | The result reports all 18 visual failures and the .NET failures as open; no passing broad-gate or story-completion claim is made. | reject |
| EC01 | medium | An offscreen nonfixed 30px button under a clipped root retained its full target size without a reachability probe. The three-engine fixture now rejects it. | patch D3 |
| EC02 | medium | Pointer partitioning used an overlapping element's uncut bounds, so a clipped 7px cover left a 23px reachable target certified as 30px. The three-engine fixture now measures the painted edge. | patch D4 |
| EC03 | medium | Fixed pseudo obstruction geometry assumed viewport coordinates even when its host establishes a transformed containing block. The three-engine painted-text fixture now requires pixel evidence. | patch D5 |
| EC04 | medium | Same offscreen reachability defect as EC01, independently reported from the B3 claim. | patch D3 |
| EC05 | medium | Same transformed fixed-pseudo geometry defect as EC03, independently reported from the B2 claim. | patch D5 |
| VG01 | medium | Pre-verified: no browser assertion checked the forced-colors paint of disabled informational palette shortcuts; generic contrast skips `aria-disabled` rows. The current state journey now does so in Chromium and Firefox. | patch D6 |
| VG-Other | medium | The hint's explicit color overrode the disabled row's `GrayText`; the CSS now assigns `GrayText` to disabled hints and context text, and the browser assertion checks the hint. | patch D6 |

D1–D6 are direct corrections in existing documentation, CSS and test helpers. No frozen intent, public API, threshold, dependency or approved screenshot changes. The previous full-candidate evidence remains historical; current checks and unresolved gates are recorded below and in the accessibility result.

### Resumed three-layer Build review — 2026-10-18

All three context-free layers returned before classification: ten Blind Hunter findings, two Edge Case Hunter findings and no Verification Gap findings. The full canonical baseline `3c8679628ebdb8995afc09f712a04fcde623695c` to candidate `1d2cc12eb467db048c41d1aee9c056f1e29e5fc1` diff was written to a system temporary file (940,978,821 bytes); nothing was staged in Git. Each finding is dispositioned separately below.

| Finding | Verdict | Verified evidence | Route |
| --- | --- | --- | --- |
| BR-BH01 native sizing exception | low | Carried BH12: User Agent Control validates appearance/inline styling rather than stylesheet provenance. Current real inventories use Spacing and make no User Agent Control claim; proving stylesheet origin adds machinery to the unused exception path. | carried reject; no new patch or deferral |
| BR-BH02 simple contrast opacity | medium | The simple helper used by real type/palette journeys ignores group opacity and stops at shadow boundaries. The new composed-ancestor regression fails on all three engines before correction. | patch P1 |
| BR-BH03 selected-option width | low | A closed 30px native select truncates its long selection. The current generated UI uses Fluent dropdown buttons, not this native-select value path; the existing native fixture deliberately permits native scrolling. Probing the native option popup or adding label-width policy is a nontrivial extension without a demonstrated changed operator consumer. | reject: unlikely current use, nontrivial fix |
| BR-BH04 missing recovery freshness | medium | Two awaited real component regressions fail against unchanged production source: a completion announces while ResultSettled is false, and an old episode consumes a replacement episode. Scope/surface checks alone do not guard result/group ownership. | patch P4 |
| BR-BH05 removed resize observations | maybe-false | observeContent registers successive materialized descendants without explicit unobserve; whether detached targets stay retained and create material memory or delivery cost is unmeasured. A pinned-engine detached-node/heap trace across repeated virtual windows would settle the outcome. | defer unverified medium |
| BR-BH06 mixed virtual windows | maybe-false | Carried current-review BH03: real tests cover materialized/later-row measurement, but a mixed-height virtual-scroll trace is still needed to establish an unseen-row spacer defect. | carried defer; no new ledger entry |
| BR-BH07 artifact size | false | Carried R-BH07/current-review BH12: the unchanged root byte gate correctly rejects retained oversized packets, and the result explicitly preserves failure rather than claiming a complete packet pass. | carried reject |
| BR-BH08 visual comparisons | false | Carried R-BH09/current-review BH12: all eighteen visual failures and the required owner decisions are recorded as open. Approved baselines cannot be regenerated as a substitute for approval. | carried reject |
| BR-BH09 human acceptance | false | Carried R-BH10/current-review BH10: all manual AT/heard-speech/actual zoom/device gates remain unperformed and block completion. No human observation or approval is inferred. | carried reject |
| BR-BH10 integrated .NET | false | Carried current-review BH12: prior broad .NET failures and later focused limitations are explicitly attributed; no full-suite pass is asserted. Current validation remains separate from those failed prior candidates. | carried reject |
| BR-EC01 fixed pseudo on transformed target | medium | pseudoBox checks ancestors starting at the target's parent. An identity-transformed target itself establishes a fixed containing block, while its clipped pseudo is incorrectly measured against the viewport. The new fixture fails on all three engines before correction. | patch P2 |
| BR-EC02 overlapping sibling glyphs | medium | Transparent-background sibling glyphs bypass the unrelated paint inventory and can erase a warning while nominal contrast certifies 21:1. The new fixture fails on all three engines before correction. | patch P3 |

P1–P4 are direct private implementation corrections under the existing opacity/paint/focus/stale-recovery requirements. No frozen intent change, public API, threshold reduction, baseline regeneration or dependency update is needed. The three helper regressions retain valid positive cases. Component regressions await the actual lifecycle task before asserting stale-completion effects. Prior evidence remains historical; manual/platform/visual/catalog/raw-size/owner/live-backend gates remain open.

### Code review pass 4 — 2026-10-23

All four layers returned before classification: Blind Hunter 16, Edge Case Hunter 17, Verification Gap 1 (pre-verified) and Acceptance Auditor 7. Synthetic Chromium probes against the HEAD helper reproduced R4-E1, R4-E3, R4-E4 and R4-E8. Each row is an individual verdict before grouping.

| Finding | Verdict | Verified evidence | Route |
| --- | --- | --- | --- |
| R4-A1 blocked attempt keeps prior outcome | medium | `9265b09f` moved the null clear after both denials; `PresentBlockedSubmissionAsync` never clears `_hiddenOutcome`; a test pins it | decision |
| R4-A2 fallback title and body | medium | Emitter `:2045`/`:2050` both fall back to `CommandWarningFallback`; the new theory pins both | patch G2 |
| R4-A3 revealed skip link | low | Page cycle runs `expectFocusVisible` on the revealed link; size/contrast are token/padding-bound | reject |
| R4-A4 reduced-motion fixture | medium | No `no-preference` positive control; outline never asserted | patch G3 |
| R4-A5 30s notice provenance | medium | AppHost `:167-178` lacks the variable; `fixtureOptions` comes from config. The 3,000ms sub-claim is false (bUnit coverage) | patch G4 |
| R4-A6 scope-change test | false | The remount assertion proves fresh per-instance `_hiddenOutcome` | reject |
| R4-A7 aria-hidden scroller | low | No such surface; the exclusion's purpose is unreviewed | reject |
| R4-V1 lease on dispose exit | medium | Pre-verified: no settlement probe on the `disposeOnClear` exit | patch G6 |
| R4-B1 multi-project ids | low | Probe: merging occurs only when the working directory is `tests/e2e/specs`; documented runs keep per-project spec ids; fails closed | reject (reclassified) |
| R4-B2 AppHost timings | medium | Same as R4-A5 | patch G4 |
| R4-B3 CLI parse guards | low | Unguarded `JSON.parse` at `:61-78`; fails closed with a poor diagnostic | patch G9 |
| R4-B4 PNG plausibility | false | Byte-bound to the final attempt's own attachment; dimensions out of scope by decision | reject |
| R4-B5 internal Playwright import | low | Recorded decision; exported subpath; fails loudly | reject |
| R4-B6 normal-focus fields | low | In-test assertions plus passed-attempt authority | reject |
| R4-B7 reduced motion | medium | Same as R4-A4 | patch G3 |
| R4-B8 unasserted focus move | low | Works today via route-focus `tabindex=-1`; direct assertion | patch G11 |
| R4-B9 overwritten reasons | low | `:259-260` replace earlier reasons; direct fix | patch G12 |
| R4-B10 per-element stale | low | Fails closed; current records match one element | reject |
| R4-B11 duplicate fallback | medium | Same as R4-A2 | patch G2 |
| R4-B12 row-metric guards | low | Cap omitted deliberately (R-G01); guards snapshot-pinned | reject |
| R4-B13 reflection | low | Established pattern; loud failure | reject |
| R4-B14 merged focus counter | low | `FocusCalls` counts two identifiers; direct fix | patch G13 |
| R4-B15 seam override proof | low | Registration order correct; the e2e lane exercises the seams | reject |
| R4-B16 Canvas probes | false | No html/body `color-scheme`; identical Canvas | reject |
| R4-E1 unrelated text-shadow | low | Probe throws "Text shadow composition needs pixel evidence"; direct reorder | patch G10 |
| R4-E2 unrelated flow pseudo | low | Fails closed; prefilter needs envelope geometry | reject |
| R4-E3 hidden ancestor | medium | Probe omitted a visible 10px button | patch G7 |
| R4-E4 escaped out-of-flow text | medium | Probe skipped a visible 1.2:1 label | patch G8 |
| R4-E5 later elevation layers | low | Real stroke margins exceed 10:1 | reject |
| R4-E6 stacked modals | low | Single modal reservation | reject |
| R4-E7 shared stale selector | low | Same as R4-B10 | reject |
| R4-E8 inline generated glyph | low | Probe false failure; fails closed; no current surface | reject |
| R4-E9 below-fold boundaries | low | Painted-state evidence by design | reject |
| R4-E10 missing text spacing | false | `ready()` applies it | reject |
| R4-E11 multi-stop shadow host | low | Fails closed; no current surface | reject |
| R4-E12 multi-project ids | low | Same as R4-B1 | reject (reclassified) |
| R4-E13 CLI parse guards | low | Same as R4-B3 | patch G9 |
| R4-E14 AppHost timings | medium | Same as R4-A5 | patch G4 |
| R4-E15 reduced motion | medium | Same as R4-A4 | patch G3 |
| R4-E16 hidden warning helper | false | Helper sees only Shell-built warnings | reject |
| R4-E17 same-identity episode | maybe-false | Reachability of a repeated `ResultIdentity` is unproven | carried defer; no new ledger entry |

### Code review pass 5 — 2026-10-23

All four layers returned before classification over the pass 4 fix delta: Blind Hunter 16, Edge Case Hunter 14, Verification Gap 6 (five pre-verified gaps plus one probe-confirmed other finding) and Acceptance Auditor 4. Two layers independently reproduced the hidden-ancestor background and nested-fixed false passes with Chromium probes.

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| R5-V1 blocked disposal gap | medium | Pre-verified: no blocked-path disposal case | patch |
| R5-V2 non-blank title gap | medium | Pre-verified: only blank-title runtime cases | patch |
| R5-V3 hidden ancestor background | medium | Probe 13.08:1 vs white pixels | patch |
| R5-V4 visibility fixtures | medium | Pre-verified: no focus/clipping fixture | patch |
| R5-V5 AppHost runtime binding | medium | Pre-verified; no AppHost harness | carried defer (R-VG02) |
| R5-V6 nested fixed escape | medium | Probe: sample dropped under absolute wrapper | patch |
| R5-A1 hidden ancestor background | medium | Same as R5-V3 (probe 21:1) | patch |
| R5-A2 nested fixed escape | medium | Same as R5-V6 | patch |
| R5-A3 zero capability | low | Spec entry named zero and multiple; only multiple/unreadable reported | patch |
| R5-A4 renderer-level A1 proof | medium | Tests assert callbacks only | patch |
| R5-E1 newer attempt race | low | Narrow pre-existing interleaving | reject |
| R5-E2 Detail equals Title | low | Direct dedupe on the same bar | patch |
| R5-E3 hidden host pseudo | low | Pre-existing exclusion; restructure needed | reject |
| R5-E4 hidden ancestor background | medium | Same as R5-V3 | patch |
| R5-E5 escaped intermediate backgrounds | medium | `background()` walks every ancestor; new exposure from R4-E4 | patch |
| R5-E6 nested fixed escape | medium | Same as R5-V6 | patch |
| R5-E7 containing-block cases | low | Direct predicate additions | patch |
| R5-E8 propagated overflow | low | Conservative extra samples only | reject |
| R5-E9 visibility collapse | low | Existing pattern; no surface | reject |
| R5-E10 null report | low | `report.suites` TypeError; direct guard | patch |
| R5-E11 null state evidence | low | `evidence.engine` TypeError; direct guard | patch |
| R5-E12 null capability | low | Silently absent; direct guard | patch |
| R5-E13 confirm-delay override | low | Explicit developer override | reject |
| R5-E14 clipping traversal | medium | `visit` returns at the hidden element (`:990`) | patch |
| R5-B1 shadow-only overlap | medium | Glyph boxes exclude shadow/stroke extent | patch |
| R5-B2 hidden host pseudo | low | Same as R5-E3 | reject |
| R5-B3 visibility coverage | medium | Same as R5-V4 | patch |
| R5-B4 helper containing blocks | low | Pre-existing, fails closed | reject |
| R5-B5 containing-block cases | low | Same as R5-E7; `contain: paint` clipping pre-existing | patch (direct part) |
| R5-B6 validator guards | low | Same as R5-A3/E10-E12 | patch |
| R5-B7 visible-form blocked attempt | false | Decision 2 is zero-field only | reject |
| R5-B8 newer attempt race | low | Same as R5-E1 | reject |
| R5-B9 blocked-path proofs | medium | Same as R5-V1/R5-A4 | patch |
| R5-B10 lease negative control | low | Direct in-callback assertion | patch |
| R5-B11 merged focus counters | low | Direct per-identifier assertions | patch |
| R5-B12 AppHost pin provenance | medium | Same as R5-V5 | carried defer |
| R5-B13 undersized reason | low | Reachable area is undersized | reject |
| R5-B14 rendered fallback | medium | Same as R5-V2; chain sub-claim carried false | patch |
| R5-B15 outline color | low | Forced colors mask authored color | patch |
| R5-B16 dead parameter | low | Single caller passes `false` | patch |

### Code review pass 6 — 2026-10-23

All four layers returned before classification: Acceptance Auditor 11, Blind Hunter 16, Edge Case Hunter 16, Verification Gap 7 (six mutation-verified gaps plus one probe-confirmed other finding). Duplicates are grouped by root cause in the Pass 6 checklist; every finding keeps the verdict below.

| Findings | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| R6-A2, R6-B15, R6-E16 | medium | Reduced-motion rule paints 35% brand outline (~1.6:1) | decision → patch (supplementary cue) |
| R6-V1–V4, R6-A4, R6-A5, R6-B16 | medium | Mutants pass all 146 synthetic fixtures; only scratch probes fail | patch |
| R6-E12 | low | `\bcontain\b` matches `contain-intrinsic-size` | patch |
| R6-A7, R6-B11, R6-E6 | low | Outside samples start at the parent | patch |
| R6-E8 | low | Zero-area fill can be neither inside nor disjoint | patch |
| R6-A8, R6-B13 | low | Separate `expectTextContrast` walk unchanged | patch |
| R6-A1, R6-B7 | medium | Claimed speech/focus assertions absent | patch |
| R6-A9, R6-B4, R6-E15 | low | No blocked-path precondition (red run currently proves it) | patch |
| R6-B5, R6-E5 | low | Scope branch focuses after a disposing callback | patch |
| R6-E4 | low | Any denial satisfies the control | patch |
| R6-B3, R6-E3, R6-V5 | low | Trim and EntityLabel branches unexercised | patch |
| R6-A3, R6-B6, R6-E14 | low | Merged/one-sided counters remain | patch |
| R6-A10, R6-B8, R6-B9, R6-E13, R6-V6 | low | Probed `suites:[null]`, `results:{}`; `{"suites":{}}` untested | patch |
| R6-B10 | low | Literal in three files | patch |
| R6-A6 | low | Missing valid counterpart | patch |
| R6-B12, R6-E7 | medium | Pre-existing containing-block fill assumption | defer |
| R6-A11 | medium | No candidate digest; result predates changes | defer (Evidence task) |
| R6-B1, R6-E1, R6-E2 | low | Heuristic normalization | reject |
| R6-B2 | low | One-sentence hidden outcome by design | reject |
| R6-B14, R6-E11, R6-V-Other | low | Synthetic only; carried R5-E3 | reject |
| R6-E9 | low | Approximation, no surface | reject |
| R6-E10 | low | Carried R5-E9 | reject |

### Code review pass 7 — 2026-10-23

All four layers returned before classification over the pass 6 fix delta: Blind Hunter 18, Edge Case Hunter 9, Verification Gap 2 (one pre-verified gap plus one probe-confirmed other finding) and Acceptance Auditor 4. Each row is an individual verdict before grouping.

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| R7-A1 sweep premise | medium | Coordinator `:228` dispatches the sweep for any changed lane; `succeeded:false` → `Idle` → no Reconnected notice | decision → patch (supplementary, corrected rationale) |
| R7-A2 result omits sweep disposition | low | No "sweep" in the result | defer (Evidence task) |
| R7-A3 in-flow zero-height fill | medium | Probe: 15.46:1 false pass; pre-existing | defer |
| R7-A4 unconstrained toThrow | low | Same as R7-B14 | patch |
| R7-V1 non-disposed scope focus | medium | Pre-verified: no runtime `focusAttemptedControl` assertion | patch |
| R7-V2 will-change regression | medium | Probe: backdrop-filter/transform-style/offset-path create containing blocks in 3 engines | patch |
| R7-B1 continuation after dispose | false | Disposed render is a renderer no-op; only logging follows | reject |
| R7-B2 post-admission scope exits | low | Immediate return; `finally` releases; unchanged | reject |
| R7-B3 vacuous focus rows | low | Positive-control half same as R7-V1 | patch (with R7-V1) |
| R7-B4 stale scope-row test text | low | Comment/flag say "clear"; restore outside `finally` | patch |
| R7-B5 distinct-Detail row | low | No distinct Detail; fallback absence unasserted | patch |
| R7-B6 case-variant title | low | Carried R6-B1/E1/E2 | reject |
| R7-B7 focus windows | false | Whole-test checks are stricter; overlay restore path unreachable | reject |
| R7-B8 IndexOf −1 | low | Throws instead of a named assertion | patch |
| R7-B9 visibility collapse | low | Carried R5-E9 | reject |
| R7-B10 containing-block fill | medium | Same root as R6-B12/R7-A3 | defer |
| R7-B11 positioned pseudo | low | Pre-existing; no surface; threading needed | reject |
| R7-B12 backdrop-filter | medium | Same as R7-V2 | patch |
| R7-B13 content-visibility hidden | low | Conservative extra sample; no surface | reject |
| R7-B14 unconstrained toThrow | low | Any error satisfies the negative | patch |
| R7-B15 CLI prefix | low | Duplicated prefix; stale message | patch |
| R7-B16 d.mts coverage | false | No TS consumer; loud typecheck failure | reject |
| R7-B17 unpinned status | false | Single source by R6-B10; loud on change | reject |
| R7-B18 sweep comment | medium | Same as R7-A1 | decision → patch |
| R7-E1 scope StateHasChanged | false | Same as R7-B1 | reject |
| R7-E2 focus after import | false | Instance-unique id; disconnected control | reject |
| R7-E3 IndexOf −1 | low | Same as R7-B8 | patch |
| R7-E4 dedupe rows | low | Distinct-Detail part with R7-B5; case part carried | patch / reject |
| R7-E5 visibility collapse | low | Carried | reject |
| R7-E6 hidden target | low | Pre-existing; guard needed | reject |
| R7-E7 positioned pseudo | low | Same as R7-B11 | reject |
| R7-E8 seeded block outside fill | medium | Known R6-B12 | defer |
| R7-E9 will-change/offset-path | medium | Same as R7-V2; bare `offset-path` undemonstrated | patch |

### Code review pass 8 — 2026-10-24

All four layers returned before classification over the pass 7 fix delta: Blind Hunter 12, Edge Case Hunter 11, Verification Gap 2 (no gaps; two other findings) and Acceptance Auditor 5. Each row is an individual verdict before grouping.

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| R8-B1 hidden attempted control | medium | Scope branch focuses `_formDomId-submit` inside `display:none`; other blocked reasons use the trigger | decision → defer (pre-existing 13.3) + patch (assertion wording) |
| R8-B2 computed offset-path | medium | Probe: `ray(0deg)` contains; Chromium/Firefox keep `transform:none` | patch |
| R8-B3 prefixed will-change aliases | low | Probe-real; no surface; needs a per-engine table | reject |
| R8-B4 unobservable backdrop clause | low | Filtered-paint throw precedes containment; deletion still passes | patch |
| R8-B5 unpinned prefixed oracle | low | Prefixed rows accept either result | patch (with R8-A3) |
| R8-B6 `\S+` report path | low | A spaced temp path fails | patch |
| R8-B7 duplicated prefix literal | low | Drift fails loudly | reject |
| R8-B8 return pin / comment | false / low | Runtime scope test fails on fall-through; comment imprecise | reject / patch (with R8-E6) |
| R8-B9 vacuous focus rows | low | No form focus by design; valid negative guard | reject |
| R8-B10 hand-kept candidates | low | No new branch; case part carried | reject |
| R8-B11 sweep wording / contrast | low / decided | Grid-host class; contrast decided by R7-A1 | patch / reject |
| R8-B12 matcher-name throw | false | The ratio is the only possible failure | reject |
| R8-A1 negative-control count | low | 10 fail, not 7 (Firefox omitted) | reject (spec edit; recorded here) |
| R8-A2 unobservable backdrop clause | low | Same as R8-B4 | patch |
| R8-A3 "every engine" comment | low | Chromium/Firefox take the escaped branch | patch |
| R8-A4 `\S+` report path | low | Same as R8-B6 | patch |
| R8-A5 sweep result record | low | Carried R7-A2 | reject (carried) |
| R8-E1 will-change case | low | Probe: `TRANSFORM` kept, contained in 3 engines | patch |
| R8-E2 prefixed aliases | low | Same as R8-B3 | reject |
| R8-E3 computed offset-path | medium | Same as R8-B2 | patch |
| R8-E4 untested backdrop branch | low | Same as R8-B4 | patch |
| R8-E5 invocation race | maybe-false | No pending interop; exact count | reject |
| R8-E6 unguarded method anchors | low | `IndexOf(…, -1)` throws | patch |
| R8-E7 return pin | false | Same as R8-B8 | reject |
| R8-E8 case-variant title | low | Carried | reject |
| R8-E9 `\S+` report path | low | Same as R8-B6 | patch |
| R8-E10 separate-line prefix | false | One `console.error`; inline duplicate fails | reject |
| R8-E11 sweep "rows" | low | Same as R8-B11 wording | patch |
| R8-V1 unobservable backdrop clause | low | Same as R8-B4 | patch |
| R8-V2 hidden attempted control | medium | Same as R8-B1 | decision → defer + patch |

## Verification

### Resumed Build verification — 2026-10-18

Root `1d2cc12eb467db048c41d1aee9c056f1e29e5fc1` plus source/test patch `sha256:3aa1747eb36b3f774c4bf71a81864f877ed9eb4addd77d7614edb600a262d837` binds P1–P4 and all final code/test inputs. The three helper counterexamples fail in all three engines before correction (9 failed), then pass with their valid positives (9 passed). Two original lifecycle regressions both fail on baseline production source. The final banner class passes 39 tests, including four delayed completion/exception cases. Debug Shell.Tests and its Counter.Web project reference build pass with 0 warnings/errors; TypeScript passes.

The complete current browser gate runs 759 cases with no retries: 736 passed / 20 failed / 3 skipped / 0 flaky. All 468 responsive/helper cases pass. The state matrix has 222 passed / 2 failed / 1 skip; the specimen suite has 46 passed / 18 visual failed / 2 skips. Chromium light palette-to-settings Tab focus fails canonically then passes in isolation. Firefox light theme-menu focus contrast fails on competing card border/shadow paint, including an isolated current repeat; an import-relocated HEAD-helper diagnostic passes, leaving attribution unresolved. Neither diagnostic replaces the failed gate. The configured Shell lane runs 3703 tests: 3699 passed / 4 existing snapshot/catalog failures / 0 skipped. The unchanged artifact cap rejects raw results (120809278 bytes) and HTML (102705183 bytes). No baseline/policy/catalog weakening is applied.

[Exact commands, source identities and bounded reports](evidence/story-13-6/2026-10-18-build-resume/verification.json) and the [current accessibility result](13-6-accessibility-result.md) retain all failures separately from passing focused evidence. Raw browser attachments remain local in `/tmp/story-13-6-resume-full/`; copied reports are not a portable complete state packet. All 26 protected PNG/text approvals and eight root submodules match prior recorded bytes/identities. Aspire starts successfully with explicit Test selection, Counter is healthy, and the owned topology is stopped after validation. Security/backend readiness and live EventStore proof remain open. SourceTools is not rerun for this private component/helper change; its prior approval failure remains historical.

Story status stays in-review and sprint stays review. Required manual AT/heard speech, actual zoom/devices, Windows, WebKit actual palette, visual/catalog/raw-size and owner/live-backend gates remain open. No completion, staging, commit or push is asserted.


### Current Build review verification — 2026-10-17

Base `0618c1102d7858c14e3d5f5af13b88b06497ab37`; the current tracked code/test patch is `sha256:65b5464381deda2e0786e3f8aa3c83ebb7b5fa00c0125aad8e8a47091626b51f`. Against the gated direct Debug Test host, `responsive-accessibility.spec.ts` passed 453/453 and `accessibility-state-matrix.spec.ts` passed 224 with one WebKit capability skip, across Chromium, Firefox and WebKit. The added geometry fixtures passed 12/12, forced-colors palette journey 6/6 and reduced-motion CSS fixture 6/6. TypeScript typecheck, serialized AppHost/Shell.Tests/SourceTools.Tests Debug builds and `git diff --check` passed. [Current review verification](evidence/story-13-6/2026-10-17-review-continuation/review-2026-10-17-verification.json) records the commands and report digest; the [result and human checklist](13-6-accessibility-result.md) separates these checks from the prior broad failures. The combined visual and full .NET gates were not rerun on this patch. Manual AT/heard speech, actual zoom and devices, Windows and owner approvals remain open; status stays in-review.

- `npm --prefix tests/e2e run typecheck`; focused Playwright specs against the running Debug host; retain existing accessibility/visual evidence.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug`; run affected classes through its DLL with xUnit v3 `-class`. Run SourceTools.Tests separately and required broader gates.
- `git diff --check`; manual matrix follows the existing log template and remains incomplete until human evidence exists.


### 2026-10-16 gate-reconciliation continuation

Final candidate `sha256:c14be5f5413337c1e5a55b7e49075804761c57015e810113b0f94accb9190732` retains both frozen intent blocks and the preceding forty source inputs. The scheduler test now observes both callback starts under its existing shared deadline. Accepted cleanup offloads only potentially blocking linked CTS disposal; immediate cancellation and all bounds remain intact. The subscription test retains its own close/restart task across rejoin. Final configured Debug Shell:3681 passed/0 failed/0 skipped. Focused scheduler88, EventStore68 and subscription41 all pass.

The rebuilt three-engine run executes381 cases:366 passed/13 failed/2 existing skips. Twelve failures are unchanged Firefox/WebKit image comparisons, causally traced to system-font resolution plus later approved layout changes; one additional Chromium dark Settings return failure remains unattributed. Six focused instrumented repetitions do not replace that canonical failure. All116 state rows per engine are retained, subject to WebKit's116 actual-palette omissions and the additional measurement-review limits. Every existing42 browser-run candidate input matches final bytes; final manifest adds only the subscription-test file.

[Current result](13-6-accessibility-result.md), [verification](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/verification.json), [input reuse](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/input-reuse.json) and [visual diagnosis](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/visual-diagnosis.md) retain exact outcomes. Windows, manual AT/zoom/devices, owner approvals and live-backend acceptance remain open. Status stays in-review; no completion, staging, commit or push is asserted.


### Current continuation — 2026-10-17

The [single current result and human checklist](13-6-accessibility-result.md) now binds source `sha256:d9728b3ffb86041c9e49906a72b374f1c65f478847abc48cf6df6a142ad2677b` and supersedes prior whole-candidate attribution. Measurement/warning review patches and focused checks pass, but complete browser/Shell verification still has failures. Existing restore assets differ from current catalog policy; no clean-restore, manual/device/platform or owner approval is inferred. This story remains in-review and incomplete.

### Resumed Build review — 2026-10-17

Baseline `008736825376e6e2b419cb8d3b81067a8f9cc3b1`. Full story diff is staged only in a system temporary file; nothing is staged in Git. Parent reruns retain the actual counterexamples under `evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/`. Each row is an individual verdict before grouping.

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| C-BH01 native value clipping | medium | A 64px input value inside a 32px control passes because TreeWalker sees no native value text. Parent reproducer confirms. | bad_spec B1 |
| C-BH02 composed clipping | medium | Shadow-root label extends beyond a 20px light-DOM clip but the separate TreeWalkers never connect the text to its clipping ancestor. | bad_spec B1 |
| C-BH03 explicit text clip | medium | inset(0 95% 0 0) text is skipped by the explicit clipPath guard and returns a pass. | bad_spec B1 |
| C-BH04 replaced paint | medium | A decoded black image covers the label; the candidate scan skips its transparent CSS background and certifies 21:1. | bad_spec B2 |
| C-BH05 pseudo overpaint | medium | A positioned opaque ::after covers direct text but supplies no Element candidate; the contrast result is 21:1. | bad_spec B2 |
| C-BH06 clipped target | medium | Only 10x10px of a 100x40px button is reachable, but unclipped bounds certify the 24px floor. | bad_spec B3 |
| C-BH07 missing role | medium | A 5px click-active menuitemcheckbox is omitted while a different passing button makes the inventory nonempty. | bad_spec B3 |
| C-BH08 aria-hidden target | medium | A visible 5px button under aria-hidden remains pointer active but is excluded from the inventory. | bad_spec B3 |
| C-BH09 scaled indicator | medium | scale(3) expands a 2px outline beyond the viewport at left:3px, while the computed ring stays 2px and geometry passes. | bad_spec B4 |
| C-BH10 query navigation | medium | Same-path query navigation leaves the old pathname identity current; after removing old detail, recovery focuses the new query's explanation. | bad_spec B5 |
| C-EC01 composed clipping | medium | Independent 199px shadow label clipped by its 60px host passes; same root cause as C-BH02. | bad_spec B1 |
| C-EC02 focus adjacency | medium | A black outline over unrelated black sibling paint reports white ancestor background and 21:1 because focus sampling ignores ring-area overlaps. | bad_spec B2 |
| C-EC03 duplicate projections | medium (confirmed) | The actual two-banner specimen and unchanged C# group arbitration reproduce the error on all three engines: old JavaScript focuses instance 0 after instance 1 loses detail. The corrected module focuses instance 1 exactly once, with zero DOM speech mutations. See the candidate-bound old/current component comparison below. | patch B5 applied |
| C-VG01 Windows fixture paths | medium | Other finding verified: split('/') retains a Windows drive path and URL.pathname becomes /D:/... when passed to Node. Quality executes these tests on windows-latest. Linux's 12 passing tests cannot certify Windows execution. | patch B6 |

Grouping follows the rows: B1 text clipping, B2 actual contrast adjacency/overpaint, B3 reachable target inventory, B4 transformed focus geometry, B5 navigation/recovery ownership, B6 portable fixture paths. The B1–B5 bad-spec entries trigger one non-frozen implementation correction; the B6 patch survives that loopback.


### Resumed implementation correction — 2026-10-17

B1–B6 are implemented. Frozen intent, production rendering, Fluent ownership, typography/density defaults, contrast thresholds and approved snapshot bytes are unchanged. The overall story stays incomplete pending the complete current verification record and its manual/platform/catalog/visual/owner gates.

| Group | Individual findings | Implementation and regression disposition |
| --- | --- | --- |
| B1 | C-BH01, C-BH02, C-BH03, C-EC01 | Text ranges now follow composed parents and assigned slots across open shadow roots. Explicit inset clips are measured; unknown masks/clips require pixel evidence. Native input glyph metrics detect the demonstrated vertical value clip. Each reproduced failure has a valid positive; nested scrollport and native top-layer behavior remain covered. |
| B2 | C-BH04, C-BH05, C-EC02 | Replaced content and positioned opaque pseudo fills cannot certify ancestor-only text contrast. Exterior outline/shadow/pseudo-border samples use their painted ring/edge regions and reject unrelated overlapping paint pending pixel evidence. SVG transparent viewport gutters are excluded by measuring actual filled/stroked shapes. Existing transparent slot/caret ownership and clipped-paint/SVG/shadow regressions remain covered. Two pre-existing synthetic contrast fixtures receive only separating margins, preserving their original negative assertions without accidental ring overlap. |
| B3 | C-BH06, C-BH07, C-BH08 | Target inventories include pointer-active menu checkbox/radio roles and aria-hidden controls. Minimums, spacing and equivalent evidence use reachable clipped bounds. Unrecoverable clips, including fully clipped essential controls, remain explicit failures. Normal page/scrollport reachability and the unfocused shell skip-link treatment have positive regressions; the initial viewport-clamping run is superseded because it falsely treated those controls as zero-size. |
| B4 | C-BH09 | Nontranslation transforms on focused paint/ancestry fail explicitly for actual indicator-envelope evidence. Ordinary translated controls and the existing pseudo-focus clipping positives remain supported. |
| B5 | C-BH10, C-EC03 | The capture identity includes query parameters. A same-path query regression explicitly verifies that the pathname stays equal. Real co-mounted `FcExpandedRowHiddenBanner` instances reproduce the physical-instance error through the existing C# group owner: the old module focuses instance 0 after instance 1 loses its detail, while the corrected module focuses instance 1 exactly once. Recovery uses the captured grid and existing non-live banner DOM seam; no public C# API is added. |
| B6 | C-VG01 | Fixture filenames use `node:path.basename`; the validator process receives `node:url.fileURLToPath`. All twelve existing retry/CLI/evidence tests pass on Linux. Windows execution is not inferred. |

The [three-engine old/current component comparison](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/b5-component-comparison.json) binds pre-correction module `sha256:6817503d7cfadd3ddfe0c5b5c06002b28abc9735486fb3824fd94cf454871117` and corrected module `sha256:6b47d8da48d808e20ac72bb337b2426306e54afd664447607e23b35208b98b8a`. All three engines reproduce both old-module failures: wrong physical projection and stale same-path query recovery. All three reject stale recovery and select the affected physical instance with the corrected module. Exactly one explanation focus and zero DOM speech mutations are observed; heard speech remains **not observed**.

Focused implementation checks: `npm --prefix tests/e2e run typecheck`; `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs` (12 passed); a three-engine measurement regression selection (237 passed before the subsequent scroll/gutter positive refinements); the corrected B5 browser selection (6 passed); a Chromium representative selection covering both themes' generated projection and Home plus B1–B5 and the two existing contrast negatives (23 passed); and the final bounded three-engine selection of every B1–B5 regression plus those existing contrast negatives (60 passed, including the final scrollport/SVG-gutter positives). The current full candidate and final all-engine rerun outcomes are recorded in the single current result/evidence packet; these focused checks do not replace those gates. Historical packets, manual AT/actual zoom/device gaps, forced-color capability limits, pending owner decisions and catalog/visual failures remain retained.


### Wrapped zero-field feedback correction — 2026-10-17

- [x] R1 — Add `VerticalGap="var(--spacingVerticalS)"` to the zero-field horizontal wrapping FluentStack, update only its generated C# text approval and prove the wrapped feedback clears the trigger's exterior ring. Keep the existing feedback and speech owners, focus retention, beside/below behavior, contrast thresholds and ordinary image snapshots.

The corrected full-browser inspection demonstrated a production overlap: at 320 CSS pixels with text spacing, zero-field feedback began at the trigger's bottom edge and covered its 2px exterior focus ring. The new direct regression reproduces that failure in both themes before rebuilding (`feedback.top=848.078125`, `ring.bottom=850.078125`). The existing Fluent vertical spacing parameter and established `--spacingVerticalS` token correct the layout with one emitter attribute. No ordinary image baseline was changed or regenerated.

After the exact AppHost rebuild, the direct wrapped-layout regression passes in light and dark on Chromium, Firefox and WebKit (6 passed). Every measured case retains trigger focus, reports a 16px feedback gap and 14px clearance beyond the 2px exterior ring, and has no focus-geometry failures. The regression uses the surrounding journeys' real Shift+Tab/Tab setup to establish keyboard focus visibility on all engines; an initial test-only omission of that setup is retained as a superseded run. Heard speech is not observed or inferred.

Only the affected generator methods were run: `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -method '*Renderer_ZeroFields_InlineSnapshot' -method '*RunGenerators_ZeroFieldCommand_EmitsInlineRendererWithoutPage' -parallel none` — 2 passed, including the updated text approval and generated compilation. Browser command: `BASE_URL=https://localhost:7201 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-zero-field-wrap-final FC_E2E_HTML_REPORT_DIR=/tmp/fc-zero-field-wrap-final-report npx --prefix tests/e2e playwright test -c tests/e2e/playwright.config.ts specs/accessibility-state-matrix.spec.ts --grep 'generated zero-field wrapped feedback clears' --workers=2` — 6 passed. Typecheck and `git diff --check` pass.

[Source-bound focused proof](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/zero-field-wrap-proof.json) and the retained `zero-field-wrap-{before,after,final,generator}.log` files distinguish the original red result, superseded keyboard-setup run and final green result. Full candidate verification remains separately authoritative; this focused correction does not close the story's manual, platform, visual, catalog or owner gates.

### Corrected implementation review — 2026-10-17

All three independent layers returned before classification. Parent replays confirm the reported concrete helper/observer failures. The raw repro scripts are retained in the dated evidence directory. The lifecycle focus-only heading exclusion is a separate parent verification correction: its documented resting state is intentionally visually hidden; nine focused three-engine checks passed, including revealed-heading clipping negatives. No general clip exemption was introduced.

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| C2-BH01 shadow metrics | medium | A shadow-root font/style change produces an actual 93px natural row while CSS and the last .NET callback remain 32px; current observer sees only document mutations/container size. | patch G1 |
| C2-BH02 failed metrics callback | medium | CSS becomes 62px, but one rejected callback leaves the cached height acknowledged; re-registration and another document mutation never retry that height. | patch G2 |
| C2-BH03 aria-hidden clipping | medium | Parent replay passes visible clipped aria-hidden text, then rejects identical paint after only the ARIA attribute changes. | patch G3 |
| C2-BH04 generated text clipping | medium | An entire generated label clipped to a 10x8px owner passes because it has no DOM Text node. | patch G4 |
| C2-BH05 native value clipping | medium | Textarea value and selected native option at 32px font inside 10px height pass; textarea scrollHeight is 38px while clientHeight is 10px. | patch G3 |
| C2-BH06 pseudo obstruction | medium | A fixed opaque pseudo box covers the focused target while its 1px host is elsewhere; focusGeometry has no failures. | patch G5 |
| C2-BH07 shadow overpaint | medium | An opaque sibling box shadow covers black text on white, but computed contrast still certifies 21:1. Border/shadow-only focus covers are likewise absent from paint geometry. | patch G5 |
| C2-BH08 dark canvas | medium | The document canvas resolves to rgb(18,18,18), but transparent-background black text is composited against hard-coded white and certified 21:1. | patch G6 |
| C2-BH09 obstructed pointer target | medium | A pointer-active overlay covers 29px of a 30px target; inventory still reports a conforming unobstructed 30x30 area. | patch G7 |
| C2-BH10 undeclared listener target | low | The synthetic bare div listener is omitted. Current changed Home cards use native button/tabindex, palette rows use role=option, and generated/Fluent controls expose semantics or tab stops. General listener discovery needs nonportable browser hooks or a new author-maintained target registry. No current bare-div consumer is demonstrated. | reject: unlikely current helper use; nontrivial discovery complexity |
| C2-EC01 reciprocal observers | medium | One grid settles after two probes; two unchanged grids execute 18 more probes per 300ms with zero .NET callbacks. Each observer sees the other synchronous temporary style probe. | patch G8 |
| C2-EC02 aria-hidden clipping | medium | Independent parent replay confirms the same painted-label omission as C2-BH03. | patch G3 |
| C2-EC03 fallback ownership | medium | In real co-mounted components, a failed import emits one fallback; Complete ignores the unelected caller, and the second owner subsequently focuses the explanation (spoken=1, second focus calls=1). | patch G9 |
| C2-VG01 later-row coverage | medium | Pre-verified gap: both observer and real ItemSize regressions contain one row. Measuring only the first row would pass; the two-row journey never grows a later row or compares its measurement. | patch G10 |

Additional parent verification: the expanded exterior-paint check rejects the real active-tab focus cue as overlapping pseudo paint (both themes). Correct the owned-paint geometry/compositing within G5 and prove the real active-tab journey; do not suppress the overlap guard for unrelated pseudo content.

#### Review patch tasks

- [x] G1/G2/G8 — `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-datagrid.js`, responsive spec: observe relevant open-shadow content changes; separately track acknowledged metrics and retry a demonstrated transient interop failure without disposal/retry loops; prevent cross-grid temporary-probe mutation feedback while retaining real external changes. Prove each original repro, quiescent multiple grids, real later changes and disposal.
- [x] G3/G4 — responsive helper/spec: retain visible aria-hidden text in clipping checks; cover textarea/native select values and generated text or explicitly require additional evidence when generated clipping cannot be measured. Preserve intentional focus-only/screen-reader states and native scrolling positives.
- [x] G5/G6 — responsive helper/spec: include actual generated/border/shadow obstruction paint and competing shadow paint; resolve the actual browser canvas for transparent backgrounds. Preserve real Fluent-owned active-tab paint, transparent SVG gutters, outlines, clips and all retained negatives.
- [x] G7 — responsive helper/spec: do not certify an undersized remaining pointer area beneath a pointer-active overlay. Retain valid partly covered targets when their remaining area meets the minimum; distinguish inactive background/owned shadow content and scroll reachability without broad exceptions.
- [x] G9 — `FcExpandedRowHiddenBanner.razor.cs`, existing banner tests: acquire/check the shared focus claim before the JS-failure fallback completes/announces. Prove failed-import plus healthy co-mounted owner emits one cue, and preserve disposal/handoff semantics.
- [x] G10 — existing real generated compact-row regression: use two rows and grow only the second, then assert the shared measured height, every physical row and generated ItemSize follow that larger extent and restore correctly. Keep the existing default/density/spacing/focus proofs.

These are corrections of demonstrated existing paths, with no new public product API, threshold change, workflow or dependency/image/policy update. Keep all positive B1–B6/R1 and parent focus-only heading behavior.


### C2 implementation corrections — 2026-10-17

G1–G10 are implemented; the complete current matrix and remaining manual/platform/catalog/visual/owner gates retain separate authority.

- **G1/G2/G8:** Row metrics observe relevant composed/shadow content, text/style changes and materialized content resize. Desired height and successful .NET acknowledgment are separate; transient delivery retries the same value, with at most three attempts per measured value or explicit re-registration. Disposal cancels scheduled measurement/retry/recovery work. Every connected grid observer drains only synchronous probe-owned row/cell style mutations while retaining external changes, so multiple unchanged grids become quiescent.
- **G3/G4:** Painted aria-hidden labels stay in clipping proof. Input, textarea and selected native option glyphs must fit vertically while legitimate native scrolling remains allowed. Generated text behind an unresolved clip explicitly requires pixel evidence. The parent's `.fc-lifecycle-heading:not(:focus-within)` treatment and its revealed-heading regression remain intact.
- **G5/G6:** Actual pseudo, border and shadow envelopes contribute obstruction/competing-paint evidence, with unsupported composition reported explicitly. Transparent backgrounds resolve the real browser Canvas. Native planes and stacking order exclude paint that is actually below the focused surface. The owned selected-tab underline is constrained only to its proven selected-control edge where computed-style serialization exceeds DOMRect by 0.000375px. Native popover elevation ownership requires the actual adopted `var(--shadow16)` rule, matching token/signature, exact catalog layer envelope and light/dark alpha pairs; authored near-opaque or enlarged shadows remain failures. No unrelated-overlay exemption or contrast-threshold change is introduced.
- **G7:** Onscreen pointer evidence partitions real hit geometry and uses the largest remaining reachable rectangle. The demonstrated 29px cover leaves a failing 1×30px area; conforming partial coverage, scroll reachability, active modal/owned-shadow content and pointer-transparent overlays retain positive proof. Fully covered background controls beneath an active native popover leave that temporary pointer surface; identified essential controls retain explicit failure evidence. No general listener registry is added; C2-BH10 remains rejected.
- **G9:** Missing-detail import failure must acquire/check the existing shared focus claim before it completes and announces. The real component pair proves one cue in both import orderings; the parent's affected class run passes 34 tests.
- **G10:** Both adaptive and real generated-grid regressions use two rows and grow only the second. The real journey verifies a distinct settled second-row font, maximum physical row height/ItemSize equality, both physical rows, retained focus, 32px Compact and 56px Roomy floors, spacing and restoration. The settled font observation polls the intermediate remount rather than retrying the journey.

[Focused implementation record](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/c2-focused-implementation.json) binds the final changed inputs and distinguishes the checks: initial C2 synthetic selection 39 passed; retained helper/C2 selection 297 passed before final native-ownership tightening; final native-owned/near-opaque negative plus actual active-toolbar/tabs/palette/settings selection 9 passed across both themes and all three engines; final two-row observer/real ItemSize selection 6 passed across all engines. Typecheck and `git diff --check` pass. Retained causal Firefox/WebKit logs identify the underlying border and owned elevation cases. Ordinary image snapshots, dependencies, policy and public product APIs are unchanged. Manual heard speech remains not observed and story completion is not inferred.


### Current matrix follow-up corrections — 2026-10-17

The fixed-source review3 run remains retained with its failures. Its three newly reached paths led to these corrections; the subsequent intermediate75/3 focused result exposed the existing native circular target defect and retains separate source attribution.

- Narrow lifecycle message bars use Fluent's multiline grid areas at 480px and below. Body text retains a flexible column, actions move below, and the real Dismiss target stays inside native paint containment. Direct browser checks prove its 24px geometry and actual click.
- Black blurred text-overpaint bounds aggregate participating layers per text fragment and retain the worst fragment, rather than multiplying shadows on disjoint lines. CSS Backgrounds3 §6.1.2's Gaussian approximation allowance supports a conservative .55 source-alpha exterior cap plus outward 8-bit quantization; each fragment expands by one CSS pixel before exteriority is accepted. Unsupported composition/transforms, sharp/colored shadows and failing contrast remain explicit pixel-proof failures. A disjoint-fragment regression and overlapping-layer/candidate negatives protect the model.
- The theme menu keeps its 48px minimum while taking natural content width, so its label and native caret stay inside the complete changed-focus ring. No owned-SVG exemption or unchanged-glyph focus credit is added; 72/48px navigation rail contracts remain unchanged.
- Both account-avatar principal branches use the existing native AvatarSize.Size40 option. The native circular 32px target could not enclose the required 24px square; no target exception or helper relaxation is used. The direct header test checks all four square corners and intermediate hit points, then opens the real account menu.

[Focused implementation, exact frozen inputs and all diagnostic/raw/report/log roots](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/review5-focused-implementation.json) records 78 passed, 0 failed, 0 skipped across all three engines and both themes with zero retries. Typecheck and diff-check pass. Approved image bytes are untouched. This scoped result does not replace the parent's complete current verification or close manual/platform/catalog/visual/owner gates; story status remains in-review.


### Final parent verification of resumed Build — 2026-10-17

Prior complete-candidate source `sha256:0bfddb4016c3bfe00e84ae88ca698b3a63c373b1ac2be5048577cdae0ee6b77a` binds the earlier 726-case three-engine run with zero retries: 705 passed / 18 failed / 3 skipped / 0 flaky; 0 nonvisual failures. That candidate's configured Debug Shell: 3695 passed / 2 existing catalog-policy failures / 3697. The final focused type/header/fresh-row selection passed 18/18 across all three engines. The complete lane also covers real lifecycle Dismiss, theme glyph/caret containment, the native account 24px square, and retained shadow/unsupported-paint negatives. Parent verification additionally corrected lifecycle narrow-grid containment, natural theme-button width and native 36px avatar hit area; no helper exemption or contrast/target threshold was reduced.

[Prior complete-candidate evidence and exact commands](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/report.md) and [updated result/checklist](13-6-accessibility-result.md) retain that candidate while separating the later review patch and every failed/interrupted attempt. All 24 approved PNG bytes, four frozen intent blocks, original 3c8679628ebdb8995afc09f712a04fcde623695c baseline and eight root submodules are unchanged. Actual WebKit palette, Windows, manual AT/zoom/devices, heard speech, visual/catalog/raw-size and owner/live-backend gates remain open. Status stays in-review, sprint stays review; no completion, staging, commit or push is asserted.


### Review continuation verification — 2026-10-17

The fourteen `R-*` review findings above produced five focused patch groups. The generated row handler now accepts every positive finite measured height acknowledged by JavaScript; the hidden-detail focus module disposes after a handled cancellation failure; target inventory distinguishes active modal dialogs from nonmodal dialogs; generated pseudo text checks its own clipping overflow; and rendered StatusOverview tests prove virtualizer remount at measured heights. R-BH03 and R-BH06 are recorded in `deferred-work.md`.

Parent verification additionally found that Fluent 5.0.1 replaces a described editor's message template with its validation error, removing the description from the DOM. The generated form now renders the stable description beside the editor, while Fluent retains its single validation message. The existing `fc-focus.js` ID lookup links both texts to the focusable control. The generator, bUnit and three-engine generated-form validation checks cover the correction; no threshold, public API or dependency declaration changed.

The current uncommitted code/test patch is based on `12e8b8e1f2a683f9b2c29ca296f2960b47e6602c` and has digest `sha256:391aa1f3684926a4201561809509038657c440eb319570546dac1d5a3b8dd28e` from `git diff --binary HEAD -- src tests | sha256sum`. Shell.Tests and SourceTools.Tests Debug builds both pass with zero warnings/errors after local restores. Focused checks pass: `CommandRendererWrapperIntegrationTests` 70/70; `CommandFormEmitterTests` 68/68; affected hidden-banner 35/35, StatusOverview 9/9 and source virtualization 18/18; responsive synthetic browser fixtures 6/6 across three engines; real destructive generated-form validation 3/3; TypeScript typecheck and `git diff --check`.

Final broad .NET gates were rerun on this patch: Shell.Tests **3709 total / 7 failed / 1 skipped**; SourceTools.Tests **1313 total / 1 failed**. Shell failures include catalog/governance and Fluent snapshot drift; SourceTools' sole packaged-analyzer test expects Fluent 5.0.0 while the declared catalog resolves 5.0.1. From `tests/e2e`, the complete Story 13.6 selection (`npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --workers=4 --retries=0`) ran **732 cases: 711 passed / 18 visual failed / 3 skipped / 0 flaky**, with **zero nonvisual failures**. The earlier 726-case packet remains historical. An exploratory 1203-case root-launched sweep used the wrong working directory for several legacy tests; its 37 failures are not a replacement for the correctly launched story gate. Its isolated WebKit abandonment-focus failure passed on a one-worker rerun from `tests/e2e`.

The [current verification summary](evidence/story-13-6/2026-10-17-review-continuation/verification.json) and [result and human checklist](13-6-accessibility-result.md) distinguish this review patch from historical whole-candidate evidence. Manual AT/heard speech, real zoom/devices, Windows/WebKit forced-palette coverage, visual/catalog/raw-size, live EventStore and owner approvals remain open. Status stays `in-review`, sprint status stays `review`; no completion, staging, commit or push is asserted.

### Retrospective tracking reconciliation — 2026-10-22

The approved [Epic 13 follow-through decision](spec-epic-13-retrospective-follow-through.md#decisions--2026-10-22)
supersedes the 2026-10-21 storage amendment's status preservation only for this reconciliation. Earlier
delivery and verification entries remain historical evidence. Story 13.6's primary status stays
`in-review`; its sprint acceptance row changes from `done` to `review` because mandatory human
observations remain absent. The [reviewed accessibility result](13-6-accessibility-result.md) still
records 752 passes, 18 visual failures and four capability skips. Structural replay does not approve
those failures, the dependency identity, or accessibility completion.

| Alias | Reconciled implementation status | Owning evidence and remaining boundary |
| --- | --- | --- |
| UX-A / 13.2 | `done` | [Navigation/focus verification](spec-13-2-make-shell-navigation-and-route-focus-deterministic.md#verification); historical delivery retained, with the newly repaired disposal boundary verified separately by the follow-through tests. Baseline catalog provenance is RC; executing identity remains unverified. |
| UX-B / 13.3 | `done` | [Command safety verification](spec-13-3-preserve-focus-and-input-through-command-safety-outcomes.md#verification); historical RC/5.0.0 mismatch and failed checks remain disclosed. |
| UX-C / 13.4 | `done` | [Announcement verification](spec-13-4-announce-projection-and-command-state-without-noise.md#verification); historical 5.0.0 focused evidence supplies no current all-engine or human speech approval. |
| UX-D / 13.5 | `done` | [Fresh-row verification](spec-13-5-preserve-fresh-row-meaning-across-visual-and-data-changes.md#verification); baseline-selected 5.0.0 provenance and historical failures/skips retained; executing identity remains unverified. |
| UX-E / 13.6 | `review` | [Reviewed packet/result and MAN-136 checklist](13-6-accessibility-result.md); final 5.0.1 assembly-bound evidence remains blocked, with mandatory human observations still not performed. |
| UX-F / 13.7 | `done` | [Reusable assertion verification](spec-13-7-provide-reusable-ux-assertions-for-adopters.md#verification); 5.0.1 .NET/docs delivery does not revalidate UX-A–E browser behavior. |

These alias corrections describe reviewed implementation delivery. They do not establish one green
Fluent identity. E13R-AI-3 and DW-1987 are satisfied by this evidence-linked reconciliation; OI-16,
FLUENT-APP-1, OI-4, D-13 and G-4 stay open, and Story 13.9's negative owner decision is unchanged.
The [follow-through result and manual handoff](13-6-accessibility-result.md#retrospective-follow-through--2026-10-22)
retain current commands, blockers and the supported historical-runtime replay.


### Resumed Build baseline — 2026-10-23

Resumed from root `4a4051c2511e5c4b7c0285fbc129c72146c1c275` on initially clean `main`; preserve the original frontmatter baseline and frozen intent. Continue the fifteen unchecked Pass 6 patch entries. Epic context was regenerated from current planning artifacts before implementation. Existing manual observations, visual/catalog/platform limitations and named owner decisions remain open. No staging, commit, push, dependency or gitlink change is authorized by this continuation.

Pre-edit runtime command: `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` exited 2. AppHost build failed with three MSB3030 errors for missing `Hexalith.Commons.UniqueIds.dll/.pdb/.xml` beneath EventStore.Contracts output, plus two MSB3026 copy warnings. No healthy runtime baseline was established. Exact-target describe reported no running AppHost; exact-target stop was issued before implementation. Startup log: `/home/administrator/.aspire/logs/cli_20261023T181127_e2fa9c1e.log`.


### Pass 6 implementation and parent verification — 2026-10-23

All fifteen pending Pass 6 patches are implemented. This local candidate is root `4a4051c2511e5c4b7c0285fbc129c72146c1c275` plus unstaged source/test patch `sha256:e78f81ed6d708f39aa123a4860ae07b0e77164247a8e2ed9ba4404520666183f`; the fourteen raw input hashes include the new TypeScript declaration. The [bounded verification receipt](tests/story-13-6-pass6-2026-10-23.json) records exact commands, outcomes, source/assembly/report hashes and external scratch paths. No durable immutable accepted-proof packet is claimed.

| Verification | Current result |
| --- | --- |
| Focused browser regressions | 57/57 across Chromium, Firefox and WebKit |
| Helper negative controls | All twelve deliberate mutations detected; 60 expected failures, zero surviving mutants |
| Scope-disposal / validator controls | Removing the production guard and using the baseline nested-report validator both fail their regressions |
| Affected Shell / generator / Node | 136/136; 103/103; 29/29; TypeScript passes |
| Configured SourceTools lane | 1,415 passed; zero failed/skipped/errors |
| Configured Shell lane | 3,760 passed / 9 failed / 0 skipped; Fluent chrome and analyzer seal pass |
| Complete per-engine browser selection | 941 passed / 12 failed / 4 skipped / 0 flaky; no retries |
| Required state files | Chromium 116, Firefox 116, WebKit 116 of 116 each; validation details in receipt |
| Artifact-size policy | All six roots below 52,428,800 bytes |
| Full artifact validator exits | Chromium 0; Firefox 1; WebKit 1 |

The Shell failures are six MCP 2.3.0 assembly-load cases and three governance checks: the approved AppHost SDK expectation, centralized catalog policy and story-validator workflow wiring. Their source/configuration is unchanged by this patch. The Debug Shell build also records three MSB3277 dependency-conflict warnings; the SourceTools build has zero warnings/errors. Broad failures are retained separately from passing focused checks.

The first combined browser command exceeded its 600-second wrapper budget after 940 passing, ten failing and four skipped log entries, leaving three cases unreported and no final outcome report. Its owned leftover Test host was stopped. The subsequent three complete per-engine runs provide final reports and separate raw/HTML roots; the interrupted attempt remains failed history. The Debug Test host is configured with the existing Test-only timings and Fluent Components/Icons 5.0.1. This supplies no live-backend acceptance.

All twenty-four approved PNGs, frozen intent, dependency configuration, root gitlinks and historical packets remain unchanged. Only two generator text approvals changed, each by the disposed guard. All eighteen manual boxes remain open. Actual AT/heard speech, browser zoom, physical devices, Windows, WebKit system-palette coverage and named owner approvals are unperformed or unresolved. Story and sprint status remain **in-progress**; Build does not claim acceptance or completion. Nothing is staged, committed or pushed. Earlier dated sections remain historical evidence.

Final repository checks: `python3 eng/validate-story-artifacts.py` and `git diff --check` pass. Recorded log hashes, source and frozen intent match; all eighteen manual boxes remain unchecked. Exact-target Aspire describe reports no running AppHost, and the owned Test-host port is closed.


### Committed-source reconciliation — 2026-10-23

Final observed root `637ecb8ba7cf9e21831ee3f3bb32eb2cceaf9990` retains the frontend source from initial root `6f6eb8982ca1e97e28586de1d4ffc540467058af`; the original frontmatter baseline and frozen intent are preserved. The [new reconciliation receipt](tests/story-13-6-committed-reconciliation-2026-10-23.json) verifies the prior Pass 6 receipt's fourteen source inputs, twenty-four PNGs, five assemblies, twenty-seven focused artifacts, eleven broad logs and three browser reports. The [current result](13-6-accessibility-result.md#current-committed-source-reconciliation--2026-10-23) separates these reused local outcomes from newly executed Node evidence (29/29), TypeScript and repository checks. No broad rerun, current-runtime acceptance or durable immutable accepted-proof retrieval is inferred.

External concurrent checkout drift first affected EventStore and Memories. An independent `/pushall` then committed updated EventStore, Memories and Parties gitlinks at the final root; all final checked-out revisions match those tracked gitlinks. The receipt records each before/after identity. No dependency or gitlink mutation was made by this task, and historical browser/.NET results do not cover that changed whole-runtime checkout.

The fifteen Pass 6 patches remain implemented. The original matrix/result tasks and resumed Evidence task stay unchecked because mandatory human observations, visual/catalog/platform/owner gates and accepted-proof retrieval remain incomplete. All eighteen manual boxes are still open and no human logs were supplied. Story/sprint remain `in-progress`; Build stops at Tasks & Acceptance. Nothing is staged, committed or pushed by this continuation.

### Code review pass 7 patches — 2026-10-24

All eight pass 7 patches are applied; decision R7-A1 was resolved as "keep supplementary on a corrected rationale". The `/pushall` auto-commit `482206ca` captured the first application, including a CA1861 build break in `CommandRendererWrapperIntegrationTests` (constant array argument); the follow-up moves that array to a static field. A three-engine probe showed that WebKit reports `-webkit-backdrop-filter` through `getPropertyValue('backdrop-filter')`, so the existing filter guard already fails closed for both spellings; the new R7 rows pin that, and only the prefixed `will-change` hint is engine-gated (`CSS.supports`).

- `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs`: 29 passed.
- `npm --prefix tests/e2e run typecheck`: clean.
- Three-engine fixture selection (`PLAYWRIGHT_SKIP_WEBSERVER=1`, `--grep` over the R5/R6/R7 containing-block, escaped-background, exterior and simple-contrast fixtures): 66 passed. Negative control: with the pre-patch `will-change` hint list, 7 rows fail (Chromium: `backdrop-filter`, `transform-style`, `offset-path`; WebKit: those three plus the prefixed hint).
- Shell `ZeroFieldFailureAccessibilityTests` and `CommandRendererWrapperIntegrationTests` (Release, direct xUnit runner): 98 passed.
- SourceTools `CommandFormEmitterTests` (Release): 68 passed.

Not run: the full Shell and SourceTools suites, the full browser gate and the Counter-host specs. The new `focusAttemptedControl` positive control was not mutation-tested, because a temporary generator edit could be auto-committed; the assertion fails whenever the call is skipped. The result file, the Evidence task, the manual boxes and the owner gates remain open, so the status stays `in-progress`.
