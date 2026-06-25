---
baseline_commit: e118a6279e1f07b411b94ba68763b33d3fa51ac5
---

# Story 8.1: Neutral header chrome + footer framing

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-25. -->

## Story

As an operator,
I want the shell header and footer to be neutral chrome with the brand accent used only as an accent,
so that the app looks modern instead of a saturated colored band.

## Acceptance Criteria

1. Given the shell header, when rendered in light or dark theme, then the header band uses `--colorNeutralBackground2` with a `--colorNeutralStroke2` bottom divider, the app title and action icons read in neutral foreground with sufficient contrast, and no brand-accent surface fill remains. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.1-Neutral-header-chrome-footer-framing; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.1-VR-1-Neutral-header-chrome-footer-framing-Minor-ship-first; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
2. Given the shell footer, when the default footer or an adopter-supplied `Footer` fragment renders, then it is framed by the same neutral chrome surface with a top divider, and the default copyright text renders through `FluentText` with `Color.Lightweight`. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.1-Neutral-header-chrome-footer-framing; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor; src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx]
3. Given the Fluent token governance rules, when Story 8.1 is complete, then no legacy Fluent v4/FAST token is introduced, `FluentConformanceTests` remains green, focused shell bUnit coverage pins the neutral header/footer markup, and visual/a11y evidence is refreshed intentionally for both light and dark themes. [Source: _bmad-output/project-context.md#Blazor-Shell-Fluxor-Rules-Consumer-Hexalith.FrontComposer.Shell; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance; tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs; tests/e2e/specs/specimen-accessibility.spec.ts]

## Tasks / Subtasks

- [x] Audit the current shell chrome before editing (AC: 1, 2, 3)
  - [x] Read `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` end to end; preserve Fluxor store initialization, skip links, cascading coordinators, header slots, account menu, navigation auto-populate, content landmark, providers, and dev-mode diagnostics.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`; confirm no parameter or state-machine change is needed for this visual-only story.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css`; keep the existing `--layout-header-height`, `--fc-color-*`, skip-link, and page-layout rules intact.
  - [x] Confirm the working tree still has the pre-8.1 markup: header `FluentStack` only has `height: 48px; padding: 0 12px;`, and footer renders raw localized text or `Footer` directly under `FluentLayoutItem`.
- [x] Implement the neutral header band (AC: 1, 3)
  - [x] In `FrontComposerShell.razor`, add the neutral surface and bottom divider to the top-level header `FluentStack` style: `background: var(--colorNeutralBackground2); border-block-end: 1px solid var(--colorNeutralStroke2);`.
  - [x] Keep the header height at `48px`, keep padding `0 12px`, and keep `HorizontalAlignment.SpaceBetween` so the title/left cluster and action/account cluster do not move.
  - [x] Do not change `FcShellOptions.AccentColor`, `IThemeService.SetThemeAsync`, `--fc-accent-base-color`, or the `--fc-color-accent` bridge variable. The accent remains available for active nav, focus, primary, links, and selected/badge states; it must simply stop painting the header surface.
  - [x] Do not introduce custom CSS classes unless the inline Fluent token style becomes unmaintainable; if CSS is added, use only Fluent 2 tokens and keep the governance guard clean.
- [x] Implement matching footer framing (AC: 2, 3)
  - [x] Wrap the footer content inside a `FluentStack` with `VerticalAlignment="VerticalAlignment.Center"` and style `padding: 8px 12px; min-height: 36px; background: var(--colorNeutralBackground2); border-block-start: 1px solid var(--colorNeutralStroke2);`.
  - [x] Render adopter-supplied `Footer` inside the framed stack instead of bypassing the chrome.
  - [x] Render the default localized copyright through `FluentText As="TextTag.Span" Size="TextSize.Size200" Color="Color.Lightweight"` so it follows Fluent v5 typography/color semantics.
  - [x] Preserve the `FooterCopyright` localization key and current year behavior; do not edit `.resx` files unless a failing test proves the resource is wrong.
- [x] Add focused shell tests (AC: 1, 2, 3)
  - [x] Extend `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` with a focused test that renders `FrontComposerShell` and asserts the header stack markup contains `background: var(--colorNeutralBackground2)` and `border-block-end: 1px solid var(--colorNeutralStroke2)`.
  - [x] Add a focused footer test asserting `border-block-start: 1px solid var(--colorNeutralStroke2)`, `background: var(--colorNeutralBackground2)`, `min-height: 36px`, and a `FluentText`/rendered default footer path rather than raw text directly under the layout item.
  - [x] Add or extend a test for adopter-supplied `Footer` so custom footer content is still rendered inside the framed footer chrome.
  - [x] Keep existing tests for header slots, `FcPaletteTriggerButton`, `FcSettingsButton`, `FcAccountMenu`, navigation auto-rendering, skip links, and shortcut routing green; do not weaken any existing assertions.
- [x] Verify visual/a11y and governance evidence (AC: 1, 2, 3)
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run the focused Shell test lane. Prefer direct xUnit v3 in-process execution if local VSTest socket permissions fail.
  - [x] Run the governance lane containing `FluentConformanceTests`, especially `Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog`.
  - [x] Run the solution default lane when possible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false`.
  - [x] Refresh visual/a11y evidence for both light and dark themes. If local browser/server constraints block Playwright, record the exact command and blocker, and leave the existing CI lane as the gate.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 8.1 focused results and any local blockers.
  - [x] Reconcile the File List against `git status --short` before moving the story to review.

### Review Follow-ups (AI)

- [ ] [AI-Review][Medium] Reshoot the 6 full-page visual baselines in CI on Windows so they encode the neutral chrome instead of the old teal band. `tests/e2e/specs/specimen-accessibility.spec.ts:188` captures `fullPage: true` of the specimen route, which renders inside `FrontComposerShell`, so the existing `*-chromium-win32.png` snapshots (light/dark × compact/comfortable/roomy) still show the pre-8.1 chrome and the `test:visual` lane will diff until regenerated. Not locally fixable: this environment is Linux/WSL2 and the baselines are `win32` platform-specific, so a local run would write `*-chromium-linux.png` (different platform + font rendering) and leave the win32 baselines stale. Run `npm --prefix tests/e2e run test:visual:update` on the Windows CI runner. [files: tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/*-chromium-win32.png]

## Dev Notes

- Story 8.1 is the first story in Epic 8 and is intentionally narrow: neutralize the framework-owned header/footer chrome. Do not implement Story 8.2's new accent-background regression guard, Story 8.3's logo slot, Story 8.4's density/grid polish, Story 8.5's rail/flyout, Story 8.6's `FcPageToolbar`, or Story 8.7's status icon generator changes in this story. [Source: _bmad-output/planning-artifacts/epics.md#Epic-8-Aspire-grade-Visual-Refresh-post-MVP-chrome-parity]
- Brownfield reality: architecture and epics already contain the Epic 8 accent-as-thread rule, but `FrontComposerShell.razor` still has the pre-8.1 header/footer markup. The recent commit `1d1f93f` updated planning/status/architecture artifacts only; it did not modify `src/Hexalith.FrontComposer.Shell`. Treat this as a real implementation story, not done-by-docs. [Source: git log -5 --oneline; git show --stat 1d1f93f; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- Root cause from the change proposal: the teal header is Shell-owned. Tenants host layout is just `<FrontComposerShell>@Body</FrontComposerShell>`, so this story must not edit the `Hexalith.Tenants` submodule or any host page. Host page-body adoption is separate Host-A work and requires explicit submodule approval. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Framework-host-boundary-decisive-for-scoping]
- Use Fluent UI v5 components and Fluent 2 tokens only. Do not copy Aspire's v4/FAST tokens (`--neutral-layer-*`, `--type-ramp-*`, `--design-unit`, `accentBaseColor`, `baseLayerLuminance`) and do not introduce raw interactive HTML controls. [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance; Hexalith.AI.Tools/hexalith-ux-instructions.md]
- The preferred implementation is the change-proposal shape: add the tokenized background/divider to the existing header `FluentStack`, then frame footer content with a `FluentStack` and `FluentText`. This keeps the change scoped and avoids inventing a new shell chrome abstraction. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.1-VR-1-Neutral-header-chrome-footer-framing-Minor-ship-first]
- Preserve header behavior exactly: `HeaderStart` defaults to `FcHamburgerToggle`, `HeaderCenter` remains optional, `HeaderEnd` defaults to palette/settings after the theme toggle, and `FcAccountMenu` remains always rendered at the far right so account controls survive adopter customization. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs; _bmad-output/project-docs/architecture.md#4-Runtime-composition-Shell]
- Preserve shell content/navigation behavior: skip links, `HasNavigation`, `NavigationWidth`, `IsSubCompactDesktopViewport`, `FrontComposerNavigation`, `FcLayoutBreakpointWatcher`, `FcSystemThemeWatcher`, `FcDensityApplier`, `FcDensityAnnouncer`, `FcProjectionConnectionStatus`, `FcPendingCommandSummary`, and the single `main` landmark remain unchanged. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellContentLandmarkTests.cs]
- No package/version/web research is needed. The relevant versions are pinned locally: .NET SDK `10.0.301`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, and Playwright `1.61.0`. Do not change package versions or add dependencies. [Source: _bmad-output/project-context.md#Technology-Stack-Versions; Directory.Packages.props; global.json]
- Use solution-level verification conventions: `.slnx` only, `TreatWarningsAsErrors=true`, `DiffEngine_Disabled=true` for Verify lanes, and Shouldly assertions rather than raw `Assert.*`. [Source: _bmad-output/project-context.md#Testing-Rules; _bmad-output/project-context.md#Code-Quality-Style-Rules]
- The working tree was clean before story creation. If dev-story sees unrelated dirty files later, do not revert them; reconcile only story-owned files in the File List. [Source: git status --short]

### Project Structure Notes

- Expected production touch points:
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` for verification only; Story 8.2 owns new accent-background guard logic.
  - `tests/e2e/specs/specimen-accessibility.spec.ts` and its snapshots only if the visual/a11y baseline must be reshot for the chrome change.
- Expected BMAD artifacts:
  - `_bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md`
  - `_bmad-output/implementation-artifacts/sprint-status.yaml`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Avoid touching:
  - `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**` submodules.
  - `src/Hexalith.FrontComposer.SourceTools/**`, generator snapshots, status badge components, density defaults, nav rail/flyout, `FcPageToolbar`, MCP, CLI, schema fingerprint/canonicalization, package version files, PublicAPI baselines, and pacts unless a Story 8.1 acceptance test proves direct ownership.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-8.1-Neutral-header-chrome-footer-framing]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.1-VR-1-Neutral-header-chrome-footer-framing-Minor-ship-first]
- [Source: _bmad-output/project-docs/architecture.md#4-Runtime-composition-Shell]
- [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- [Source: _bmad-output/project-context.md]
- [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs]
- [Source: tests/e2e/specs/specimen-accessibility.spec.ts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Audited `FrontComposerShell.razor`, `FrontComposerShell.razor.cs`, `FrontComposerShell.razor.css`, existing shell layout tests, Fluent governance tests, and e2e specimen/a11y scripts.
- 2026-06-25: Confirmed RED phase for the three new Story 8.1 bUnit tests: header neutral chrome, default footer `FluentText` frame, and adopter footer frame all failed before implementation.
- 2026-06-25: Exact solution build/test and Playwright lanes are locally blocked by environment constraints recorded in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- 2026-06-25: QA Generate E2E Tests workflow added focused Playwright chrome coverage for light/dark shell header/footer neutral token rendering; local runtime remains blocked by Kestrel socket permissions and normal e2e typecheck by stale TypeScript 5.9.3 in `node_modules`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.1 created as a narrow Shell chrome implementation story.
- Validation pass completed against the create-story checklist; guardrails added for Fluent v5/Fluent 2 tokens, header/footer scope, existing shell behavior preservation, governance verification, visual/a11y evidence, and submodule boundaries.
- Implemented neutral shell header chrome using `--colorNeutralBackground2` and `--colorNeutralStroke2` while preserving header height, padding, slot/account/action ordering, accent bridge variables, and shell behavior.
- Framed default and adopter-supplied footer content in matching neutral Fluent chrome; default footer now renders through `FluentText` with lightweight color semantics while preserving localization/current-year behavior.
- Added focused bUnit coverage for header neutral tokens, default footer `FluentText`, and custom footer content inside the framed chrome; existing shell layout behavior tests remain green at 27/27.
- Verified the focused Story 8.1 direct xUnit lane, full `FrontComposerShellTests` class, and Fluent legacy-token governance method. Local solution/browser lanes are blocked by NuGet audit network, intentionally uninitialized nested submodule projects, and sandbox socket restrictions; exact commands and blockers are recorded in the test summary.
- QA Generate E2E Tests added `tests/e2e/specs/shell-chrome.spec.ts` plus `test:fc-shell-chrome` to pin live browser header/footer computed styles against neutral Fluent tokens in light and dark themes, including accent-surface regression and contrast checks.

### File List

- `_bmad-output/implementation-artifacts/8-1-neutral-header-chrome-and-footer-framing.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/specs/shell-chrome.spec.ts`

### Change Log

- 2026-06-25: Captured baseline commit and moved Story 8.1 into implementation.
- 2026-06-25: Added neutral Fluent token header/footer chrome and focused bUnit coverage for default/custom footer paths.
- 2026-06-25: Recorded focused test/governance success plus local solution/Playwright blockers; moved Story 8.1 to review.
- 2026-06-25: QA Generate E2E Tests added focused Playwright shell chrome coverage and recorded local validation blockers.
- 2026-06-25: Senior Developer Review (AI) — adversarial review; verified build/tests/governance locally (not just claims), confirmed AC1/AC2/AC3 implementation and File List accuracy; recorded one Medium CI follow-up (visual baseline reshoot); moved Story 8.1 to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-25
**Outcome:** Approve (status → done; 0 Critical, 0 High blocking, 1 Medium follow-up, 1 Low note)

### What was verified (not merely trusted)

The dev record claimed local build/test lanes were "blocked by environment constraints." The review re-ran them via the direct xUnit v3 in-process executable (which bypasses the VSTest socket transport the dev hit) and confirmed the implementation independently:

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/...csproj -c Release` → **0 warnings / 0 errors**.
- `FrontComposerShellTests` class → **27/27** pass (incl. the 3 new Story 8.1 chrome pins).
- `FluentConformanceTests` governance class → **5/5** pass, incl. `Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog` (empty backlog → guard blocks any legacy token; the new `--colorNeutralBackground2`/`--colorNeutralStroke2` are camelCase Fluent 2 tokens, not matched).
- **Full `Hexalith.FrontComposer.Shell.Tests` assembly → 1966/1966** pass, 0 failed/skipped — no regression anywhere in the Shell suite.

### Acceptance Criteria

- **AC1 (neutral header chrome): IMPLEMENTED.** `FrontComposerShell.razor:51` adds `background: var(--colorNeutralBackground2); border-block-end: 1px solid var(--colorNeutralStroke2);` to the existing header `FluentStack`, preserving `height: 48px`, `padding: 0 12px`, and `HorizontalAlignment.SpaceBetween`. No brand-accent surface fill remains (CSS isolation file paints no header accent; accent bridge `--fc-accent-base-color`/`--fc-color-accent` left intact for nav/focus/links only). bUnit pin also asserts absence of `--colorBrandBackground`/`--colorCompoundBrandBackground`.
- **AC2 (footer framing): IMPLEMENTED.** `FrontComposerShell.razor:133-147` wraps both the adopter-supplied `Footer` and the default copyright inside a neutral `FluentStack` (`padding: 8px 12px; min-height: 36px; background: var(--colorNeutralBackground2); border-block-start: 1px solid var(--colorNeutralStroke2);`); the default copyright now renders through `FluentText As="TextTag.Span" Size="TextSize.Size200" Color="Color.Lightweight"` while preserving the `FooterCopyright` key and current-year behavior.
- **AC3 (governance + evidence): PARTIAL by design.** No legacy v4/FAST token introduced (verified), `FluentConformanceTests` green (verified), focused shell bUnit coverage pins the markup (verified). The visual/a11y baseline was **not** refreshed — see M1; the story explicitly permits leaving CI as the gate when Playwright is blocked, and the dev recorded the exact commands/blockers, so the `[x]` on the evidence task is justified via its documented-blocker completion path rather than a false claim.

### File List vs git reality

Accurate. All 7 listed deliverables match `git status`. The only undocumented working-tree change is `_bmad-output/story-automator/orchestration-8-20260625-123921.md`, which is an auto-maintained story-automator tracking file under the review-excluded `_bmad-output/` tree — not a Story 8.1 deliverable. No discrepancy.

### Findings

- **M1 (Medium) — stale visual baselines.** `specimen-accessibility.spec.ts:188` captures `fullPage: true` of the specimen route, which renders inside `FrontComposerShell`; the 6 existing `*-chromium-win32.png` snapshots still encode the teal chrome, so the `test:visual` lane will diff until they are reshot. **Not auto-fixable from this environment** (Linux/WSL2 vs `win32`-pinned, platform-specific snapshots). Tracked as a Review Follow-up to regenerate on the Windows CI runner. Does not block automation (Medium, not Critical).
- **L1 (Low) — documented-blocker `[x]` tasks.** "Run the solution-level build/test" and "Refresh visual/a11y evidence" are marked `[x]` via the documented-blocker fallback (per-project build succeeded; CI is the visual gate) rather than direct success. Consistent with the established project convention and honestly recorded in `test-summary.md`; noted for transparency, no action required.
- **e2e contract check (informational):** `shell-chrome.spec.ts` is structurally sound and could not be silently broken — its hardcoded strings/selectors were cross-checked against source: `AppTitle`="Hexalith FrontComposer", `FooterCopyright`="Hexalith FrontComposer © {0}", `data-testid="fc-settings-button"`, and `settings.page.ts` (`openViaButton`/`selectTheme`/`getByTitle('Change theme')`) all match. Local execution remains blocked by the specimen-host Kestrel socket; CI is the gate.

### Conclusion

Implementation matches the change-proposal shape exactly, is scoped to the Shell (no submodule/host edits), and is fully green at build + unit/governance level. No Critical or High blocking issues. Approved and moved to **done**, with M1 carried as a CI follow-up.
