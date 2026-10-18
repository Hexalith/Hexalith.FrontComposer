---
title: 'Story 13.6: Make browser accessibility verification repeatable'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'dispatch'
story_id: '13.6'
story_key: '13-6-i-ux-e-verify-responsive-and-assistive-accessibility'
review_loop_iteration: 0
baseline_commit: '1f8c7b2b4424b6232121a7cc567f7129e46577d3'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.6 has Chromium palette-to-Settings focus and Firefox theme-menu measurement failures. Combined artifact roots exceed the size limit.

**Approach:** Reproduce and correct demonstrated defects; run the existing suite per engine against one candidate. Retain complete portable evidence in the existing story result.

## Boundaries & Constraints

**Always:** Preserve AE-01–AE-09, Fluent ownership, typography/density, speech and focus/contrast thresholds. Retain failed attempts and the 52,428,800-byte limit per results/HTML root. Missing evidence or failed gates keeps story/sprint in review.

**Never:** Add blind delays, replace keyboard journeys with programmatic focus, weaken checks, discard captures, regenerate approved PNG/text baselines, change dependencies/policy/submodules, or stage/commit/push. Never infer manual AT, heard speech, actual 400% zoom, physical-device/Windows results, WebKit forced-palette support or owner approval.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Failure handling |
|---|---|---|---|
| Dismissal | Actual palette; Escape then Tab; delayed callbacks | Restore once; preserve operator's next focus | Unreproduced race stays open |
| Menu paint | Opaque native menu over page border/shadow | Measure genuinely shielded focus paint | Transparent/outside/competing-plane paint stays fail-closed |
| Packet | Three engines, same candidate | All reports/attachments replay portably within each root limit | Preserve raw failures and capability skips |

</frozen-after-approval>

## Code Map

- `tests/e2e/specs/accessibility-state-matrix.spec.ts:572` — failing transition; theme-menu journey reproduces Firefox.
- `tests/e2e/helpers/responsive-accessibility.ts:1194` — `sample()` shielding; inspect composed ancestry, radii and ring bounds.
- `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js` — `containDialogFocus`/`restoreOverlayOrigin`; `Components/Layout/FcCommandPalette.razor.cs` in the same Shell project owns registration/disposal.
- `tests/e2e/specs/responsive-accessibility.spec.ts` — delayed-return/paint counterexamples; reproduce without extra test-only containment registration.
- `tests/e2e/playwright.config.ts` — output/report variables support separate engine runs; preserve capture policy.
- `tests/e2e/scripts/validate-a11y-artifacts.mjs` and `a11y-state-evidence.mjs` — size/attempt/PNG/capability validation. Reuse `2026-10-18-attempt-ownership/validate-state-packet.mjs` under story evidence for portability.

## Tasks & Acceptance

**Execution:**

- [x] `tests/e2e/specs/{responsive-accessibility,accessibility-state-matrix}.spec.ts` — reproduce both failures; cover delayed callbacks, opaque shielding, transparent/outside rings and competing planes. Prove red/green causality.
- [x] `tests/e2e/helpers/responsive-accessibility.ts`, Shell `wwwroot/js/fc-focus.js` and `Components/Layout/FcCommandPalette.razor.cs` — correct reproduced defects; preserve negative tests and keyboard order.
- [x] `_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-18-repeatable-browser/` — retain complete per-engine runs of all three accessibility specs, commands, source/assembly/submodule/baseline identities, reports and attachments. Bind unique run IDs to one candidate. Validate original roots; retain raw reports plus hash-verified relocation mapping/replay and complete HTML assets.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — reference current proof separately from history; retain manual checklist and unresolved visual/catalog/platform/owner gates.

**Acceptance Criteria:**

- Given a fix, when causal/regression tests run, then the defect fails before correction and passes afterward without reduced requirements.
- Given the final candidate, when three engines execute the suite/validators, then every outcome and size result is reported; subsets cannot establish completion.
- Given relocated evidence, when replay checks hashes/ownership, then every attachment resolves with unchanged outcomes.

## Implementation Notes

- Implemented the palette registration/late Fluent return guard and rounded native-plane coverage, with causal failures before correction and 18/18 focused passes across all engines.
- Complete final browser gates: 765 total, 744 passed, 18 unchanged visual failures, three capability skips; every executed nonvisual case passes. Per-engine roots meet the original size cap. Debug build, TypeScript and 24 Node tests pass; configured Shell lane retains 3699/3703 with the four existing snapshot/catalog failures.
- Parent diff/matrix audit verified the actual delayed palette journey, Settings without test-only registration, reused-host close events and positive/negative paint cases passed in every final engine report. The candidate's seven source/config files, 37 assemblies, 26 protected baselines, eight submodules and frozen intent match.
- Parent audit caught ignored support logs and stale current-result fields. Byte-identical nonignored log copies and isolated Git-visible replay close those gaps; original raw outcomes remain unchanged. Relocation verifies 7310 files, 968 JSON paths and 709 JUnit markers while retaining the failed overall gate.
- The original story and sprint stay in review. Manual/platform/visual/catalog/owner gates remain open. Owned Aspire resources are stopped; nothing staged, committed or pushed.

### Review patch verification

P1–P7 are corrected. Focused review tests pass (14 browser / one capability skip; 25 tooling); the parent reran all three complete browser gates against source patch `b6b5b7a761b4927d21ee0fe9ba5daf78ce31ea87928f28b3e2a3b842e8fe5fc3`: 774 total, 752 passed, 18 retained visual failures, four capability skips, no flaky or runner errors. All executed nonvisual cases pass; every root meets the original cap. The fresh reviewed packet replays from Git-visible inputs with 2079 files and 709 JSON/JUnit paths verified, preserving failed validators and outcome identities. The existing .NET failure evidence is reused against unchanged C# and 37 assembly hashes. All 18 manual boxes, frozen intent, approvals and submodules are unchanged; owned Aspire is stopped.

### Completion

This done status closes the approved bounded follow-up only; Story 13.6 and sprint remain in review for the retained visual/catalog/manual/platform/owner gates. All eleven review findings have individual dispositions; seven patch groups are verified, two unverified timing hypotheses are deferred, and one unverified low-impact hypothesis is rejected. The approved no-staging/no-commit/no-push instruction takes precedence over the workflow's default local-commit step.

## Spec Change Log

## Review Triage Log

All three independent layers returned before classification. Verification-gap review found no gaps. Each finding is recorded individually before grouping.

| Finding | Verdict | Evidence and route |
|---|---|---|
| BH01 effective zoom | medium | Parent Chromium replay certifies 21:1 for the zoomed corner; computed radii and viewport rectangles use different scales. Patch P1. |
| BH02 noncircular corners | medium | Parent Chromium replay certifies 21:1 with supported bevel corners crossing the ring. Circular coverage cannot establish that paint. Patch P2. |
| BH03 Settings entry order | maybe-false | Settings still awaits entry before containment, unchanged at baseline. The normal real journey passes; an actual delayed Settings-entry/native-return reproduction is needed to establish harm. Defer as pre-existing, unverified medium. |
| BH04 late native initialization | maybe-false | Fluent initializes its shadow dialog in a zero-delay connected callback; current containment caches the host even if the native dialog is absent. Missing registration is plausible, but operator harm after permanent vendor-slot suppression is not established. Defer unverified medium; settle with delayed real initialization and dismissal. |
| BH05 prevented cancel | maybe-false | A prevented native cancel could consume the dismissal marker, but neither owned shipped dialog installs such a listener; Fluent's prevent-Escape path suppresses the key before cancel. Reject unverified low: a supported caller and observable later-close failure would be needed. |
| BH06 recipe overwrite | medium | The runner writes browser-runs.json before exclusive log opens; rerunning against retained logs replaces evidence before EEXIST. Patch P3: preflight/exclusive destination creation. |
| BH07 incomplete launch record | medium | Unconditional report reads/root traversal throw before lane metadata is appended when npx fails or reports are absent. Patch P4: retain command/termination/incomplete disposition without claiming success. |
| BH08 inherited report paths | medium | Runner merges caller environment without setting JSON/JUnit paths, while config honors both variables. Reports can escape lane roots. Patch P5: bind both paths explicitly. |
| BH09 mutable validator inputs | medium | Replay hashes its wrapper, but imported state validation, spawned full validator and specimen manifest are read from current checkout without identity checks. Patch P6: bind and verify those runtime bytes. |
| BH10 duplicate engines/identity | medium | Length-three check accepts duplicate Chromium records and repeatedly checks the same replay row; candidate/run cross-record identities are unchecked. Patch P7: require exact unique engine coverage and matching identities. |
| EC01 zoomed native plane | medium | Same independently reproduced viewport-radius defect as BH01, including ancestor zoom. Patch P1. |

P1 combines BH01/EC01 only. P2–P7 remain separate roots. All patches are private, direct guards/corrections to demonstrated input or retention defects; no intent change or public API is required. Preserve passing actual palette/Settings/native-close tests and every prior raw packet/outcome. Deferred hypotheses do not establish new confirmed product failures.

### Post-`1f8c7b2b` review addendum (2026-10-18)

Review of `1f8c7b2b..HEAD` (source only) found changes beyond this spec's intent; they are accepted as intentionally bundled and recorded here rather than split out of main.

- **Documented Unrelated Changes — SDK floor 10.0.401 → 10.0.402:** `global.json`, CI workflows, IDE-parity matrix/doc/script and two governance tests. Governance now rejects 10.0.401; runners and dev machines must have 10.0.402 installed.
- **Documented Unrelated Changes — `CommandFormEmitter`:** `PresentHiddenOutcomeAsync(null)` now runs after state-guard and admission instead of before them, so a parent callback cannot precede the synchronous guards. Only the two `CommandFormEmitterTests` `verified.txt` snapshots changed as a result. Pinned behaviorally by `EarlierHiddenOutcomeIsClearedOnlyByAnAdmittedAttempt`.
- **Deferred (unverified medium):** `containDialogFocus` reads the shadow `<dialog>` once at registration (BH04); settle by checking in Chromium that it is non-null at registration for `fc-palette-root` and Settings.

## Verification

- `npm --prefix tests/e2e run typecheck`; `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs`.
- Run causal cases on all engines, then three accessibility specs per `--project --retries=0` with existing Test settings and separate roots. Start/inspect the exact AppHost through Aspire before code edits; stop owned resources afterward.
- Run artifact validation per engine and relocation replay; retain all failures.
- If Shell changes, Debug-build Shell.Tests and run its configured lane/five exclusions, including `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background`. Existing snapshot/catalog failures remain blockers.
- `git diff --check`; verify all 26 protected baselines and eight root submodule identities remain unchanged.
