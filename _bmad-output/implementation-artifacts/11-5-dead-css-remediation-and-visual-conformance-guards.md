---
created: 2026-07-09
epic: 11
story: 5
story_key: 11-5-dead-css-remediation-and-visual-conformance-guards
source_epics: _bmad-output/planning-artifacts/epics.md
baseline_commit: 0c7e5c74f18b2a5c11c70a77a727713373720964
status: in-progress
---

# Story 11.5: Dead-CSS Remediation and Visual-Conformance Guards

Status: in-progress

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a FrontComposer maintainer,
I want Fluent-root scoped CSS defects, undefined error tokens, and stylesheet-link drift guarded by rendered and browser evidence,
so Shell visual regressions fail in CI instead of shipping as silent no-op styles.

## Acceptance Criteria

1. Given the Shell components `FcProjectionConnectionStatus`, `FcColumnPrioritizer`, `FcSettingsDialog`, `FcDensityPreviewPanel`, `FcDevModeAnnotation`, `FcDevModeToggleButton`, and `FcDevModeOverlay`, when the story is complete, then each affected scoped style is reachable through a raw scoped root, `::deep` descendant selector, inline style, or component parameter, and the evidence includes rendered-DOM or computed-style proof rather than source-string-only assertions. (M6)

2. Given Shell CSS uses legacy or undefined error tokens, when the governance lane runs, then `--error`, `--error-background*`, `--error-foreground*`, and bare `error-background*` / `error-foreground*` usages fail until migrated to Fluent 2 red/status tokens such as `--colorPaletteRed*` or component-supported error colors. (M5)

3. Given Shell visual governance runs, when `wwwroot/css` files drift, scoped CSS classes are assigned only to Fluent component roots, or legacy error tokens are introduced, then the build fails with actionable file/class/token details. The guards must not become a blanket ban on Fluent `Class=` when the style is reachable by raw wrapper, `::deep`, inline style, or component parameter.

## Tasks / Subtasks

- [x] Reconfirm the current brownfield state before editing. (AC: 1, 2, 3)
  - [x] Read this story, `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md`, Epic 11 in `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md`, and `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` before changing code.
  - [x] Read every UPDATE candidate listed in "Current Files To Read Before Editing" completely before editing it.
  - [x] Classify the dirty worktree before editing and preserve unrelated changes; do not revert existing release/sprint/submodule state.
  - [x] Treat the current implementation as possibly already partially complete. If a requirement is already satisfied, confirm and pin it with evidence instead of duplicating wrappers, selectors, or tests.

- [x] Strengthen or verify the three Story 11.5 governance guards first. (AC: 2, 3)
  - [x] In `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`, ensure `Shell_wwwroot_css_files_are_linked_through_frontcomposer_shell` enumerates Shell `wwwroot/css/*.css` files and fails when `FrontComposerShell` does not expose a matching `<link>`.
  - [x] Ensure `Shell_scoped_css_does_not_target_classes_only_on_fluent_component_roots` fails when a same-component scoped-CSS selector targets a class assigned only to a `Fluent*` component root and passes when the style is reachable through a raw scoped wrapper, `::deep`, inline style, or component parameter.
  - [x] Ensure the legacy-token guard catches `--error`, `--error-background*`, `--error-foreground*`, bare `error-background*`, and bare `error-foreground*`, while allowing Fluent 2 tokens such as `--colorPaletteRedBorder2`, `--colorPaletteRedBackground3`, and `--colorPaletteRedForeground1`.
  - [x] Keep failure messages actionable with the offending file path, class/token, and suggested remediation pattern.

- [x] Confirm or remediate the seven scoped-CSS surfaces. (AC: 1)
  - [x] `FcProjectionConnectionStatus`: keep status/pulse styling reachable from a raw host and preserve reduced-motion handling.
  - [x] `FcColumnPrioritizer`: keep the gear/popover visual hook reachable from a raw prioritizer root and preserve accessible button labeling.
  - [x] `FcSettingsDialog`: keep the footer and mobile Done button styling reachable from a raw footer root and preserve dialog/accordion behavior.
  - [x] `FcDensityPreviewPanel`: keep the density preview layout styling reachable from a raw wrapper and preserve density preview selectors used by browser evidence.
  - [x] `FcDevModeAnnotation`, `FcDevModeToggleButton`, and `FcDevModeOverlay`: keep DevMode annotation, toggle, drawer, source, copy, and icon-button styling reachable from raw scoped roots or `::deep` descendants.
  - [x] Do not replace Fluent interactive controls with raw buttons, inputs, or custom controls; repo UX rules require Fluent components for interactive UI.

- [x] Confirm or migrate legacy error-token usage. (AC: 2)
  - [x] Search the source tree, test tree, and Shell CSS for `--error`, `--error-background`, `--error-foreground`, `error-background`, and `error-foreground`.
  - [x] Replace source CSS usages with Fluent 2 red/status tokens or component-supported error semantics; representative current targets include `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldPlaceholder.razor.css`, `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.css`, DevMode scoped CSS, and `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-empty-state.css`.
  - [x] Keep test fixture strings that intentionally assert the guard catches legacy tokens, but make their intent clear.

- [x] Pin rendered-DOM and browser/computed-style evidence. (AC: 1, 3)
  - [x] Maintain or add focused bUnit coverage in `FcProjectionConnectionStatusTests`, `FcColumnPrioritizerTests`, `FcSettingsDialogTests`, `FcDensityPreviewPanelTests`, and `FcDevModeVisualReachabilityTests` proving the rendered DOM exposes raw roots and descendants that make the scoped selectors reachable.
  - [x] Maintain or add browser evidence in `tests/e2e/specs/specimen-accessibility.spec.ts` for computed style, responsive/mobile state, and `prefers-reduced-motion` behavior. Source-string assertions alone are not enough for this story.
  - [x] If browser execution is locally blocked, record the exact command, exact blocker, CI lane/owner/artifact that will supply the browser evidence, and named responsibility in the story completion notes.

- [x] Validate and reconcile artifacts before review. (AC: 1, 2, 3)
  - [x] Run the focused governance lane for `FluentConformanceTests`.
  - [x] Run focused Shell component lanes for the affected test classes.
  - [x] Run the e2e typecheck and the relevant Playwright/browser lane when feasible.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` unless a local environment blocker occurs; if blocked, record exact command, exact blocker, and focused fallback evidence.
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release` and `git diff --check`.
  - [x] Run `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md`.
  - [x] Reconcile the story File List and Completion Notes against actual `git diff --name-only`; include `spec-11-5-dead-css-remediation-and-visual-conformance-guards.md` if it is updated during implementation.

### Review Findings

- [x] [Review][Patch] Reopen Story 11.5 and rebuild its evidence from a clean baseline — The fixed baseline expanded to 353 files at the review-start `HEAD`, while the prior review-promotion commit `289bb099` also changed `references/Hexalith.Builds` and `references/Hexalith.Commons` despite the story claiming only the story and sprint files were dirty. User decision applied: reopened the story at clean baseline `0c7e5c74f18b2a5c11c70a77a727713373720964`, reran the required evidence, and kept the story in progress because the broad solution lane has one unrelated package-inventory governance failure.
- [x] [Review][Patch] Synchronize the prior Story 11.5 spec as the rework contract — User decision applied: the prior spec remains active, now shares the clean rework baseline and records the rebuilt evidence and blocker.
- [x] [Review][Patch] Add normal-motion computed-style coverage for the reconnect pulse [tests/e2e/specs/specimen-accessibility.spec.ts:274]

## Dev Notes

### Story Context

Epic 11 is architecture-review remediation for post-MVP release hardening. Story 11.5 specifically closes the visual-conformance findings around dead scoped CSS on Fluent component roots, undefined/FAST-era error tokens, and `wwwroot/css` link drift. It follows the authoritative Epic 11 order after Stories 11.1 through 11.4 are complete; do not infer order from numeric or file sorting. [Source: `_bmad-output/planning-artifacts/epics.md`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

The source planning artifacts explicitly warn that bUnit source-string checks are insufficient for this defect class because Fluent component roots may not receive the Blazor CSS-isolation scope attribute. The story must therefore include rendered DOM, computed style, or browser evidence for every repaired visual hook. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`; `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md`]

There is already a prior automation artifact at `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md`. Treat it as useful context and intent, not as a substitute for this standard ready-for-dev story. If implementation updates the spec artifact, keep this story's File List and notes reconciled.

### Brownfield Implementation Snapshot

Current rework inspection on 2026-07-11 found much of Story 11.5 already present in the working tree. The dev agent must verify these facts against the live files before relying on them:

- `FcProjectionConnectionStatus.razor` currently wraps the `FluentMessageBar` in a raw `div.fc-projection-connection-status-host`; its scoped CSS targets `.fc-projection-connection-status-host ::deep .fc-projection-connection-status` and `.fc-projection-connection-status-host ::deep .fc-projection-connection-status-pulse`, including a reduced-motion rule.
- `FcColumnPrioritizer.razor` currently has a raw `div.fc-column-prioritizer`; its scoped CSS targets `.fc-column-prioritizer ::deep .fc-column-prioritizer-gear`.
- `FcSettingsDialog.razor` currently has a raw `div.fc-settings-footer`; its scoped CSS targets `.fc-settings-footer ::deep .fc-settings-done`, including mobile width behavior.
- `FcDensityPreviewPanel.razor` currently has a raw `.fc-density-preview-wrapper`; its scoped CSS targets `.fc-density-preview-wrapper ::deep .fc-density-preview`.
- The three DevMode components currently use raw hosts/drawer roots and `::deep` selectors for annotation, unsupported state, toggle, drawer source, copy button, and icon button styling.
- `FcFieldPlaceholder.razor.css`, `FcDestructiveConfirmationDialog.razor.css`, DevMode CSS, and `wwwroot/css/fc-empty-state.css` currently use Fluent 2 red palette tokens such as `--colorPaletteRedBorder2` or `--colorPaletteRedBackground3`, not raw `--error` tokens.
- `FrontComposerShell.razor` and `FrontComposerShell.razor.cs` currently expose links/path properties for Shell, density, projection, and empty-state CSS.
- `FluentConformanceTests.cs` currently includes the stylesheet-link guard, the scoped-CSS-on-Fluent-root detector, and a `LegacyErrorToken` regex that covers `--error*` and bare error foreground/background spellings.
- Focused component tests and `tests/e2e/specs/specimen-accessibility.spec.ts` currently include Story 11.5 visual reachability/computed-style checks.

Do not assume "already present" means "done." Confirm the guards are strong, run the evidence lanes, fill any missed cases, and update story/spec/sprint artifacts honestly.

### Current Files To Read Before Editing

Read each likely UPDATE file completely before changing it:

- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` - primary home for package, token, stylesheet-link, and scoped CSS governance.
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor`
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor`
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldPlaceholder.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.css`
- `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-empty-state.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcProjectionConnectionStatusTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcColumnPrioritizerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityPreviewPanelTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeVisualReachabilityTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/VisualReachabilityTestSupport.cs`
- `tests/e2e/specs/specimen-accessibility.spec.ts`

### Architecture Compliance

- Keep the fix inside the Shell UI/governance/testing surface. Do not change Contracts kernel, route contracts, MCP lifecycles, EventStore auth/realtime behavior, package versions, generated output, or sibling submodules for this story.
- Preserve .NET 10 / C# 14 repo standards, nullable-enabled code, `TreatWarningsAsErrors`, central package management, `.slnx` solution commands, and one C# type per file for any new top-level type.
- Use Fluent UI Blazor v5 and the current repo pins: `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` `5.0.0-rc.4-26180.1` from `references/Hexalith.Builds/Props/Directory.Packages.props`; `global.json` currently pins SDK `10.0.301`.
- Do not add raw interactive HTML controls where Fluent components exist. Raw elements are appropriate as scoped CSS hosts/wrappers only.
- Do not redefine the app theme or resurrect legacy FAST token names. Use Fluent 2 token names already present in the codebase, such as `--colorNeutral*`, `--colorBrand*`, and `--colorPaletteRed*`.
- Do not weaken `FluentConformanceTests` to make the current code pass. Tighten the guard or add narrow allow logic only when the rendered style is genuinely reachable.
- Do not initialize nested submodules, use recursive submodule commands, or modify `references/Hexalith.*` paths for this story.

### Anti-Patterns To Avoid

- Do not rely only on reading `.razor` or `.razor.css` source text as proof that styles render.
- Do not put an isolated CSS class only on a `Fluent*` component root unless the style lands through inline `Style`, a component parameter, or a provably reachable descendant selector.
- Do not introduce broad regex bans that block legitimate words containing `error` or all Fluent `Class=` usage.
- Do not replace Fluent component semantics with custom raw controls to make CSS easier.
- Do not mark browser evidence complete if Playwright was skipped without documenting the exact blocker and CI owner/artifact.
- Do not edit unrelated release-gate, CI-alignment, or submodule state currently present in the worktree.

### Testing Requirements

Minimum focused .NET lanes:

```bash
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~FluentConformanceTests"
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~FcProjectionConnectionStatusTests|FullyQualifiedName~FcColumnPrioritizerTests|FullyQualifiedName~FcSettingsDialogTests|FullyQualifiedName~FcDensityPreviewPanelTests|FullyQualifiedName~FcDevModeVisualReachabilityTests"
```

Browser/e2e evidence should include the relevant Playwright lane from `tests/e2e` for `specimen-accessibility.spec.ts` after confirming the local script names in `tests/e2e/package.json`. At minimum, run the e2e typecheck if browsers are unavailable.

Required story artifact gate before review:

```bash
python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md
```

Required broad lane when feasible:

```bash
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
dotnet build Hexalith.FrontComposer.slnx -c Release
git diff --check
```

If local VSTest sockets, restore, browser install, Playwright browser execution, vulnerability feed access, or environment permissions block validation, record the exact command and exact failure. Do not mark validation complete without focused fallback evidence.

### Latest Technical Information

- Microsoft Learn's current ASP.NET Core Blazor CSS isolation guidance says `::deep` applies styles to descendant elements under the generated scope identifier, and that the scoped attribute is otherwise applied to the right-most element. It also states scoped CSS applies to HTML elements rather than Razor components, which supports the raw wrapper plus `::deep` remediation pattern. Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0
- The Fluent UI Blazor v5 migration guide reports that v5 moved all components to `FluentComponentBase`; the `FluentButton` component documentation exposes `Class` and `Style` parameters, so reachable `Class=` and inline `Style` remain valid remediation tools when used with proof. Source: Fluent UI Blazor MCP migration guide and `FluentButton` docs; public docs site: https://www.fluentui-blazor.net/
- Fluent 2 web alias color tokens include `colorPaletteRedBackground*`, `colorPaletteRedForeground*`, and `colorPaletteRedBorder*`, supporting migration away from `--error*` token names. Source: https://fluent2.microsoft.design/color-tokens2/
- Playwright `page.emulateMedia` supports `forcedColors` and `reducedMotion`, including `reducedMotion: "reduce"`, which is appropriate for computed-style evidence around the connection-status pulse. Source: https://playwright.dev/docs/api/class-page#page-emulate-media

### Previous Story Intelligence

Stories 11.2 through 11.4 were completed through dev/review automation and repeatedly exposed the need for direct focused evidence plus artifact reconciliation. Keep the File List, Completion Notes, and test evidence in sync with the actual diff before review.

Story 11.4 validation passed focused Contracts/Shell/Testing lanes, e2e typecheck, story validation, diff check, and Release build after review patches. Treat that as the current bar for Epic 11 evidence quality when feasible.

Story 11.2 and 11.3 previously encountered unrelated standard-lane blockers in SourceTools/IDE parity while focused lanes passed. If this happens again, record the exact blocker instead of broadening Story 11.5 scope.

### Git Intelligence

The 2026-07-09 review-promotion attempt used baseline `d02f2b423719950d220332820a85b800464a78ec` and was later found to share commits with unrelated release and submodule-pointer changes. The 2026-07-11 review reopened the story by user decision and established clean baseline `0c7e5c74f18b2a5c11c70a77a727713373720964` before applying the review patches.

### Documented Blockers

- `eng/release-package-inventory.json` - The broad filtered solution lane fails because the packable `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj` project is missing from the release inventory. This baseline package-boundary work is outside Story 11.5 and aligns with Story 11.14 ownership; Story 11.5 remains in progress until the broad gate is green.

## Documented Unrelated Changes

None at the 2026-07-11 rework baseline.

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md`.
- Sprint-status key: `11-5-dead-css-remediation-and-visual-conformance-guards`.
- Prior automation context file: `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md`.
- Primary implementation area: `src/Hexalith.FrontComposer.Shell/Components/` and `src/Hexalith.FrontComposer.Shell/wwwroot/css/`.
- Primary test area: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/`, `tests/Hexalith.FrontComposer.Shell.Tests/Components/`, and `tests/e2e/specs/`.
- Avoid `docs/_site/**`, `obj/**`, generated output, package version files, Contracts split files, MCP artifacts, release-gate artifacts, CI-alignment artifacts, and submodule paths unless the user explicitly redirects the work.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 11 source of record, Story 11.5 acceptance criteria, and authoritative implementation order.
- Source: `_bmad-output/planning-artifacts/prd.md` - FR29, NFR-3, NFR-4, NFR-6, NFR-11, and release-hardening context.
- Source: `_bmad-output/planning-artifacts/prd-addendum-2026-07-05.md` - post-readiness remediation context.
- Source: `_bmad-output/planning-artifacts/architecture.md` - Shell UI architecture, Fluent v5 rules, and governance expectations.
- Source: `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` - M5/M6 visual governance findings.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md` - original Story 11.5 framing.
- Source: `_bmad-output/planning-artifacts/ux-design.md`, `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`, and `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` - accessibility, density, and visual evidence constraints.
- Source: `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md` - rendered DOM/computed-style evidence requirements.
- Source: `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` - File List and evidence reconciliation requirements.
- Source: `_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md` - public/behavioral doc drift check.
- Source: `_bmad-output/implementation-artifacts/epic-11-context.md` - Epic 11 carry-forward context.
- Source: `_bmad-output/implementation-artifacts/spec-11-2-projection-realtime-resilience.md`, `_bmad-output/implementation-artifacts/spec-11-3-mcp-cross-request-lifecycle-and-operability.md`, and `_bmad-output/implementation-artifacts/spec-11-4-security-validation-hardening.md` - prior Story 11 implementation/review lessons.
- Source: `_bmad-output/project-context.md` - project stack, coding, testing, docs, and submodule rules.
- Source: `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` - repository-wide LLM instructions.
- Source: `references/Hexalith.AI.Tools/hexalith-ux-instructions.md` - UI/UX implementation rules for Fluent v5 and visual evidence.
- Source: Microsoft Learn Blazor CSS isolation - https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0
- Source: Fluent UI Blazor docs site - https://www.fluentui-blazor.net/
- Source: Fluent 2 color tokens - https://fluent2.microsoft.design/color-tokens2/
- Source: Playwright `page.emulateMedia` API - https://playwright.dev/docs/api/class-page#page-emulate-media

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-09: Create-story analysis loaded root AGENTS instructions, Hexalith LLM instructions, BMAD create-story workflow/config/discovery/template/checklist, project context, submodule context files, sprint status, Epic 11 source, PRD/addendum, architecture, UX artifacts, architecture quality review, sprint change proposals, visual/review/doc-drift checklists, prior Story 11 artifacts, current Story 11.5 spec artifact, live Shell UI/governance/e2e files, current package pins, current dirty worktree, Microsoft Learn Blazor CSS isolation guidance, Fluent UI Blazor MCP migration/component docs, Fluent 2 color token guidance, and Playwright media emulation guidance.
- 2026-07-09: Confirmed no standard Story 11.5 file existed before creation; `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md` existed as an earlier automation/spec context file.
- 2026-07-09: Confirmed `11-5-dead-css-remediation-and-visual-conformance-guards` was the next authoritative backlog story in `sprint-status.yaml`; Epic 11 was already `in-progress`.
- 2026-07-09: Current code inspection found most intended Story 11.5 remediation and guards already present, so the story deliberately frames implementation as confirm-and-pin/reconcile work rather than a greenfield rewrite.
- 2026-07-09: Dev-story activation loaded BMAD workflow/config, root project context and project-context persistent facts, Hexalith LLM/UX instructions, complete sprint status, complete Story 11.5, spec artifact, Epic 11 source section, visual evidence checklist, story reconciliation checklist, and every listed update candidate before implementation edits.
- 2026-07-09: Reclassified worktree after story activation; only this story file and `sprint-status.yaml` were dirty from the dev-story status transition. No unrelated release, CI, or submodule paths were dirty in the live worktree.
- 2026-07-09: Confirmed current Shell UI/governance/e2e implementation already satisfies Story 11.5. No production or test source changes were required beyond story/sprint evidence updates.
- 2026-07-09: Validation evidence: focused governance 39/39 passed; focused affected component lane 31/31 passed; e2e typecheck passed; Playwright `test:a11y` passed 21/21 including Story 11.5 computed-style/reduced-motion evidence; visual governance validator passed with no committed visual baseline changes; broad filtered solution lane passed; Release build passed 0 warnings/0 errors; `git diff --check` passed.
- 2026-07-11: Review rework established clean baseline `0c7e5c74f18b2a5c11c70a77a727713373720964`; current changed-file reconciliation is limited to the canonical story, prior spec, sprint status, and Story 11.5 Playwright evidence.
- 2026-07-11: Normal-motion browser proof now requires the CSS-isolated `fc-sync-status-pulse-*` animation name, `0.7s` duration, `24` iterations, and `alternate` direction before separately proving reduced motion yields `none` / `0s`.
- 2026-07-11: Dev-story completion revalidation started from clean `main` at `f1d8d73edc7fe69cf3cc3220ec5b29f144c55c37`. Focused governance passed 39/39, affected component tests passed 31/31, e2e typecheck passed, visual-baseline governance passed with no committed baseline drift, and Chromium accessibility/visual evidence passed 22/22.
- 2026-07-11: The broad filtered solution lane failed outside Story 11.5: Shell passed 2214/2216, with the known package-inventory failure plus a polling-driver timeout; Testing passed 56/57 because the clean packed consumer could not restore `Hexalith.FrontComposer.Contracts.UI`; all other executed projects passed. The polling-driver test passed 1/1 on focused rerun, while the package inventory and packed-consumer failures reproduced deterministically.
- 2026-07-11: `aspire start --apphost src/Hexalith.FrontComposer.AppHost --non-interactive --format Json` could not establish the runtime baseline because the current Tenants submodule still references `FcShellOptions`; a stop/retry removed initial lock noise and reproduced four `CS0246` errors in `Hexalith.Tenants.UI`. No AppHost was left running.
- 2026-07-11: Reconciliation against baseline `0c7e5c74` found that commit `f1d8d73e` also advanced `references/Hexalith.Memories` from `c5a999e1` to `eb959d7f`. This committed pointer change is unrelated to Story 11.5 and was not modified or reverted during this run.

### Implementation Plan

- Confirm-and-pin, not greenfield remediation: verify the already-present raw scoped hosts, `::deep` selectors, Fluent 2 red tokens, stylesheet-link guard, scoped-CSS Fluent-root detector, and browser computed-style evidence.
- Keep Story 11.5 source behavior unchanged because the current implementation and tests already satisfy all acceptance criteria.
- Reconcile only BMAD story/sprint artifacts after validation.

### Completion Notes List

- The 2026-07-09 review-promotion evidence below is retained as historical context but was superseded after review found a mixed completion range and an undocumented `Hexalith.Commons` submodule pointer change.
- Story context created by BMAD create-story workflow on 2026-07-09.
- Story status set to `ready-for-dev`.
- Sprint status updated so Story 11.5 is `ready-for-dev`.
- No source code was changed by story creation.
- Dev-story confirm-and-pin completed on 2026-07-09. The seven scoped-CSS surfaces use reachable raw roots or `::deep` descendants, legacy error-token usage is absent from Shell source CSS, and Story 11.5 governance/browser evidence is already present and passing.
- Visual component evidence checklist:
  - Required: yes.
  - Rendered DOM attachment: focused bUnit component lane passed 31/31 for `FcProjectionConnectionStatusTests`, `FcColumnPrioritizerTests`, `FcSettingsDialogTests`, `FcDensityPreviewPanelTests`, and `FcDevModeVisualReachabilityTests`.
  - Scoped CSS / Fluent targeting: `FluentConformanceTests` passed 39/39, including stylesheet-link drift, scoped-CSS Fluent-root detection, legacy error-token, and Shell accent-as-thread guards.
  - Computed style / behavior: `npm --prefix tests/e2e run test:a11y` passed 21/21, including `story 11.5 scoped Fluent-root visual hooks are reachable` and reduced-motion pulse evidence.
  - Accessibility interaction: the same Playwright lane passed keyboard, focus-visible, forced-colors/reduced-motion, zoom/reflow, status icon focus/hover/touch, and axe checks.
  - Shell accent-as-thread guard: passed through `FluentConformanceTests`.
  - Visual/browser lane: local Chromium lane passed; no CI handoff blocker.
  - Snapshot/baseline intent: no committed visual baseline changes detected by `npm --prefix tests/e2e run validate:visual-governance`.
- Broad validation passed: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`, `dotnet build Hexalith.FrontComposer.slnx -c Release`, and `git diff --check`.
- Story artifact validation passed: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md`. The validator reported the already documented pre-existing unrelated baseline-diff paths only.
- Completion revalidation on 2026-07-11 reconfirmed every Story 11.5-focused gate: governance 39/39, affected components 31/31, e2e typecheck, Chromium 22/22, and visual-baseline governance all passed.
- Story 11.5 remains `in-progress` because the mandatory broad regression gate is not green. The deterministic blockers are the unlisted/unpacked `Hexalith.FrontComposer.Contracts.UI` package in release/package-consumer validation; the isolated Shell polling timeout passed on focused rerun and is recorded as transient evidence, not as a green broad lane.
- No production, test, package-inventory, or submodule content was changed by this dev-story run. Updating `eng/release-package-inventory.json` or package publication wiring is outside Story 11.5 and remains owned by Story 11.14.

### 2026-07-11 Review Rework Evidence

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
| --- | --- | --- | --- | --- | --- |
| Focused governance | `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FluentConformanceTests"` | Passed, 39/39 | None | Not needed | Local required lane |
| Affected components | `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FcProjectionConnectionStatusTests\|FullyQualifiedName~FcColumnPrioritizerTests\|FullyQualifiedName~FcSettingsDialogTests\|FullyQualifiedName~FcDensityPreviewPanelTests\|FullyQualifiedName~FcDevModeVisualReachabilityTests"` | Passed, 31/31 | None | Not needed | Local required lane |
| E2E typecheck | `npm --prefix tests/e2e run typecheck` | Passed | None | Not needed | Local required lane |
| Browser/a11y/visual | `npm --prefix tests/e2e run test:a11y` | Passed, 22/22 | None | Focused Story 11.5 test also passed 1/1 | Local Chromium lane |
| Visual baseline governance | `npm --prefix tests/e2e run validate:visual-governance` | Passed; no committed baseline changes | None | Not needed | Local required lane |
| Broad filtered solution | `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` | Failed; Shell 2215/2216 with one baseline package-inventory governance failure | During `CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable`; `eng/release_evidence.py inventory` reports `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj: unexpected packable project missing from release package inventory` | Story-focused governance 39/39, components 31/31, browser 22/22; Contracts 200/200, Contracts.UI 9/9, CLI 67/67, MCP 372/372, and SourceTools 1062/1062 passed in the broad run | Not overridden; story remains in progress |
| Release build | `dotnet build Hexalith.FrontComposer.slnx --configuration Release` | Passed, 0 warnings / 0 errors | None | Not needed | Local required lane |
| Artifact reconciliation | `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md` | Passed | None | Not needed | Local hard gate |
| Diff hygiene | `git diff --check` | Passed; line-ending normalization warnings only | None | Not needed | Local required lane |

### Change Log

- 2026-07-11: Revalidated Story 11.5 focused and browser evidence; kept the story in progress after deterministic Contracts.UI release/package-consumer regression failures, recorded the AppHost/Tenants baseline blocker, and reconciled the unrelated committed Memories pointer.
- 2026-07-11: Reopened by user decision during code review; reset to clean baseline `0c7e5c74f18b2a5c11c70a77a727713373720964`, synchronized the prior spec for rework, and added normal-/reduced-motion reconnect-pulse browser evidence.
- 2026-07-09: Confirmed Story 11.5 implementation and evidence already present; updated story and sprint tracking to `review` after successful focused, browser, broad test, Release build, visual governance, and diff-check validation.

### File List

Changed from rework baseline:

- `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/spec-11-5-dead-css-remediation-and-visual-conformance-guards.md`
- `tests/e2e/specs/specimen-accessibility.spec.ts`

Committed unrelated pointer change present in the rework range (not modified by this run):

- `references/Hexalith.Memories` - advanced from `c5a999e1` to `eb959d7f` in commit `f1d8d73e`; unrelated to Story 11.5 and preserved as found.

Pre-existing evidence verified during rework (unchanged from baseline):

- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` - pre-existing evidence verified during rework, unchanged.
- `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md` - pre-existing evidence verified during rework, unchanged.
- `_bmad-output/planning-artifacts/epics.md` - pre-existing evidence verified during rework, unchanged.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` - pre-existing evidence verified by the 39/39 focused governance lane, unchanged.
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldPlaceholder.razor.css` - pre-existing evidence verified during rework, unchanged.
- `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.css` - pre-existing evidence verified during rework, unchanged.
- `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-empty-state.css` - pre-existing evidence verified during rework, unchanged.
