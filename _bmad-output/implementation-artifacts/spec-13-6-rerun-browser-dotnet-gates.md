---
title: 'Story 13.6: Re-run browser and .NET gates for the current candidate'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'dispatch'
story_id: '13.6'
review_loop_iteration: 0
baseline_commit: 'd1e54138c45950bd7b6ff74269fef06828040b00'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The latest Story 13.6 patch has no full current browser or .NET gate. Its last broad run had 18 image comparisons and eight .NET tests failing, so readiness cannot rely on focused results.

**Approach:** Rebuild the current Debug Test host and test assemblies, run the full three-spec browser gate and full Shell/SourceTools test assemblies, investigate each current failure, fix demonstrated code or test defects, and rerun affected gates. Publish exact candidate-bound results and a prepared human checklist.

## Boundaries & Constraints

**Always:** Preserve the existing Story 13.6 intent, accessibility thresholds, Fluent V5 ownership, candidate identity and raw failed proof. Keep story/sprint in review until dated human AT, actual 400% browser zoom, physical-device observations and required owner approvals are recorded. Distinguish automation from human evidence.

**Never:** Regenerate or change approved visual baselines without their owner approval; weaken assertions, update dependencies/submodules, or infer manual observations. Do not stage, commit or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Failure Handling |
| --- | --- | --- | --- |
| Browser | Current Debug host; three Story 13.6 specs; all engines | Complete pass/fail/skip inventory including ordinary visual comparisons | Retain raw report, screenshots and exact failed names |
| .NET | Rebuilt Shell and SourceTools assemblies | Complete per-project result and failure diagnosis | Fix current defects; retain unrelated policy blockers explicitly |
| Human gates | No supplied dated observations or approvals | Checklist ready; review remains open | Never promote automated DOM/viewport checks to human pass |

</frozen-after-approval>

## Code Map

- `tests/e2e/playwright.config.ts`, three Story 13.6 specs and `specimen-accessibility.spec.ts-snapshots/` — configured browser projects, host, and approved images; do not update snapshots.
- `tests/Hexalith.FrontComposer.Shell.Tests/`, `tests/Hexalith.FrontComposer.SourceTools.Tests/` — full .NET test assemblies and current failures; inspect catalog/governance and generator snapshots before editing.
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` and `evidence/story-13-6/` — candidate-bound result and preserved historical evidence.
- `docs/accessibility-verification/manual-log-template.md` — human evidence template; do not mark checklist observations complete.

## Tasks & Acceptance

**Execution:**
- [x] Build the Debug Test host and both test projects, then run the three-spec, three-engine browser gate and full Shell/SourceTools assemblies with raw logs/results.
- [x] Diagnose every current visual and .NET failure; correct verified defects without editing image baselines; rerun the affected full gates.
- [x] Record full source candidate identity, commands, exact outcomes, residual blockers and human AT/zoom/device/approval checklist in the Story 13.6 result.

**Acceptance Criteria:**
- Given the current candidate, when automated gates run, then the result names every pass, fail, skip, exact command and retained proof without conflating earlier runs.
- Given no dated human observation or owner approval, when readiness is assessed, then Story 13.6 and its sprint entry remain in review with each human gate open.

## Implementation Notes

- Rebuilt the Debug Counter.Web host and both full test assemblies. Repaired a native-dialog focus return race in `fc-focus.js` and added a cross-engine regression. The lifecycle matrix now refreshes genuine timed states before each paint mode; the full shell-chrome audit has a 120-second budget without changing assertions.
- Kept all 24 approved PNGs and both approved generated-grid `.verified.txt` files unchanged. Retained actual/diff PNGs and `.received.txt` files as separate proof. No dependency, submodule, accessibility threshold or policy change was made.
- Current automated gates and human checklist are in `13-6-accessibility-result.md`. Story and sprint remain in review. The earlier Chromium palette-to-settings Tab failure passed in eight focused repeats and the final full run; its intermittent cause remains open.
- Review patch: deferred native-dialog return now waits through arbitrary dismissal delay, treats Escape in the open dialog as dismissal, and uses the route heading if a connected origin cannot take focus. The synthetic browser case holds the dialog for 20 frames before Escape and checks both origin and heading return. Timed lifecycle evidence refreshes the real confirmation immediately before each inventory and checks visibility afterward.
- Follow-up focus cleanup: once the native dialog closes, an unavailable origin and route heading get a bounded five-second heading-mount window; then the retry timer and global input listeners are released. The open-dialog wait remains unbounded until dismissal or a replacement interaction.

## Spec Change Log

- 2026-10-18: Completed full current-candidate rerun and evidence handoff; changed only execution/task notes outside the frozen human intent.
- 2026-10-18: Applied review findings BH-01/03/05/07/08/12, EC-01/02/03 and VG-01 outside frozen intent. The prior full gates remain retained historical evidence for their earlier source digest; the current patch has focused proof and a final full rerun recorded below.
- 2026-10-18: Closed the post-dismissal no-destination polling gap outside frozen intent; retained the first failed test-harness attempt and its corrected focused proof separately.
- 2026-10-18: Ran the final 750-case browser gate and both full .NET assemblies for source digest `0c1217adfe38e3a488dba91350ff098f6eab0de227714eee99f12b456374af60`; recorded exact outcomes and kept all human gates open.

## Review Triage Log

- Visual baseline regeneration and generated-grid snapshot replacement require recorded owner approval. Fluent/catalog alignment, Release MSB3277 conflicts, the earlier intermittent Chromium keyboard failure's cause and all human AT/zoom/device gates remain open.

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH-01 | medium: after 12 open-dialog frames the callback stops; later dismissal leaves body focus. | patch |
| BH-02 | maybe-false: a second restore would clear a pending origin, but no production double-disposal path was established; settle with a real caller sequence. | defer |
| BH-03 | medium: keydown Escape while a deferred return watches the open dialog sets `userMovedFocus` and cancels return. | patch |
| BH-04 | maybe-false: cleanup is absent on a thrown focus call, but no supported-browser throw path was shown; settle with a reachable exception source. | defer |
| BH-05 | medium: two-frame synthetic dialog cannot exercise the retry cutoff or delayed dismissal. | patch |
| BH-06 | false: the evidence is keyed to lifecycle state, not lifecycle instance; refreshing the same real state is allowed. | reject |
| BH-07 | medium: an auto-dismiss can occur between refresh and later icon/overflow measurements, recording idle paint under a confirmation label. | patch |
| BH-08 | medium: source manifest hashes Counter.Web but omits the loaded Shell DLL and exact host launch command, weakening browser-to-source attribution. | patch |
| BH-09 | maybe-false: no artifact-size threshold was found in repository guidance or validator scripts; the packet measures 1,199,053,461 bytes and the gate remains open. | defer |
| BH-10 | low: only key raw reports have SHA-256; a complete 8,000-file digest would aid forensics but adds a large manifest without a required gate. | reject |
| BH-11 | false: the old prose is under `Historical review evidence — 2026-10-17` and explicitly calls itself prior/current to that dated review. | reject |
| BH-12 | medium: one Chromium example does not classify all 18 image failures, especially the broader Firefox/WebKit differences. | patch |
| EC-01 | medium: same 12-frame loss as BH-01, confirmed by the stop branch. | patch |
| EC-02 | medium: a connected hidden/inert origin repeatedly fails `focusTarget`, while route-heading fallback is never tried. | patch |
| EC-03 | medium: same transient-state expiry gap as BH-07. | patch |
| VG-01 | medium: preverified gap; the two-frame test would pass even if the retry budget were cut below a slower real dismissal. | patch |
| Follow-up focus cleanup | medium: after a dialog closed with no focusable origin or heading, the deferred return polled forever and retained two document input listeners. | patched; focused regression |

The earlier review findings above were checked by the retained 15-case focused, three-engine run. The current follow-up focus behavior was checked by the 6-case run. The 18 visual comparisons are classified by engine, theme and density in `evidence/story-13-6/2026-10-18-gate-rerun/visual-diff-inventory-parent-final.json`; all approved expected bytes remain unchanged. The preceding full browser and .NET outcomes stay failed historical proof. The final full run of this source digest is recorded below.

## Verification

**Commands:**
- `npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --workers=4 --retries=0` — current source digest: 750 total; 729 passed, 18 protected visual comparisons failed, 3 skipped, 0 flaky. The prior 747-case run is historical. Full Test-host environment and raw reports are recorded in the result.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -parallel none` — current source digest: 3709 total; 7 failed, 1 skipped, 0 errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll` — current source digest: 1313 total; 1 failed, 0 skipped/errors.
- `PLAYWRIGHT_SKIP_WEBSERVER=1 ... npx playwright test specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --grep 'owned overlay return waits through Escape dismissal|delayed overlay disposal preserves an operator Tab|owned palette return cancels|all eleven actual lifecycle wrapper states' --workers=4 --retries=0` — focused review patch: 15 passed, 0 failed, 0 skipped across Chromium, Firefox and WebKit. Exact command, host binding and raw reports are in `evidence/story-13-6/2026-10-18-gate-rerun/verification.json`.
- `PLAYWRIGHT_SKIP_WEBSERVER=1 ... npx playwright test specs/responsive-accessibility.spec.ts --grep 'owned overlay return waits through Escape dismissal|owned overlay return releases watchers when no destination appears after dismissal' --workers=4 --retries=0` — current follow-up focus patch: 6 passed, 0 failed, 0 skipped across Chromium, Firefox and WebKit. Raw reports are in `evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focus-final/`; exact command and source binding are in the verification manifest.
