---
baseline_commit: 68a64e607e1fc5f0424ad5d9ed42c1ab28aaef88
---

# Story 8.3: Brand/logo cell in header-start

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-25. -->

## Story

As an operator,
I want a proper brand lockup at the top-left,
so that the header reads as a branded product surface like the Aspire logo cell.

## Acceptance Criteria

1. Given the default shell with no logo option supplied, when `FrontComposerShell` renders, then the current zero-config header remains visually and behaviorally unchanged: `HeaderStart` still defaults to `FcHamburgerToggle`, `AppTitle` still resolves from `FcShellResources.AppTitle`, no logo markup is emitted, and Story 8.1 neutral chrome plus Story 8.2 accent-surface guard remain green. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.3-Brand-logo-cell-in-header-start; _bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md; _bmad-output/implementation-artifacts/8-2-accent-as-thread-policy-and-regression-guard.md]
2. Given an adopter supplies a logo mark, when the header-start cluster renders, then the mark appears inside the existing left header `FluentStack` after `HeaderStart`/default hamburger and before `AppTitle`, with tightened lockup spacing, without replacing `HeaderStart`, `HeaderCenter`, `HeaderEnd`, navigation, footer, account, palette, settings, or shortcut behavior. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.3-VR-9-Brand-logo-cell; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
3. Given the framework default logo mark is explicitly enabled and no adopter logo is supplied, when the header renders, then it uses the existing `FcFluentIcons` factory for a default 20px mark, renders it as a decorative logo cell with stable test markup, and does not introduce a Fluent icon package, raw interactive controls, legacy Fluent v4/FAST tokens, or an accent background. [Source: _bmad-output/project-docs/architecture.md#7-Architecturally-significant-decisions-observed; src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
4. Given `FrontComposerShell`'s public parameter surface is append-only, when the logo API is added, then existing parameters keep their names, types, metadata order, and null defaults; new parameters are appended at the tail and pinned in `FrontComposerShellParameterSurfaceTests`. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs]

## Tasks / Subtasks

- [x] Audit the current header-start cluster before editing (AC: 1, 2, 4)
  - [x] Read `FrontComposerShell.razor` and confirm the left header cluster still contains `HeaderStart`/`FcHamburgerToggle`, `FluentText` `AppTitle`, and optional `HeaderCenter` in that order.
  - [x] Read `FrontComposerShell.razor.cs` and preserve the current parameter surface; do not reorder or rename existing parameters.
  - [x] Read `FrontComposerShellTests.cs` and `FrontComposerShellParameterSurfaceTests.cs` to reuse existing bUnit and parameter-order patterns.
- [x] Add the optional logo API with safe defaults (AC: 1, 2, 3, 4)
  - [x] Append `[Parameter] public RenderFragment? HeaderLogo { get; set; }` to `FrontComposerShell`.
  - [x] Append `[Parameter] public bool ShowDefaultHeaderLogo { get; set; }` after `HeaderLogo`; default `false` preserves no-logo behavior.
  - [x] Update XML docs and the parameter-surface remarks to state that additions are append-only and that null/false emits no logo.
  - [x] Do not place a `RenderFragment` in `FcShellOptions`; Blazor fragments belong on the component parameter surface, not options/config.
- [x] Render the brand lockup inside the existing header-start/title stack (AC: 1, 2, 3)
  - [x] In `FrontComposerShell.razor`, render the logo between `HeaderStart`/default `FcHamburgerToggle` and the `FluentText` app title.
  - [x] If `HeaderLogo` is non-null, render that fragment and ignore `ShowDefaultHeaderLogo`.
  - [x] If `HeaderLogo` is null and `ShowDefaultHeaderLogo` is true, render a framework default mark through `<FluentIcon Value="@FcFluentIcons.Apps20()" />`.
  - [x] Add `@using Hexalith.FrontComposer.Shell.Components.Icons` to `FrontComposerShell.razor` if required; match the existing `FcSettingsButton`, `FcPaletteTriggerButton`, and `FcHamburgerToggle` pattern.
  - [x] Give the logo wrapper a stable selector such as `data-testid="fc-shell-brand-logo"` and mark the default icon decorative (`aria-hidden="true"`) unless a future product requirement gives it a spoken name.
  - [x] Use Fluent layout parameters for spacing, for example a `HorizontalGap` on the existing `FluentStack`; avoid new CSS unless a failing visual/test case proves it is necessary.
  - [x] Keep the logo as an accent thread at most; never use `--fc-color-accent` or `--fc-accent-base-color` as `background`/`background-color`.
- [x] Add focused tests (AC: 1, 2, 3, 4)
  - [x] Extend `FrontComposerShellParameterSurfaceTests` with appended entries `HeaderLogo:RenderFragment` and `ShowDefaultHeaderLogo:Boolean`.
  - [x] Add a default-shell test proving no brand-logo selector is emitted when `HeaderLogo` is null and `ShowDefaultHeaderLogo` is false.
  - [x] Add an adopter-logo test proving supplied logo markup renders before the app title and after the default/adopter `HeaderStart` content.
  - [x] Add a default-logo opt-in test proving `ShowDefaultHeaderLogo=true` renders the `FcFluentIcons.Apps20()` path through `FluentIcon`, with stable `data-testid` markup.
  - [x] Re-run the Story 8.1 header/footer tests and Story 8.2 Fluent governance guard; do not weaken existing assertions.
- [x] Preserve Epic 8 boundaries (AC: 1, 3)
  - [x] Do not implement Story 8.4 density/grid polish, Story 8.5 nav rail/flyout, Story 8.6 `FcPageToolbar`, or Story 8.7 status icons.
  - [x] Do not edit `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`, package versions, `.slnx` structure, PublicAPI baselines, pacts, SourceTools, MCP, CLI, schema fingerprint code, generated output, or visual snapshots unless a Story 8.3 test proves direct ownership.
- [x] Verify and record evidence (AC: 1, 2, 3, 4)
  - [x] Run `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false`.
  - [x] Run the focused logo/header test lane. If VSTest socket permissions fail locally, use the built xUnit v3 in-process executable as established by Stories 8.1 and 8.2.
  - [x] Run `FrontComposerShellTests`, `FrontComposerShellParameterSurfaceTests`, and `FluentConformanceTests`.
  - [x] Run the solution default lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false`.
  - [x] Run existing light/dark shell chrome browser coverage when feasible: `npm --prefix tests/e2e run test:fc-shell-chrome`. If browser/server constraints block it, record the exact command and blocker.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 8.3 commands, pass/fail counts, and local blockers.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: Story 8.3 is a narrow Shell chrome API and markup story. The zero-config header was intentionally stabilized by Story 8.1 and locked by Story 8.2; this story must add a logo path without silently changing existing adopters' header. [Source: _bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md#Acceptance-Criteria; _bmad-output/implementation-artifacts/8-2-accent-as-thread-policy-and-regression-guard.md#Acceptance-Criteria]
- The source docs say "adopter-supplied or a default `FcFluentIcons` mark" and also "no change when no logo is supplied." Resolve that by making logo rendering opt-in: `HeaderLogo` supplies custom markup; `ShowDefaultHeaderLogo=true` enables the framework default; null/false emits no logo. This satisfies both the default-mark option and the additive/no-change constraint. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.3-Brand-logo-cell-in-header-start; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.3-VR-9-Brand-logo-cell]
- Current header structure to preserve: the outer header `FluentStack` owns the 48px neutral surface and bottom divider; the left stack renders `HeaderStart` or `FcHamburgerToggle`, then `FluentText` app title, then optional `HeaderCenter`; the right stack renders theme/dev/palette/settings/account actions. Do not move account/menu/shortcut ownership. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- `FrontComposerShellParameterSurfaceTests` locks public parameters by metadata token order and states additions must be append-only. Do not insert `HeaderLogo` visually in the property list even though it renders visually near `HeaderStart`; append it after `ContentLabelledBy` and update the expected list. [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs]
- Use the existing icon factory, not a new package. `FcFluentIcons.Apps20()` is already the default bounded-context/navigation icon and is a safe framework default mark unless Product supplies a more specific glyph in a later story. [Source: src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs; _bmad-output/project-docs/architecture.md#7-Architecturally-significant-decisions-observed]
- `FrontComposerShell.razor` does not currently import `Hexalith.FrontComposer.Shell.Components.Icons`; nearby header controls do import it locally before calling `FcFluentIcons.*`. Follow that pattern instead of moving icon usings globally. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsButton.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteTriggerButton.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor]
- Keep Fluent v5 and Fluent 2 discipline: prefer `FluentStack` gap parameters and `FluentIcon`; do not add raw `<button>/<input>/<select>/<textarea>`, legacy v4/FAST tokens, hand-rolled typography, or theme redefinition. If a non-interactive wrapper is needed for `data-testid`/`aria-hidden`, a plain element is acceptable because no Fluent component owns that semantic wrapper. [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- The default logo should be decorative unless Product/UX later defines it as a home link. Do not make it clickable in this story; a clickable logo would introduce navigation semantics, focus order, localization, and route behavior not present in Story 8.3. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.3-Brand-logo-cell-in-header-start]
- No package/version web research is needed. Relevant versions are pinned locally: .NET SDK `10.0.302`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, and Playwright `1.61.0`. Do not change package versions or add dependencies. [Source: _bmad-output/project-context.md#Technology-Stack-Versions; Directory.Packages.props; global.json]
- Story 8.1 review carries a Medium follow-up to reshoot Windows full-page visual baselines. Do not reshoot those baselines as part of Story 8.3 unless the implementation legitimately changes the default no-logo visuals; record the inherited follow-up if broad visual lanes still report it. [Source: _bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md#Review-Follow-ups-AI]
- Current working tree before Story 8.3 creation contains an unrelated modified `_bmad-output/story-automator/orchestration-8-20260625-123921.md`. Do not revert or include it in the Story 8.3 File List unless the dev-story workflow itself owns a new change to that file. [Source: git status --short]

### Project Structure Notes

- Expected production touch points:
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` for verification only.
- Expected BMAD artifacts:
  - `_bmad-output/implementation-artifacts/8-3-brand-logo-cell-in-header-start.md`
  - `_bmad-output/implementation-artifacts/sprint-status.yaml`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Avoid touching:
  - `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`
  - `src/Hexalith.FrontComposer.SourceTools/**`
  - `src/Hexalith.FrontComposer.Mcp/**`
  - `src/Hexalith.FrontComposer.Cli/**`
  - `src/Hexalith.FrontComposer.Schema/**`
  - `Directory.Packages.props`, `.slnx` structure, PublicAPI baselines, pacts, generated snapshots, density defaults, nav rail/flyout, page toolbar files, and status badge/icon emitters.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-8.3-Brand-logo-cell-in-header-start]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.3-VR-9-Brand-logo-cell]
- [Source: _bmad-output/project-docs/architecture.md#4-Runtime-composition-Shell]
- [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- [Source: _bmad-output/project-docs/architecture.md#7-Architecturally-significant-decisions-observed]
- [Source: _bmad-output/project-context.md]
- [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- [Source: tests/e2e/specs/shell-chrome.spec.ts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Create-story analysis loaded BMAD config, sprint status, Epic 8, the 2026-06-25 change proposal, project context, Hexalith UX rules, architecture section 4.1, Stories 8.1 and 8.2, current shell header markup, shell parameter surface tests, icon factory, shell layout tests, Fluent governance tests, recent git history, and current git status.
- 2026-06-25: Resolved the source-doc tension between "default mark" and "no change when no logo is supplied" into an explicit opt-in default-logo flag plus nullable custom logo fragment.
- 2026-06-25: Dev-story activation loaded BMAD workflow/config, root and submodule project context files, Hexalith UX rules, sprint status, Story 8.3, `FrontComposerShell.razor`, `FrontComposerShell.razor.cs`, `FrontComposerShellTests.cs`, `FrontComposerShellParameterSurfaceTests.cs`, and `FcFluentIcons.cs`.
- 2026-06-25: RED phase failed as expected before implementation because `HeaderLogo` and `ShowDefaultHeaderLogo` did not exist. After implementation, Release build and focused direct xUnit lanes passed; solution VSTest, Pact mock server, and Playwright/Kestrel lanes are locally socket-blocked.

### Implementation Plan

- Append `HeaderLogo` and `ShowDefaultHeaderLogo` to `FrontComposerShell` and update the metadata-order parameter test.
- Render custom logo or opt-in default `FcFluentIcons.Apps20()` between header-start and app title inside the existing left header stack.
- Add focused bUnit tests for no-logo default, custom logo ordering, default-logo opt-in, and existing header/footer preservation.
- Verify with Shell layout tests, parameter-surface tests, Fluent governance, and feasible browser chrome coverage.
- Completed as planned without adding dependencies, CSS, option/config fragments, package changes, or later Epic 8 scope.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.3 created as a narrow Shell brand-lockup story with explicit safe-default semantics.
- Validation pass completed against the create-story checklist; guardrails added for append-only API shape, no-logo default preservation, default icon factory reuse, Fluent v5/Fluent 2 governance, Story 8.1/8.2 preservation, verification lanes, and submodule/later-Epic-8 boundaries.
- Appended `HeaderLogo` and `ShowDefaultHeaderLogo` to `FrontComposerShell`; null/false emits no logo and keeps the default hamburger/title path intact.
- Rendered the optional brand-logo cell between `HeaderStart`/`FcHamburgerToggle` and `AppTitle`, with custom logo precedence over the opt-in default `FcFluentIcons.Apps20()` mark.
- Added focused bUnit/parameter tests for no-logo default, custom-logo ordering and precedence, default-logo opt-in markup, and append-only parameter order.
- Verified Release Shell.Tests build 0/0, focused header/parameter lane 31/31, Fluent conformance 17/17, and broad non-Pact Shell lane 1973/1973. Recorded local VSTest, Pact, and Kestrel socket blockers in test-summary.md.
- QA Generate E2E Tests added Counter specimen routes/layouts for opt-in default and adopter logo states plus Playwright shell-chrome assertions for zero-config, default logo, custom logo, and header-start/title ordering. Browser execution remains locally Kestrel socket-blocked; Playwright discovery lists the new tests.

### File List

- `_bmad-output/implementation-artifacts/8-3-brand-logo-cell-in-header-start.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `samples/Counter/Counter.Web/Components/Layout/HeaderCustomLogoLayout.razor`
- `samples/Counter/Counter.Web/Components/Layout/HeaderDefaultLogoLayout.razor`
- `samples/Counter/Counter.Web/Components/Pages/HeaderCustomLogoSpecimen.razor`
- `samples/Counter/Counter.Web/Components/Pages/HeaderDefaultLogoSpecimen.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- `tests/e2e/specs/shell-chrome.spec.ts`

### Change Log

- 2026-06-25: Implemented Story 8.3 brand/logo cell API and markup; added focused tests and recorded verification evidence. Status set to review.
- 2026-06-25: QA Generate E2E Tests added rendered-browser Story 8.3 coverage and specimen host routes; recorded validation evidence and sandbox blockers.
- 2026-06-25: Senior Developer Review (AI) — adversarial review independently re-built and re-ran the focused lane (48/48 green incl. the three new logo tests). All 4 ACs confirmed implemented; File List accurate. 0 Critical/High/Medium. Auto-fixed 1 Low (pinned adopter-logo non-decorative a11y branch with a unit assertion). Status set to done.

## Senior Developer Review (AI)

**Reviewer:** Administrator
**Date:** 2026-06-25
**Outcome:** Approve

### Verification (independently re-run, not trusted from the dev record)

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false --no-restore` → **0 warnings / 0 errors**.
- Focused lane (`FrontComposerShellTests` + `FrontComposerShellParameterSurfaceTests` + `FluentConformanceTests`) via the direct xUnit v3 in-process runner → **48/48**, after the review fix.
- The three new logo tests were re-run **by name** and confirmed passing: `HeaderLogo_WhenNotProvidedAndDefaultDisabled_EmitsNoBrandLogoCell`, `HeaderLogo_WhenProvided_RendersBetweenHeaderStartAndAppTitle`, `HeaderLogo_WhenDefaultLogoOptedIn_RendersDecorativeAppsIconCell`.

### Acceptance Criteria

- **AC1 — IMPLEMENTED.** Zero-config header is unchanged: `HeaderStartHorizontalGap` is `null` when no logo, no logo markup is emitted, default `FcHamburgerToggle` + `AppTitle` remain. Story 8.1/8.2 governance guards stay green in the 48/48 lane.
- **AC2 — IMPLEMENTED.** Adopter `HeaderLogo` renders inside the existing left `FluentStack`, after `HeaderStart`/hamburger and before `AppTitle`; other slots, navigation, footer, and actions are untouched. Pinned by the ordering test.
- **AC3 — IMPLEMENTED.** Opt-in default reuses the existing `FcFluentIcons.Apps20()` 20px mark, rendered as a decorative (`aria-hidden="true"`) cell with stable `data-testid="fc-shell-brand-logo"`. No new icon package, raw interactive control, legacy Fluent v4/FAST token, or accent background (governance guards green).
- **AC4 — IMPLEMENTED.** `HeaderLogo:RenderFragment` and `ShowDefaultHeaderLogo:Boolean` are appended after `ContentLabelledBy`; existing parameters keep name/type/order. Pinned by `Parameter_surface_matches_story_3_2_contract`.

### File List vs git reality

Matches. The only modified file not in the File List (`_bmad-output/story-automator/orchestration-8-20260625-123921.md`) is correctly excluded per Dev Notes, and the `FcPageHeader.*` changes seen against the story baseline belong to the already-committed `61b3b65`, not Story 8.3.

### Findings

- **0 Critical / 0 High / 0 Medium.** The implementation is clean and correctly scoped (append-only API, custom-logo-wins precedence, decorative-only-default `aria-hidden`, gap gated so zero-config spacing is untouched).
- **Low (auto-fixed):** The custom-logo bUnit test asserted ordering/precedence but never pinned that an **adopter-supplied** logo is *not* marked `aria-hidden` — that accessibility branch was only covered by the sandbox-blocked e2e lane. Added `adopterLogoCell.GetAttribute("aria-hidden").ShouldBeNull()` to `HeaderLogo_WhenProvided_RendersBetweenHeaderStartAndAppTitle`; re-verified 48/48.
- **Low (accepted / follow-up):** The e2e custom-logo check `not.toHaveAttribute('aria-hidden', 'true')` is weaker than asserting attribute absence (it would pass on an `aria-hidden="false"` regression). Recommend tightening to assert absence. Not edited here because the `test:fc-shell-chrome` lane is sandbox-blocked from local execution (Kestrel socket + stale-TypeScript typecheck), so the change could not be verified.
