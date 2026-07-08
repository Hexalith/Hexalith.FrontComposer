---
title: '11.5 Dead-CSS remediation and visual-conformance guards'
type: 'feature'
created: '2026-07-06T22:29:05+02:00'
status: 'in-progress'
baseline_revision: 'd620581a836625fcd63faeceb01e7ca6c13f1f33'
review_loop_iteration: 0
followup_review_recommended: false
context:
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
- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` -- add the three Story 11.5 governance guards with focused positive/negative regex or fixture rows -- closes the unlinked stylesheet, dead scoped CSS, and `--error*` token blind spots before or with remediation.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor`, `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor`, and `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor.css` -- move status/pulse and gear-pin styling to reachable raw roots, `::deep`, or inline/component parameters while preserving existing attributes and selectors -- restores connection and grid affordance visuals.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor`, and `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor.css` -- move mobile Done and preview layout styling to reachable raw roots, `::deep`, or inline/component parameters -- restores settings and density preview visuals.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor`, and `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor.css` -- move Fluent-root DevMode styling to reachable roots and replace `--error` with Fluent 2 error/status tokens -- restores DevMode visuals and token conformance.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldPlaceholder.razor.css` and `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.css` -- replace remaining `--error*` variables with Fluent 2 error/status tokens or component parameters -- removes undefined FAST-era token usage.
- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcProjectionConnectionStatusTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcColumnPrioritizerTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityPreviewPanelTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeVisualReachabilityTests.cs` -- assert rendered DOM carries reachable roots, inline styles, or `::deep` source anchors for every fixed component -- provides default-lane proof that source-string-only checks missed.
- [ ] `tests/e2e/specs/specimen-accessibility.spec.ts` or an adjacent focused e2e spec -- add computed-style or behavior proof for the reconnect pulse/reduced-motion path and at least one Fluent-root remediation representative; record visual-governance/a11y artifact status -- satisfies E8-AI-1 visual evidence.
- [ ] `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md` and `sprint-status.yaml` -- record status transitions, file list, visual evidence checklist results, and validation commands -- keeps BMAD artifacts auditable.

**Acceptance Criteria:**
- Given the seven scoped-CSS files named by Story 11.5, when the focused component tests and Governance lane run, then no class defined only in component-scoped CSS remains assigned only to a Fluent component root.
- Given Shell global stylesheets under `wwwroot/css`, when a stylesheet is added or renamed, then `FluentConformanceTests` fails unless `FrontComposerShell` links it through `HeadContent` and a path property.
- Given Shell CSS contains `--error`, `--error-foreground-rest`, or another `--error-*` token, when the legacy-token guard runs, then the build fails until the token is migrated to Fluent 2 semantics.
- Given the reconnecting projection status, column prioritizer, settings dialog mobile Done button, density preview, and DevMode surfaces render, when visual evidence is collected, then each changed visual has rendered-DOM or computed-style proof and no accessibility-critical affordance regresses.

## Spec Change Log

## Review Triage Log

## Design Notes

Prefer the Story 8.6 `FcPageToolbar` precedent: if the class is on a Fluent component and the rendered node cannot receive the component scope attribute, use a raw wrapper plus `::deep` for descendants, or move layout onto `Style`/component parameters where the style must land on the Fluent element itself. Do not let the scoped-CSS detector become a broad ban on Fluent `Class=`; it should fail only when a same-component `.razor.css` selector would be dead.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FluentConformanceTests"` -- expected: Story 11.5 guards green and self-tests cover the new patterns.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FcProjectionConnectionStatusTests|FullyQualifiedName~FcColumnPrioritizerTests|FullyQualifiedName~FcSettingsDialogTests|FullyQualifiedName~FcDensityPreviewPanelTests|FullyQualifiedName~FcDevMode"` -- expected: all fixed visual reachability pins pass.
- `npm --prefix tests/e2e run typecheck` -- expected: Playwright evidence additions compile.
- `npm --prefix tests/e2e run test:a11y` -- expected: accessibility, media, zoom, and visual specimen gate passes or exact local blocker plus named CI responsibility is recorded.
- `npm --prefix tests/e2e run validate:visual-governance` -- expected: visual baseline governance passes.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- expected: 0 warnings, 0 errors.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md` -- expected: story artifact valid after implementation evidence is appended.
- `git diff --check` -- expected: clean.
