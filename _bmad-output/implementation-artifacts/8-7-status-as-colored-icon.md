---
baseline_commit: 8e4bc8c10f17c0106dc2cc8b3a38f91461e4ebe9
---

# Story 8.7: Status as colored icon

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-25. -->

## Story

As an operator,
I want status shown as a colored icon with the label on hover/focus,
so that statuses are scannable and lightweight like the Aspire dashboard without losing accessibility.

## Acceptance Criteria

1. Given a `[ProjectionBadge]` status enum member, when it is rendered in generated projection output, then the mapped member renders as a colored Fluent status icon rather than a `FluentBadge` pill: Success -> green checkmark circle, Danger/Error/Rejected -> red dismiss circle, Neutral/Unknown -> grey question circle, Warning -> amber warning glyph, Info -> blue info glyph, and the existing Accent slot remains an explicit highlight mapping that does not paint a background. The generator output and Verify snapshots must be regenerated intentionally. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.7-Status-as-colored-icon-UX-DR2-amendment; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.7-VR-7-Status-as-colored-icon-UX-DR2-amendment]
2. Given a rendered status icon, when a pointer hovers it, touch activates it, or keyboard focus reaches it, then `FluentTooltip` reveals the status label and the focus target always exposes the contextual `aria-label` (`"{ColumnHeader}: {Label}"`, localized as today). The accessible name must not depend on hover-only content. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR2; src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor.cs; src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx]
3. Given numeric count, freshness, and navigation badges, when Story 8.7 is implemented, then those count/status-chip surfaces keep their current `FluentBadge` pill behavior unless they are directly rendering `[ProjectionBadge]` status members. `FrontComposerNavigation` aggregate counts, "New" badges, `FcStatusFilterChips`, `FcHomeCard` counts, and command lifecycle badges are not converted by accident. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.7-VR-7-Status-as-colored-icon-UX-DR2-amendment; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor; src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcStatusFilterChips.razor; src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor]
4. Given architecture.md section 4.1 and epics.md UX-DR2 already describe the Story 8.7 amendment, when implementation completes, then production docs and inventory no longer describe status as a pill-only `FcStatusBadge` model. Update stale references in component inventory, data grid docs, diagnostics comments, and test names/comments as needed while preserving the localized resource keys unless a rename is explicitly justified. [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance; _bmad-output/project-docs/component-inventory.md#A-Blazor-UI-components-Hexalith.FrontComposer.Shell; docs/reference/components/datagrid.md]
5. Given project UI governance, when implementation and tests are complete, then Shell and generated output still use pinned Fluent v5 components and Fluent 2 tokens only: no raw `<button>/<input>/<select>/<textarea>`, no Fluent UI icon package, no package version changes, no legacy v4/FAST tokens, no accent as `background`/`background-color`, and no hand-edits to generated `obj/**` files. [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md; _bmad-output/project-context.md#Technology-Stack-Versions; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]

## Tasks / Subtasks

- [x] Audit current status/badge surfaces before editing (AC: 1, 2, 3)
  - [x] Read `FcStatusBadge.razor(.cs)`, `FcDesaturatedBadge.razor(.cs)`, `SlotAppearanceTable.cs`, `FcStatusFilterChips.razor(.cs)`, `FrontComposerNavigation.razor`, and `FcHomeCard.razor`.
  - [x] Read `ColumnEmitter.cs` badge paths: `EmitEnumBadgeChildContent`, `EmitBadgeSwitch`, and `EmitInlineEnumRenderFragment`.
  - [x] Read `ProjectionRoleBodyEmitter.cs` detail-field badge path so DetailRecord/Timeline/StatusOverview roles keep parity with DataGrid.
  - [x] Confirm the pinned Fluent package still exposes `FluentIcon.Color`, `FluentTooltip.Anchor`, `UseTooltipService`, and `Color.Success/Error/Warning/Info/Neutral/Accent`.
- [x] Add the status icon component and exhaustive slot mapping (AC: 1, 2, 5)
  - [x] Create `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusIcon.razor` and `FcStatusIcon.razor.cs`.
  - [x] Replace or retire `SlotAppearanceTable` with an exhaustive status-icon mapping for every `BadgeSlot`: Neutral, Info, Success, Warning, Danger, Accent. Do not leave `Accent` to a default/fallback path; use `Color.Accent` and an explicit highlight icon such as `FcFluentIcons.Star16()` unless product/UX has a better existing mapping.
  - [x] Reuse/extend `FcFluentIcons`; add missing circle glyphs such as `CheckmarkCircle16`, `DismissCircle16`, and `InfoCircle16` instead of adding a new icons package. Preserve Story 8.3/8.5 behavior of `Apps20()` and active rail icons.
  - [x] Render `FluentIcon` with the resolved `Icon` and `Color`; use `Color.Custom` only if a Fluent enum color is unavailable, and then use Fluent 2 tokens only.
  - [x] Make the focus target keyboard reachable (`tabindex="0"` or a Fluent focusable wrapper), carry `aria-label`, `role` only if appropriate, stable `data-testid="fc-status-icon"`, and `data-fc-badge-slot`.
  - [x] Anchor a `FluentTooltip` to the focus target using a stable generated id; set `UseTooltipService="false"` when dynamic content requires it, following the existing navigation tooltip pattern.
  - [x] Keep the label available to the tooltip and accessible name, but do not render the old visible text pill in the normal icon state.
- [x] Migrate generator emission from status badge to status icon (AC: 1, 2)
  - [x] Update `ColumnEmitter.EmitBadgeSwitch` so annotated enum members open `FcStatusIcon` with `Slot`, `Label`, and `ColumnHeader`.
  - [x] Preserve declared-but-unannotated enum members as plain humanized text and out-of-range runtime values as the localized `StatusBadgeUnknownStateFallback` fail-soft label.
  - [x] Preserve nullable enum `em-dash` behavior before the switch.
  - [x] Ensure DataGrid `TemplateColumn<T>` emission remains valid and sortable; do not regress the Story 2.3 fix that avoided `PropertyColumn<T>` for status components.
  - [x] Update comments/test names from `FcStatusBadge` to `FcStatusIcon` where the generated status indicator is meant, without changing unrelated badge/count surfaces.
- [x] Reassess optimistic/status wrappers without changing count badges accidentally (AC: 3)
  - [x] Update `FcDesaturatedBadge` only if it still represents the semantic status indicator path; preserve its visible pending/rejected/idempotent state text and localized aria label.
  - [x] Keep `FcStatusFilterChips` as `FluentBadge` chips for filter toggles; those are controls/count/filter affordances, not generated status enum cells.
  - [x] Keep navigation aggregate counts and "New" badges as `FluentBadge` pills.
  - [x] Keep command lifecycle `FluentBadge` status summaries unless a focused test proves they are `[ProjectionBadge]` output.
- [x] Update tests and snapshots (AC: 1-5)
  - [x] Rename or replace `FcStatusBadgeTests` with `FcStatusIconTests`, covering every `BadgeSlot`, icon/color mapping, aria-label localization in English and French, focusable anchor, tooltip content, and absence of `FluentBadge` pill markup.
  - [x] Update `BadgeProjectionRenderTests` to assert its generated badge projection rows render `fc-status-icon` instances with the expected `aria-label`s and `data-fc-badge-slot` values.
  - [x] Update `RazorEmitterBadgeColumnTests` and `RoleSpecificProjectionApprovalTests.ActionQueueProjection_Renders...` to assert generator output references `FcStatusIcon`, still supplies `ColumnHeader`, still parses as valid C#, and leaves zero-mapping enum paths free of status components.
  - [x] Regenerate affected SourceTools `.verified.txt` snapshots intentionally, including badge-column and role-specific projection approvals.
  - [x] Update or add `FcFluentIcons` tests if the icon factory has coverage for named icon paths/variants.
  - [x] Keep `FcDesaturatedBadgeTests`, `FcStatusFilterChipsTests`, `FrontComposerNavigationTests`, and `FluentConformanceTests` green; update only where their owned surface truly changed.
- [x] Add focused E2E/a11y coverage (AC: 2, 3, 5)
  - [x] Update `tests/e2e/specs/specimen-accessibility.spec.ts`: the type specimen should expect six `fc-status-icon` instances instead of six `fc-status-badge` pills.
  - [x] Add keyboard focus coverage proving the tooltip label appears on focus and the focused icon has the contextual accessible name.
  - [x] Add hover/tap coverage where Playwright can exercise the tooltip reliably; if local Kestrel/socket permissions block browser execution, record the exact blocker in the test summary.
  - [x] Keep `fc-tbl-contract.spec.ts` status filter chip expectations as chips; do not convert filter-chip selectors.
- [x] Update documentation and inventory (AC: 4)
  - [x] Update `docs/reference/components/datagrid.md` so generated status cells are described as icons with tooltip/focus labels, while numeric counts and filter chips remain badges.
  - [x] Update `_bmad-output/project-docs/component-inventory.md` display/status row from `FcStatusBadge` to the new colored-icon model.
  - [x] Verify `epics.md` UX-DR2 and `architecture.md` section 4.1 already contain the Story 8.7 amendment; adjust only if implementation chooses a different explicit mapping for `BadgeSlot.Accent`.
  - [x] Update stale XML doc comments in `FcDiagnosticIds`, `DiagnosticDescriptors`, and SourceTools emitters that still claim generated code reaches `FcStatusBadge`.
- [x] Verify and record evidence (AC: 1-5)
  - [x] Run `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1 /nr:false`.
  - [x] Run `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false`.
  - [x] Run focused SourceTools tests for badge emission and role-specific approvals.
  - [x] Run focused Shell tests for `FcStatusIcon`, generated badge projection rendering, status filter chips, navigation badges, and `FluentConformanceTests`.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` when feasible.
  - [x] Run relevant Playwright a11y/visual coverage for the type specimen, or record exact Kestrel/socket blockers in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: generated `[ProjectionBadge]` output currently opens `Hexalith.FrontComposer.Shell.Components.Badges.FcStatusBadge` from `ColumnEmitter.EmitBadgeSwitch`; that helper is also used by non-grid role bodies through `EmitInlineEnumRenderFragment` and `ProjectionRoleBodyEmitter`. Updating only the DataGrid branch would leave DetailRecord/Timeline/StatusOverview stale. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs; src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs]
- `FcStatusBadge` currently wraps `FluentBadge`, resolves slot color/appearance through `SlotAppearanceTable`, renders visible text, and builds localized aria labels using `StatusBadgeAriaLabelTemplate`. Story 8.7 supersedes the pill rendering for generated status members, but the aria-label localization behavior should be preserved. [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor; src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor.cs; src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx]
- `BadgeSlot` has six declared values, while the Epic 8 mapping text explicitly names only success/error/neutral/warning/info. The implementation must handle `Accent` deliberately because the Counter specimen includes `[ProjectionBadge(BadgeSlot.Accent)]`. Treat Accent as a highlight extension using `Color.Accent` and an explicit icon unless Product/UX changes the mapping in the docs. [Source: src/Hexalith.FrontComposer.Contracts/Attributes/BadgeSlot.cs; samples/Counter/Counter.Specimens.Domain/SpecimenStatusProjection.cs]
- `FcFluentIcons` already contains reusable 16px glyphs (`QuestionCircle16`, `Warning16`, `Star16`) and the project rule is to use this factory rather than a FluentUI icons NuGet package. Add missing glyphs here, one static factory per icon, and do not change existing `Apps20()` regular/filled paths. [Source: src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs; _bmad-output/project-context.md#Blazor-Shell-Fluxor-Rules]
- The pinned Fluent UI Blazor package `5.0.0-rc.3-26138.1` exposes `FluentTooltip.Anchor`, `FluentTooltip.UseTooltipService`, `FluentIcon.Value`, `FluentIcon.Color`, and `Color.Success/Error/Warning/Info/Neutral/Accent`. Use those local APIs; no external web research or package upgrade is needed. [Source: Directory.Packages.props; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml]
- Existing navigation tooltips use a stable `id` on the focusable `FluentButton` plus `<FluentTooltip Anchor="@anchorId" Positioning="@Positioning.After" UseTooltipService="false">`. Use the same pinned pattern unless bUnit/Playwright proves a better local approach. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor]
- Count and "New" badges are intentionally still pills. Story 8.5 review pinned navigation count badges as `FluentBadge` Filled/Brand and projection "New" badges as Tint/Informative. Do not break that while changing status enum cells. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#8-5-review-comment; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs]
- Story 8.4 and Story 8.6 reviews both found dead CSS caused by assuming Fluent web-component/scoped-CSS selectors applied when rendered DOM did not match. For the status icon, prefer component parameters and rendered attributes over CSS. If CSS is needed, prove it attaches to rendered DOM, not just source strings. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#8-4-review-comment; _bmad-output/implementation-artifacts/8-6-reusable-fcpagetoolbar.md#Senior-Developer-Review-AI]
- `docs/` is published DocFX content, not scratch. Update docs only where they are the product/component reference affected by this story. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- No submodule edits are authorized. Do not touch `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, or `Hexalith.Commons/**`; Host-A Tenants page-body adoption is a separate correct-course. [Source: AGENTS.md; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Framework-host-boundary]
- No external web research is needed for technology choice. Relevant versions and APIs are pinned locally: .NET SDK `10.0.302`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, and Playwright `1.61.0`. Do not change package versions. [Source: global.json; Directory.Packages.props; _bmad-output/project-context.md#Technology-Stack-Versions]

### Project Structure Notes

- Expected Shell production touch points:
  - `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusIcon.razor`
  - `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusIcon.razor.cs`
  - `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor` and `.razor.cs` only to remove, obsolete, or adapt the old pill component intentionally
  - `src/Hexalith.FrontComposer.Shell/Components/Badges/FcDesaturatedBadge.razor` and `.razor.cs` only if it must delegate to the new status-icon component while preserving visible optimistic text
  - `src/Hexalith.FrontComposer.Shell/Components/Badges/SlotAppearanceTable.cs` replaced/renamed by an icon mapping table if still needed
  - `src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs`
- Expected SourceTools touch points:
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs` comments or direct role-specific emission only if required
  - affected `.verified.txt` snapshots under `tests/Hexalith.FrontComposer.SourceTools.Tests/`
- Expected docs/tests touch points:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusIconTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Generated/BadgeProjectionRenderTests.cs`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs`
  - `tests/e2e/specs/specimen-accessibility.spec.ts`
  - `docs/reference/components/datagrid.md`
  - `_bmad-output/project-docs/component-inventory.md`
- Avoid touching:
  - `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`
  - `Directory.Packages.props`, `.slnx` structure, PublicAPI baselines, pacts, generated `obj/` output
  - `FrontComposerNavigation` badge behavior except tests that prove it stays unchanged
  - `FcPageToolbar`, `FrontComposerShell` header/footer chrome, density defaults, and grid sticky-header work unless a compile failure directly requires a comment/test update
  - unrelated modified `_bmad-output/story-automator/orchestration-8-20260625-123921.md`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR2]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-8.7-Status-as-colored-icon-UX-DR2-amendment]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.7-VR-7-Status-as-colored-icon-UX-DR2-amendment]
- [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- [Source: _bmad-output/project-docs/component-inventory.md#A-Blazor-UI-components-Hexalith.FrontComposer.Shell]
- [Source: _bmad-output/project-context.md]
- [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/FcDesaturatedBadge.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/FcDesaturatedBadge.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/SlotAppearanceTable.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusBadgeTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/BadgeProjectionRenderTests.cs]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs]
- [Source: tests/e2e/specs/specimen-accessibility.spec.ts]
- [Source: /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Create-story analysis loaded BMAD workflow/config/project-context, Hexalith LLM and UX rules, sprint status, Epic 8 source, the 2026-06-25 Aspire visual refresh proposal, architecture/project context, Story 8.6 review learnings, current badge/status/generator/test/source files, pinned Fluent UI package XML, recent git history, and current git status.
- 2026-06-25: Discovery loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; no planning-artifact PRD, architecture, or UX markdown files matched the workflow patterns, so project architecture/context docs were loaded as the relevant architecture source.
- 2026-06-25: Confirmed Story 8.7 status was `backlog` in `sprint-status.yaml`; Epic 8 is already `in-progress`; previous story 8.6 is `done`.
- 2026-06-25: Confirmed pinned Fluent UI Blazor `5.0.0-rc.3-26138.1` exposes `FluentTooltip`, `FluentIcon`, and the required `Color` enum values.
- 2026-06-25: Confirmed working tree has unrelated modified `_bmad-output/story-automator/orchestration-8-20260625-123921.md`; story boundaries instruct dev-story to leave it alone unless explicitly owned.
- 2026-06-25: Dev-story activation completed; baseline commit `8e4bc8c10f17c0106dc2cc8b3a38f91461e4ebe9` captured and sprint/story status moved to `in-progress`.
- 2026-06-25: Audited current status components, count/filter/navigation badge surfaces, SourceTools badge emission paths, role-specific projection paths, and pinned Fluent API XML before editing.
- 2026-06-25: RED phase added `FcStatusIconTests`, generated render assertions, SourceTools generator assertions, and E2E status-icon expectations; focused Shell build failed before implementation because `FcStatusIcon`/`StatusIconTable` did not exist.
- 2026-06-25: Implemented `FcStatusIcon`, exhaustive `StatusIconTable`, circle status glyph factories in `FcFluentIcons`, and switched `ColumnEmitter.EmitBadgeSwitch` to emit `FcStatusIcon` for generated `[ProjectionBadge]` members.
- 2026-06-25: Intentionally regenerated SourceTools Verify snapshots for enum/badge mapping and role-specific projection approvals.
- 2026-06-25: Validation passed for required Release builds, focused Shell lanes, focused SourceTools lanes, broad Shell non-Contract lane, and E2E TypeScript; VSTest and Playwright browser execution remain socket-blocked locally with exact blockers recorded in `tests/test-summary.md`.
- 2026-06-25: QA generate-e2e-tests workflow re-run added runtime generated-render fail-soft coverage for unannotated enum members and out-of-range enum values; focused Shell lane now passes 19/19.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.7 created as a Shell + SourceTools generator story that replaces generated `[ProjectionBadge]` status pills with colored Fluent icons while preserving accessible names, tooltip/focus label reveal, nullable/plain-text/fail-soft enum paths, and count badge behavior.
- Validation pass completed against the create-story checklist; guardrails added for exhaustive `BadgeSlot` mapping including Accent, no icon package/package changes, no raw controls, no legacy tokens, no accent background fills, no generated-output hand edits, no submodule edits, and no prior Epic 8 chrome/nav/toolbar regression.
- Implemented generated status icon rendering with focusable `FcStatusIcon`, localized contextual `aria-label`, anchored `FluentTooltip`, explicit `BadgeSlot` to icon/color mapping, and no `FluentBadge` pill markup for generated `[ProjectionBadge]` output.
- Preserved non-generated pill surfaces: `FcDesaturatedBadge`, `FcStatusFilterChips`, navigation count/"New" badges, home count badges, and command lifecycle summaries remain `FluentBadge` based.
- Updated SourceTools emission, comments, tests, and Verify snapshots so DataGrid, DetailRecord, Timeline, StatusOverview, and template/slot fallback paths consistently emit `FcStatusIcon` while preserving nullable em-dash, unannotated-member text, unknown fallback, and sortable `TemplateColumn<T>` behavior.
- Updated docs, inventory, diagnostics comments, E2E coverage, and test summary for the Story 8.7 colored-icon model.
- Added QA gap coverage proving generated unannotated enum states and unknown runtime enum values render as non-icon fail-soft text.
- Validation evidence: SourceTools.Tests Release build 0/0; Shell.Tests Release build 0/0; focused Shell 19/19; non-generated badge regression 62/62; focused SourceTools 41/41; broad Shell non-Contract 2005/2005; E2E typecheck passed. Broad SourceTools has 2 pre-existing Story 8.6 page-toolbar docs failures; solution VSTest and Playwright browser lanes are socket-blocked locally.

### File List

- `_bmad-output/implementation-artifacts/8-7-status-as-colored-icon.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/project-docs/component-inventory.md`
- `docs/reference/components/datagrid.md`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusIcon.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusIcon.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Badges/StatusIconTable.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusIconTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/BadgeProjectionRenderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/BadgeProjectionSpecimen.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterScopeGuardrailTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.EnumAndBadgeMappings_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.StatusOverviewProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.TimelineProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.WhenStateTypoProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs`
- `tests/e2e/specs/specimen-accessibility.spec.ts`

### Change Log

- 2026-06-25: Implemented Story 8.7 status icon model for generated `[ProjectionBadge]` output, updated tests/snapshots/docs, and moved story to review.
- 2026-06-25: Story-automator review (auto-fix). Independently reproduced focused evidence via the direct xUnit v3 runner: SourceTools.Tests + Shell.Tests Release builds 0W/0E; Shell `FcStatusIcon`+`BadgeProjectionRender` 19/19; SourceTools badge-column/role-approval/scope-guardrail 28/28; badge regression (`FcDesaturatedBadge`+`FcStatusFilterChips`+`FluentConformance`) 23/23; e2e typecheck passed. Auto-fixed 1 MEDIUM and moved story to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme — 2026-06-25 (story-automator adversarial review, auto-fix mode)

**Outcome:** Approve (after auto-fix). 0 CRITICAL, 1 MEDIUM fixed, 0 remaining.

### AC validation

- **AC1 — colored status icon mapping:** IMPLEMENTED. `StatusIconTable` maps every `BadgeSlot` exhaustively (test `StatusIconTable_IsExhaustive`): Success→`CheckmarkCircle16`/`Color.Success`, Danger→`DismissCircle16`/`Color.Error`, Neutral→`QuestionCircle16`/`Color.Neutral`, Warning→`Warning16`/`Color.Warning`, Info→`InfoCircle16`/`Color.Info`, Accent→`Star16`/`Color.Accent` (explicit highlight, no background). Verify snapshots regenerated intentionally; all role-approval `.verified.txt` reference `FcStatusIcon` with zero `FcStatusBadge`.
- **AC2 — tooltip + accessible name:** IMPLEMENTED. Focusable `<span role="img" tabindex="0">` carries the always-present localized contextual `aria-label` (`"{ColumnHeader}: {Label}"`, EN/FR verified) — identical to the prior `FcStatusBadge` localization — and anchors a `FluentTooltip UseTooltipService="false"`. Accessible name does not depend on hover-only content.
- **AC3 — count/chip/nav surfaces unchanged:** IMPLEMENTED. `FcDesaturatedBadge`, `FcStatusFilterChips`, navigation count/"New" badges, and command/home summaries remain `FluentBadge` pills; regression lane green; `FcStatusBadge`/`SlotAppearanceTable` correctly retained for the optimistic pill path.
- **AC4 — docs/inventory:** IMPLEMENTED. `datagrid.md`, `component-inventory.md`, `FcDiagnosticIds`, `DiagnosticDescriptors`, and emitter comments updated to the colored-icon model; no stale `FcStatusBadge`/`fc-status-badge` references remain in published `docs/`; resource keys preserved.
- **AC5 — governance:** IMPLEMENTED. `FluentConformanceTests` green; no package changes, no raw `button/input/select/textarea` (a focusable `<span>` is permitted), no legacy v4/FAST tokens, no accent background fill, no `obj/**` hand-edits. `CS0618` suppression for `Color.Neutral`/`Color.Accent` is story-mandated and locally scoped.

### Findings

- **[MEDIUM][Fixed]** `tests/e2e/specs/specimen-accessibility.spec.ts` — the new touch test called `locator.tap()`, but all three Playwright projects run desktop devices with `hasTouch: false`, so Playwright would throw `"The page does not support tap. Use hasTouch context option to enable touch support."` on every browser lane in CI. The defect was masked locally because Kestrel/socket binding is blocked, so the browser lanes never run. Fixed by scoping the touch test under a nested `test.describe` with `test.use({ hasTouch: true })`, which inherits the parent locale and console/network guard hooks. Post-fix e2e typecheck passes.

### Cleared as non-defects

- `StatusIconTable.Resolve` throwing `KeyNotFoundException` on an unsafe-cast slot mirrors the existing `SlotAppearanceTable.Resolve` contract (documented programmer-error, unreachable from generated code) — intentional parity.
- Retained `FcStatusBadge`, `SlotAppearanceTable`, and `FcStatusBadgeTests` are correct because `FcDesaturatedBadge` still depends on the pill component.
- Process-wide static `_nextAnchorId` counter is sound for DOM-id uniqueness.

### Local blockers (unchanged from dev)

- Solution-wide VSTest and Playwright browser execution remain socket-blocked locally (`SocketException (13): Permission denied`); browser-level e2e verification deferred to CI. Focused xUnit v3 lanes, Release builds, and e2e typecheck all run and pass locally.
