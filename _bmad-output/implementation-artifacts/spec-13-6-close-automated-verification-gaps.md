---
title: 'Story 13.6: Close automated verification gaps'
type: 'bugfix'
created: '2026-10-16'
status: 'in-review'
route: 'dispatch'
story_id: '13.6'
review_loop_iteration: 0
baseline_commit: '0129bd39095892283371f97836ade5768fef3809'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

> **Evidence policy (2026-10-23):** proof is the repository's tests plus CI, recorded here as commands, pass/fail counts, commit SHA and CI run link (PRD §7). The committed `evidence/story-13-6/` folders this record links to were removed by [`spec-evidence-storage-simplification.md`](spec-evidence-storage-simplification.md); `evidence/` folders are ignored local scratch and `eng/check-committed-files.sh` rejects new evidence commits.

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.6 has fifteen measurement gaps, a Settings focus-return failure, missing regressions and unreliable retry artifact validation. Passing inventories cannot establish acceptance.

**Approach:** Repair measurements/journeys, prove feedback/scope behavior, validate each engine without pooling attempts, and refresh candidate-bound evidence. Preserve approved story intent.

## Boundaries & Constraints

**Always:** Preserve Fluent V5 ownership, typography/density, 32px default rows, 72/48px rails, 75rem measure, tenant isolation and single-owner speech. Keep existing user/submodule edits. Classify unsupported paint as uncovered; retain every failed run and exact attribution. Local builds use Debug; hosted builds retain Release/NuGet.

**Never:** Weaken axe/contrast/geometry thresholds, auto-update image baselines, upgrade dependencies, add public APIs/workflows, or infer human observations/approvals. Manual AT, actual 400% zoom, physical devices, Windows execution, WebKit actual forced-palette coverage and separate owner/live-backend acceptance stay open until evidenced.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Failure handling |
|---|---|---|---|
| Paint | Fractional pseudo/placeholder opacity, effective glyph fill, SVG shapes/use/zero strokes, generated text, sibling backgrounds, filters | Actual meaningful paint determines contrast; unsupported composition cannot pass | Registered negatives and valid positives |
| Focus | Invisible ancestry, descendant obstruction, collapsed/permanent shadows, inset pseudo border | Visible changed indicator and correct adjacent paint; no false geometry/contrast pass | Retain R01–R15 dispositions |
| Keyboard/modal | Boundary refocus trap; toolbar→palette→Settings; held actual Fluent return callback | Real keyboard progression, initial origin return and later operator movement preserved | Fix demonstrated race; retain failures |
| Reflow/targets | 320px with/without spacing; nested scrollports; stale/Essential exceptions | Complete text; only labelled grid/tab horizontal scrolling; justified applicable exceptions | No vacuous inventories |
| Feedback/scope | Blank ordinary warning; French retry hint; scope changes during feedback/recovery | Nonblank localized safe feedback; no stale focus, feedback or speech | Reuse shell scope remount/cancellation |
| Evidence | Failed first attempt, successful retry, partial/mixed engines or attempts | One complete authoritative attempt per test, engine and run; no fragment pooling | Reject missing/ambiguous evidence |

</frozen-after-approval>

## Code Map

- Review source: `evidence/story-13-6/2026-10-16-completion/gate-reconciliation/review/triage.md`, relative to this spec. All R01–R15 remain open at baseline.
- Existing palette callback fixture holds the actual Fluent 25ms timer. Settings suppression already exists; its presence does not resolve the retained failure at state-matrix line 381.
- Linux CI runs three engines; Windows runs Chromium. Extend those jobs. `FrontComposerShell` keys descendants by scope generation; prove clearing before adding production behavior.
- No unresolved intent choices or irreversible operations. Footprint: existing helpers, fixtures, generator/tests, CI and evidence; no new public surface.

## Tasks & Acceptance

**Execution:**

- [x] `tests/e2e/helpers/responsive-accessibility.ts` — repair R02–R14 paint/focus measurements; validate scroll regions and applicable target exceptions; reject unresolved paint rather than invent ratios.
- [x] `tests/e2e/specs/responsive-accessibility.spec.ts` — register R01–R15 negative/positive regressions, harden `verifyKeyboardCycle`, check ordinary reflow clipping, and extend actual delayed-return proof to Settings.
- [x] `tests/e2e/specs/accessibility-state-matrix.spec.ts` and `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js` — reproduce the complete dark Settings journey, repair demonstrated origin/timing failure, and cover held recovery across scope changes without weakening assertions.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, its `CommandFormEmitterTests.cs` and affected `.verified.txt` files under `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/` — replace blank ordinary warning text with existing safe localized fallback; preserve retry guidance.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs`, `CommandRendererWrapperIntegrationTests.cs` alongside it, and `Components/DataGrid/FcExpandedRowHiddenBannerTests.cs` in that project — prove French retry copy, ordinary warning fallback, shell tenant/user remount clearing and cancellation of held stale recovery/speech.
- [x] `tests/e2e/scripts/a11y-state-evidence{,.test}.mjs`, `validate-a11y-artifacts.mjs`, `tests/e2e/specs/accessibility-state-matrix.spec.ts`, `tests/e2e/playwright.config.ts`, `.github/workflows/quality.yml` — mirror target checks; report and bind attempts to authoritative Playwright outcomes; test retry/partial/mixed cases; validate each Linux engine/run, preserving Windows checks and artifact bounds.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, new dated evidence under its existing `evidence/story-13-6/`, and `deferred-work.md` alongside this spec — record full source identity/raw hashes, current commands/results, per-finding dispositions and remaining gates; preserve prior evidence and append ledger resolutions.

**Acceptance Criteria:**

- Given each matrix case, when regressions run, then false passes are rejected and valid controls still pass without weaker thresholds.
- Given the rebuilt candidate, when the existing three-engine lane and configured Shell lane run, then results and retained artifacts match that candidate; visual/platform/manual blockers remain explicit and cannot count as story completion.

## Implementation Notes

- Implementation discovery (2026-10-16): the new scroll inventory reproduced 96px icon flyout buttons overflowing the 48px navigation rail. `FrontComposerNavigation.razor` now uses Fluent `IconOnly` for the icon-only more-options trigger, preserving both 72px/48px widths. A browser regression checks both widths and media palettes.
- Implementation discovery (2026-10-16): inherited Test environment variables do not override the Counter launch profile. The existing AppHost Counter fixture now passes Test and existing specimen/timing flags only for explicit local Test run mode. Development and publish retain their original behavior; the branch and Test-only fixed scope controls have gating assertions. Include `src/Hexalith.FrontComposer.AppHost/Program.cs`, `AccessibilitySpecimenSeams.cs`, `AccessibilitySpecimenUserContextAccessor.cs` and `AccessibilityScenarioControls.razor` in candidate hashes.

- Implementation discovery (2026-10-16): `samples/Counter/Counter.Web/Program.cs` registers gated scope seams after the demo user accessor so fixed Test scope controls reach the same shell boundary. `PendingCommandPollingCoordinatorTests.cs` required a demonstrated cancellation-callback synchronization repair: await its existing callback-finished signal before provider completion can unregister the callback. The five-second budget and production behavior remain unchanged; constrained before/after and configured-lane evidence are retained.

- Parent verification discovery (2026-10-16): clipped paint inventories require explicit visible-surface inspection for remote missing-item, projection and fresh-row cues. Those calls now record normal/forced-color focus before each root scroll and reassert activeElement identity afterward. The genuine recovered notice otherwise expires during the dual-palette capture; the existing `Hexalith:Shell:ProjectionReconnectedNoticeDurationMs` option is set to 30000 only for the Test host/Playwright/CI fixture. Its production default stays 3000, and no locator timeout or notice re-trigger was added.

The user authorized proceeding with this spec on 2026-10-16 while preserving the pre-existing EventStore and Tenants submodule edits. The untracked spec is the selected implementation input.

## Spec Change Log

## Review Triage Log

All three review layers completed before classification. Every finding has an individual verdict below; grouping followed classification. No finding is discarded because a file was absent from the initial Code Map.

| Finding | Verdict | Verified evidence | Route |
|---|---|---|---|
| BH01 | medium | The newly supported axis-aligned SVG stroke has a zero geometric dimension and sample() drops its painted envelope; the existing diagonal-only R03 fixture misses it. | patch G01 |
| BH02 | medium | Pseudo filters are separate computed styles; sample() inspects the originating element/ancestors and therefore certifies filter:opacity(0) pseudo paint. | patch G02 |
| BH03 | medium | The new nonancestor guard excludes relative/sticky/static overlapping backgrounds; ancestor white can replace actual black under glyphs. | patch G03 |
| BH04 | medium | outerShadows and contrast admit a negative-spread shadow fully concealed by an opaque control; the changed-style layer draws no exposed focus pixels. | patch G04 |
| BH05 | medium | A permanent opaque pseudo background is omitted from adjacency/resting comparison, so a same-color focus border with unchanged pixels passes. | patch G05 |
| BH06 | medium | pseudoOutside omits margins while pseudoBox includes them, producing contradictory geometry/contrast adjacency for a margin-shifted border. | patch G06 |
| BH07 | medium | related() exempts every static descendant before inspecting transforms, allowing an enlarged opaque descendant to cover the ring. | patch G07 |
| BH08 | low | Stylesheet sizing can invalidate the nativeUnmodified flag. No current caller claims User Agent Control (real inventories use only Spacing); CSS-origin provenance would add machinery for an unused path. | reject: unlikely current use; nontrivial provenance logic |
| BH09 | low | The pre-existing Equivalent branch accepts a decorative same-size element. No current inventory claims Equivalent; proving equivalent behavior requires additional semantics and activation machinery. | reject: unused path; complex proof beyond a direct correction |
| BH10 | medium | CLI owned.find selects a retained failed WebKit capability before the authoritative successful retry; the helper correctly rejects that stale choice. | patch G08 |
| BH11 | medium | The new authoritative report reader ignores report.errors, so successful individual tests can hide runner/global teardown failure. | patch G09 |
| BH12 | medium | The unchanged snapshot guard checks name, location and nonzero size only; malformed PNG bytes can satisfy it. This pre-existing image-integrity gap is independent of the new test/retry binding. | defer: image integrity and snapshot-byte authority |
| EC01 | low | Same stylesheet-authored native-control sizing defect as BH08; no actual User Agent Control claim exists, and provenance validation adds complexity. | reject: unlikely current use; nontrivial provenance logic |
| EC02 | medium | The static-descendant early return ignores transformed opaque bounds and bypasses obstruction checks. | patch G07 |
| EC03 | medium | Static sibling paint overlapped by negatively margined text is omitted by the absolute/fixed-only background guard. | patch G03 |
| EC04 | medium | Positioned generated glyphs use the host background even when they extend onto a different backdrop. | patch G02 |
| EC05 | medium | Generated pseudo filters/blending are not checked before foreground compositing, allowing invisible glyphs to pass. | patch G02 |
| EC06 | medium | New generated-text sampling hardcodes 4.5, incorrectly rejecting conforming 32px text between 3 and 4.5. | patch G10 |
| EC07 | medium | The permanent pseudo background masks a transparent-to-identical focus border; neither geometry nor contrast accounts for that paint. | patch G05 |
| EC08 | medium | The CLI chooses the first same-basename capability file rather than the authoritative final result attachment. | patch G08 |
| EC09 | medium | restoreOverlayOrigin newly requires HTMLElement, discarding a recorded operator move to a focusable SVG Element and stealing its focus. | patch G11 |
| EC10 | medium | The claim of rejecting unresolved generated paint is contradicted by the demonstrated position/filter cases in EC04/EC05. | patch G02 |
| EC11 | medium | The valid-control claim is contradicted by the demonstrated hardcoded generated-text threshold in EC06. | patch G10 |
| VG01 | medium | Pre-verified gap: the runtime warning test searches the whole markup for one fallback shared by title and body, so either part masks a regression in the other. | patch G12 |
| VG02 | medium | Other finding verified against sample() area guard: horizontal/vertical strokes disappear while a passing rectangle remains; same root as BH01. | patch G01 |
| VG03 | medium | Other finding verified against CLI first-match selection: retained failed capability attempts break successful-retry evidence; same root as BH10. | patch G08 |
| P01 | medium | git check-ignore confirms 61 inventoried raw .log files are omitted from the reviewable Git artifact, leaving dated report links and hash inventory incomplete after ordinary staging/checkout. | patch G13 |
| P02 | medium | Parent direct Chromium helper reproduction returns 21:1 and no geometry failures for black focus shadow beneath a permanent .95-alpha white shadow; the lower layer is measured in isolation. | patch G14 |
| P03 | medium | Parent full-lane reproduction rejects Fluent required-label after glyphs solely for their ordinary positive 4px inline margin; the supported normal-flow geometry was rejected too broadly. | patch G15 |
| P04 | medium | Parent full-lane warning text ranges extend below a scrollport and are compared with the visible footer; overflow clipping must bound actual sampled paint before sibling overlap checks. | patch G16 |
| P05 | medium | Final-candidate parent run reproduces empty painted inventories for generated-missing-external, projection connection/status and fresh-row presentation in both Chromium themes: controls can remain focused in view while their measured status/cue is outside the scrollport. Each demonstrated test must inspect visible paint without hiding the retained-focus proof. | patch G17 |
| P06 | medium | Full three-engine browser assertions pass, but the artifact validator rejects four required state inventories per engine: rejected lifecycle icons and policy-denied control boundaries are outside the sampled scrollport in both themes. Their capture calls must explicitly inspect visible required paint and assert it is nonempty. | patch G18 |
| P07 | medium | Fourth complete parent run captures the old selected palette row 15 ms after query input, before the 150 ms debounced result commit; the subsequent detached-row cue sample is empty although the replacement CounterProjection row has a visible outline. Await the exact filtered result set and its selected state before settling paint and measuring the unchanged strict cue. | patch G19 |

### Resumed Build review — 2026-10-17

All three independent layers returned before classification. Eleven new Chromium regressions reproduce every new measurement failure against the unchanged helper; [red results](evidence/story-13-6/2026-10-16-gap-closure/resume-2026-10-17/red/playwright.json) retain the failed assertions. Current Test AppHost startup and served focus-script identity were separately inspected. Source inputs and approved images match the previous packet; EventStore/Tenants heads have advanced, so its whole-candidate identity is historical.

| Finding | Verdict | Verified evidence | Route |
|---|---|---|---|
| R-BH01 | medium | Different offsets bypass opaque shadow coverage despite a fully enclosing white layer; the offset-shadow regression accepts an invisible new black layer before patching. | patch H01 |
| R-BH02 | medium | Static descendants return related before their bounds are checked; the negative-margin regression covers the target and ring while geometry passes. | patch H02 |
| R-BH03 | medium | Pseudo contrast samples ignore their own clip; the inset(50%) border/cue regression returns a nonempty passing inventory. | patch H03 |
| R-BH04 | medium | Generated text uses the host bounds despite horizontal overflow; a nowrap generated label receives its white host background beyond that host. | patch H04 |
| R-BH05 | medium | The overlap scan exempts descendants; the positioned opaque child covering direct paragraph text remains absent from its reported background. | patch H05 |
| R-BH06 | medium | The overlap scan visits only one root; both document-over-shadow and shadow-over-slotted regressions return passing black-on-white ratios. | patch H06 |
| R-BH07 | medium | Effective fill ignores text stroke; the white 10px stroke hiding black glyphs still receives a passing ratio. | patch H07 |
| R-BH08 | medium | A diagonal two-point path has a nonzero box and no fill area but supplies a passing icon sample. | patch H08 |
| R-BH09 | medium | A zero-length dash followed by a gap longer than the path supplies a passing stroke sample without visible paint. | patch H09 |
| R-BH10 | medium | pseudoOutside skips the static transformed containing block; an inset black border on a black button is compared with the white page. | patch H10 |
| R-EC01 | low | carried: same nativeUnmodified stylesheet-sizing claim as prior BH08/EC01; no current inventory uses User Agent Control, and provenance checks add complexity. | reject: existing disposition |
| R-EC02 | medium | Independently reproduced same cross-root overlap omission as R-BH06, including slotted text. | patch H06 |
| R-EC03 | medium | Independently reproduced same static transformed containing-block omission as R-BH10. | patch H10 |
| R-EC04 | medium | The unresolved-paint claim is contradicted by the two verified paths in R-EC02/R-EC03; both return passing inventories. | patch H06/H10 |
| R-EC05 | low | carried: claim repeats the unchanged native-control exception concern in R-EC01 and prior BH08/EC01; no real inventory claims it. | reject: existing disposition |
| R-VG01 | medium | Pre-verified gap: ordinary warning runtime tests use only blank Title/Detail/EntityLabel; they cannot detect null-title or available entity-label regressions. | patch H11 |
| R-VG02 | medium | Pre-verified gap: AppHost fixture settings have source-text assertions while the usual browser lane starts Counter directly. Executed Test startup does not provide a reusable Development/publish configuration harness. | defer: automated effective AppHost configuration coverage |

All patch groups correct demonstrated existing helper/test paths, introduce no public surface and preserve the frozen intent. H01–H11 do not change production styles or approved screenshots. The single deferred runtime-harness gap is appended to the ledger; carried low findings are neither patched nor deferred again.

## Verification

Implementation execution evidence: [dated report](evidence/story-13-6/2026-10-16-gap-closure/report.md). Automated implementation tasks and parent Build review patches are complete; retained visual/platform/manual gates remain open. See the [final parent verification](evidence/story-13-6/2026-10-16-gap-closure/root-review/verified/report.md).

- Run `npm --prefix tests/e2e run typecheck` and `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs`.
- Start/inspect the AppHost through Aspire before code changes. Use its Debug Test specimen endpoint with isolated output/report paths and `PLAYWRIGHT_SKIP_WEBSERVER=1`.
- Run focused browser negatives and actual modal regressions, then `npm --prefix tests/e2e test -- specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=chromium --project=firefox --project=webkit --workers=3`; validate each engine/run and visual governance without regenerating images.
- Build affected Shell/SourceTools test projects in Debug; invoke focused xUnit classes directly, then the configured Shell lane with its five existing exclusions. Retain exact blockers and focused fallback evidence.
- Check `git diff --check`, candidate hashes, preserved intent/image hashes; perform Build review. Human gates remain unperformed.

### Parent review patch verification (2026-10-16)

All 19 patch groups G01–G19 are applied. Final candidate `8a2058c7769a7761d377906fb32f0990c962bd85e2bfa20125e3e6573d5cdd69`: browser **529 passed, 12 failed, 2 skipped / 543**, with all nonvisual cases passing on their final attempt; configured Debug Shell **3693/3693**, Node authority regressions **12/12**, SourceTools emitter **68/68** and CI governance **10/10** pass. Input reuse is hash-verified. The interrupted first parent attempt and all failed diagnostics remain retained. The one pre-existing PNG-integrity concern is appended to the deferred-work ledger; current PNG files in the dated views were independently decoded. Frozen intent and all 24 image baselines are unchanged.

Status remains `in-review` and sprint status remains `in-progress`: the twelve preserved visual failures and manual/platform/owner acceptance cannot count as story completion. The Build verification rule halts advancement when a failed check cannot be resolved within approved scope. No image regeneration or human approval is inferred.


### Resumed review verification — 2026-10-17

H01–H11 are applied. Real-surface follow-up fixes keep clipped grid content and transparent Fluent labels/carets owned by their controls and resolve WebKit slots to actual projected elements. The opaque flow/slotted-child negative regressions still fail safely. Thirteen new cases per engine execute; current targeted proof is 46/46 passed. All three frozen intent blocks and all 24 image baselines are unchanged.

Final source `sha256:d9728b3ffb86041c9e49906a72b374f1c65f478847abc48cf6df6a142ad2677b`: complete browser **567 passed, 12 failed, 1 flaky, 2 skipped / 582**; configured direct Debug Shell **3692 passed / 3 failed / 3695**; renderer 70/70, emitter 68/68 and Node 12/12 pass. Existing no-restore Fluent 5.0.0 assets are sealed; catalog requests 5.0.1 and a different Localization version, so clean-restore compatibility is unproven. Isolated cancellation and instrumented Settings passes do not erase complete-run failures. [Current verification](evidence/story-13-6/2026-10-16-gap-closure/resume-2026-10-17/report.md) retains every browser attempt and exact attribution.

Verification has unresolved failures outside this continuation's permitted dependency/image changes. The Build verification rule halts advancement. Status stays in-review, sprint stays in-progress, and all manual/platform/owner gates remain open. No commit, push, image update or dependency upgrade occurred.
