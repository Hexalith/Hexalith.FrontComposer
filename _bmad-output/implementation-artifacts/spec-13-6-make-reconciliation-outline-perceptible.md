---
title: 'Story 13.6: Make the Reconciliation Outline Perceptible'
type: 'bugfix'
created: '2026-11-03'
status: 'done'
route: 'oneshot'
artifact_role: 'supporting-implementation-record'
primary_story: '_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md'
baseline_commit: 'f842711d67004200e3248b3c42b7d9bcb2146907'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The reconciliation outline conveys that a grid was refreshed, including partial reconnects without the Reconnected notice. Its translucent stroke is too faint to establish the required 3:1 non-text contrast.

**Approach:** The user selected the recommended informative-outline option with “apply recommended”. Use an opaque accessible Fluent stroke, measure at least 3:1 in the existing light/dark themes and supported forced colors, preserve the 700ms lifetime, static reduced-motion rendering and stronger keyboard focus. Reuse existing CSS and browser checks without new components, settings, timers or announcements. Preserve historical evidence, frozen primary criteria and incomplete human/independent gates. This bounded fix does not complete the primary story.

</frozen-after-approval>

## Implementation Notes

Route assessment: no intent gaps, no irreversible operations, no new API. Scope: the reconciliation rules in `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css`, focused assertions in `tests/e2e/specs/responsive-accessibility.spec.ts`, this supporting record and append-only primary/result evidence. Prior working-tree changes are the assistant's evidence additions from this same authorized session and must be preserved. No staging, branch, commit, push or dependency update is requested by this application step.

Reuse `--colorNeutralStrokeAccessible`, present in the selected Fluent package, subject to computed contrast proof. Remove translucent outline overrides rather than adding an overriding theme. Preserve the existing animation, offset and marker scheduling; keep focused grid's 2px outline. Use the existing contrast helper to measure the actual marked grid, distinguish unsupported WebKit system-palette paint from media emulation, and demonstrate failure on the old stroke. Run the narrow affected cases on Chromium/Firefox/WebKit, TypeScript and story/whitespace validators. Evidence stays in this record and the owning result; raw reports stay local. Supporting completion must not advance primary sprint status.

Aspire baseline: the exact existing AppHost started without rebuilding; `aspire wait counter-web --timeout 45` reports Healthy. `aspire describe` is saved in external scratch. This is Counter readiness only, not full-topology acceptance. Stop that owned AppHost before editing/building the Test host.

Implementation: replaced the translucent outline and its color-mix/reduced-motion overrides with `--colorNeutralStrokeAccessible`, preserving its 1px inset and existing 700ms animation/state ownership. Supported forced colors use CanvasText; the existing 2px keyboard outline retains precedence. The background-flash fallback is unchanged. Added a real-grid regression in the existing responsive spec covering light/dark, four forced-color/motion combinations, both flash endpoints, an opaque stroke and keyboard-focus priority. The older fixture still independently checks motion.

Red evidence: initial diagnostics measured no outline because the real grid was outside the viewport. After adding `scrollIntoViewIfNeeded`, the unchanged product fails both Chromium themes on actual contrast: light 1.7875:1 and dark 1.4269:1 at the flash start. Reports `red/`, `red-diagnostic/` and `red-measured/` remain separate under `/tmp/fc-13-6-outline-4x7rv22s`; only the last proves the contrast failure. No failing expectation was weakened.

Verification: `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q` passes with zero warnings/errors. From `tests/e2e`, `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-13-6-outline-4x7rv22s/green FC_E2E_HTML_REPORT_DIR=/tmp/fc-13-6-outline-4x7rv22s/green-html npx --no -- playwright test specs/responsive-accessibility.spec.ts --grep 'reconciliation outline retains|type states retain contrast' --workers=2 --retries=0` passes **12/12**, four per engine, zero failures/skips/flaky cases/retries. `npm --prefix tests/e2e run typecheck` exits 0. The Test host uses the prior documented environment/timing settings, was rebuilt/restarted for the CSS change and stopped after verification; port 5070 is closed.

The report contains 36 outline measurements: authored-theme minima light **4.5437:1**, dark **7.0463:1**; supported system-palette minimum **21:1**. WebKit still exposes media emulation without actual system paint and retains authored-contrast measurements; no WebKit system-paint pass is claimed. The changed cue invokes the existing capability revalidation trigger: these new checks remeasure the changed cue and confirm the local replacement limitation, without broadening its earlier owner disposition or supplying platform/human evidence. Report SHA-256 `ad7c9377ac3b6c7dc9ae0ab8ba37652f1be3c6cacf23e522826029e1ab87d2d1`; source/test patch SHA-256 `b1a668f960caef5419440715a525cf8db424913318c93050138026f90cb37099` on base `f842711d67004200e3248b3c42b7d9bcb2146907`. Raw parsed measurements are `outline-measurements.json` in the same scratch directory.

The user’s “apply recommended” resolves the outline's functional classification in favor of an informative cue. It supplies no independent release approval. Historical reports/frozen primary criteria and Story/sprint in-progress status are preserved. Existing hosted successes qualify the earlier committed source, not this local CSS/test patch; no new hosted run or complete Story 13.6 browser/visual gate is claimed.

Final verification after review: the same browser command with `FC_E2E_OUTPUT_DIR=/tmp/fc-13-6-outline-4x7rv22s/final` and `FC_E2E_HTML_REPORT_DIR=/tmp/fc-13-6-outline-4x7rv22s/final-html` passes **12/12**, zero failures/skips/flaky outcomes/retries. It now binds system paint to the grid's effective `forced-color-adjust:auto` and CanvasText outline, asserts one animation iteration/zero delay/no fill, and records 24 focused-outline contrast measurements in addition to 36 informative-outline measurements. Source/test patch SHA-256 `0536a29c4130ae950f3468ceaed596a0f614bb67910d011f7bff0573ee1d0737`; final browser report SHA-256 `d73099661deabf701e27cc9322905ee151dc213f8228d145ba616081c7e6b150`. The initial green run remains bound to its original test revision.

The first review-strengthened run (`green-reviewed/`) was **six passed / six failed**: the additional `expectFocusVisible(grid)` call conservatively rejects internal opaque sticky TH borders as competing paint, even when those borders lie away from the inset focus ring. A separate `node /tmp/fc-13-6-outline-4x7rv22s/geometry-diagnostic.mjs` intercepts all projection stylesheet requests with the pre-change CSS and reproduces the exact same failure on old/new source. Both screenshots are byte-identical, SHA-256 `41f9c4311f7b73c58346d04103c7accbff4f867d43d97d144c8af2e030e8eee9`; the assistant inspected the unobscured full ring. The diagnostic first assumed one stylesheet request, then correctly required at least one (the app requests it twice). Current focus paint remains 2px with the same computed color and 14.5801:1 contrast in that comparison. The new contrast/priority assertions remain; the unsupported generic geometry call was removed from this narrow regression and its pre-existing measurement limitation is recorded in deferred work. No helper threshold, production focus style or existing test was changed to bypass it; no all-mode geometry pass is claimed.

Final local housekeeping: `npm --prefix tests/e2e run typecheck`, `python3 eng/validate-story-artifacts.py`, `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-6-make-reconciliation-outline-perceptible.md`, and `git diff --check` pass. Existing primary frozen content/task boxes, earlier assistant evidence additions and CRLF are preserved. The owned Test host stopped; port 5070 is closed. This supporting record is done; primary Story/sprint remain in-progress. No staging, commit, branch, push or publication was performed.

## Review Triage Log

Independent context-free Blind Hunter review performed in this session; one configured layer, none omitted. Six findings returned (changed payload 31.085 kB; finding floor 6), with the following dispositions:

- BH-1 — low, patched: the leading result referred to a review receipt that had not yet been appended. This section records the reviewer and every disposition.
- BH-2 — medium, patched: the capability probe alone did not bind system-palette attribution to the actual grid. The regression now verifies its effective adjustment is auto and its resolved outline equals CanvasText on capable engines; final three-engine checks pass.
- BH-3 — medium, patched for focused contrast: width alone could certify an invisible/low-contrast 2px ring. Twenty-four actual focused-outline samples now meet 3:1. The added generic geometry proof separately encounters a pre-existing conservative sticky-header limitation, reproduced on the original CSS with byte-identical old/new paint; that limitation is deferred, not relabelled as a geometry pass.
- BH-4 — low, patched: normal-mode animation verification now asserts one iteration, zero delay and no fill in addition to the existing 700ms duration.
- BH-5 — low, patched: exact successful story/whitespace validation commands and results are recorded above.
- BH-6 — low, patched: the earlier native-evidence section now distinguishes all six scratch directories, the four primary summary/script/hash locations, and the two focused zoom locations.

The first scoped artifact check rejected the record with `story File List not found or empty; changed files missing from story File List`. Added the repository-required scoped File List below and reran the exact scoped command successfully; no validator policy was relaxed.

## File List

- `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css`
- `tests/e2e/specs/responsive-accessibility.spec.ts`
- `_bmad-output/implementation-artifacts/spec-13-6-make-reconciliation-outline-perceptible.md`
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
