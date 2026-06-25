---
baseline_commit: 14d410505dd0d32462e4ca7b9421ee72556cbc58
---

# Story 8.2: Accent-as-thread policy + regression guard

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-25. -->

## Story

As an adopter developer,
I want a documented and guarded rule that the brand accent is never a chrome surface fill,
so that the neutral-header design cannot silently regress.

## Acceptance Criteria

1. Given `architecture.md` section 4.1, when Story 8.2 is complete, then it states that the accent (`FcShellOptions.AccentColor`, default `#0097A7`) is a thread used for active nav, focus, primary buttons, links, and badge/selected states, and MUST NOT fill header, navigation, or footer surfaces; those chrome surfaces stay on `--colorNeutralBackground*` with neutral dividers. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.2-Accent-as-thread-policy-regression-guard; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
2. Given the Shell `FluentConformanceTests` Governance guard, when a Shell `.razor` or `.css` source uses `--fc-color-accent` or `--fc-accent-base-color` as a `background` or `background-color` value, then the guard fails the build with an actionable file list and an empty shrink-only allowlist. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.2-VR-2-Accent-as-thread-doc-narrow-regression-guard; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
3. Given Story 8.1's neutral shell chrome, when Story 8.2 is verified, then the new guard passes against the current neutral header/footer implementation, the existing legacy-token Governance guard remains green, and no production Shell behavior, package version, Fluent token policy, submodule, or later Epic 8 story scope is changed. [Source: _bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md#Senior-Developer-Review-AI; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css]

## Tasks / Subtasks

- [x] Confirm the architecture rule before editing tests (AC: 1)
  - [x] Read `_bmad-output/project-docs/architecture.md` section 4.1 and verify the existing "Accent is a thread, never a chrome fill" paragraph still matches AC1.
  - [x] If the paragraph has drifted, update only that section; do not rewrite unrelated architecture, UX-DR2, nav, density, toolbar, or status-icon text.
  - [x] Keep the rule additive: `--fc-color-accent` and `--fc-accent-base-color` remain valid semantic/thread variables, but they are forbidden as chrome surface backgrounds.
- [x] Add the accent-as-background Governance guard (AC: 2, 3)
  - [x] Extend `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` with a focused `[Fact]`, for example `Shell_chrome_styles_never_use_accent_as_surface_background`.
  - [x] Enumerate Shell source `.css` and `.razor` files under `src/Hexalith.FrontComposer.Shell`, with the same `EnumerationOptions` and `IsBuildOutput` discipline used by `AssertNoLegacyTokens`.
  - [x] Detect `background:` and `background-color:` declarations whose value references `var(--fc-color-accent)` or `var(--fc-accent-base-color)`.
  - [x] Do not flag non-background accent usage: custom property definition (`--fc-color-accent: var(--fc-accent-base-color)`), `color`, `border`, `outline`, focus rings, active-nav indicators, links, primary/button affordances, badges, selected states, and Story 8.5's future accent left-bar are allowed thread uses.
  - [x] Include an empty shrink-only allowlist, modeled after the legacy-token `migrationBacklog`, plus stale-entry detection so any future allowlisted file must be removed once clean.
  - [x] Make the failure message list repository-relative paths and explain that chrome backgrounds must use neutral Fluent 2 tokens, not the accent bridge variables.
- [x] Add RED-phase confidence before finalizing implementation (AC: 2)
  - [x] Temporarily introduce an accent background in a Shell `.razor` or `.css` source and confirm the new guard fails for that file.
  - [x] Remove the temporary violation before committing.
  - [x] Do not keep synthetic fixture files unless they are needed for a durable unit test; if fixtures are added, keep them under the test project and outside the production Shell scan root.
- [x] Preserve Story 8.1 and later Epic 8 boundaries (AC: 3)
  - [x] Do not change `FrontComposerShell.razor` unless the new guard reveals a genuine accent-as-background violation.
  - [x] Do not change `FrontComposerShell.razor.css` except for a genuine guard-driven cleanup; its current `--fc-color-accent: var(--fc-accent-base-color)` mapping is allowed and is pinned by `SlotMappingRegressionTests`.
  - [x] Do not reshoot the Story 8.1 Windows full-page visual baselines as part of this story unless a Story 8.2 code change legitimately changes visuals; that CI follow-up is inherited from 8.1.
  - [x] Do not implement Story 8.3 logo slot, Story 8.4 density/grid polish, Story 8.5 nav rail/flyout, Story 8.6 `FcPageToolbar`, or Story 8.7 status icons.
  - [x] Do not edit `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`, package versions, PublicAPI baselines, pacts, SourceTools, MCP, CLI, schema fingerprint code, or generated output.
- [x] Verify and record evidence (AC: 1, 2, 3)
  - [x] Run `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false`.
  - [x] Run the new guard directly. If VSTest socket permissions fail locally, use the built xUnit v3 in-process executable as established by Story 8.1.
  - [x] Run the full `FluentConformanceTests` governance class, especially the existing legacy-token guard and the new accent-as-background guard.
  - [x] Run the focused shell chrome tests from Story 8.1 to prove neutral header/footer behavior was not disturbed.
  - [x] Run the solution default lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false`.
  - [x] Run existing light/dark shell chrome browser coverage when feasible: `npm --prefix tests/e2e run test:fc-shell-chrome`.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 8.2 commands, pass/fail counts, and exact local blockers.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Story 8.2 is a guard story, not a visual redesign story. The visual implementation from Story 8.1 is already done: `FrontComposerShell.razor` uses neutral header/footer backgrounds (`--colorNeutralBackground2`) and neutral dividers (`--colorNeutralStroke2`). The new work should prevent that principle from regressing. [Source: _bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md#Acceptance-Criteria; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- Brownfield reality: AC1 is already substantially present in `_bmad-output/project-docs/architecture.md` lines 140-149. Treat the documentation task as verify-and-adjust, not as permission to rewrite architecture. [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- Brownfield gap: `FluentConformanceTests` currently has raw-control, legacy-token, v5 accordion, and generator accordion guards, but no accent-as-background guard. Add the new guard in that class to keep all Shell Fluent governance in one place. [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- Use the existing static-scan style: `RegexOptions.Compiled | RegexOptions.CultureInvariant`, `EnumerationOptions` with `FileAttributes.ReparsePoint | FileAttributes.Hidden`, `IgnoreInaccessible = true`, `IsBuildOutput` exclusions, `Shouldly` assertions, and repository-relative offender paths. [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- The guard must distinguish forbidden surface fills from allowed accent thread uses. Forbidden examples are `background: var(--fc-color-accent)`, `background-color: var(--fc-accent-base-color)`, or equivalent declarations in Shell `.razor` inline `Style=` / `.razor.css` / `.css` files. Allowed examples include `--fc-color-accent: var(--fc-accent-base-color)`, `color: var(--fc-color-accent)`, border/focus/outline usage, active nav indicators, links, primary actions, badges, and selected states. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Section-1-Issue-Summary; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- Keep the allowlist empty on completion. If the dev temporarily uses an allowlist to prove stale-entry behavior, remove the temporary entry and violation before review. Adding a permanent carve-out contradicts the story unless Product/Architecture explicitly changes AC2. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.2-Accent-as-thread-policy-regression-guard; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- Do not weaken the existing legacy-token guard. The Epic 8 change proposal bans copying Aspire's v4/FAST tokens such as `--neutral-layer-*`, `--type-ramp-*`, `--design-unit`, `accentBaseColor`, and `baseLayerLuminance`; every rule must stay translated to Fluent 2 tokens and Fluent v5 components. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Section-1-Issue-Summary; Hexalith.AI.Tools/hexalith-ux-instructions.md]
- No package/version/web research is needed. Relevant versions are pinned locally: .NET SDK `10.0.301`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, and Playwright `1.61.0`. Do not change package versions or add dependencies. [Source: _bmad-output/project-context.md#Technology-Stack-Versions; Directory.Packages.props; global.json]
- Story 8.1 review identified stale Windows full-page visual baselines as a Medium follow-up. That is not Story 8.2 scope unless this story changes rendered visuals; record it as inherited if a broad visual lane still reports it. [Source: _bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md#Review-Follow-ups-AI]
- The current working tree before Story 8.2 creation contains an unrelated modified story-automator orchestration file. Do not revert unrelated files; reconcile only Story 8.2-owned files. [Source: git status --short]

### Project Structure Notes

- Expected production/documentation touch points:
  - `_bmad-output/project-docs/architecture.md` only if the existing section 4.1 accent-as-thread paragraph has drifted.
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`
- Expected verification-only files:
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/SlotMappingRegressionTests.cs`
  - `tests/e2e/specs/shell-chrome.spec.ts`
- Expected BMAD artifacts:
  - `_bmad-output/implementation-artifacts/8-2-accent-as-thread-policy-and-regression-guard.md`
  - `_bmad-output/implementation-artifacts/sprint-status.yaml`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Avoid touching:
  - `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`
  - `src/Hexalith.FrontComposer.SourceTools/**`
  - `src/Hexalith.FrontComposer.Mcp/**`
  - `src/Hexalith.FrontComposer.Cli/**`
  - `src/Hexalith.FrontComposer.Schema/**`
  - `Directory.Packages.props`, `.sln`, `.slnx` structure, PublicAPI baselines, pacts, generated snapshots, status badge/icon emitters, density defaults, nav rail/flyout, and page toolbar files.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-8.2-Accent-as-thread-policy-regression-guard]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.2-VR-2-Accent-as-thread-doc-narrow-regression-guard]
- [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- [Source: _bmad-output/project-context.md]
- [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md]
- [Source: _bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/SlotMappingRegressionTests.cs]
- [Source: tests/e2e/specs/shell-chrome.spec.ts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Create-story analysis loaded BMAD config, sprint status, Epic 8, the 2026-06-25 change proposal, project context, Hexalith UX rules, architecture section 4.1, Story 8.1, current Shell chrome markup/CSS, Shell governance tests, and recent git history.
- 2026-06-25: Dev-story verified architecture.md §4.1 already satisfies AC1; no architecture edit was needed.
- 2026-06-25: Added Shell accent-as-background static scan guard, proved RED by temporarily adding `background: var(--fc-color-accent);` to `FrontComposerShell.razor.css`, then removed the temporary violation.
- 2026-06-25: Local VSTest and Playwright lanes remain socket-blocked (`System.Net.Sockets.SocketException (13): Permission denied`); direct xUnit v3 in-process lanes provide focused verification.

### Implementation Plan

- Verify the existing architecture policy first and avoid documentation churn if AC1 is already met.
- Add a Shell governance `[Fact]` beside the existing Fluent conformance guards, reusing source enumeration and build-output exclusions.
- Detect only `background`/`background-color` declarations that reference the Shell accent bridge variables, leaving allowed accent thread uses untouched.
- Keep the allowlist empty with stale-entry enforcement and validate the guard with a temporary RED-phase production-source violation.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.2 created as a narrow documentation-confirmation plus Governance-regression-guard story.
- Validation pass completed against the create-story checklist; guardrails added for existing architecture truth, Story 8.1 preservation, allowed vs forbidden accent uses, empty shrink-only allowlist behavior, verification lanes, and submodule/later-Epic-8 boundaries.
- Confirmed `_bmad-output/project-docs/architecture.md` §4.1 already states the accent-as-thread/no-chrome-fill rule with the expected default accent and neutral chrome tokens.
- Added `Shell_chrome_styles_never_use_accent_as_surface_background` to Shell Fluent governance with an empty shrink-only allowlist and repository-relative failure paths.
- Verified RED/green behavior, full Fluent governance, and Story 8.1 neutral header/footer pins through the direct xUnit v3 in-process runner.
- Recorded broader local blockers: solution VSTest, Pact mock server, and Playwright/Kestrel cannot bind sockets in this sandbox.

### File List

- `_bmad-output/implementation-artifacts/8-2-accent-as-thread-policy-and-regression-guard.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`

### Change Log

- 2026-06-25: Added Shell governance guard for accent bridge variables used as background/background-color values, confirmed architecture policy, and recorded validation evidence.
- 2026-06-25: Story-automator adversarial review (auto-fix). Re-verified all dev claims by building and running the suite locally (sockets were NOT blocked in this environment): FluentConformanceTests 17/17, Story 8.1 preservation lane 28/28, full Shell.Tests assembly 1978/1978 (incl. the Pact contract test). Independently re-proved RED (temporary `background: var(--fc-color-accent)` in `FrontComposerShell.razor.css` failed the guard with the exact repo-relative offender path) then GREEN. Auto-fixed 1 LOW: corrected the inaccurate `AccentSurfaceBackgroundDeclaration` regex comment that miscredited the negative lookbehind. Outcome: Approve → done. 0 Critical / 0 High / 0 Medium.

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-25 · **Outcome:** ✅ Approve (Status → done)

### Verification performed (claims re-checked, not trusted)
- Build: `dotnet build … Shell.Tests … -c Release` → 0 warnings / 0 errors.
- New guard + matcher pins: `FluentConformanceTests` → **17/17** (the new `Shell_chrome_styles_never_use_accent_as_surface_background` fact, the 5 forbidden-declaration theory cases, and the 6 allowed-thread-use theory cases all green).
- RED re-proof: appended `background: var(--fc-color-accent)` to `FrontComposerShell.razor.css`, rebuilt, ran the guard → **failed 1/1** with offender `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` and the actionable Fluent-2-token message; reverted to a byte-identical baseline and reconfirmed **17/17** GREEN.
- Story 8.1 preservation: `FrontComposerShellTests` + `SlotMappingRegressionTests` → **28/28**.
- Regression scope: full `Hexalith.FrontComposer.Shell.Tests` assembly → **1978/1978** (the dev's environment reported a Pact mock-server socket block; this environment had none, so the contract test also passed). No production `src/**` content changed (the lone `*.razor.css` git entry is a CRLF/LF artifact, byte-identical to baseline).

### AC verdict
- **AC1 — IMPLEMENTED.** `architecture.md` §4.1 already states the accent-is-a-thread / never-a-chrome-fill rule with default `#0097A7`, neutral `--colorNeutralBackground{1,2}` + `--colorNeutralStroke2` chrome, and even references this Story 8.2 guard. Verify-and-keep was the correct call; no doc churn.
- **AC2 — IMPLEMENTED.** The guard fails the build on `background`/`background-color` declarations referencing `var(--fc-color-accent)` / `var(--fc-accent-base-color)` across Shell `.css` and `.razor`, with a repo-relative offender list and an empty shrink-only allowlist plus stale-entry detection (modeled on `AssertNoLegacyTokens`).
- **AC3 — IMPLEMENTED.** New guard green against current neutral chrome, existing legacy-token guard green, no production behavior / package version / Fluent-token policy / submodule / later-Epic-8 scope changed (1978/1978 confirms).

### File List reconciliation
Accurate. The four listed files are the only Story-8.2-owned changes. `Hexalith.AI.Tools` (submodule pointer) and `_bmad-output/story-automator/orchestration-8-20260625-123921.md` were already dirty before this story and are correctly excluded (Dev Notes acknowledge the orchestration file).

### Findings (0 Critical / 0 High / 0 Medium / 3 Low)
- **[LOW · FIXED]** The `AccentSurfaceBackgroundDeclaration` comment claimed the negative lookbehind "excludes custom property names such as `--fc-color-accent`". It does not — that variable contains no `background` keyword, so the regex never matches it regardless of the lookbehind. The lookbehind's real job is to avoid matching `background` as the suffix of a longer identifier (e.g. a custom-property *definition* `--…-background: …`). Comment rewritten to describe the actual mechanism. `FluentConformanceTests.cs`.
- **[LOW · Accepted]** The guard is case-sensitive (`background`, lowercase). Uppercase CSS property names would evade it. This is consistent with the deliberate case-sensitivity of the sibling raw-control and legacy-token guards and the codebase's strictly-lowercase CSS authoring; changing only this guard would make it inconsistent for a risk that does not occur. No change.
- **[LOW · Accepted / future hardening]** Per AC2's literal "`background`/`background-color` value" scope, an accent applied via `background-image` (longhand gradient) or via an intermediate custom property (`--x: var(--fc-color-accent); background: var(--x);`) would not be flagged. The `background:` shorthand gradient *is* caught. These are inherent static-scan boundaries deliberately left at AC2's scope; the story explicitly warns against guard expansion, so no change here. Candidate hardening for a later governance story if accent chrome regressions recur.
