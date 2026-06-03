---
baseline_commit: f40dece9298eb154c1b6df2b605703300b913915
---

# Story 1.2: Confirm and apply the FC-LYT page-layout contract

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this first.** There is **no `<PageLayout>` component, no layout-mode
> enum, and no max-measure / constrained-width wrapper anywhere in the repo today.** `<PageLayout>`
> appears **only** in the planning docs (`epics.md`, the readiness request) as the *name of a contract
> to confirm* — not as existing code. The shell's content area
> (`FrontComposerShell.razor:96-106`) currently renders `@ChildContent` **edge-to-edge** inside a
> `FluentLayoutItem Area=Content` with `Padding.All3` and a bare `<div id="fc-main-content">` — no
> width constraint. So this story is **(1) decide + document the FC-LYT contract**, **(2) build the
> minimal mechanism that lets a page opt into a constrained measure** while full-width stays the
> default (preserving today's behaviour = regression-safe), and **(3) pin both modes with bUnit
> tests**. Do **not** restructure `FluentLayout`, the header/nav/footer regions, or the existing
> responsive/viewport machinery. This is a focused, additive content-measure feature plus a confirmed
> contract — not a layout rewrite.

## Story

As an adopter developer,
I want a confirmed full-width vs. constrained `<PageLayout>` contract on `FrontComposerShell`,
so that every page renders at the correct measure without per-page layout hacks.

## Acceptance Criteria

**AC1 — The FC-LYT contract is documented (full-width vs constrained, default, opt-in mechanism) and applied. *(AR1, FR9)***
**Given** the `FC-LYT` contract is documented — naming the two modes, the **default** mode, and the **opt-in mechanism** a page uses to declare its measure,
**When** a page declares **constrained** layout,
**Then** its content renders within the constrained max-measure (centred within the content area),
**And** a **full-width** page (the default) spans the full content area exactly as it does today.

**AC2 — The contract is confirmed by Product/UX or escalated with an owner, and queued for the FC-DOC linkage. *(AR1, AR4)***
**Given** the contract document,
**When** Product/UX reviews it,
**Then** it is marked **confirmed** (default + opt-in mechanism agreed) **or** the open question is **escalated with a named owner** in the contract doc,
**And** the contract is queued to be linked from the published component docs by **FC-DOC / Story 1.5** (which owns the CI-gated `docs/` site — do **not** scratch-write `docs/` in this story).

**AC3 — bUnit renders of `FrontComposerShell` in each layout mode expose the expected container/data attribute.**
**Given** a bUnit render of `FrontComposerShell` in **each** layout mode (full-width and constrained),
**When** rendered,
**Then** the rendered DOM exposes the expected layout container / `data-*` attribute for each mode (e.g. `#fc-main-content[data-fc-page-layout="full-width"]` vs `…="constrained"`), so the mode is assertable and cannot silently regress.

## Tasks / Subtasks

- [x] **Task 1 — Decide + write the FC-LYT contract doc (AC: #1, #2) — the DECISION deliverable**
  - [x] Author `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md` (create `_bmad-output/contracts/` if absent; **never** write to the CI-gated `docs/` site — project-context "docs/ is a PUBLISHED DocFX site" rule). Mirror the structure/tone of the Story 1.0 spike note (`_bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md`).
  - [x] State the **two modes**, the **default**, and the **opt-in mechanism**. **Recommended decision** (carry unless Product/UX overrides): **`FullWidth` = default** (preserves today's edge-to-edge behaviour → zero regression risk; right for the DataGrid-heavy read-only MVP, Epic 2), **`Constrained` = explicit opt-in** for prose/forms/detail pages that need a readable measure. This reading matches the AC wording ("when a page **declares** constrained … full-width pages span"). Record the constrained **max-measure value** as a CSS custom property (recommend `--fc-page-max-inline-size`, default e.g. `75rem`/`1200px`) so it is themeable and not a magic number — flag the exact number as a Product/UX input.
  - [x] Add a **Confirmation** section: mark `confirmed` OR escalate the open question (default choice + max-measure value) with a **named owner** (per the readiness-request owner column: *FrontComposer + Product/UX*). YOLO mode: if no live confirmation is available, write it as **escalated with owner = "Product/UX (pending)"** and proceed — the AC explicitly permits escalate-with-owner.
  - [x] Add an **FC-DOC linkage** note: the published component-docs cross-link is **owned by Story 1.5 (FC-DOC)**; record this contract's path so 1.5 can link it. Do not pre-create a `docs/` page.

- [x] **Task 2 — Add the page-layout mode type (AC: #1)**
  - [x] Add a `public enum FcPageLayoutMode { FullWidth, Constrained }` (XML-doc'd, `FullWidth` first so it is the `default`/zero value). Home it in `src/Hexalith.FrontComposer.Contracts/Rendering/` beside the existing render-mode enums (`FcRenderMode.cs`, `DensityLevel.cs`, `CommandRenderMode.cs`) — namespace `Hexalith.FrontComposer.Contracts.Rendering`. **Multi-TFM guard:** Contracts targets `net10.0`+`netstandard2.0`; a plain enum with no FluentUI/net10 types needs **no** `#if NET10_0_OR_GREATER` guard (see `FcRenderMode.cs` — unguarded). Keep it FluentUI-free.
  - [x] If this adds a new public type to a project carrying a `PublicAPI.Shipped.txt`, update that baseline **intentionally** (Contracts is part of the v1.0 API-freeze target — `PublicAPI.Shipped.txt` owns the strict surface). Check whether Contracts has `PublicAPI.*.txt`; if so, append the new symbol.

- [x] **Task 3 — Build the opt-in mechanism + apply it to the shell content area (AC: #1, #3) — the CODE deliverable**
  - [x] **Recommended mechanism — mirror the existing cascaded `LayoutHamburgerCoordinator` pattern** (the repo's established child→shell layout-signalling precedent; `FrontComposerShell.razor:35` cascades `_hamburgerCoordinator` with `IsFixed="true"`, defined in `Components/Layout/LayoutHamburgerCoordinator.cs`). Add an `internal` instance-per-shell `FcPageLayoutCoordinator` holding the active `FcPageLayoutMode` (default `FullWidth`), cascade it `IsFixed="true"` from `FrontComposerShell`, and have the shell's `#fc-main-content` div bind `data-fc-page-layout="@(...full-width|constrained)"` + a `class="fc-page-layout fc-page-layout--constrained"` toggle from the coordinator's mode.
  - [x] Add a small **`FcPageLayout`** component (`Components/Layout/FcPageLayout.razor`) a page drops into its content to declare its mode: `<FcPageLayout Mode="FcPageLayoutMode.Constrained">…</FcPageLayout>`. It reads the cascaded coordinator and sets the mode (register-on-first-render / reset-on-dispose, exactly like `FcHamburgerToggle` registers with `LayoutHamburgerCoordinator`). This satisfies the readiness-request `<PageLayout>` naming intent while keeping full-width the zero-config default.
  - [x] **Constrained CSS:** add the `.fc-page-layout--constrained` rule applying `max-inline-size: var(--fc-page-max-inline-size, 75rem); margin-inline: auto;`. **Placement decision (follow the established split):** body/global-targeted or generated-output-targeted classes go in a **global** static web asset (`wwwroot/css/`) — see `fc-density.css` / `fc-projection.css` and the scoped-CSS caveat documented in `FrontComposerShell.razor.css:38-41`. Since `#fc-main-content` is rendered **by `FrontComposerShell` itself**, the constrained rule MAY live in the scoped `FrontComposerShell.razor.css` (it targets the shell's own DOM). Prefer scoped (`FrontComposerShell.razor.css`) unless the class must also reach generator-emitted DOM — if so, use a global asset like `fc-projection.css`. Justify the choice in a code comment.
  - [x] **Do NOT add a `[Parameter]` to `FrontComposerShell` if avoidable.** The coordinator + `FcPageLayout`-child design needs none, which keeps the **locked parameter surface green** (see Task 4). If a design review insists on a shell-level `DefaultPageLayout` parameter, it MUST be **appended** to the surface-lock array (append-only — Task 4).
  - [x] Preserve `Padding.All3` on the Content `FluentLayoutItem` and the existing `<FcProjectionConnectionStatus />` / `<FcPendingCommandSummary />` / `@ChildContent` order inside `#fc-main-content` (`FrontComposerShell.razor:101-105`). The measure constraint wraps/annotates that container; it does not reorder it.

- [x] **Task 4 — Update the parameter-surface lock ONLY if a shell parameter was added (AC: #3)**
  - [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs` locks the 7-parameter surface **append-only** (no rename/retype/removal/reorder). If Task 3 added **no** `[Parameter]` to `FrontComposerShell`, this test stays **untouched and green** — verify it. If a parameter WAS added, **append** it as the last array entry (e.g. `"DefaultPageLayout:FcPageLayoutMode"`) and update the test's XML-doc rationale comment.

- [x] **Task 5 — Pin both layout modes with bUnit tests (AC: #3)**
  - [x] Add `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story12PageLayoutTests.cs`. Extend the existing layout test base (`LayoutComponentTestBase` — initialises the Fluxor store, mocks the theme service, stubs the beforeunload/keyboard JS modules) and follow `FrontComposerShellTests.cs` for render-and-query house style. `JSInterop.Mode = Loose` via the base.
  - [x] **Default (full-width):** render `FrontComposerShell` with plain `ChildContent` and assert `#fc-main-content` carries `data-fc-page-layout="full-width"` and does **not** carry the constrained class.
  - [x] **Constrained:** render `FrontComposerShell` with `ChildContent` containing `<FcPageLayout Mode="FcPageLayoutMode.Constrained">…</FcPageLayout>` and assert `#fc-main-content[data-fc-page-layout="constrained"]` + the `fc-page-layout--constrained` class is present. Use Shouldly (`ShouldBe`/`ShouldContain` — never raw `Assert.*`).
  - [x] Add a focused `FcPageLayout` unit/component test: declaring a mode flips the cascaded coordinator; dispose resets to `FullWidth` (mirror the `FcHamburgerToggle` register/unregister expectation).
  - [x] **Method naming:** three-part `Subject_Scenario_Expectation`; file is plural `…Tests.cs` (house style — see `FrontComposerShellTests.cs`).

- [x] **Task 6 — Build clean + run the test lanes (DoD)**
  - [x] `dotnet build -c Release` clean (TWAE — **zero** warnings; a new public enum needs XML docs on the type+members where Contracts re-raises CS1591).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — everything this story touches green. Re-confirm the Story 1.1 regression baseline: the **13 pre-existing full-lane failures** (8 Shell + 3 SourceTools + 2 Cli) recorded against baseline `0db0fb0`/`f40dece` are NOT introduced here — re-verify by stashing to `f40dece` if any failure looks new (see Story 1.1 Completion Notes).
  - [x] Keep the Story 1.0 spike suite (`Spike/Story10ShellIntegrationSpikeTests.cs`) and the existing `FrontComposerShellTests.cs` / `FrontComposerShellParameterSurfaceTests.cs` green — this story builds directly on that pinned shell.

## Dev Notes

### What already exists vs. what's new

| Concern | State today | This story |
|---|---|---|
| `<PageLayout>` component | **Does not exist** (planning-doc name only) | Build minimal `FcPageLayout` + coordinator |
| Layout-mode enum | **Does not exist** | Add `FcPageLayoutMode { FullWidth, Constrained }` |
| Constrained max-measure | **Does not exist** — content is edge-to-edge | Add `.fc-page-layout--constrained` + `--fc-page-max-inline-size` |
| Content container | `#fc-main-content` div, no `data-*` mode attr (`FrontComposerShell.razor:101`) | Add `data-fc-page-layout` + class toggle |
| Child→shell layout signalling | `LayoutHamburgerCoordinator` cascaded `IsFixed` (`FrontComposerShell.razor:35`) | **Mirror this exact pattern** |
| FC-LYT contract doc | **Does not exist** | Author under `_bmad-output/contracts/` |

### Exact anchors (read these before coding)

- **Shell content area to modify** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:96-106`:
  ```razor
  <FluentLayoutItem Area="@LayoutArea.Content" Padding="@Padding.All3">
      <FcLayoutBreakpointWatcher /> <FcSystemThemeWatcher /> <FcDensityApplier /> <FcDensityAnnouncer />
      <div id="fc-main-content" tabindex="-1">   @* ← add data-fc-page-layout + class here *@
          <FcProjectionConnectionStatus /> <FcPendingCommandSummary /> @ChildContent
      </div>
  </FluentLayoutItem>
  ```
- **Cascaded-coordinator precedent to copy** — `FrontComposerShell.razor:35` (`<CascadingValue Value="_hamburgerCoordinator" IsFixed="true">`), field at `FrontComposerShell.razor.cs:97` (`private readonly LayoutHamburgerCoordinator _hamburgerCoordinator = new();`), type at `Components/Layout/LayoutHamburgerCoordinator.cs` (instance-per-shell, `internal`, register/clear-on-dispose). `FcHamburgerToggle` is the register-on-first-render / clear-on-dispose example to mirror for `FcPageLayout`.
- **`data-*` + CSS-var precedent** — `wwwroot/css/fc-density.css` drives `--fc-spacing-unit` off `[data-fc-density="…"]`; `<body data-fc-density>` is set by `FcDensityApplier`. FC-LYT uses the same shape: `[data-fc-page-layout]` / `.fc-page-layout--constrained` → `--fc-page-max-inline-size`.
- **Scoped-vs-global CSS rule** — `FrontComposerShell.razor.css:38-41` documents WHY body-level + generator-output classes must be **global** (CSS isolation suffixes selectors). `#fc-main-content` is the shell's **own** DOM, so its constrained rule is safe in scoped `FrontComposerShell.razor.css`. Comment the choice.
- **Enum home + style** — `src/Hexalith.FrontComposer.Contracts/Rendering/FcRenderMode.cs` (unguarded public enum, XML-doc'd members, file-scoped namespace) is the exact template for `FcPageLayoutMode`.
- **Locked parameter surface** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs:31-39` (the 7-entry `actual.ShouldBe([...])`). Append-only.
- **bUnit base + render style** — `tests/.../Components/Layout/LayoutComponentTestBase.cs` (Fluxor store + theme mock + JS stubs) and `FrontComposerShellTests.cs` (render-and-query, `data-testid` assertions, viewport-tier setup).

### The FC-LYT design decision (encode in the contract doc)

The story's first job is a **decision**, not just code. Recommended, carry unless Product/UX overrides:

- **`FullWidth` is the default.** It is the zero/first enum value and exactly today's behaviour → **no regression** for any existing page, and it's the right default for the DataGrid-dense read-only MVP (Epic 2). The AC phrasing ("when a page **declares** constrained … full-width pages span the content area") treats *constrained* as the opt-in action — consistent with FullWidth-default.
- **`Constrained` is opt-in** via `<FcPageLayout Mode="Constrained">`, applying `max-inline-size: var(--fc-page-max-inline-size, 75rem); margin-inline: auto;` to centre content at a readable measure. The exact max value is a **Product/UX input** — ship a sensible default as a CSS custom property and flag it for confirmation.
- **Why coordinator + child component, not a shell parameter:** a page lives **inside** `@ChildContent`, below the shell, and cannot set a shell `[Parameter]`. The cascaded coordinator is the repo's existing, proven answer to "a child wants to influence the shell's layout" (`LayoutHamburgerCoordinator`). Reusing it keeps the locked parameter surface untouched.

### Must-not-break (regression surface)

A layout story must leave the shell working end-to-end. Preserve:

- **Default behaviour is unchanged.** With no `FcPageLayout` present, content must render full-width exactly as before (`data-fc-page-layout="full-width"`, no constrained class, `Padding.All3` intact). Pin this as the AC3 default test.
- **Header / Navigation / Footer regions and the responsive machinery** (`HasNavigation`, `IsSubCompactDesktopViewport`, `NavigationWidth`, `FcLayoutBreakpointWatcher`, `ViewportTier`) are out of scope — do not touch (`FrontComposerShell.razor:36-118`, `.razor.cs:206-226`).
- **`#fc-main-content` identity + skip-link target.** The id is the `href="#fc-main-content"` skip-link target (`FrontComposerShell.razor:29`) and `tabindex="-1"` focus target — keep both; only **add** the `data-*`/class.
- **Cascade order.** Add the page-layout `CascadingValue` without disturbing the existing `_hamburgerCoordinator` cascade wrapping `FluentLayout` (`FrontComposerShell.razor:35-119`).
- **Locked parameter surface** stays green unless a shell parameter is intentionally added + appended (Task 4).
- **ADR-030 scoped-lifetime / single-writer:** the coordinator is an instance-per-shell field (not DI), exactly like `LayoutHamburgerCoordinator` — do not register it as a singleton or capture scoped services in it.

### Previous story intelligence (Story 1.1 — `done`)

- Story 1.1 was "confirm-and-pin + one new guard"; **1.2 has the same shape** — confirm the FC-LYT contract, add the minimal mechanism, pin both modes. Reuse 1.1's test fixtures and house style.
- 1.1 added the bootstrap fail-fast guard under `Extensions/` and tests under `tests/.../Extensions/` and `tests/.../Components/Layout/`. Your `FcPageLayout`/coordinator go under `Components/Layout/`; tests beside `FrontComposerShellTests.cs`.
- **Pre-existing failures baseline:** 1.1 recorded **13 full-lane failures** (8 Shell + 3 SourceTools + 2 Cli) reproduced identically on baseline `0db0fb0` — NOT regressions. Don't chase them; if a failure looks new, stash to `f40dece` and compare (1.1's documented method).
- **Docs discipline (learned the hard way in 1.1 Task 5):** `docs/` is the CI-gated DocFX site; FC-DOC / Story 1.5 owns published component docs. The FC-LYT contract doc goes to `_bmad-output/`, and the docs cross-link is **deferred to Story 1.5** (AC2).

### Git intelligence

- HEAD `f40dece` = Story 1.1 (`feat(story-1.1): Bootstrap a minimal, bootable shell`). Its `src/` changes were confined to `Extensions/` (bootstrap markers/validator/gate) — **no layout-component changes** — so the shell content area is exactly as documented above.
- Working tree has one unrelated modified file (`_bmad-output/story-automator/orchestration-*.md`); leave it alone.
- Branch `feat/<desc>` (e.g. `feat/story-1-2-fc-lyt-page-layout`), never commit to `main`. Conventional Commit `feat(story-1.2): …` (new mechanism → minor). Run `/bmad-code-review` before flipping to done.

### Latest tech / FluentUI notes

- **FluentUI v5 RC** (`Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`, exact pin — ADR-003). The constrained measure is a **CSS concern on the shell's own `#fc-main-content`**, not a FluentUI component swap — no new FluentUI API surface, so the v5 RC pin is untouched. `FluentLayoutItem Area=Content` keeps owning the region; you constrain the inner div. (If you genuinely need FluentUI layout API confirmation, `docs/fluent-ui-v5-contingency.md` lists the load-bearing layout APIs.)
- Use **logical properties** (`max-inline-size`, `margin-inline`) not `max-width`/`margin-left/right` — consistent with FluentUI v5's RTL-aware styling and future-proof for localized RTL cultures (FC-L10N, Story 1.4).

### Project-context rules that bite here

- **No copyright/license headers** (0 of 483 files). **File-scoped namespaces, Allman braces, `_camelCase` fields, `Async` suffix, `I`-prefixed interfaces.** **`sealed`** the coordinator/components where inheritance isn't intended.
- **`ConfigureAwait(false)` on every await in `src/`** (CA2007 → build error via TWAE) — EXCEPT inside Blazor `.razor.cs` components, where `#pragma warning disable CA2007` is already applied (see `FrontComposerShell.razor.cs:22`); mirror that if `FcPageLayout.razor.cs` awaits.
- **`TreatWarningsAsErrors=true`** — XML docs required on the new **public** enum (Contracts re-raises CS1591 on public-API surfaces); fix warnings, don't blanket-suppress. **CRLF, 4-space indent, final newline, UTF-8.**
- **`.slnx` only**; **centralized package versions** (no `Version=` in `.csproj`); **no new third-party analyzer/CSS framework**.
- **Dependency direction points DOWN to Contracts** — `FcPageLayoutMode` in Contracts is fine; the Shell references Contracts. Don't make Contracts depend on the Shell.
- **Generated/BMAD docs → `_bmad-output/`, never `docs/`.**

### Project Structure Notes

- New code: `src/Hexalith.FrontComposer.Contracts/Rendering/FcPageLayoutMode.cs` (enum), `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor` (+ `.razor.cs` if needed), `FcPageLayoutCoordinator.cs` (internal, beside `LayoutHamburgerCoordinator.cs`), edits to `FrontComposerShell.razor` (content div) and CSS (scoped `FrontComposerShell.razor.css` preferred, or a `wwwroot/css/` asset).
- New tests: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story12PageLayoutTests.cs` (+ surface-lock edit only if a shell parameter was added).
- Contract doc: `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md` (create the folder).
- **Alignment:** the enum-in-Contracts/component-in-Shell split honours the dependency-down-to-Contracts rule; the cascaded-coordinator reuse honours the established layout-signalling pattern. No structural variances expected.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2: Confirm and apply the FC-LYT page-layout contract] (story + ACs)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Shell Foundation & Bootstrap] (FR9, AR1; UX-DR7 page-layout contract)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:22] (🔴 FC-LYT ask + owners: FrontComposer + Product/UX)
- [Source: _bmad-output/implementation-artifacts/1-1-bootstrap-a-minimal-bootable-shell.md] (previous story; confirm-and-pin shape, pre-existing-failure baseline, docs discipline)
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules] (startup wiring, ADR-030 scoped lifetime, single-writer, icons)
- [Source: _bmad-output/project-context.md#Code Quality & Style Rules] (TWAE, .slnx, dependency-down-to-Contracts, docs/ vs _bmad-output/)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:35,96-106,101] (cascade precedent + content area + #fc-main-content to annotate)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:97,202-226] (_hamburgerCoordinator field, HasNavigation/viewport machinery to leave alone)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/LayoutHamburgerCoordinator.cs] (cascaded instance-per-shell coordinator pattern to mirror)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css:38-41] (scoped-vs-global CSS isolation rule)
- [Source: src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-density.css] (data-attr + CSS-var precedent)
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/FcRenderMode.cs] (public-enum style template + namespace)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs:24-40] (locked, append-only parameter surface)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs] (bUnit render-and-query house style)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/LayoutComponentTestBase.cs] (layout bUnit base: Fluxor store + theme mock + JS stubs)
- [Source: docs/fluent-ui-v5-contingency.md] (load-bearing FluentUI layout APIs, if FluentUI confirmation is needed)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — bmad-create-story workflow

### Debug Log References

- `dotnet build -c Release Hexalith.FrontComposer.slnx` → **Build succeeded, 0 Warning(s), 0 Error(s)** (TWAE clean; new public enum carries XML docs on type + both members).
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` → **13 failures total (8 Shell + 3 SourceTools + 2 Cli)**, matching the Story 1.1 pre-existing baseline against `f40dece` **exactly**. The 8 Shell failures are Governance (×4), Navigation `HandleAppInitialized_StoredRoute_DispatchesHydratedActions` (×1), and Generated snapshot/Verify (×3) — **none layout/FC-LYT-related**. No new regressions introduced.
- Targeted lane (`Story12PageLayoutTests` + `FrontComposerShellTests` + `FrontComposerShellParameterSurfaceTests` + `FcHamburgerToggleTests` + `Story10ShellIntegrationSpikeTests`) → **40 passed, 0 failed**.

### Completion Notes List

- **AC1 (documented + applied):** FC-LYT contract authored at `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md` — two modes (`FullWidth` default / `Constrained` opt-in), default rationale, opt-in mechanism, and themeable max-measure (`--fc-page-max-inline-size`, default `75rem`). Applied via `FcPageLayoutMode` enum + `FcPageLayout` component + cascaded `FcPageLayoutCoordinator`; constrained content centres at the max measure, full-width spans the content area exactly as before.
- **AC2 (confirmed-or-escalated + FC-DOC queued):** contract marked **escalated** with named owner *Product/UX (pending)* for the two open inputs (default choice, exact max value), per the AC's escalate-with-owner allowance (no live confirmation available — YOLO). FC-DOC cross-link explicitly deferred to **Story 1.5**; contract path recorded for 1.5 to link. No `docs/` site write (project-context docs discipline honoured).
- **AC3 (both modes pinned):** `#fc-main-content` now carries `data-fc-page-layout="full-width|constrained"` + a `fc-page-layout` / `fc-page-layout--constrained` class toggle, driven by the cascaded coordinator. `Story12PageLayoutTests` pins both modes and the `FcPageLayout` register/reset lifecycle.
- **Mechanism:** mirrors the established `LayoutHamburgerCoordinator` child→shell cascade pattern (instance-per-shell field, `IsFixed` cascade, register-on-first-render / reset-on-dispose like `FcHamburgerToggle`). The shell subscribes to the coordinator's `Changed` event and re-renders via `InvokeAsync(StateHasChanged)`; `SetMode` no-ops on unchanged mode so the render loop cannot re-enter.
- **No shell `[Parameter]` added** → the locked 7-parameter surface (`FrontComposerShellParameterSurfaceTests`) stays **untouched and green** (Task 4 verified, no edit needed).
- **Regression-safe:** with no `FcPageLayout` present, content stays full-width with `Padding.All3` intact and `#fc-main-content` id + `tabindex="-1"` adjacency preserved (skip-link/focus target + existing locked test substring).
- **CSS placement:** constrained rule lives in the **scoped** `FrontComposerShell.razor.css` (commented) — `#fc-main-content` is the shell's own DOM so CSS-isolation scoping matches it; logical properties (`max-inline-size`/`margin-inline`) used for RTL-awareness.
- Contracts has **no** `PublicAPI.*.txt` baseline, so the new public enum required no API-surface update (Task 2 sub-step verified).

### File List

- `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md` (new — FC-LYT contract decision doc)
- `src/Hexalith.FrontComposer.Contracts/Rendering/FcPageLayoutMode.cs` (new — `FcPageLayoutMode { FullWidth, Constrained }` enum)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayoutCoordinator.cs` (new — internal instance-per-shell coordinator)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor` (new — opt-in page-layout component markup)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor.cs` (new — component code-behind, register/reset lifecycle)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` (modified — cascade coordinator + annotate `#fc-main-content`)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` (modified — coordinator field, `Changed` subscription, layout attr/class properties, dispose unhook)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` (modified — scoped `.fc-page-layout--constrained` rule)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story12PageLayoutTests.cs` (new — both-mode + lifecycle bUnit pins; review-fix: + `FcPageLayout_WhenModeRebindAfterFirstRender_UpdatesCoordinator`)
- `_bmad-output/implementation-artifacts/tests/1-2-test-summary.md` (new — QA automation summary; bmad-qa-generate-e2e-tests sweep)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — 1-2 ready-for-dev → in-progress → review → done)

### Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (story-automator adversarial review) — 2026-06-03
**Outcome:** Approve (auto-fix applied). Status → **done** (0 critical issues remaining).

Verified against git reality: build `-c Release` clean (0 warnings / 0 errors, TWAE); targeted layout lane **43 passed / 0 failed**; full Shell lane = **8 pre-existing baseline failures only** (Governance ×4, Navigation `HandleAppInitialized_StoredRoute` ×1, Generated ×3) — confirmed identical to the Story 1.1 baseline against `f40dece`, **none layout/FC-LYT-related, no new regressions**. All three ACs IMPLEMENTED (AC1 documented + applied, AC2 escalated-with-named-owner + FC-DOC deferred to 1.5, AC3 both modes pinned). All 6 tasks verified genuinely done; parameter-surface lock untouched and green.

**Findings (0 High, 1 Medium, 2 Low):**

- **[M1 — FIXED]** `FcPageLayout.Mode` is a public value `[Parameter]`, but it was only pushed to the coordinator inside `OnAfterRender(firstRender)` behind the `firstRender` gate — so a page that rebinds `Mode` after first paint silently failed to update `#fc-main-content`. The "mirror `FcHamburgerToggle`" rationale is imperfect: the toggle registers a stable `@ref`, not a changeable value. Fix: re-apply `SetMode(Mode)` on every render (the coordinator no-ops on an unchanged mode, so no render loop). Pinned by new test `FcPageLayout_WhenModeRebindAfterFirstRender_UpdatesCoordinator`.
- **[L1 — DOCUMENTED]** Multiple/nested `FcPageLayout` instances are lossy (single-writer, last-writer-wins; disposing one resets the coordinator regardless of the other). Declaring two measures on one page is nonsensical and out of MVP scope — limitation recorded in `FcPageLayoutCoordinator`'s XML doc rather than redesigned.
- **[L2 — FIXED]** The QA artifact `_bmad-output/implementation-artifacts/tests/1-2-test-summary.md` was a new untracked file absent from the File List (it lives under `_bmad-output/`, excluded from code review). Added to the File List for completeness.

## Change Log

- 2026-06-03 — Story 1.2 reviewed (story-automator-review). Adversarial review against git reality: build clean, ACs implemented, no new regressions (8 pre-existing baseline failures confirmed). Auto-fixed M1 (`FcPageLayout.Mode` rebind after first render was ignored — now re-applies on every render; new regression test added), documented the single-writer multi-instance limitation (L1) and completed the File List (L2). 0 critical → Status → done.
- 2026-06-03 — Story 1.2 implemented (dev-story). Authored the FC-LYT contract doc (escalated to Product/UX with owner, FC-DOC linkage deferred to Story 1.5); added `FcPageLayoutMode` enum to Contracts; built the cascaded `FcPageLayoutCoordinator` + `FcPageLayout` opt-in component mirroring `LayoutHamburgerCoordinator`; annotated `#fc-main-content` with `data-fc-page-layout` + constrained-class toggle and a scoped `max-inline-size` rule; pinned both modes + lifecycle with `Story12PageLayoutTests`. No shell `[Parameter]` added → parameter-surface lock untouched. Release build clean (0/0); full filtered lane = the 13 pre-existing failures only (no new regressions). Status → review.
- 2026-06-03 — Story 1.2 created (create-story). Comprehensive context-engine analysis completed — confirmed no `<PageLayout>` exists today; scoped the story as decide-contract + minimal opt-in mechanism (cascaded coordinator mirroring `LayoutHamburgerCoordinator`, `FullWidth` default, `data-fc-page-layout` attribute + constrained max-measure CSS) + bUnit pins for both modes. Status → ready-for-dev.
