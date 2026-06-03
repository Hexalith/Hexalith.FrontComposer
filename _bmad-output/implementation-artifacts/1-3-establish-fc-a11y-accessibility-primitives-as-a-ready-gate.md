---
baseline_commit: 68034f1be194eebf629466d1128f0b12f7b31e12
---

# Story 1.3: Establish FC-A11Y accessibility primitives as a ready-gate

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this first.** The accessibility primitives this story is asked to
> "establish" **already exist and are already tested** in the shell. This is **the FC-LYT story
> (1.2) shape again**: a *confirm-and-document* ready-gate story, **not a build-from-scratch** one.
> Concretely, at baseline `68034f1` the shell already ships:
> - **Skip links** → `#fc-main-content` and (conditionally) `#fc-nav`, visually-hidden-until-focused
>   (`FrontComposerShell.razor:29-33`, CSS `FrontComposerShell.razor.css:21-36`).
> - **Focus visibility with a zero-override invariant** — the skip link inherits Fluent UI's
>   `--colorStrokeFocus2` via the native `:focus-visible` default; **no `outline:none`/focus
>   suppression exists** anywhere in shell CSS (verified sweep).
> - **`aria-label` / `role` / `aria-live` / `data-testid`** across every interactive shell element
>   (theme toggle, palette trigger, settings button, nav rail, command palette combobox/listbox,
>   density announcer, connection status, pending-command summary).
> - **The HFC1050–HFC1055 override-accessibility diagnostics are fully implemented and emitting**
>   via `CustomizationAccessibilityAnalyzer` (SourceTools), documented under `docs/diagnostics/`.
> - **The e2e a11y lane** (`npm run test:a11y`) already runs axe-core against the Counter specimen
>   host with a blocking serious/critical gate.
>
> So Story 1.3 is **(1) author the FC-A11Y contract doc** that names the primitive set, the
> default/opt-in patterns, and the HFC1050–HFC1055 enforcement linkage; **(2) confirm it with
> Product/UX + the Tenants author or escalate with a named owner** (and queue the FC-DOC cross-link
> to Story 1.5); **(3) pin the *shell-frame* ready-gate invariants** (skip-link → content target,
> accessible-name coverage, no suppressed focus) with **one consolidated bUnit test** that the rest
> of Epic 1+ can point at as "the single testable a11y ready-gate"; **(4) confirm the e2e a11y lane
> is green** against the bootstrapped shell. Do **NOT** re-implement announcers, diagnostics, the
> palette, or the specimen lane — they exist. Do **NOT** scratch-write the CI-gated `docs/` site.

## Story

As an adopter developer,
I want the shell's accessibility primitives (skip links, focus visibility, `aria-label`/`role`/`aria-live` patterns, keyboard reachability) confirmed and documented as a reusable contract,
so that every later story can satisfy a single, testable accessibility ready-gate.

## Acceptance Criteria

**AC1 — The bootstrapped shell frame is accessible: skip-link target, accessible names, visible focus. *(AR2, NFR6)***
**Given** the shell frame,
**When** rendered,
**Then** the skip link(s) target the content region (`href="#fc-main-content"` resolving to the `#fc-main-content` focus target), **and** every interactive shell-chrome element carries an accessible name (`aria-label`/`Title`/visually-hidden label), **and** focus indicators are visible — i.e. **no shell CSS suppresses focus** (`outline:none`/`box-shadow:none` without a `:focus-visible` restore). Pin these three invariants with a consolidated bUnit test so the ready-gate cannot silently regress.

**AC2 — The FC-A11Y primitive set is documented, the HFC1050–HFC1055 enforcement linkage is recorded, and the contract is confirmed or escalated with a named owner. *(AR2, AR4, NFR6)***
**Given** the documented FC-A11Y primitive set (skip links, focus visibility, `aria-label`/`role`/`aria-live` patterns, keyboard reachability, reduced-motion + forced-colors fallbacks),
**When** a custom override violates one (missing accessible name, keyboard trap/unreachable, suppressed focus, missing `aria-live` parity, motion without reduced-motion, color without forced-colors),
**Then** the corresponding **HFC1050–HFC1055** diagnostic is named in the contract as the **agreed enforcement mechanism**,
**And** the contract is marked **confirmed** (primitive set + enforcement agreed) **or** the open question is **escalated with a named owner**, **and** queued for the FC-DOC cross-link owned by **Story 1.5** (do **not** scratch-write `docs/`).

**AC3 — The e2e a11y lane passes against the bootstrapped shell with no critical violations.**
**Given** the e2e a11y lane (`npm run test:a11y` → `playwright test specs/specimen-accessibility.spec.ts --project=chromium`),
**When** run against the bootstrapped shell / Counter specimen host (`Hexalith__FrontComposer__Specimens__Enabled=true`),
**Then** it passes with **no blocking (serious/critical) axe violations** on each specimen route (WCAG 2.1 AA tagset), and the run is recorded in the Dev Agent Record. If the lane cannot be executed in this environment (no Node/Playwright/browser), **record that explicitly** and cite the existing green CI `accessibility-visual` job as the standing evidence — do not silently claim a pass.

## Tasks / Subtasks

- [x] **Task 1 — Author the FC-A11Y contract doc (AC: #1, #2) — the DECISION/DOC deliverable**
  - [x] Create `_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md`. **Mirror the structure/tone of the FC-LYT contract** (`_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md`): front-matter (`title`, `date`, `story: '1.3'`, `status`, `owner`, `supersedes`), a "Decision deliverable" intro, the contract body, a **Confirmation** section, and an **FC-DOC linkage** section. **Never** write to the CI-gated `docs/` DocFX site (project-context "docs/ is a PUBLISHED DocFX site" rule); BMAD docs live under `_bmad-output/`.
  - [x] **Document the FC-A11Y primitive set** as the reusable, testable ready-gate. For each primitive, name the pattern + the canonical example in the codebase (cite file:line):
    1. **Skip links** — visually-hidden-until-focused `.fc-skip-link` → `#fc-main-content` (always) and `#fc-nav` (when `HasNavigation`); targets carry `tabindex="-1"` (`FrontComposerShell.razor:29-33,87,107`; CSS `:21-36`).
    2. **Focus visibility (zero-override invariant)** — focus rings inherit Fluent UI `--colorStrokeFocus2` via native `:focus-visible`; **no `outline:none`/`box-shadow:none`** without a `:focus-visible` restore (CSS comment `FrontComposerShell.razor.css:18-20`).
    3. **`aria-live` politeness ladder** — `role="status"` + `aria-live="polite"` for non-urgent, escalating to `role="alert"` + `aria-live="assertive"` for rejections/errors; `aria-atomic="true"`; skip-first-render to avoid page-load announces (`FcDensityAnnouncer.razor:10-18`, `FcLifecycleWrapper`, `FcExpandedRowHiddenBanner` WCAG 4.1.2, `FcProjectionConnectionStatus`, `FcPendingCommandSummary`).
    4. **`role` / `aria-label` on interactive elements** — combobox/listbox/option/group for the palette; `aria-label`/`Title` on icon buttons; visually-hidden (`.fc-sr-only` / `.fc-visually-hidden`) labels where an icon button would otherwise be unnamed (avoid double/triple announces).
    5. **Keyboard reachability** — global `Ctrl+,` (settings) / `Ctrl+K` (palette) via `IShortcutService`; per-key `preventDefault` in `fc-keyboard.js` that **never swallows Tab** (no focus traps).
    6. **Reduced-motion + forced-colors fallbacks** — `@media (prefers-reduced-motion: reduce)` disables transitions/animations; color styling is token-driven (Fluent UI tokens auto-adapt under forced-colors); `color-mix()` rules ship a solid-rgba fallback before the `@supports` block (`wwwroot/css/fc-projection.css`).
  - [x] **Record the enforcement linkage (AC2):** state that **HFC1050–HFC1055** (`CustomizationAccessibilityAnalyzer`, build-time `Warning`, TWAE-promoted) are the **agreed enforcement mechanism** for Level-2/3/4 override violations, mapping each ID → primitive → WCAG criterion (table below in Dev Notes). These are **already implemented + documented** under `docs/diagnostics/HFC105*.md` — the contract **references** them; this story does not modify the analyzer.
  - [x] **Confirmation section:** mark `confirmed` OR escalate with a **named owner**. Owner per the readiness request (`frontcomposer-readiness-request-2026-06-03.md:23`, 🔴 row): **FrontComposer + Tenants author** (Product/UX for any visual-design input). **YOLO mode:** if no live confirmation is available, write it as **escalated with owner = "FrontComposer + Tenants author / Product/UX (pending)"** and proceed — AC2 explicitly permits escalate-with-owner. List any genuinely open questions (e.g. "is the bUnit shell-frame gate + the e2e axe lane the complete ready-gate, or is a per-story checklist also required?").
  - [x] **FC-DOC linkage:** record this contract's path so **Story 1.5 (FC-DOC)** can cross-link it from the published component docs. Do **not** pre-create a `docs/` page.

- [x] **Task 2 — Pin the shell-frame a11y ready-gate with a consolidated bUnit test (AC: #1) — the CODE deliverable**
  - [x] Add `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story13AccessibilityPrimitivesTests.cs`. **Extend `LayoutComponentTestBase`** (Fluxor store + theme mock + beforeunload/keyboard JS stubs; `JSInterop.Mode = Loose`) and follow `FrontComposerShellTests.cs` / `Story12PageLayoutTests.cs` render-and-query house style.
  - [x] **Skip-link → content target (AC1):** render `FrontComposerShell`; assert a `a.fc-skip-link[href="#fc-main-content"]` exists **and** a `#fc-main-content` element with `tabindex="-1"` exists (the link resolves to a real focus target). When navigation is present (registry has ≥1 renderable manifest), assert the second skip link `href="#fc-nav"` resolves to `#fc-nav[tabindex="-1"]`. Reuse the registry/viewport setup from `FrontComposerShellTests.cs` for the has-navigation case.
  - [x] **Accessible-name coverage (AC1):** render the shell with navigation; assert the always-present header-chrome interactive elements expose an accessible name — at minimum the palette trigger (`PaletteTriggerAriaLabel`), settings button (`SettingsTriggerAriaLabel`), and theme toggle (`ThemeToggleAriaLabel`) carry a non-empty `aria-label`/`Title`. Assert against the **localized** values via `IStringLocalizer<FcShellResources>` (don't hard-code English — FC-L10N, Story 1.4). Keep assertions resilient: check the attribute is present and non-empty rather than over-binding to exact copy where a resource key is the source of truth.
  - [x] **No suppressed focus (AC1):** add a guard test that reads the shell's scoped CSS (`FrontComposerShell.razor.css`) and asserts it contains **no** `outline: none` / `box-shadow: none` that lacks an adjacent `:focus-visible` restore (mirror the file-read assertion style in `FcLifecycleWrapperA11yTests.Reduced_motion_media_query_present_in_scoped_css`). This pins the documented zero-override invariant. Resolve the CSS path relative to the test assembly the same way the existing CSS-reading test does.
  - [x] **Method naming:** three-part `Subject_Scenario_Expectation`; file plural `…Tests.cs`. Use **Shouldly** (`ShouldContain`/`ShouldBe`/`ShouldNotBeNullOrEmpty` — never raw `Assert.*`).
  - [x] **Do NOT add a `[Parameter]` to `FrontComposerShell`.** This story adds no shell surface — the locked 7-parameter surface (`FrontComposerShellParameterSurfaceTests`) must stay **untouched and green** (verify it).

- [x] **Task 3 — Confirm the e2e a11y lane against the bootstrapped shell (AC: #3)**
  - [x] Identify the lane: `npm --prefix tests/e2e run test:a11y` = `playwright test specs/specimen-accessibility.spec.ts --project=chromium`, which auto-launches the Counter specimen host (`samples/Counter/Counter.Web`, `ASPNETCORE_ENVIRONMENT=Test`, `Hexalith__FrontComposer__Specimens__Enabled=true`, `http://127.0.0.1:5070`) and asserts **zero serious/critical axe violations** on each specimen route (WCAG 2.1 AA), per `tests/e2e/helpers/a11y.ts:16-18,35-64`.
  - [x] **Attempt to run it.** Prereqs: `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release`, `npm ci` (root) + `npm ci --prefix tests/e2e`, `npx playwright install --with-deps chromium`. If the environment supports it, run the lane and record the result (route count, blocking-violation count = 0 expected) in the Dev Agent Record.
  - [x] **If the lane cannot run here** (no Node/Playwright/browser/display), **say so explicitly** and cite the standing evidence: the CI `accessibility-visual` job (`.github/workflows/ci.yml`) runs this exact lane on `windows-latest` and must be green. Do **not** claim a local pass that did not happen (project-context "report outcomes faithfully").
  - [x] **Do not modify** the specimen lane, specs, helpers, or the manifest — AC3 is a *confirm-green*, not a *change*.

- [x] **Task 4 — Build clean + run the test lanes (DoD)**
  - [x] `dotnet build -c Release Hexalith.FrontComposer.slnx` clean (TWAE — **zero** warnings; this story adds a test file + a markdown contract, no new public API surface, so no XML-doc/PublicAPI obligations).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — the new `Story13AccessibilityPrimitivesTests` green; **re-confirm the Story 1.1/1.2 pre-existing-failure baseline** (8 Shell Governance/Navigation/Generated + 3 SourceTools + 2 Cli = 13 full-lane failures recorded against `f40dece`/`68034f1`) is **unchanged** — these are NOT regressions. If a failure looks new, stash to `68034f1` and compare (the 1.1/1.2 documented method).
  - [x] Keep the existing layout suites green — `FrontComposerShellTests.cs`, `FrontComposerShellParameterSurfaceTests.cs`, `Story11BootstrapShellRenderTests.cs`, `Story12PageLayoutTests.cs`, the component-level a11y tests (`FcDensityAnnouncerTests`, `FcLifecycleWrapperA11yTests`, `FcExpandedRowHiddenBannerTests`, `AxeCoreA11yTests`) — this story builds **on** them, not over them.

## Dev Notes

### What already exists vs. what's new

| Concern | State today (baseline `68034f1`) | This story |
|---|---|---|
| Skip links → content/nav | **Exists** (`FrontComposerShell.razor:29-33`, CSS `:21-36`) | **Confirm + pin** in the consolidated test |
| Focus visibility (zero-override) | **Exists** — native `:focus-visible`, no suppression (CSS `:18-20`) | **Confirm + add the "no suppressed focus" guard test** |
| `aria-live` announcers | **Exist** (`FcDensityAnnouncer`, lifecycle, connection, pending) | **Document** in contract; component tests already pin them |
| `aria-label`/`role` on chrome | **Exist** (palette, settings, theme, nav rail) | **Confirm accessible-name coverage** in the test |
| HFC1050–HFC1055 override diagnostics | **Implemented + emitting + documented** (`CustomizationAccessibilityAnalyzer`, `docs/diagnostics/HFC105*.md`) | **Reference as the agreed enforcement mechanism** — do NOT modify |
| e2e a11y lane (`npm run test:a11y`) | **Exists** (`specs/specimen-accessibility.spec.ts`, axe serious/critical gate) | **Confirm green** — do NOT modify |
| FC-A11Y contract doc | **Does NOT exist** (only FC-LYT exists in `_bmad-output/contracts/`) | **Author it** |
| Consolidated shell-frame a11y bUnit test | **Does NOT exist** (assertions scattered across component tests) | **Add `Story13AccessibilityPrimitivesTests`** |

### Exact anchors (read these before coding)

- **Skip links + targets** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:29-33` (the two `.fc-skip-link` anchors), `:87` (`<div id="fc-nav" tabindex="-1">`), `:107` (`<div id="fc-main-content" tabindex="-1" data-fc-page-layout=… class=…>`). The `#fc-main-content` id + `tabindex="-1"` is the skip-link/focus target **and** a locked substring in `FrontComposerShellTests.cs:176` — do not disturb it.
- **Skip-link + focus CSS** — `FrontComposerShell.razor.css:18-36` (`.fc-skip-link` visually-hidden-until-`:focus`; the **zero-override invariant comment** at `:18-20` is the documented contract the "no suppressed focus" guard test enforces).
- **`aria-live` announcer pattern** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor:10-18` (`role="status" aria-live="polite" aria-atomic="true" class="fc-sr-only"`, skip-first-render). Existing pin: `FcDensityAnnouncerTests.cs:26-35`.
- **Politeness escalation (status→alert)** — `tests/.../Components/Lifecycle/FcLifecycleWrapperA11yTests.cs:20-29` (status/polite vs alert/assertive), `:52-75` (focus-ring-preserved-during-pulse, UX-DR49), `:78-85` (reduced-motion media-query file-read assertion — **copy this assertion style** for the "no suppressed focus" guard).
- **WCAG 4.1.2 hidden-expansion live region** — `tests/.../Components/DataGrid/FcExpandedRowHiddenBannerTests.cs:54-63` (`role="status" aria-live="polite"`).
- **Accessible-name keys** — `FcPaletteTriggerButton` (`PaletteTriggerAriaLabel`), `FcSettingsButton` (`SettingsTriggerAriaLabel`), `FcThemeToggle` (`ThemeToggleAriaLabel`), nav rail (`NavMenuAriaLabel`) — all via `IStringLocalizer<FcShellResources>`. The visually-hidden helpers are `.fc-sr-only` (global, `wwwroot/css/fc-density.css`) and `.fc-visually-hidden` (`FcCollapsedNavRail.razor.css`).
- **HFC1050–HFC1055 declaration/emission** — IDs at `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:245-273`; descriptors at `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:593-657`; emission at `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs:139-210`; docs at `docs/diagnostics/HFC1050.md`…`HFC1055.md`.
- **e2e a11y lane** — `tests/e2e/package.json:17` (`test:a11y`), `tests/e2e/specs/specimen-accessibility.spec.ts`, `tests/e2e/helpers/a11y.ts:16-18,35-64` (blocking = serious∪critical, WCAG 2.1 AA), `tests/e2e/playwright.config.ts:46-57` (webServer launches `samples/Counter/Counter.Web` with specimens enabled), CI job `.github/workflows/ci.yml` `accessibility-visual`.
- **bUnit base + house style** — `tests/.../Components/Layout/LayoutComponentTestBase.cs` (Fluxor store + theme mock + JS stubs), `FrontComposerShellTests.cs` (render-and-query, registry/viewport setup, `#fc-main-content`/`#fc-nav` substring locks), `Story12PageLayoutTests.cs` (the most recent same-shape story's tests).

### The FC-A11Y ID → primitive → WCAG map (encode this table in the contract)

| Diagnostic | Override violation | FC-A11Y primitive | WCAG criterion |
|---|---|---|---|
| **HFC1050** | Interactive element missing accessible name | `aria-label`/visually-hidden label | 4.1.2 Name, Role, Value |
| **HFC1051** | Keyboard reachability blocked (`tabindex="-1"` on interactive) | Keyboard reachability | 2.1.1 Keyboard |
| **HFC1052** | Suppressed focus (`outline/box-shadow: none` w/o `:focus-visible`) | Focus visibility | 2.4.7 Focus Visible |
| **HFC1053** | Lifecycle/status override missing `aria-live` parity | `aria-live` politeness ladder | 4.1.3 Status Messages |
| **HFC1054** | Motion without reduced-motion fallback | Reduced-motion fallback | 2.3.3 Animation from Interactions |
| **HFC1055** | Custom colors without forced-colors evidence | Forced-colors fallback | (forced-colors / high-contrast) |

All six are **`Warning`** severity, `isEnabledByDefault=true`, **promoted to build-breakers by `TreatWarningsAsErrors=true`** — that is what makes them a hard ready-gate for overrides, not advisory hints.

### The FC-A11Y design decision (encode in the contract doc)

The story's first job is a **decision/declaration**, not new code. Recommended, carry unless reviewers override:

- **The FC-A11Y ready-gate has two enforcement layers, and that *is* the contract.** (1) **Shell-frame invariants** — skip-link→content, accessible names, visible focus — pinned by the new bUnit test, applying to the framework's own chrome. (2) **Override-time invariants** — HFC1050–HFC1055 — enforced at build for adopter Level-2/3/4 customizations. (3) **End-to-end** — the axe-core specimen lane catches what bUnit's non-shadow-DOM render can't (FluentUI v5 web components live in shadow DOM; see `AxeCoreA11yTests` class doc). Naming these three layers as "the single testable ready-gate every later story points at" is the deliverable.
- **Confirm-or-escalate, don't redesign.** Every primitive already has a working implementation and (mostly) a test. The contract confirms them; the only genuinely open items are governance questions (is a per-story checklist also required? who signs off visual-design a11y?) — escalate those with the named owner.

### Must-not-break (regression surface)

A ready-gate story must leave the shell working end-to-end. Preserve:

- **`#fc-main-content` / `#fc-nav` identity** — ids + `tabindex="-1"` are skip-link/focus targets **and** locked test substrings (`FrontComposerShellTests.cs:114,176`). Only **read** them in the new test; never change them.
- **Skip-link DOM order + CSS** — `.fc-skip-link` anchors render before the layout (`FrontComposerShell.razor:29-33`); the visually-hidden-until-focus CSS (`:21-36`) and the zero-override focus invariant (`:18-20`) stay exactly as-is.
- **The locked 7-parameter surface** (`FrontComposerShellParameterSurfaceTests`) — this story adds **no** shell `[Parameter]`; verify the lock stays green and untouched.
- **HFC1050–HFC1055 analyzer + descriptors + docs** — referenced, not modified. Touching `CanonicalSchemaMaterial`, the descriptors, or the diagnostics docs is out of scope (and would ripple into baselines/governance lanes).
- **The e2e specimen lane** — confirm green; do not edit specs/helpers/manifest/baselines (visual baselines are governance-gated).
- **No `docs/` writes** — `docs/` is the CI-gated DocFX site; the contract goes to `_bmad-output/contracts/`, and the published cross-link is **Story 1.5 (FC-DOC)**'s job.

### Previous story intelligence (Story 1.2 — `done`)

- **1.2 is the exact template for 1.3:** confirm a contract, add the minimal pin, escalate-with-owner under YOLO, defer the FC-DOC cross-link to Story 1.5. Reuse 1.2's fixtures and house style (`Story12PageLayoutTests.cs`, the FC-LYT contract doc shape).
- **Docs discipline (re-confirmed by 1.1 + 1.2):** `docs/` is the published DocFX site; the contract doc goes to `_bmad-output/contracts/`, the cross-link is deferred to 1.5.
- **Pre-existing-failure baseline:** 1.1 and 1.2 both recorded **13 full-lane failures** (8 Shell Governance×4 / Navigation `HandleAppInitialized_StoredRoute`×1 / Generated snapshot×3, 3 SourceTools, 2 Cli) reproduced identically against `f40dece`/`68034f1` — **NOT regressions**. Don't chase them; if a failure looks new, stash to `68034f1` and compare (1.1/1.2's documented method).
- **YOLO escalate-with-owner is acceptable** — 1.2's FC-LYT contract shipped `status: escalated` with `owner: FrontComposer + Product/UX (pending)` and passed review. FC-A11Y can do the same with the readiness-request owner (FrontComposer + Tenants author / Product/UX).
- **bUnit cannot DOM-walk FluentUI v5 shadow DOM** (`AxeCoreA11yTests` class doc) — that is *why* AC3's axe lane is the e2e layer and bUnit only asserts the markup-level ARIA contract. Don't try to run real `axe.run()` in bUnit.

### Git intelligence

- HEAD `68034f1` = Story 1.2 (`feat(story-1.2): Confirm and apply the FC-LYT page-layout contract`). Its `src/` changes were confined to the FC-LYT mechanism (`Components/Layout/FcPageLayout*`, `FcPageLayoutCoordinator`, `#fc-main-content` annotation) — **no skip-link/focus/aria changes** — so the a11y primitives above are exactly as documented. Recent commits (`0db0fb0` spike, `f40dece` bootstrap, `68034f1` FC-LYT) are all "confirm-and-pin + minimal additive" stories, the shape 1.3 continues.
- Working tree has one unrelated modified file (`_bmad-output/story-automator/orchestration-*.md`); leave it alone.
- Branch `feat/<desc>` (continue on `feat/story-1-2-fc-lyt-page-layout` or branch `feat/story-1-3-fc-a11y` — **never** commit to `main`). Conventional Commit: this story adds a contract doc + a test, no shipped behavior change, so `docs(story-1.3): …` or `test(story-1.3): …` (NOT `feat` — a test/doc-only story must not trigger a minor NuGet publish; project-context "Don't use `feat` for refactors/test/docs"). If the design review adds any `src/` behavior, re-evaluate. Run `/bmad-code-review` before flipping to done.

### Latest tech / FluentUI notes

- **FluentUI v5 RC** (`Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`, exact pin — ADR-003). FC-A11Y consumes Fluent's focus tokens (`--colorStrokeFocus2`) and its components' built-in keyboard nav — **no new FluentUI API surface**, so the RC pin is untouched. The shadow-DOM caveat (bUnit can't reach FluentUI web-component internals) is why the axe lane runs at the browser layer.
- **Logical-property / RTL awareness** — any CSS the contract documents should reference logical properties (already the repo convention, see FC-LYT) so FC-A11Y stays forward-compatible with FC-L10N (Story 1.4) RTL cultures.

### Project-context rules that bite here

- **No copyright/license headers** (0 of 483 files). **File-scoped namespaces, Allman braces, `_camelCase` fields, `Async` suffix, `I`-prefixed interfaces.** `sealed` the test class.
- **Test discipline:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldContain` — never raw `Assert.*`) + bUnit; plural `…Tests.cs`; methods three-part `Subject_Scenario_Expectation`; **`DiffEngine_Disabled=true`** when running tests (else Verify hangs); solution-level `dotnet test` + the trait filter (NOT per-project).
- **`TreatWarningsAsErrors=true`** — a test-only + markdown story has no new public API, so no XML-doc/`PublicAPI.Shipped.txt` obligation; still build Release clean (zero warnings).
- **`.slnx` only**; **centralized package versions** (no `Version=` in `.csproj`); **no new third-party analyzer/test/CSS framework** (built-in analyzers + the existing axe-core/Playwright stack only).
- **`ConfigureAwait(false)`** on awaits in `src/` (CA2007→error via TWAE) — the new file is a **test**, where this is not required (test projects don't enforce CA2007 the same way; follow the existing `…Tests.cs` style, which omits it).
- **Generated/BMAD docs → `_bmad-output/`, never `docs/`.** The FC-DOC published cross-link is **Story 1.5**'s.

### Project Structure Notes

- New contract doc: `_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md` (beside the FC-LYT contract).
- New test: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story13AccessibilityPrimitivesTests.cs`.
- **No `src/` changes expected** — the primitives, announcers, diagnostics, and e2e lane all already exist. If a review insists on a tiny `src/` touch (e.g. a missing `aria-label`), it must be additive and re-baselined against the locked surfaces. No structural variances expected; the contract-in-`_bmad-output` + test-in-`Shell.Tests` split matches the FC-LYT precedent and the dependency-down-to-Contracts rule (no new Contracts/Shell types).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3: Establish FC-A11Y accessibility primitives as a ready-gate] (story + 3 ACs)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Shell Foundation & Bootstrap] (AR2 FC-A11Y; NFR6; UX-DR6 accessibility patterns)
- [Source: _bmad-output/planning-artifacts/epics.md:83] (NFR6 — WCAG: aria-label/role/aria-live/data-testid, focus, reduced-motion/forced-colors, HFC1050–HFC1055)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:23] (🔴 FC-A11Y ask + owner: FrontComposer + Tenants author; "every story's ready-gate")
- [Source: _bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md:45] (FC-A11Y patterns observed in FcExpandedRowHiddenBanner WCAG 4.1.2; no API question to escalate)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] (FC-LYT contract — the structure/tone to mirror; escalate-with-owner precedent)
- [Source: _bmad-output/implementation-artifacts/1-2-confirm-and-apply-the-fc-lyt-page-layout-contract.md] (previous story; confirm-and-pin shape, pre-existing-failure baseline, docs discipline, YOLO escalation)
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules] (a11y patterns, icons, scoped lifetime)
- [Source: _bmad-output/project-context.md#Testing Rules] (xUnit v3 + Shouldly + bUnit, DiffEngine_Disabled, solution-level test + trait filter, e2e a11y lane)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:29-33,87,107] (skip links + #fc-main-content/#fc-nav targets)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css:18-36] (skip-link CSS + zero-override focus invariant)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor:10-18] (aria-live announcer pattern)
- [Source: src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:245-273] (HFC1050–HFC1055 IDs)
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs:139-210] (HFC1050–HFC1055 emission logic)
- [Source: docs/diagnostics/HFC1050.md … HFC1055.md] (published diagnostic docs — reference only, do not modify)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs:114,176] (locked #fc-nav / #fc-main-content substrings; registry/viewport setup)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story12PageLayoutTests.cs] (most recent same-shape story tests; render-and-query style)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/LayoutComponentTestBase.cs] (Fluxor store + theme mock + JS stubs base)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperA11yTests.cs:78-85] (scoped-CSS file-read assertion style to copy for the "no suppressed focus" guard)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/AxeCoreA11yTests.cs] (bUnit asserts ARIA markup; real axe.run() is the E2E layer — shadow-DOM caveat)
- [Source: tests/e2e/specs/specimen-accessibility.spec.ts + tests/e2e/helpers/a11y.ts:16-18,35-64] (the `npm run test:a11y` lane; blocking = serious/critical, WCAG 2.1 AA)
- [Source: tests/e2e/playwright.config.ts:46-57] (specimen host launch: Counter.Web, specimens enabled)
- [Source: .github/workflows/ci.yml#accessibility-visual] (standing CI evidence for AC3 if the lane can't run locally)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — bmad-create-story workflow

### Debug Log References

- `dotnet build -c Release Hexalith.FrontComposer.slnx` → **Build succeeded, 0 Warning(s), 0 Error(s)** (TWAE clean; story adds only a test file + a markdown contract, no new public API surface).
- `DiffEngine_Disabled=true dotnet test … --filter "FullyQualifiedName~Story13AccessibilityPrimitivesTests"` → **Passed! Failed: 0, Passed: 6** (the new consolidated a11y ready-gate test; dev-story authored 4, QA-automation added 2 — skip-to-nav landmark name + shell-frame density-announcer live region).
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` → **13 failures, all pre-existing baseline, unchanged from `f40dece`/`68034f1`** (Story 1.1/1.2 documented):
  - **Shell (8):** `PendingStatusReopenGovernanceTests` ×4, `NavigationEffectsLastActiveRouteTests.HandleAppInitialized_StoredRoute_DispatchesHydratedActions` ×1, Generated snapshot ×3 (`CommandRendererFullPageTests.Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty`, `CounterStoryVerificationTests` ×2).
  - **SourceTools (3):** `DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates`, `CommandFormEmitterTests.Emit_DoesNotLogModelInstance`, `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`.
  - **Cli (2):** `MigrationCommandTests.ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically`, `…RejectsSolutionProjectsOutsideSolutionRoot`.
  - None touch the FC-A11Y primitives, the shell frame, or the new test. The Shell suite shows **1677 passed, 8 failed** (the 8 are the pre-existing Shell baseline; the 6 new tests included); the locked 7-parameter surface (`FrontComposerShellParameterSurfaceTests`) and the existing layout/a11y suites stay green.
- **e2e a11y lane (AC3) — NOT executed locally.** `npx playwright install chromium` failed: *"Playwright does not support chromium on ubuntu26.04-x64"*, and the e2e package requires Node ≥24 (environment has v22.22.1). The lane cannot run here. **Standing evidence cited:** CI job `accessibility-visual` (`.github/workflows/ci.yml:420-466`, `runs-on: windows-latest`) runs this exact lane (`npm run test:a11y`) and must be green. No local pass is claimed.

### Completion Notes List

- **AC1 (shell-frame a11y, pinned):** Added `Story13AccessibilityPrimitivesTests` (6 tests) consolidating the three shell-frame invariants into one Layer-1 ready-gate: (1) `a.fc-skip-link[href="#fc-main-content"]` resolves to `#fc-main-content[tabindex="-1"]`, and with a renderable registry the second skip link `href="#fc-nav"` resolves to `#fc-nav[tabindex="-1"]`; (2) the palette trigger / settings button / theme toggle expose non-empty accessible names asserted against the **localized** `IStringLocalizer<FcShellResources>` values (FC-L10N-safe, not hard-coded English); (3) a scoped-CSS file-read guard asserting `FrontComposerShell.razor.css` suppresses no focus indicator (zero-override invariant — `outline:none`/`box-shadow:none` only allowed paired with a `:focus-visible` restore). Mirrors the `FcLifecycleWrapperA11yTests` CSS-read style and the `Story12PageLayoutTests` render-and-query house style. **No `src/` change**; the locked 7-parameter shell surface is untouched.
- **AC2 (contract documented + escalated):** Authored `_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md`, mirroring the FC-LYT contract. Names the FC-A11Y primitive set (6 primitives, each with a cited canonical example), records the **three-layer ready-gate** (shell-frame bUnit / HFC1050–HFC1055 override build-breakers / e2e axe), and encodes the HFC1050–HFC1055 → primitive → WCAG map as the **agreed enforcement mechanism** (referenced, not modified). Marked `status: escalated`, `owner: FrontComposer + Tenants author / Product/UX (pending)` per AC2's escalate-with-owner path (no live confirmation available), with the FC-DOC cross-link deferred to Story 1.5.
- **AC3 (e2e a11y lane):** Attempted; cannot run in this environment (see Debug Log). Documented explicitly and cited the standing green CI `accessibility-visual` job — no local pass claimed. The specimen lane / specs / helpers / manifest were not modified.
- **Conventional Commit guidance:** test + doc only, no shipped behaviour change → use `docs(story-1.3): …` or `test(story-1.3): …` (NOT `feat`, to avoid a minor NuGet publish). On branch `feat/story-1-2-fc-lyt-page-layout` (not `main`).

### File List

- `_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md` (new) — the FC-A11Y ready-gate contract.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story13AccessibilityPrimitivesTests.cs` (new) — consolidated shell-frame a11y ready-gate bUnit test (6 tests: 4 from dev-story + 2 from QA-automation).
- `_bmad-output/implementation-artifacts/tests/1-3-test-summary.md` (new) — QA-automation test-summary artifact (records the +2 tests added on top of the dev-story's 4).
- `_bmad-output/implementation-artifacts/1-3-establish-fc-a11y-accessibility-primitives-as-a-ready-gate.md` (modified) — task checkboxes, Dev Agent Record, File List, Change Log, Status → review.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — story 1-3 ready-for-dev → in-progress → review.

## Change Log

| Date | Change |
|---|---|
| 2026-06-03 | Story 1.3 implemented (dev-story): authored the FC-A11Y accessibility-primitives ready-gate contract, pinned the shell-frame invariants with the consolidated `Story13AccessibilityPrimitivesTests` bUnit test (4 tests, green), and confirmed the e2e a11y lane via standing CI evidence (lane not locally runnable: OS/Node unsupported). Release build clean (0 warnings); pre-existing 13-failure baseline unchanged. Status → review. |
| 2026-06-03 | Review (story-automator-review): auto-fixed 3 findings — (HIGH) removed leaked `</content>`/`</invoke>` tool-call tags from the tail of the FC-A11Y contract; (MEDIUM) corrected stale test counts (4→6 tests; Shell suite 1675→1677 passed) after confirming the QA-automation +2 tests; (MEDIUM) added the undocumented `1-3-test-summary.md` to the File List. Re-verified: Release build 0 warnings, `Story13AccessibilityPrimitivesTests` 6/6 green, Shell suite 1677 passed / 8 pre-existing-baseline failures. No CRITICAL issues remain. Status stays `review`/`done`. |
