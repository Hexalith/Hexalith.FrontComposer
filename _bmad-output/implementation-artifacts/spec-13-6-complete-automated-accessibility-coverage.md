---
title: 'Story 13.6: Complete automated accessibility coverage'
type: 'feature'
created: '2026-10-16'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c3708690f8e1dbebfb49682c67a8aba88fd4a092'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.6 has passing focused checks but lacks real zero-field browser failures, generated-detail removal integration, complete observable state/theme coverage and cross-browser execution.

**Approach:** Complete those automated checks through existing gated Counter specimens and browser helpers, run available engines and the configured Shell lane, and refresh one candidate-bound result before human validation. Run the requested UX evidence review.

## Boundaries & Constraints

**Always:** Preserve the approved 13.6 decisions, Fluent V5 ownership, exact default density/typography/rail/measure contracts, tenant isolation and single-owner speech. Scenario controls use fixed safe data and existing Test/specimen gates; ordinary routes and six visual baselines keep their content. Record every state/AE row, executed result, reuse and external blocker precisely. Keep manual AT, actual zoom, tablet/phone and human approvals open. UX evidence review cannot supply those observations.

**Never:** Add backend workflows, public testing APIs, dependency upgrades, arbitrary failure text, independent palettes or axe exclusions. Infer heard speech, actual zoom or physical devices from automation. Infer separate OI-16/G-4/Fluent/Product/live-fresh-row approval. Auto-push or claim Windows execution on Linux.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Failure handling |
|---|---|---|---|
| Zero-field | Safe validation, returned/exception rejection, forbidden/unavailable/rate-limit/transport, admission/policy denial, acceptance | Visible safe feedback, retained trigger focus, one automation-observed owner, no pre-accept retry; next attempt clears, accepted uses pending summary | Fix actual generated path; retain failed proof |
| Detail removal | Two real generated rows; focused detail; matching completed result removes it | Focus non-live neutral explanation only after focus loss; external focus survives; repeated results silent; filter-only path distinct | Cancel stale scope/navigation recovery |
| AE state matrix | Each changed surface/state in light/dark, 320px plus spacing, forced colors/reduced motion | 4.5:1 normal text, 3:1 large/nontext/focus; targets/whole focus visible; semantics and immediate text/icon/current-state cues survive | No vacuous samples or unlisted omissions |
| Platforms | Chromium/Firefox/WebKit locally; existing Windows CI | Execute supported checks; Windows command includes new specs and artifacts | Exact blocker/owner/command; no fabricated pass |

</frozen-after-approval>

## Code Map

- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor` owns existing query-gated fixtures and real `CounterProjectionView`; `CounterProjectionTemplate.razor` exposes `Context.DefaultBody` under `generatedGrid=true`.
- `samples/Counter/Counter.Specimens.Domain/` generated specimen commands; `samples/Counter/Counter.Web/Program.cs`, `CounterSampleCommandService.cs` gate registries/policies and Test-only opt-in failures. Reuse derived fields and scoped fixed scenario state.
- `samples/Counter/Counter.Web/CounterProjectionEffects.cs`: matching LoadRequested/Loaded actions carry scope/generation; complete real results, never set banner flags directly.
- `tests/e2e/helpers/responsive-accessibility.ts`, `specs/responsive-accessibility.spec.ts`, `fixtures/lifecycle.fixture.ts`: reuse geometry/spacing/keyboard tools; measure actual composed-tree text, borders/icons/focus. Real wrapper states exceed the fixture's six-state list.
- `.github/workflows/quality.yml` already runs three Linux engines and Windows `npm run test:a11y`; extend that script in `tests/e2e/package.json` and existing artifact validation, preserve workflow names.

## Tasks & Acceptance

**Execution:**
- [x] `samples/Counter/Counter.Specimens.Domain/`, `samples/Counter/Counter.Specimens/`, `samples/Counter/Counter.Web/` — expose real generated zero-field and projection-result scenarios through existing opt-in gates; cover production gate rejection and safe admission/policy seams.
- [x] `tests/e2e/specs/responsive-accessibility.spec.ts`, new `tests/e2e/specs/accessibility-state-matrix.spec.ts`, `tests/e2e/helpers/responsive-accessibility.ts`, `tests/e2e/fixtures/lifecycle.fixture.ts` — cover shell/Home/nav, toolbar/tabs/popovers, projection/detail/states, generated forms/lifecycle, destructive/abandonment dialogs and palette/settings. Reuse existing journeys; assert text/nontext/focus contrast, actual mode cues, targets, clipping, keyboard and single-owner DOM sequences. Add negative fixtures for new measurement logic.
- [x] `tests/e2e/package.json`, `tests/e2e/scripts/validate-a11y-artifacts.mjs` — include coverage in existing Windows lane and validate required artifacts without weakening gates.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, `evidence/story-13-6/`, original 13.6 spec — refresh surface/state/AE mapping, candidate hashes and honest platform/manual status. Preserve earlier evidence attribution; record UX review and finding dispositions.

**Acceptance Criteria:**
- Given gated real generated components, when the scenario matrix runs, then every listed outcome/surface has nonempty passing assertions or an explicit external execution blocker.
- Given available browser engines and configured Shell tests, when verification finishes, then exact current results and CI follow-ups bind the final candidate; manual and separate owner gates remain truthful.

## Implementation Notes

2026-10-16 completion implementation exposes gated real generated zero-field and completed-result removal scenarios, records 116 state rows per engine, and adds composed-tree contrast/geometry/keyboard negatives. Forced-color evidence remeasures required icons, boundaries and focus/current cues after actual color replacement. Persistent modal-session focus tracking preserves operator movement after entry intent consumption. Default Fluent density/typography/rail/measure contracts and current visual baselines are preserved.


## Spec Change Log

- 2026-10-16: implementation and evidence refreshed without changing frozen intent. Unsupported Linux WebKit forced-palette cells, failing comparisons/configured Shell checks, Windows and all manual/owner gates remain explicit.


## Review Triage Log

- Scoped UX accessibility/Fluent review found four Medium accessibility items and no Critical/High or Fluent findings on pre-patch candidate `sha256:c0500f85acc0a4dcff6e1b8baf55b1819ed768da0ec2764a5efe6e73538b5487`. Control-owned contrast alternatives, shadow/pseudo focus geometry, open-shadow occlusion and post-switch state cue/artifact checks are patched with regression evidence. The subsequent Medium actual-entry focus-return race is also resolved through persistent modal-session tracking. All five dispositions and independent follow-up proof are finalized in [the scoped UX report](evidence/story-13-6/2026-10-16-completion/ux-review/validation-report.md); the parent source/JUnit/ledger audit passed.


### Required Build review — reviewed candidate 16abc0fa7184b360f471da731c789306d43a8825bc104f04329c581fea288410

All three layers returned before triage. Each verified issue affects the reliability of the new developer evidence; no new production failure is inferred. Every smallest fix stays inside existing helpers, fixtures or the existing CI command and adds no shipped public surface. All route to patch; no intent/spec renegotiation or code reversion is needed.

| Finding | Verdict | Evidence | Route |
|---|---|---|---|
| BH01 | medium | focusGeometry accepts an unchanged permanent sharp shadow as focus paint; expectFocusVisible only asserts active focus and calls that presence check. No focus-state distinction disproves the synthetic result. | resolved — patch G01 |
| BH02 | medium | The occlusion guard requires a larger maximum numeric ancestor z-index outside the hit box; later same-level positioned paint can cover the outline while failing that guard. | resolved — patch G02 |
| BH03 | medium | painted() excludes transparent and rgba alpha zero only, so computed color(srgb ... / 0) is counted as an outline. | resolved — patch G03 |
| BH04 | medium | Focus geometry checks viewport/overflow but neither the target nor ancestors reject clip-path, clip or masks; the demonstrated clipped outline passes. | resolved — patch G04 |
| BH05 | medium | Icon sampling composites fill/stroke color but never multiplies fillOpacity/strokeOpacity; the low-alpha SVG is reported as opaque. | resolved — patch G05 |
| BH06 | medium | measureSurfaceContrast visible() returns false for all clip/clip-path owners, silently dropping fully visible low-contrast text while other samples keep the scope nonempty. | resolved — patch G06 |
| BH07 | medium | Artifact targets are accepted solely when target.failure is falsy; undersized unexcepted records with inconsistent conformity flags pass. | resolved — patch G07 |
| BH08 | medium | Required-state candidates and system snapshots are found by global basename, allowing two incomplete engines to cover one nominal inventory and cross-own images. | resolved — patch G08 |
| BH09 | medium | evidence() switches media and measures color/overflow/text but does not call focused-target geometry before restoring normal colors. | resolved — patch G09 |
| BH10 | medium | The delayed fixture waits before Complete(missing), and navigation disposes that producer timer. It does not hold an already-entered recovery after captured detail removal. | resolved — patch G10 |
| EC01 | medium | outerShadows returns an entry for zero-offset, zero-blur, nonpositive spread and hardIndicator then accepts it despite no visible outer paint. | resolved — patch G13 |
| EC02 | medium | The same painted() alpha parser used for shadows accepts fully transparent color(srgb ... / 0), matching BH03 at the shadow caller. | resolved — patch G03 |
| EC03 | medium | The same missing effective SVG fill/stroke opacity as BH05 produces a falsely opaque contrast sample. | resolved — patch G05 |
| VG01 | medium | Pre-verified gap: package test:a11y runs Playwright specs only and the existing CI node-test list omits a11y-state-evidence.test.mjs; direct local execution does not wire CI. | resolved — patch G11 |
| VG02 | medium | Focus outline samples always use parent background, including outline-offset:-2px where the visible edge lies on the control background; black-on-black is measured against white. | resolved — patch G12 |
| VG03 | medium | Focus contrast samples every shadow color, including permanent inset borders, then allows that paint to mask a 1:1 outline within the control owner. This shares unqualified focus-decoration acceptance with BH01. | resolved — patch G01 |

### Patch outcome

All 16 individual findings retain their verified verdicts above and are resolved through 13 direct patches. The current parent three-spec run, Node negative suite and strict engine/run artifact validation cover the patched paths. Subsequent verification also fixed the duplicated Fluent return and shared grid and narrow specimen focus obstructions; no frozen intent changed. Failed/incomplete attempts remain retained.

## Verification

- Serialized Debug builds of Counter.Web and affected test projects; direct xUnit v3 affected classes and configured Shell lane excluding existing GovernanceBuild/Performance/e2e-palette/NightlyProperty/Quarantined traits. Record live dependency skips separately; do not run Release-spawning governance builds locally.
- `npm --prefix tests/e2e run typecheck`; new/affected specs on Chromium, Firefox, WebKit against rebuilt Debug Test host, existing visual comparisons and relevant journeys. Windows execution belongs to its existing hosted job.
- Existing artifact/governance validators, `git diff --check`, independent candidate hashes; scoped headless UX Validate accessibility/Fluent lenses, with all Critical/High findings dispositioned. Human checklist stays open.

### 2026-10-16 executed result

Current candidate `sha256:f8d9a2b2a0608d5ed0bc461f433734e153b2836fa1237658539cc638bb2c03cf`: three accessibility specs with visual-baseline cases run separately execute 363 cases: **361 passed, 0 failed, 2 existing skips**. All 116 state rows per engine are retained and owned by run `story-13-6-build-review-final-d74b284b59ab`. Chromium/Firefox strict state validation passes; WebKit's 116 actual forced-palette cells remain uncovered. Latest configured Shell is 3680/1 on separately attributed a42 inputs; affected 163 pass within it and the scheduler's serial 88 pass does not replace that failed gate. The current affected 163 checks pass and separate visual comparisons are 6 passed / 12 failed. The preceding `16abc` ten-spec run retains 12 visual failures and 8 project skips. Windows and all manual/owner gates remain open. [Current result](13-6-accessibility-result.md) and [verification](evidence/story-13-6/2026-10-16-completion/build-review/verification.json) record exact attribution. Story completion is not asserted.


### 2026-10-16 gate-reconciliation continuation

Final candidate `sha256:c14be5f5413337c1e5a55b7e49075804761c57015e810113b0f94accb9190732` retains both frozen intent blocks and the preceding forty source inputs. The scheduler test now observes both callback starts under its existing shared deadline. Accepted cleanup offloads only potentially blocking linked CTS disposal; immediate cancellation and all bounds remain intact. The subscription test retains its own close/restart task across rejoin. Final configured Debug Shell:3681 passed/0 failed/0 skipped. Focused scheduler88, EventStore68 and subscription41 all pass.

The rebuilt three-engine run executes381 cases:366 passed/13 failed/2 existing skips. Twelve failures are unchanged Firefox/WebKit image comparisons, causally traced to system-font resolution plus later approved layout changes; one additional Chromium dark Settings return failure remains unattributed. Six focused instrumented repetitions do not replace that canonical failure. All116 state rows per engine are retained, subject to WebKit's116 actual-palette omissions and the additional measurement-review limits. Every existing42 browser-run candidate input matches final bytes; final manifest adds only the subscription-test file.

[Current result](13-6-accessibility-result.md), [verification](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/verification.json), [input reuse](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/input-reuse.json) and [visual diagnosis](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/visual-diagnosis.md) retain exact outcomes. Windows, manual AT/zoom/devices, owner approvals and live-backend acceptance remain open. Status stays in-review; no completion, staging, commit or push is asserted.

### Continuation review triage — all three layers returned

Eighteen individual Medium findings / fifteen groups concern preceding unchanged helper/test implementation, outside the current two-gate diagnostic intent. Each remains explicitly open as evidence-hardening work; these dispositions do not claim broader Story13.6 acceptance. No finding targets the three new changes.

| Finding | Verdict | Verified evidence | Route / group |
|---|---|---|---|
| BH01 | medium | `tests/e2e/specs/responsive-accessibility.spec.ts:574` — The fallback manually wraps after an unprevented Tab and unchanged boundary index; a handler that refocuses the last button without preventDefault is accepted. The retained isolated probe reproduced the trap three times. | defer / R01 |
| BH02 | medium | `tests/e2e/helpers/responsive-accessibility.ts:631` — Pseudo opacity is checked only for zero; sample() receives no fractional pseudo alpha. The retained black border at .01/.1 opacity reports21:1. | defer / R02 |
| BH03 | medium | `tests/e2e/helpers/responsive-accessibility.ts:602` — The selector accepts path/polygon/circle/rect only. A passing rectangle masks an unmeasured low-contrast ellipse/line/polyline/use. | defer / R03 |
| BH04 | medium | `tests/e2e/helpers/responsive-accessibility.ts:605` — Sampling fill/stroke opacity never tests stroke-width. A fill:none, stroke:black, stroke-width:0 rectangle is reported as meaningful21:1 icon paint. | defer / R04 |
| BH05 | medium | `tests/e2e/helpers/responsive-accessibility.ts:535` — background() rejects background images and fractional group opacity but not filters/blending. brightness(0) can make white text black-on-black while its computed color still reports21:1. | defer / R05 |
| BH06 | medium | `tests/e2e/helpers/responsive-accessibility.ts:600` — Text nodes use textCss.color without the effective -webkit-text-fill-color. White glyph fill over white with color:black is falsely measured as21:1. | defer / R06 |
| BH07 | medium | `tests/e2e/helpers/responsive-accessibility.ts:531` — background() composites ancestors only. An opaque black positioned sibling underneath black glyphs is absent from the reported white background. | defer / R07 |
| BH08 | medium | `tests/e2e/helpers/responsive-accessibility.ts:285` — focusGeometry skips a visual whose own opacity is0 but does not reject opacity0 ancestry; its contour can be reported visible in an invisible group. | defer / R08 |
| BH09 | medium | `tests/e2e/helpers/responsive-accessibility.ts:371` — related() exempts every descendant through contains(element,candidate); an opaque child extending across the target and ring is skipped by occlusion measurement. | defer / R09 |
| BH10 | medium | `tests/e2e/helpers/responsive-accessibility.ts:592` — The text traversal samples actual text nodes/input content, not pseudo generated text. A low-contrast ::before label disappears beside a passing paragraph. | defer / R10 |
| EC01 | medium | `tests/e2e/helpers/responsive-accessibility.ts:238` — outerShadows discards collapsed spread only at zero offset. A10px offset with -100px spread draws no pixels yet geometry/contrast pass; retained screenshots were identical resting/focused. | defer / R11 |
| EC02 | medium | `tests/e2e/helpers/responsive-accessibility.ts:625` — audit.changed qualifies a changed complete shadow list; all layers, including a permanent black layer, are then sampled. Appending an invisible white layer can produce a false focus pass with identical pixels. | defer / R12 |
| EC03 | medium | `tests/e2e/helpers/responsive-accessibility.ts:631` — Pseudo border samples always pass outside=true. A black border wholly inside a black control is compared against white ancestry and reported21:1 despite identical resting/focused pixels. | defer / R13 |
| EC04 | medium | `tests/e2e/helpers/responsive-accessibility.ts:617` — Same root as BH02, independently reproduced with a black pseudo focus border at opacity.1. The pseudo alpha is never passed into sample(). | defer / R02 |
| EC05 | medium | `tests/e2e/helpers/responsive-accessibility.ts:589` — Placeholder sampling reads the pseudo color but not its separate opacity; an opacity.1 black placeholder on white is reported21:1. | defer / R14 |
| EC06 | medium | `tests/e2e/helpers/responsive-accessibility.ts:602` — Same selector omission as BH03; independent ellipse fixture leaves only the passing rectangle in the contrast inventory. | defer / R03 |
| VG01 | medium | `tests/e2e/specs/responsive-accessibility.spec.ts:236` — Pre-verified regression gap: the held actual Fluent25ms callback test opens palette only. Settings coverage ends at initial origin return; removing its vendor-return suppression has no equivalent delayed-operator-movement regression. | defer / R15 |
| VG02 | medium | `tests/e2e/helpers/responsive-accessibility.ts:617` — Independently reproduced black pseudo underline with opacity.1 passes geometry and reports21:1 rather than effective approximately1.25:1. Same root as BH02/EC04. | defer / R02 |


Reproducers and every grouping/disposition are retained in [the continuation report](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/review/triage.md) and the append-only deferred ledger.
