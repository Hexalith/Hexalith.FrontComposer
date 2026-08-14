---
title: '11.5 Dead-CSS remediation and visual-conformance guards'
type: 'feature'
created: '2026-07-06T22:29:05+02:00'
status: 'done'
baseline_commit: '7100bd52493846e93303b355ea8cae1ae23ea875'
baseline_revision: '0c7e5c74f18b2a5c11c70a77a727713373720964'
review_loop_iteration: 1
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-4-security-validation-hardening.md'
  - '{project-root}/_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** Several Shell visuals are silently inert because component-scoped CSS classes sit on Fluent component roots that do not receive the CSS-isolation scope attribute. The current Governance lane also misses Shell `wwwroot/css` link drift and undefined/FAST-era `--error*` tokens, so the same visual defect class can return without a failing test.

**Approach:** Add the three guard classes first or alongside the fixes, then move the affected styling onto raw scoped roots, `::deep` selectors, or rendered inline/component parameters. Prove the changed visual hooks with rendered-DOM, source reachability, and browser/computed-style evidence per the visual component evidence checklist.

## Boundaries & Constraints

**Always:** Use FrontComposer/Fluent UI Blazor v5 patterns and Fluent 2 tokens; preserve accessible names, roles, live-region behavior, reduced-motion handling, stable `data-testid` selectors, and support-safe copy; keep CSS to layout/visual hooks that Fluent does not own; keep the Shell legacy-token and accent-surface backlogs empty.

**Block If:** A named visual cannot be fixed without changing a public component API, generated command/projection markup, Fluent package version, Contracts/Contracts.UI split scope, or a visual/browser lane cannot be run and no named CI responsibility plus fallback evidence can be recorded.

**Never:** Do not add raw interactive HTML controls, theme redefinitions, legacy Fluent v4/FAST tokens, new package dependencies, submodule changes, broad redesign, visual baseline updates without rationale, or unrelated Epic 11 route/package/security work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Dead scoped CSS on Fluent root | A class defined in `Component.razor.css` is assigned only to a `Fluent*` component root | Guard fails unless the style is moved to a raw scoped wrapper, `::deep` from a scoped raw root, or a rendered inline/component parameter | Build fails with file, class, and component name |
| Shell global stylesheet drift | A new `src/Hexalith.FrontComposer.Shell/wwwroot/css/*.css` file is added without a `FrontComposerShell` head link/path | Governance test fails | Build fails with missing stylesheet name |
| Undefined error token | Shell CSS uses `--error`, `--error-background*`, or `--error-foreground*` | Legacy-token guard catches it and CSS is migrated to Fluent 2 error/status tokens | Build fails with offending file |

</intent-contract>

## Code Map

- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` -- existing Fluent governance home; extend with stylesheet-link guard, scoped-CSS-on-Fluent-root detector, and `--error*` legacy-token coverage.
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor` and `.razor.css` -- status margin and reconnect pulse currently target a class on `FluentMessageBar`.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor` and `.razor.css` -- gear pinning targets a `FluentButton` class while the raw wrapper already provides a scoped root.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor` and `.razor.css` -- mobile Done width targets a `FluentButton`; footer/body raw wrappers remain valid CSS roots.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor` and `.razor.css` -- preview layout class is on `FluentStack`; wrapper and badge are valid raw roots.
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`, `FcDevModeToggleButton.razor`, `FcDevModeOverlay.razor`, and their `.razor.css` files -- annotation/toggle/copy/source classes touch Fluent roots and need reachable styling.
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldPlaceholder.razor.css` and `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.css` -- additional `--error*` token migration targets once the regex is widened.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` and `.razor.cs` -- authoritative head links and static-web-asset path properties for Shell global CSS.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcProjectionConnectionStatusTests.cs`, `Components/DataGrid/FcColumnPrioritizerTests.cs`, `Components/Layout/FcSettingsDialogTests.cs`, `Components/Layout/FcDensityPreviewPanelTests.cs`, and `Components/DevMode/*Tests.cs` -- focused bUnit rendered-DOM reachability pins.
- `tests/e2e/specs/specimen-accessibility.spec.ts` and `tests/e2e/scripts/*visual*/*a11y*` -- browser/computed-style and visual-governance evidence lane.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- move Story 11.5 through ready/in-progress/review/done with evidence.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` -- add the three Story 11.5 governance guards with focused positive/negative regex or fixture rows -- closes the unlinked stylesheet, dead scoped CSS, and `--error*` token blind spots before or with remediation.
- [x] `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor`, `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor`, and `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor.css` -- move status/pulse and gear-pin styling to reachable raw roots, `::deep`, or inline/component parameters while preserving existing attributes and selectors -- restores connection and grid affordance visuals.
- [x] `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor`, and `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor.css` -- move mobile Done and preview layout styling to reachable raw roots, `::deep`, or inline/component parameters -- restores settings and density preview visuals.
- [x] `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor`, and `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor.css` -- move Fluent-root DevMode styling to reachable roots and replace `--error` with Fluent 2 error/status tokens -- restores DevMode visuals and token conformance.
- [x] `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldPlaceholder.razor.css` and `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.css` -- replace remaining `--error*` variables with Fluent 2 error/status tokens or component parameters -- removes undefined FAST-era token usage.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcProjectionConnectionStatusTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcColumnPrioritizerTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityPreviewPanelTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeVisualReachabilityTests.cs` -- assert rendered DOM carries reachable roots, inline styles, or `::deep` source anchors for every fixed component -- provides default-lane proof that source-string-only checks missed.
- [x] `tests/e2e/specs/specimen-accessibility.spec.ts` -- add computed-style or behavior proof for the reconnect pulse under both normal and reduced motion and at least one Fluent-root remediation representative; record visual-governance/a11y artifact status -- satisfies E8-AI-1 visual evidence.
- [x] `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- record status transitions, file list, visual evidence checklist results, and validation commands -- keeps BMAD artifacts auditable.

**Acceptance Criteria:**
- Given the seven scoped-CSS files named by Story 11.5, when the focused component tests and Governance lane run, then no class defined only in component-scoped CSS remains assigned only to a Fluent component root.
- Given Shell global stylesheets under `wwwroot/css`, when a stylesheet is added or renamed, then `FluentConformanceTests` fails unless `FrontComposerShell` links it through `HeadContent` and a path property.
- Given Shell CSS contains `--error`, `--error-foreground-rest`, or another `--error-*` token, when the legacy-token guard runs, then the build fails until the token is migrated to Fluent 2 semantics.
- Given the reconnecting projection status, column prioritizer, settings dialog mobile Done button, density preview, and DevMode surfaces render, when visual evidence is collected, then each changed visual has rendered-DOM or computed-style proof and no accessibility-critical affordance regresses.

## Spec Change Log

- 2026-08-14: Confirm-and-pin implementation pass at HEAD `7100bd52493846e93303b355ea8cae1ae23ea875`. No production or test source changes were required: the seven scoped-CSS remediations, three governance guards, Fluent 2 error-token migration, bUnit reachability pins, and normal/reduced-motion computed-style proof are already present and passing. Recorded this run's verification evidence, confirmed the package-inventory blocker remains resolved from the earlier Story 11.14 / 2026-07-12 close (this pass only re-ran `PackageInventory_IsExplicitLockstepAndReviewable`, 1/1), and reconciled the File List to the new `baseline_commit`. `baseline_commit` is the 2026-08-14 confirm-and-pin HEAD; `baseline_revision` remains the July rework SHA.

## Review Triage Log

- 2026-08-14: Confirm-and-pin at unchanged HEAD `7100bd52`. The worktree has only the documented unrelated `references/Hexalith.Builds` dirt plus the untracked NuGet spec. All Story 11.5 acceptance criteria remain satisfied by the existing remediations and guards. The package-inventory blocker remains resolved from the earlier Story 11.14 / 2026-07-12 close; this pass only re-ran `PackageInventory_IsExplicitLockstepAndReviewable` (1/1). Chromium a11y/visual specimen lane passed 22/22, including visual baselines.
- 2026-07-11: Reopened by user decision because the prior review-promotion range mixed Story 11.5 evidence with unrelated submodule-pointer changes and could not pass artifact reconciliation at a stable endpoint.
- 2026-07-11: User chose to keep this prior spec active and synchronize it with the canonical Story 11.5 rework contract.
- 2026-07-11: Rework baseline reset to clean commit `0c7e5c74f18b2a5c11c70a77a727713373720964`; required patch adds normal-motion computed-style proof alongside the existing reduced-motion proof.
- 2026-07-11: Story-focused evidence passed; the broad filtered solution lane remains red only at `CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable`, so both story artifacts remain `in-progress` pending that baseline blocker.

## Rework Evidence

### 2026-08-14 confirm-and-pin (baseline `7100bd52493846e93303b355ea8cae1ae23ea875`)

- Focused governance: 39/39 passed.
- Affected component tests: 32/32 passed (`FcDevMode` filter includes the three visual-reachability pins plus one additional DevMode test).
- E2E typecheck: passed.
- Chromium accessibility/visual specimen lane: 22/22 passed, including `story 11.5 scoped Fluent-root visual hooks are reachable in normal and reduced motion` and all six visual baselines.
- Visual baseline governance: passed with no committed baseline changes.
- Release solution build: passed with 0 warnings and 0 errors.
- Package-inventory governance: this pass only re-ran `PackageInventory_IsExplicitLockstepAndReviewable` (1/1); the Contracts.UI inventory blocker remains resolved from the earlier Story 11.14 / 2026-07-12 close.
- Diff hygiene: `git diff --check` passed.

Visual component evidence checklist:
- Required: yes.
- Rendered DOM attachment: focused bUnit component lane passed 32/32 for `FcProjectionConnectionStatusTests`, `FcColumnPrioritizerTests`, `FcSettingsDialogTests`, `FcDensityPreviewPanelTests`, and `FcDevMode*`.
- Scoped CSS / Fluent targeting: `FluentConformanceTests` passed 39/39, including stylesheet-link drift, scoped-CSS Fluent-root detection, and legacy `--error*` token guards.
- Computed style / behavior: `npm --prefix tests/e2e run test:a11y` passed 22/22, including normal-motion pulse (`fc-sync-status-pulse*`, `0.7s`, `24`, `alternate`), reduced-motion (`none` / `0s`), density-preview border/padding, and mobile Done-button width.
- Accessibility interaction: the same Playwright lane passed keyboard, focus-visible, forced-colors/reduced-motion, zoom/reflow, status icon focus/hover/touch, and axe checks.
- Shell accent-as-thread guard: passed through `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background`.
- Visual/browser lane: local Chromium `test:a11y` passed; no CI handoff blocker.
- Snapshot/baseline intent: unchanged; `validate:visual-governance` reported no committed visual baseline changes.

### Historical 2026-07-11 rework

- Focused governance: 39/39 passed.
- Affected component tests: 31/31 passed.
- E2E typecheck: passed.
- Chromium accessibility/visual specimen lane: 22/22 passed, including normal-motion and reduced-motion reconnect-pulse computed styles.
- Visual baseline governance: passed with no committed baseline changes.
- Release solution build: passed with 0 warnings and 0 errors.
- Canonical story artifact validation: passed against clean baseline `0c7e5c74f18b2a5c11c70a77a727713373720964`.
- Broad filtered solution lane: failed at the then-current package-inventory governance check (`CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable`) because `Hexalith.FrontComposer.Contracts.UI.csproj` was missing from `eng/release-package-inventory.json`. That blocker remains resolved from the earlier Story 11.14 / 2026-07-12 close; this 2026-08-14 pass did not re-run the broad filtered solution lane.

## Documented Blockers

- None for Story 11.5. The `eng/release-package-inventory.json` Contracts.UI gap remains resolved from the earlier Story 11.14 / 2026-07-12 close. This pass only re-ran `PackageInventory_IsExplicitLockstepAndReviewable` (1/1). The broad filtered solution lane was not re-run in this confirm-and-pin pass (out of the spec's required command list); focused governance, component, browser, and Release-build gates are green.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/spec-bump-latest-hexalith-nuget-packages.md` -- unrelated untracked BMAD spec from concurrent workspace work; not part of Story 11.5 and preserved as found.
- `references/Hexalith.Builds` -- unrelated classified path; gitlink remains pinned at `606d9f119965c273104d707b9cc8c179fe648237` with a dirty worktree; not part of Story 11.5 and was not reset or edited.

## File List

- `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md` -- confirm-and-pin evidence, confirmed the earlier Story 11.14 / 2026-07-12 package-inventory close, and `baseline_commit` for this implementation pass
- `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md` -- pre-existing story artifact, unchanged
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- pre-existing; Story 11.5 already `done`, not regressed
- `tests/e2e/specs/specimen-accessibility.spec.ts` -- pre-existing Story 11.5 browser evidence, unchanged

## Design Notes

Prefer the Story 8.6 `FcPageToolbar` precedent: if the class is on a Fluent component and the rendered node cannot receive the component scope attribute, use a raw wrapper plus `::deep` for descendants, or move layout onto `Style`/component parameters where the style must land on the Fluent element itself. Do not let the scoped-CSS detector become a broad ban on Fluent `Class=`; it should fail only when a same-component `.razor.css` selector would be dead.

## Verification

**Commands (2026-08-14 confirm-and-pin):**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FluentConformanceTests"` -- passed: 39/39.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FcProjectionConnectionStatusTests|FullyQualifiedName~FcColumnPrioritizerTests|FullyQualifiedName~FcSettingsDialogTests|FullyQualifiedName~FcDensityPreviewPanelTests|FullyQualifiedName~FcDevMode"` -- passed: 32/32.
- `npm --prefix tests/e2e run typecheck` -- passed.
- `Hexalith__FrontComposer__Specimens__Enabled=true DiffEngine_Disabled=true npm --prefix tests/e2e run test:a11y` -- passed: 22/22, including Story 11.5 normal/reduced-motion pulse and six visual baselines.
- `npm --prefix tests/e2e run validate:visual-governance` -- passed; no committed visual baseline changes.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- passed: 0 warnings, 0 errors.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md` -- passed; documented the unrelated concurrent `spec-bump-latest-hexalith-nuget-packages.md` scratch spec.
- `git diff --check` -- passed.

## Suggested Review Order

- Start here: confirm-and-pin records already-landed remediations, not new product code
  [`spec-11-5-dead-css-remediation-and-visual-conformance-guards.md:79`](spec-11-5-dead-css-remediation-and-visual-conformance-guards.md#L79)

- Intent still owns the three guards and reachable-style remediations
  [`spec-11-5-dead-css-remediation-and-visual-conformance-guards.md:25`](spec-11-5-dead-css-remediation-and-visual-conformance-guards.md#L25)

- 2026-08-14 evidence pins governance, bUnit, and Chromium a11y/visual results
  [`spec-11-5-dead-css-remediation-and-visual-conformance-guards.md:91`](spec-11-5-dead-css-remediation-and-visual-conformance-guards.md#L91)

- Unrelated Builds dirt and NuGet spec are classified, not absorbed
  [`spec-11-5-dead-css-remediation-and-visual-conformance-guards.md:127`](spec-11-5-dead-css-remediation-and-visual-conformance-guards.md#L127)

- Verification commands and pass counts for the confirm-and-pin rerun
  [`spec-11-5-dead-css-remediation-and-visual-conformance-guards.md:145`](spec-11-5-dead-css-remediation-and-visual-conformance-guards.md#L145)
