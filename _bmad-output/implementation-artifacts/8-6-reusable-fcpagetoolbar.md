---
baseline_commit: dccec20851f162f04ae8aef4d794a43908cb5f85
---

# Story 8.6: Reusable `FcPageToolbar`

Status: ready-for-dev

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-25. -->

## Story

As an adopter developer,
I want a reusable page-toolbar component matching the Aspire toolbar pattern,
so that every page presents a consistent search/filter/view/tab strip.

## Acceptance Criteria

1. Given `FcPageToolbar`, when it renders under `FcPageHeader`, then it provides one reusable toolbar row with a leading search input, filter trigger and popover, view/overflow menu, right-aligned actions slot, stable `data-testid`s, and no layout or landmark conflict with the page header. Use the pinned Fluent v5 primitives that actually exist in this repo: `FluentTextInput TextInputType="TextInputType.Search"` for search, `FluentButton` + `FluentPopover` for filters, `FluentMenuButton`/`FluentMenu` for view actions, and `FluentStack` with `role="toolbar"` if the pinned package still has no `FluentToolbar` component. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.6-Reusable-FcPageToolbar; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.6-VR-6-Reusable-FcPageToolbar; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml]
2. Given a consumer supplies optional tab items, when the toolbar renders, then it exposes an underline tab strip using `FluentTabs`/`FluentTab`, supports active-tab binding or callback, remains keyboard reachable, and omits the tab strip entirely when no tabs are supplied. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.6-Reusable-FcPageToolbar; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
3. Given existing page wrappers, when `FcPageToolbar` is introduced, then existing `FcPageHeader.Actions` and `FcAggregateListPage.Toolbar` behavior remains source-compatible; no host/submodule page is converted in this story, and Tenants.UI adoption remains the separate Host-A track. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Framework-host-boundary]
4. Given project UI governance, when implementation and tests are complete, then Shell still uses Fluent v5 components and Fluent 2 tokens only: no raw `<button>/<input>/<select>/<textarea>`, no legacy v4/FAST tokens, no accent as a `background`/`background-color`, and no package version or PublicAPI baseline changes outside an explicitly proven need. [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
5. Given the FC-DOC contract from Story 1.5, when the component ships, then `docs/reference/components/page-toolbar.md` is authored with conforming front matter and required sections, linked from `docs/reference/components/index.md`, and validated by the docs gate without adding a fifth top-level DocFX TOC entry. [Source: _bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md; docs/reference/components/index.md; docs/toc.yml]

## Tasks / Subtasks

- [ ] Audit pinned Fluent and existing FrontComposer toolbar-adjacent surfaces before editing (AC: 1, 2, 3)
  - [ ] Confirm from the pinned RC XML/DLL that `FluentToolbar` and `FluentSearch` are unavailable and that `FluentTextInput`, `FluentButton`, `FluentPopover`, `FluentMenuButton`, `FluentMenu`, `FluentTabs`, and `FluentTab` are available.
  - [ ] Read `FcPageHeader.razor(.cs/.css)`, `FcAggregateListPage.razor(.cs)`, `FcProjectionGlobalSearch.razor(.cs)`, `FcColumnFilterCell.razor(.cs)`, `FcColumnPrioritizer.razor(.cs/.css)`, and `FcThemeToggle.razor(.cs)` before designing the component API.
  - [ ] Treat stale comments that say `FluentSearch` as comments only; do not add a `FluentSearch` dependency or raw `<input>`.
- [ ] Add the reusable Shell component (AC: 1, 2, 4)
  - [ ] Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor` and `FcPageToolbar.razor.cs`; add `FcPageToolbar.razor.css` only if layout/positioning cannot be expressed with Fluent component parameters.
  - [ ] Keep one C# type per `.cs` file. If tab/filter/view item models are needed, put each public/internal record or class in its own file named for the type.
  - [ ] Use `FluentTextInput` with `TextInputType.Search`, `Value`, `ValueChanged`, `Placeholder`, `aria-label`, and a stable selector such as `data-testid="fc-page-toolbar-search"`.
  - [ ] Render filter affordance only when `FilterContent` is supplied: a `FluentButton` trigger with accessible name, `aria-haspopup`, `aria-expanded`, and a `FluentPopover` anchored to the trigger with role/labeling pinned by tests.
  - [ ] Render view/overflow affordance only when view menu content is supplied, using `FluentMenuButton`/`FluentMenu` semantics already used by `FcThemeToggle` and `FcAccountMenu`.
  - [ ] Render a right-aligned `Actions` slot without wrapping it in a card; actions remain caller-owned `RenderFragment`.
  - [ ] Render optional `FluentTabs` under the toolbar row only when tab content is supplied; use underline/subtle visual treatment without legacy tokens or custom theme ramps.
  - [ ] Ensure compact/wrap behavior works at narrow widths: search must not overlap filter/view/actions, and long labels must wrap or be omitted in favor of accessible names.
- [ ] Preserve integration seams (AC: 3)
  - [ ] Do not change the existing `FcPageHeader` parameter contract unless required; if changed, additions must be append-only and covered by `FcPageHeaderTests`.
  - [ ] Do not break `FcAggregateListPage.Toolbar`; the simplest path is documenting that callers can pass `<FcPageToolbar ... />` through the existing slot.
  - [ ] Do not modify `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, or `Hexalith.Commons/**`; Host-A adoption is explicitly out of scope.
  - [ ] Do not implement Story 8.7 status-icon generator work.
- [ ] Add focused tests (AC: 1, 2, 3, 4)
  - [ ] Add `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageToolbarTests.cs`.
  - [ ] Cover default rendering, search value/callback, accessible names, stable selectors, filter popover open/close state, view menu rendering, right-aligned actions slot, optional tabs, and absence of empty markup when optional slots are omitted.
  - [ ] Add or update an aggregate-list/header integration test proving `<FcPageToolbar>` composes through `FcAggregateListPage.Toolbar` under `FcPageHeader.Actions`.
  - [ ] Keep `FluentConformanceTests` green and add a narrow guard only if the implementation introduces a new pattern that could regress raw controls, legacy tokens, or accent background fills.
  - [ ] If bUnit cannot model a Fluent-owned keyboard behavior, document the limitation in the test name/comment and cover it with Playwright when local socket permissions allow.
- [ ] Add FC-DOC documentation (AC: 5)
  - [ ] Create `docs/reference/components/page-toolbar.md` with front matter matching existing component docs: `genre: reference`, `audience: adopter`, `ownerStory: 8-6-reusable-fcpagetoolbar`, `status: published`, `reviewed: 2026-06-25`, unique `uid`, and stable `slug`.
  - [ ] Include FC-DOC sections: Overview, Usage, Parameters / slots, Layout, Accessibility, Localization, Related.
  - [ ] Mark any `csharp` fences as `compile` or `no-compile reason="..."`; use `razor` fences for component examples.
  - [ ] Add the page to `docs/reference/components/index.md`; leave `docs/toc.yml` top-level Diataxis entries unchanged.
- [ ] Verify and record evidence (AC: 1-5)
  - [ ] Run `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false`.
  - [ ] Run focused Shell tests for `FcPageToolbarTests`, `FcPageHeaderTests`, `FcAggregateListPageTests`, and `FluentConformanceTests`.
  - [ ] Run docs validation: `pwsh ./eng/validate-docs.ps1`.
  - [ ] Run the solution default lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false`.
  - [ ] Run relevant Playwright a11y/visual coverage when feasible, or record exact Kestrel/socket blockers in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
  - [ ] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: the Epic text says `FluentToolbar` and `FluentSearch`, but the pinned Fluent UI Blazor package `5.0.0-rc.3-26138.1` exposes no `FluentToolbar` or `FluentSearch` types in the local XML/DLL. Existing source implements search with `FluentTextInput TextInputType="TextInputType.Search"`. Implement the Aspire toolbar pattern with the available pinned Fluent v5 primitives; do not add package dependencies or raw controls to force missing type names. [Source: Directory.Packages.props; src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml]
- `FcPageHeader` already owns the visual route header, document title, heading focus contract, `Actions`, and `Metadata`. `FcPageToolbar` should compose below/inside the existing `Actions` seam rather than rewriting page-header landmark behavior. The header root must stay `role="presentation"` and must not create a second banner landmark. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageHeaderTests.cs]
- `FcAggregateListPage<TItem>` already documents that its `Toolbar` slot is rendered through `FcPageHeader.Actions`, while `Filters`, `Commands`, `States`, `Body`, and `Pager` remain separate domain-owned regions. Do not collapse those slots into `FcPageToolbar`; the reusable toolbar is an optional component callers can place in the existing toolbar slot. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcAggregateListPageTests.cs]
- Existing filter/search behavior is Fluxor/domain specific (`GlobalSearchChangedAction`, `ColumnFilterChangedAction`, prioritizer hidden-column state). `FcPageToolbar` should not take a dependency on projection state, query services, `IBadgeCountService`, or domain localizers. Keep it generic: values, callbacks, labels, and fragments come from the caller. [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor.cs; src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnFilterCell.razor.cs; src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor]
- If adding a small tab model for `FluentTabs`, prefer a narrow record such as `FcPageToolbarTab` in its own file and keep labels/icons caller-supplied. Do not invent routing state or persist tab selection in Fluxor for this story; selection is local/caller-owned unless an existing host explicitly passes a callback. [Source: _bmad-output/project-context.md#Blazor-Shell-Fluxor-Rules]
- Use Fluent v5 components and Fluent 2 tokens only. CSS may handle layout/positioning and responsive wrapping, but must not recreate type ramps, foreground colors, button styling, or theme surfaces already owned by Fluent. The Story 8.2 guard blocks accent variables in `background`/`background-color`; use accent only as a thread if a selected tab indicator needs it. [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- Story 8.4 review caught dead CSS that targeted FluentDataGrid `::part()` selectors which do not match v5 light-DOM markup. For this component, verify rendered DOM in bUnit before pinning selectors or CSS assumptions; tests should prove behavior/markup, not just string presence in unused CSS. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#8-4-review-comment]
- Story 8.5 left navigation complete and preserved the accent-as-thread rule. Do not alter `FrontComposerNavigation`, `FcHamburgerToggle`, navigation state/effects, or rail CSS unless a compile failure directly requires it. [Source: _bmad-output/implementation-artifacts/8-5-icon-label-navigation-rail-and-projection-flyout.md]
- FC-DOC pages live under `docs/reference/components/` and are published docs, not scratch. The component index already exists under the Reference TOC group, so Story 8.6 should add the new page to the index without changing the four top-level `docs/toc.yml` groups. [Source: _bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md; docs/reference/components/index.md; docs/toc.yml]
- No external web research is needed for package choice. Relevant versions and APIs are pinned locally: .NET SDK `10.0.301`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, and Playwright `1.61.0`. Do not change package versions or add dependencies. [Source: global.json; Directory.Packages.props; _bmad-output/project-context.md#Technology-Stack-Versions]

### Project Structure Notes

- Expected Shell production touch points:
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor`
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor.cs`
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor.css` only if layout needs scoped CSS
  - optional model files such as `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbarTab.cs`
- Expected documentation touch points:
  - `docs/reference/components/page-toolbar.md`
  - `docs/reference/components/index.md`
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageToolbarTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageHeaderTests.cs` only if the header contract is touched
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcAggregateListPageTests.cs` for composition through the existing toolbar slot
  - `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` only for focused new governance coverage if needed
- Expected BMAD artifacts:
  - `_bmad-output/implementation-artifacts/8-6-reusable-fcpagetoolbar.md`
  - `_bmad-output/implementation-artifacts/sprint-status.yaml`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md` after dev-story verification
- Avoid touching:
  - `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`
  - `src/Hexalith.FrontComposer.SourceTools/**` except no known need for this story
  - `src/Hexalith.FrontComposer.Mcp/**`, `src/Hexalith.FrontComposer.Cli/**`, `src/Hexalith.FrontComposer.Schema/**`
  - `Directory.Packages.props`, `.slnx` structure, PublicAPI baselines, pacts, generated `obj/` output, Story 8.7 status badge/icon emitter files
  - unrelated modified `_bmad-output/story-automator/orchestration-8-20260625-123921.md`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-8.6-Reusable-FcPageToolbar]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.6-VR-6-Reusable-FcPageToolbar]
- [Source: _bmad-output/project-docs/architecture.md#4-Runtime-composition-Shell]
- [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- [Source: _bmad-output/project-docs/architecture.md#4.2-Page-section-layout-pattern-FluentAccordion-project-wide-guideline]
- [Source: _bmad-output/project-docs/architecture.md#4.3-Layout-component-policy-project-wide-guideline]
- [Source: _bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md]
- [Source: _bmad-output/project-context.md]
- [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcThemeToggle.razor]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageHeaderTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcAggregateListPageTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- [Source: docs/reference/components/index.md]
- [Source: docs/toc.yml]
- [Source: /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Create-story analysis loaded BMAD workflow/config/project-context, Hexalith LLM and UX rules, sprint status, Epic 8 source, the 2026-06-25 Aspire visual refresh proposal, architecture/project context, Story 8.5, current page-header/list-page/search/filter/menu/tab source files, FC-DOC docs, pinned Fluent UI package XML/DLL checks, recent git history, and current git status.
- 2026-06-25: Confirmed Story 8.6 status was `backlog` in `sprint-status.yaml`; Epic 8 is already `in-progress`; previous story 8.5 is `done`.
- 2026-06-25: Confirmed pinned Fluent UI Blazor `5.0.0-rc.3-26138.1` exposes `FluentTextInput`, `FluentButton`, `FluentPopover`, `FluentMenuButton`, `FluentMenu`, `FluentTabs`, and `FluentTab`, but not `FluentToolbar` or `FluentSearch`.
- 2026-06-25: Confirmed the working tree has unrelated modified `_bmad-output/story-automator/orchestration-8-20260625-123921.md`; story boundaries instruct dev-story to leave it alone unless explicitly owned.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.6 created as an additive Shell layout component story with explicit guardrails around pinned Fluent v5 API reality, `FcPageHeader`/`FcAggregateListPage` source compatibility, FC-DOC documentation, Fluent governance, and Host-A submodule boundaries.
- Validation pass completed against the create-story checklist; guardrails added for no raw controls, no legacy tokens, no accent background fills, no package changes, no submodule edits, no Story 8.7 scope creep, and no dependency on nonexistent `FluentToolbar`/`FluentSearch` types.

### File List

- `_bmad-output/implementation-artifacts/8-6-reusable-fcpagetoolbar.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
