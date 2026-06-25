---
baseline_commit: 8f90a5f
---

# Story 8.4: Compact default density + grid polish

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-25. -->

## Story

As an operator,
I want a compact default density and Aspire-dense projection grids,
so that more data is readable at a glance, while I can still change density.

## Acceptance Criteria

1. Given a fresh Desktop or CompactDesktop session with no stored density preference and no adopter `FcShellOptions.DefaultDensity`, when the shell settles after the viewport watcher emits, then `FrontComposerDensityState.EffectiveDensity` and the body `data-fc-density` are `Compact`; Tablet/Phone still force `Comfortable` for touch targets. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.4-Compact-default-density-grid-polish; src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs; src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs]
2. Given `FcSettingsDialog`, when the user selects Compact, Comfortable, or Roomy, then the live preference path still dispatches `UserPreferenceChangedAction`, persists the user preference, re-applies after reload at Desktop/CompactDesktop, and `Restore defaults` clears the preference back to the new factory Compact default rather than a hard-coded Comfortable fallback. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.cs; tests/e2e/specs/settings-persistence.spec.ts]
3. Given generated projection grids, when rendered under Compact density, then the grid uses Aspire-dense spacing with coherent row height and virtualization math: visual row height, `FluentDataGrid.ItemSize`, and `DataGridDensityMetrics.ResolveRowHeightPx` agree; row hover uses `--colorSubtleBackgroundHover`; cell/header padding is density-coupled through `--fc-spacing-unit`; no legacy Fluent v4/FAST token, raw interactive control, or accent surface background is introduced. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.4-VR-5-Compact-default-density-grid-polish; src/Hexalith.FrontComposer.Shell/Components/Rendering/DataGridDensityMetrics.cs; src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
4. Given the generated `FluentDataGrid`, when the emitter output is inspected or rendered, then projection grids generate sticky headers using the pinned Fluent v5 RC API `GenerateHeader = DataGridGeneratedHeaderType.Sticky`; do not use the older v4 `GenerateHeaderOption.Sticky` name. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml#FluentDataGrid.GenerateHeader]

## Tasks / Subtasks

- [x] Audit density defaults before editing (AC: 1, 2)
  - [x] Read `DensityPrecedence.cs`, `FrontComposerDensityFeature.cs`, `DensityEffects.cs`, `FcSettingsDialog.razor.cs`, `FcShellOptions.cs`, and current density tests.
  - [x] Identify all hard-coded Comfortable fallback assumptions and update only those that represent the factory default; preserve ADR-040 Tablet/Phone forcing.
  - [x] Do not change persistence schema: stored values remain `DensityLevel?` under `{tenantId}:{userId}:density`.
- [x] Change unset desktop factory density to Compact (AC: 1, 2)
  - [x] Update `DensityPrecedence.GetFactoryDefault(DensitySurface.Default)` so Desktop/CompactDesktop no-preference/no-deployment-default resolves to `DensityLevel.Compact`.
  - [x] Update `FrontComposerDensityFeature.GetInitialState()` only if tests prove first-render behavior must be Compact; if left Comfortable for bootstrap safety, explicitly pin the viewport-emitted Desktop recompute to Compact.
  - [x] Keep `tier <= ViewportTier.Tablet` returning `DensityLevel.Comfortable`.
  - [x] Keep adopter `FcShellOptions.DefaultDensity` above the factory default and below user preference.
  - [x] Update `FcSettingsDialog.IsForcedByViewport` and `RestoreDefaultsAsync` paths so reset/forced-note logic uses the resolver/new factory default rather than `?? DensityLevel.Comfortable`.
- [x] Add focused density tests (AC: 1, 2)
  - [x] Update `DensityPrecedenceTests`: `Default` surface + Desktop + no preferences resolves Compact; CompactDesktop resolves Compact; Tablet/Phone still resolve Comfortable even with Compact preference/default.
  - [x] Update `DensityEffectsTests`/`HydrationTests`: no stored value hydrates safely, and a later Desktop/CompactDesktop viewport action recomputes to Compact.
  - [x] Update `DensityFeatureTests`/`FcDensityApplierTests` only if the initial feature state changes.
  - [x] Update `FcSettingsDialogTests` for reset-to-default and forced-note behavior under the new unset default.
  - [x] Update e2e comments/assertions that still say the default density is Comfortable.
- [x] Polish generated projection grid density and hover (AC: 3)
  - [x] Read `DataGridDensityMetrics.cs`, `fc-density.css`, `fc-projection.css`, `ProjectionRoleBodyEmitter.cs`, and `RazorEmitterVirtualizationTests.cs`.
  - [x] Add a stable generated-grid class such as `fc-projection-grid` to every emitted projection `FluentDataGrid`; avoid styling every FluentDataGrid globally.
  - [x] Add density-coupled CSS in `fc-projection.css` for grid row/cell/header padding and subtle row hover using Fluent 2 tokens only.
  - [x] If the CSS changes actual row height, update `DataGridDensityMetrics.ResolveRowHeightPx` in the same change so `Virtualize` `ItemSize` remains truthful. Do not create CSS-only row-height drift.
  - [x] Preserve existing `.fc-row-expanded`, `.fc-expand-button`, `.fc-expand-in-row-detail`, and `.fc-reconciliation-sweep` behavior.
- [x] Emit sticky headers for generated grids (AC: 4)
  - [x] Verify current generated output lacks `GenerateHeader`; if still absent, update `ProjectionRoleBodyEmitter` for both `EmitStandardDataGrid` and `EmitStatusOverviewBody`.
  - [x] Emit `builder.AddAttribute(seq++, "GenerateHeader", Microsoft.FluentUI.AspNetCore.Components.DataGridGeneratedHeaderType.Sticky);`.
  - [x] Do not use `GenerateHeaderOption.Sticky`; that is the older v4 type name and will be wrong for the pinned v5 RC.
  - [x] Regenerate/update Verify snapshots only for intentional generated output changes.
- [x] Add grid emitter and CSS governance coverage (AC: 3, 4)
  - [x] Extend `RazorEmitterVirtualizationTests` or `RazorEmitterStrategyDispatchTests` to pin `Class`, `GenerateHeader`, `ItemSize`, and `SetKey(_density)` together.
  - [x] Add Shell test coverage for `DataGridDensityMetrics` row-height values and any changed compact/comfortable/roomy mapping.
  - [x] Add or extend a governance/static test that `fc-projection.css` uses `--fc-spacing-unit` and Fluent 2 hover token(s), while existing `FluentConformanceTests` remain green.
- [x] Preserve Epic 8 and repo boundaries (AC: 1, 3, 4)
  - [x] Do not implement Story 8.5 nav rail/flyout, Story 8.6 `FcPageToolbar`, or Story 8.7 status icons.
  - [x] Do not edit `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`, package versions, `.slnx` structure, PublicAPI baselines, pacts, MCP, CLI, schema fingerprint code, or generated output under `obj/`.
  - [x] Leave the existing unrelated modified `_bmad-output/story-automator/orchestration-8-20260625-123921.md` alone unless the dev-story workflow explicitly owns a new change to it.
- [x] Verify and record evidence (AC: 1, 2, 3, 4)
  - [x] Run `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false`.
  - [x] Run focused density/settings/grid lanes: `DensityPrecedenceTests`, `DensityEffectsTests`, `HydrationTests`, `FcSettingsDialogTests`, `FcDensityApplierTests`, `FluentConformanceTests`.
  - [x] Run SourceTools emitter lanes that cover generated `FluentDataGrid` output and update snapshots intentionally when the emitter changes.
  - [x] Run the solution default lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false`.
  - [x] Run relevant Playwright density/shell-grid coverage when feasible; record exact Kestrel/socket/browser blockers if local execution is blocked.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 8.4 commands, counts, and blockers.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: density already has a four-tier resolver: viewport force, user preference, adopter deployment default, then factory default. Story 8.4 changes the factory default for the Shell `Default` surface at Desktop/CompactDesktop; it must not remove the Tablet/Phone Comfortable accessibility floor. [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs; _bmad-output/project-context.md#Blazor-Shell-Fluxor-Rules-Consumer]
- Current no-preference Desktop factory default is Comfortable because `DensitySurface.Default` falls through to `_ => DensityLevel.Comfortable`; `DensitySurface.DataGrid` and `DevModeOverlay` already resolve Compact. The likely minimal source change is the `GetFactoryDefault` switch plus tests and any hard-coded dialog fallbacks. [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs; src/Hexalith.FrontComposer.Contracts/Rendering/DensitySurface.cs]
- `DensityEffects.GetHydrationTier()` intentionally caps placeholder Desktop to Tablet during `AppInitialized` because the browser has not measured the viewport yet. Do not delete that without proving no mobile/desktop mismatch. The story should be satisfied by the real Desktop/CompactDesktop `ViewportTierChangedAction` recomputing to Compact after measurement. [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs; tests/Hexalith.FrontComposer.Shell.Tests/State/HydrationTests.cs]
- `FcSettingsDialog.SelectedDensity` writes live changes by dispatching `UserPreferenceChangedAction`; reducers stay pure and only consume the pre-resolved action payload. Preserve that flow. Reset dispatches `UserPreferenceClearedAction` and must resolve the new effective value through `DensityPrecedence`, not by duplicating defaults. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.cs; tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityReducerPurityTest.cs]
- `fc-density.css` is a global static asset by design because CSS isolation would break `body[data-fc-density]` and shared `.fc-sr-only`. Keep density tokens there; put projection-grid selectors in `fc-projection.css`, which is also global because classes are emitted by SourceTools into adopter namespaces. [Source: src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-density.css; src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css]
- Generated projection grids already bind `Virtualize=true`, `DisplayMode=Table`, `ItemSize=DataGridDensityMetrics.ResolveRowHeightPx(_density)`, `OverscanCount=3`, `ItemKey`, and `builder.SetKey(_density)`. Preserve that shape. If row visual height moves toward an Aspire-dense value, `DataGridDensityMetrics` must move with it so virtualization does not overlap or gap rows. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs; src/Hexalith.FrontComposer.Shell/Components/Rendering/DataGridDensityMetrics.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs]
- Current `DataGridDensityMetrics` values are Compact `32f`, Comfortable `44f`, Roomy `56f`. The source docs call for an Aspire-dense "~46px" feel, so the dev must decide with tests whether this is a compact visual row target, a comfortable-row target, or only padding/hover polish. Do not leave metric comments saying one value while the implementation returns another. [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/DataGridDensityMetrics.cs; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.4-VR-5-Compact-default-density-grid-polish]
- `ProjectionRoleBodyEmitter` currently does not emit `GenerateHeader` in either the standard grid or status-overview grid path. The pinned Fluent v5 RC XML documents `FluentDataGrid<T>.GenerateHeader` with enum `DataGridGeneratedHeaderType`; `DataGridGeneratedHeaderType.Sticky` is the sticky-header member. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml#P-Microsoft-FluentUI-AspNetCore-Components-FluentDataGrid-1-GenerateHeader]
- Use Fluent v5 components and Fluent 2 tokens only. Valid tokens for this story include `--fc-spacing-unit` for density-coupled spacing and `--colorSubtleBackgroundHover` for row hover. Do not use `--design-unit`, `--neutral-*`, `--accent-*`, `--type-ramp-*`, raw interactive controls, or accent bridge variables in `background`/`background-color`. [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- Previous Story 8.3 completed with zero-config header preservation, optional logo API, and no later Epic 8 work. Do not disturb header slots, account/menu/actions, footer framing, or Story 8.2 accent-surface governance while changing density and grid styles. [Source: _bmad-output/implementation-artifacts/8-3-brand-logo-cell-in-header-start.md]
- No external web research is needed for package choice. Relevant versions are pinned locally: .NET SDK `10.0.301`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, and Playwright `1.61.0`. Do not change package versions or add dependencies. [Source: _bmad-output/project-context.md#Technology-Stack-Versions; Directory.Packages.props; global.json]

### Project Structure Notes

- Expected Shell production touch points:
  - `src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs`
  - `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityFeature.cs` only if first-render default changes
  - `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs` only if bootstrap/viewport recompute tests require it
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.cs`
  - `src/Hexalith.FrontComposer.Shell/Components/Rendering/DataGridDensityMetrics.cs`
  - `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css`
- Expected SourceTools production touch point:
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPrecedenceTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/State/HydrationTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityApplierTests.cs` if feature initial state changes
  - `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` for verification and any static CSS guard
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs`
  - SourceTools Verify snapshots only when emitter output intentionally changes
- Expected BMAD artifacts:
  - `_bmad-output/implementation-artifacts/8-4-compact-default-density-and-grid-polish.md`
  - `_bmad-output/implementation-artifacts/sprint-status.yaml`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Avoid touching:
  - `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`
  - `src/Hexalith.FrontComposer.Mcp/**`
  - `src/Hexalith.FrontComposer.Cli/**`
  - `src/Hexalith.FrontComposer.Schema/**`
  - `Directory.Packages.props`, `.slnx` structure, PublicAPI baselines, pacts, generated `obj/` output, nav rail/flyout files, page toolbar files, and status badge/icon emitters.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-8.4-Compact-default-density-grid-polish]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.4-VR-5-Compact-default-density-grid-polish]
- [Source: _bmad-output/project-docs/architecture.md#4-Runtime-composition-Shell]
- [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- [Source: _bmad-output/project-docs/architecture.md#4.3-Layout-component-policy-project-wide-guideline]
- [Source: _bmad-output/project-context.md]
- [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md]
- [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs]
- [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/DataGridDensityMetrics.cs]
- [Source: src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-density.css]
- [Source: src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- [Source: /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Create-story analysis loaded BMAD config, sprint status, Epic 8, the 2026-06-25 change proposal, project context, Hexalith UX rules, architecture sections 4.1/4.3, Story 8.3, current density state/effects/dialog files, current projection CSS, SourceTools grid emitter, emitter tests, Fluent v5 local package XML, recent git history, and current git status.
- 2026-06-25: Verified `ProjectionRoleBodyEmitter` already emits `ItemSize` from `DataGridDensityMetrics` and `SetKey(_density)`, but does not emit `GenerateHeader`.
- 2026-06-25: Confirmed pinned Fluent UI Blazor v5 RC uses `DataGridGeneratedHeaderType.Sticky` for `FluentDataGrid.GenerateHeader`; v4 `GenerateHeaderOption.Sticky` is not the right API for this repo.
- 2026-06-25: Dev-story loaded BMAD workflow/config/project-context and Hexalith UX rules, marked story in-progress, and preserved existing `baseline_commit`.
- 2026-06-25: RED phase confirmed missing behavior: focused Shell lane failed 8 expected tests and SourceTools virtualization lane failed 1 expected test before production changes.
- 2026-06-25: Validation: Release Shell.Tests build 0/0; Release SourceTools.Tests build 0/0; focused Shell 50/50; focused SourceTools virtualization 8/8; SourceTools approval 29/29; broad SourceTools 1026/1026; broad Shell non-Contract 1987/1987.
- 2026-06-25: Local blockers recorded: solution VSTest socket transport aborts with `SocketException (13)`, full Shell Contract/Pact mock server cannot start locally, and Playwright/Kestrel cannot bind sockets.
- 2026-06-25: QA generate e2e tests added fresh-session Compact default, Restore defaults back-to-Compact, Tablet forced-Comfortable, and generated-grid compact render-contract coverage; e2e typecheck passes after correcting the stale TypeScript deprecation floor, while focused Playwright execution remains blocked by Kestrel socket permissions.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.4 created as a density/grid polish story with explicit safe boundaries around ADR-040 viewport forcing, density persistence, Fluent v5 token governance, virtualization row-height coherence, and sticky-header API versioning.
- Validation pass completed against the create-story checklist; guardrails added for no hard-coded Comfortable fallback, no CSS-only row-height drift, no v4 Fluent sticky-header enum, no later Epic 8 scope, and root-submodule/no-package-change discipline.
- Implemented Compact as the unset Desktop/CompactDesktop factory density while preserving bootstrap Comfortable initial state and Tablet/Phone Comfortable forcing.
- Updated settings reset and forced-viewport logic to resolve through `DensityPrecedence` instead of duplicating a Comfortable fallback.
- Added generated-grid `fc-projection-grid` class, scoped projection CSS padding/hover polish with Fluent 2 tokens, and sticky headers via `DataGridGeneratedHeaderType.Sticky`.
- Added focused density, settings, metric, CSS governance, and SourceTools emitter coverage; intentionally updated SourceTools and Shell generated snapshots for the new grid attributes.

### File List

- `_bmad-output/implementation-artifacts/8-4-compact-default-density-and-grid-polish.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs`
- `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/DataGridDensityMetricsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPrecedenceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/HydrationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.BasicProjection_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DescriptionWithEscapeEdgeCases_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DisplayNameOverrides_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.EnumAndBadgeMappings_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.GuidTruncation_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.NullableProperties_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueNoEnumProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardWrongShapeProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.StatusOverviewProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.WhenStateTypoProjection_Approval.verified.txt`
- `tests/e2e/page-objects/settings.page.ts`
- `tests/e2e/specs/fc-tbl-contract.spec.ts`
- `tests/e2e/specs/settings-persistence.spec.ts`
- `tests/e2e/tsconfig.json`

### Change Log

- 2026-06-25: Implemented compact desktop factory density, generated-grid scoped polish, Fluent v5 sticky headers, focused regression coverage, and validation evidence for Story 8.4.
- 2026-06-25: Senior Developer Review (AI) — found and auto-fixed AC3 dead-CSS defect (`::part()` selectors do not match FluentDataGrid's light-DOM table; padding + row hover were no-ops, masked by a governance test pinning the broken selector string). Retargeted real table elements and hardened the governance test. Status review → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-25 · **Outcome:** Approve (after auto-fix)

### Scope verified

- File List reconciles with `git status --short`; the only extra working-tree change (`_bmad-output/story-automator/orchestration-8-20260625-123921.md`) is the one the story explicitly scopes out.
- **AC1 (Compact default)** — PASS. `DensityPrecedence.GetFactoryDefault(DensitySurface.Default)` now returns `Compact`; bootstrap remains `Comfortable` until the viewport watcher emits, then Desktop/CompactDesktop recompute to `Compact` (new `DensityEffectsTests` + `HydrationTests`); Tablet/Phone keep the ADR-040 `Comfortable` floor.
- **AC2 (settings + restore defaults)** — PASS. `FcSettingsDialog` resolves through `DensityPrecedence` in `SelectedDensity`, `IsForcedByViewport`, and `RestoreDefaultsAsync`; no hard-coded `?? Comfortable` fallback remains in the dialog; reset falls through to `Compact` (covered by `FcSettingsDialogTests`).
- **AC4 (sticky headers)** — PASS. Emitter outputs `GenerateHeader = DataGridGeneratedHeaderType.Sticky` (v5 RC API, not the v4 `GenerateHeaderOption`); rendered snapshot shows `row-type="sticky-header"`; pinned by `RazorEmitterVirtualizationTests`.

### Finding fixed (HIGH — AC3 not functionally delivered)

`fc-projection.css` styled the grid with `.fc-projection-grid ::part(cell)`, `::part(column-header)`, and `::part(row):hover`. FluentDataGrid v5 RC renders a **light-DOM `<table>`** (`<td>`, `<th class="column-header">`, `<tr class="fluent-data-grid-row">`) with **no shadow DOM and no `part=` attributes** — verified against the rendered grid snapshot — so shadow-part selectors matched zero elements and AC3's density-coupled padding + subtle row hover never rendered. The governance test `Projection_grid_css_uses_density_spacing_and_fluent2_hover_token` asserted the literal broken selector string, so it stayed green while masking the defect.

**Fix applied:** retargeted real table elements (`.fc-projection-grid td`, `.fc-projection-grid th.column-header`, `.fc-projection-grid tbody tr.fluent-data-grid-row:hover`); padding-block stays inside the virtualized `ItemSize` so `DataGridDensityMetrics` row-height math remains truthful (no CSS-only drift); kept Fluent 2 tokens only (`--fc-spacing-unit`, `--colorSubtleBackgroundHover`). Hardened the governance test to assert the working selectors and added a `ShouldNotContain("::part(")` regression guard.

### Verification

- Focused Shell lane (FluentConformance, DataGridDensityMetrics, DensityPrecedence, FcSettingsDialog, DensityEffects, Hydration): **50/50 passed** in Release after the fix.
- CSS change has no effect on bUnit markup snapshots (CSS is not embedded in rendered markup); no snapshot churn. SourceTools emitter output untouched.
