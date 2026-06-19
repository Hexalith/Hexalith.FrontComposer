# Sprint Change Proposal — Layout-component policy: use FluentStack / FluentGrid / FluentLayout instead of `<div>` + CSS flex/grid

_Workflow: bmad-correct-course · Date: 2026-06-19 · Mode: Incremental · Author: Administrator · Status: **APPROVED & APPLIED** — the two guideline doc edits AND the four HIGH-confidence Shell conversions are landed and test-verified this pass (uncommitted). See "Implementation status" at the bottom._

> Trigger (Administrator): _"find any layout using div that can be replaced by FluentStack, FluentGrid, FluentLayout."_
>
> Establishes a project-wide **layout-component** guideline (`architecture.md` §4.3) — the layout sibling
> of the prior governance passes:
> - `sprint-change-proposal-2026-06-17-fluent-ui-project-policy.md` — §4.1 "Fluent v5 only" (**components**).
> - `sprint-change-proposal-2026-06-17-fluent-accordion-page-sections.md` — §4.2 FluentAccordion (**page sections**).
>   This proposal adds §4.3 (**layout primitives**) to the same family.
>
> **Scoping decisions (Administrator, 2026-06-19, via correct-course):**
> 1. Mode = **Incremental**.
> 2. Outcome = **Codify §4.3 + shrink-only backlog** (not a one-time cleanup, not audit-only).
> 3. Execution = **Docs + proposal + run the HIGH-confidence conversions this pass** (the third option:
>    apply doc edits, write the proposal, *and* convert the clean Shell divs now with tests green).
> 4. Enforcement = **design guideline in `architecture.md`**, code-review-checked — **no**
>    `…FluentConformanceTests` governance guard (same call as §4.2).

---

## Section 1 — Issue Summary

**Problem.** FrontComposer has a project-wide *component* policy (§4.1) and a *page-section* policy (§4.2),
but **no policy for the layout primitives themselves**. Hand-authored components stack children with bare
`<div>` + CSS `display:flex`/`grid`, duplicating layout the Fluent v5 design system already owns
(`FluentStack`, `FluentGrid`, `FluentLayout`) — even though the framework's own generated output and
`FrontComposerShell` already render through those components.

**Issue type:** New requirement / UI-pattern standardization via direct stakeholder directive (not a
defect). It **reinforces** existing requirements (NFR6 a11y, UX-DR1/DR2 Fluent tokens, ADR-003
build-on-Fluent-v5); no FR/NFR is modified.

**Discovery.** Stakeholder directive 2026-06-19, followed by a 67-file `.razor` inventory of `src/`,
`samples/`, `tests/` (submodules excluded per `CLAUDE.md`), reconciled against the **verified** Fluent v5
`FluentStack`/`FluentGrid` APIs (MCP `fluent-ui-blazor`, pinned `5.0.0-rc.3-26138.1`).

**Evidence — Fluent layout components are already the de-facto primitive in the framework:**

| Spot | Mechanism | Status |
|---|---|---|
| `FrontComposerShell.razor` | page chrome via `FluentLayout`/`FluentLayoutItem`; header rows via `FluentStack` | ✅ already conforms |
| Generated projection/command bodies | emitter renders `FluentStack`/accordion | ✅ already conforms |
| `FcPageHeader`, `FcCustomizationDiagnosticPanel`, Counter samples | already use `FluentStack` | ✅ partial |
| Hand-authored Shell components (`FcAccountMenu`, `FcSettingsDialog`, `FcCollapsedNavRail`, `FcProjectionLoadingSkeleton`, …) | bare `<div>` + CSS flex | ⛔ the gap this proposal closes |

---

## Section 2 — Impact Analysis

### Epic Impact — none
Epics 1–7 are **Done** and already build on Fluent v5. No epic is added/removed/resequenced/invalidated.
Cross-cutting layout conformance anchored to §4.1/NFR6.

### Story Impact — none
No formal story. Recorded as a UI-pattern/conformance change (same handling as the prior correct-course passes).

### Artifact Conflicts

| Artifact | Impact | Action |
|---|---|---|
| **PRD** | N/A — no authored PRD; `epics.md` FR/NFR unchanged | none |
| **Architecture** (`_bmad-output/project-docs/architecture.md`) | §4.1/§4.2 don't cover layout primitives | ✏️ add **§4.3** (**done**) |
| **AI rules** (`_bmad-output/project-context.md`) | Blazor Shell rules lack the layout-component pattern | ✏️ add bullet (**done**) |
| **UI/UX** | no authored UX spec to conflict; change *reinforces* the Fluent token/a11y contracts | positive |
| **Tests** | enforcement = guideline (no guard) → **no `…FluentConformanceTests` added**. Converted components keep their existing bUnit assertions (class names + `data-testid`/`role` preserved) | ✅ verified green this pass |
| **Governance guard (§4.1)** | the legacy-token guard has a **stale-entry assertion** — risk if a conversion fully cleans a backlog `.css`. Mitigated: skeleton conversion leaves `--neutral-stroke-rest` in the header rule, so `FcProjectionLoadingSkeleton.razor.css` stays correctly in the backlog | ✅ guard green |
| **Public API** | no public surface change | none |
| **`docs/` (Gate 2d)** | guideline doc only, no new public component | none |
| **Memory** | add a `project` memory recording the §4.3 pattern + the keep-as-div carve-outs | ✏️ this pass |
| **CI / IaC / deployment / observability** | none | none |

### Technical Impact
- **This pass:** 2 doc edits (§4.3 + project-context) + **4 Shell component conversions** (8 source files:
  4 `.razor` + 4 `.razor.css`). Release build clean (0 warnings, TWAE); affected lanes green.
- **Key API correction vs. the raw scan:** `FluentGrid` is a **fixed 12-point breakpoint grid**
  (`FluentGridItem Xs/Sm/Md/Lg`), **not** content-driven `grid-template-columns: repeat(auto-fill|auto-fit,
  minmax(...))`. The `.fc-home-cards` / `CounterCardLayoutTemplate` card walls therefore **do not map** to
  `FluentGrid` (conversion would change responsive behavior) and stay CSS grid. The change is almost
  entirely a **`FluentStack`** consolidation; **no clean `FluentGrid` conversions exist**, and `FluentLayout`
  is **already** in use — nothing to do there.
- **`FluentStack` gotcha (now in §4.3):** defaults `Width="100%"`. Replacing an `inline-flex` (`FcAccountMenu`)
  or a fixed-width rail (`FcCollapsedNavRail`) requires an explicit `Width` (`fit-content` / `48px`), else
  layout regresses.

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (selected).** Codify §4.3 + project-context this pass, run the
HIGH-confidence Shell conversions now, and track the remainder as a shrink-only backlog. No epic
restructuring; additive to §4.1/§4.2.

- **Option 2 — Rollback:** N/A — nothing to revert; the rule didn't exist as a written standard.
- **Option 3 — MVP review:** N/A — MVP not threatened.

**Effort:** Low–Medium · **Risk:** Low (class names + a11y attributes preserved; verified against a clean
baseline — see §6) · **Timeline:** no epic/milestone slip.

### Mapping rules (decided this pass)
- `<div>` whose **only** role is 1-D flex stacking (`display:flex`+`flex-direction`+`gap`, ± alignment) →
  **`FluentStack`**.
- `<div>` forming a **responsive 12-point column grid** → **`FluentGrid`/`FluentGridItem`**.
- **page header/nav/content/footer scaffold** → **`FluentLayout`/`FluentLayoutItem`**.

### Stays a `<div>` (layout the design system does NOT own — never converted)
Positioning/overlays (`position:absolute|fixed|sticky`, `z-index`); sr-only/`aria-live` regions; `role`/
semantic-element landmarks (`role="status|alert|region|group"`, `<header>`/`<section>`/`<aside>` — nest a
`FluentStack` *inside*); **`auto-fill`/`auto-fit minmax()` card walls** (FluentGrid can't express them);
**`@media` direction flips** (FluentStack has one static `Orientation`); **density-token-bound gaps**
(`var(--design-unit*)`); single-child wrappers.

**Why a guideline, not a guard.** A regex cannot separate a delegatable flex stack from a
positioning/sr-only/landmark/auto-fill/density-coupled `<div>` without false positives — exactly as for
§4.2. Enforced by code review against the §4.3 definition; progress tracked by the shrink-only backlog (§4.D).

---

## Section 4 — Detailed Change Proposals

### 4.A — Architecture guideline (`architecture.md` new §4.3) — **APPLIED**
Added §4.3 "Layout-component policy (project-wide guideline)" immediately after §4.2: the mapping rules, the
keep-as-`div` exclusion list (incl. the FluentGrid auto-fill caveat, the `@media`-flip caveat, and the
density-token-gap caveat), the `FluentStack Width="100%"` default gotcha, the RC attribute-splatting caveat,
the guideline-not-guard framing + shrink-only backlog, and the first-burn-down record. (Full text in the file.)

### 4.B — AI-agent rule (`project-context.md`, "Blazor Shell & Fluxor Rules") — **APPLIED**
Added a "Layout uses Fluent v5 layout components (project-wide guideline)" bullet after the §4.2 accordion
bullet; cross-links `architecture.md` §4.3 and the `[[fluent-v5-only-domain-ui]]` / `[[no-fluent-theme-redefinition]]` memories.

### 4.C — Conversions applied this pass (4 clean Shell divs → FluentStack)

| File | Old | New | CSS shrink |
|---|---|---|---|
| `Components/Layout/FcAccountMenu.razor` | `<div class="fc-account-menu" data-testid=…>` (`inline-flex; center; gap .5rem`) | `<FluentStack Orientation="Horizontal" VerticalAlignment="Center" HorizontalGap="8px" Width="fit-content" Class="fc-account-menu" data-testid=…>` | deleted the `.fc-account-menu` flex rule (kept `__name` ellipsis/`@media` + `__menu-user`) |
| `Components/Layout/FcSettingsDialog.razor` | `<div class="fc-settings-body" data-testid=…>` (`flex column; gap 16px; padding 16px`) | `<FluentStack Orientation="Vertical" VerticalGap="16px" Class="fc-settings-body" data-testid=…>` | dropped flex/gap, **kept** `padding:16px` + `h3` rhythm; **footer divs unchanged** (`@media column-reverse` flip + flex-item sizing) |
| `Components/Layout/FcCollapsedNavRail.razor` | `<div class="fc-collapsed-rail" role="navigation" aria-label=… data-testid=…>` (`flex column; gap 4px; center; 48px; height 100%`) | `<FluentStack Orientation="Vertical" VerticalGap="4px" HorizontalAlignment="Center" Width="48px" Class="fc-collapsed-rail" role="navigation" aria-label=… data-testid=…>` | dropped flex/gap/align/width, **kept** `padding:4px 0; height:100%` |
| `Components/Rendering/FcProjectionLoadingSkeleton.razor` | 4× `<div class="fc-projection-skeleton-row[ …-header]">` (`flex; gap 12px; center`) | 4× `<FluentStack Orientation="Horizontal" VerticalAlignment="Center" HorizontalGap="12px" Class="fc-projection-skeleton-row[ …-header]">` | dropped flex/gap/align, **kept** row padding + the `--neutral-stroke-rest` header border (file stays in §4.1 backlog) |

Invariants preserved across all four: existing **class names** (selector hooks), **`data-testid`** (e2e
contract), **`role="navigation"`/`aria-label`** (a11y landmark) — all splatted by `FluentStack` and verified
in each component's bUnit lane. The root `role="status"` skeleton landmark and the card/timeline/grid
wrapper divs were **not** converted (semantic landmark / no flex).

### 4.D — Conversion backlog (shrink-only; tracks the rest)

| File | Element | Target | Conf. | Disposition / why |
|---|---|---|---|---|
| **(applied this pass)** `FcAccountMenu`, `FcSettingsDialog` (body), `FcCollapsedNavRail`, `FcProjectionLoadingSkeleton` (rows) | flex stacks | FluentStack | HIGH | ✅ converted + verified |
| `FcHomeDirectory.razor` | `.fc-home-directory` | FluentStack V | **deferred** | gap/padding use the legacy `--design-unit` token (itself in the §4.1 backlog). Hardcoding the gap breaks density scaling; moving the var onto a FluentStack param relocates a legacy token into the `.razor` and **trips the §4.1 guard**. Convert **after** the token is migrated to Fluent 2. |
| `FcDevModeOverlay.razor` | `.fc-devmode-drawer__header` | FluentStack H/SpaceBetween | **excluded** | it is a semantic **`<header>` element, not a `<div>`** (outside the literal trigger), wrapping a trivial 2-child layout. Keep the `<header>`. The drawer/overlay themselves are fixed-position → keep-as-div. |
| `FcCommandPalette`, `FcPaletteResultList`, `FcDensityPreviewPanel`, `FcPendingCommandSummary` | various | FluentStack | MED | carry `role="dialog"`+`@onkeydown`/`@ref`, repeated `role=option` rows, specimen preview, or `display:grid`-used-as-stack — convert with care (nest FluentStack inside landmark divs); not a clean win. |
| `samples/Counter/.../CounterPage`, `CounterCardLayoutTemplate` | page / card wall | FluentStack / **(grid → keep)** | MED / **N/A** | sample-only; the card wall is `auto-fill minmax()` → **keep CSS grid** (FluentGrid can't express it). |
| **Keep-as-div (not backlog)** | overlays, drawers, badges, sr-only/`aria-live`, `role`/semantic landmarks, `auto-fill` card walls, `@media` flips, density-token gaps | — | — | the §4.3 exclusion list. |

**FluentGrid:** no clean conversions (all candidate grids are `auto-fill`/`auto-fit minmax()`). **FluentLayout:**
already in use (`FrontComposerShell`) — nothing to do.

---

## Section 5 — Implementation Handoff

**Scope classification: Minor–Moderate.** Two doc edits + four in-repo Shell conversions (applied this pass,
verified green); a small shrink-only backlog routed for future Developer pickup. No epic/PRD/architecture
replan; MVP unaffected.

**Routing:**
- **This pass (DONE):** Developer (this agent) applied §4.3 + project-context + the 4 HIGH conversions; build
  + affected lanes verified.
- **Backlog (future Developer, `bmad-dev-story`/`bmad-quick-dev`):** the MED candidates above, each converted
  per the §4.3 definition; `FcHomeDirectory` only after its `--design-unit` gap is migrated to a Fluent 2 token.
- **Submodules (Tenants.UI, Admin.UI):** any analogous div-flex layout there is a **separate per-repo
  correct-course** (submodule edits need explicit approval; never touch nested submodules).

**Constraints (project-context + CLAUDE.md):** no direct commits to `main` (feature branch + PR);
Conventional Commits — guideline docs = `docs`, the component conversions = `refactor` (layout reshaping,
**not** `feat` — no false minor bump); `.slnx` only; `TreatWarningsAsErrors`; run the default + Governance
lanes with `DiffEngine_Disabled=true`; **do NOT commit** unless explicitly requested.

**Success criteria:**
- ✅ `architecture.md` §4.3 + `project-context.md` bullet carry the layout-component guideline, the keep-as-div
  exclusions, and the FluentGrid/`Width`/density caveats.
- ✅ `FcAccountMenu`, `FcSettingsDialog`, `FcCollapsedNavRail`, `FcProjectionLoadingSkeleton` converted to
  `FluentStack`; redundant flex CSS removed; non-layout CSS preserved.
- ✅ Release build clean (0 warnings, TWAE); affected component + shell + **Governance** lanes green; **no new
  test failures vs. a clean baseline** (identical 9 pre-existing failures with and without the change — see §6).
- ✅ FluentGrid auto-fill card walls and `<header>`/density/positioning divs **left unchanged** (no over-application).
- ⏳ Live visual check under Aspire (optional follow-up).
- ⏳ Commit/PR (not done — not requested).

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ (1.1 no formal story — stakeholder directive · 1.2 new-requirement/standardization,
  not a defect · 1.3 67-file `.razor` inventory + verified Fluent APIs as evidence)
- **§2 Epic Impact:** ✅ N/A (Epics 1–7 Done; no epic add/remove/resequence)
- **§3 Artifact Conflicts:** 3.1 N/A (no PRD) · 3.2 ✅ architecture §4.3 (done) · 3.3 ✅ positive (no UX spec) ·
  3.4 ✅ project-context (done) + conversions test-verified + memory; no public-API/CI/IaC impact; **no guard**
- **§4 Path Forward:** ✅ Option 1 (Direct Adjustment)
- **§5 Proposal Components:** ✅ this document
- **§6 Final Review/Handoff:** ✅ approved (Administrator: "Docs + proposal + run HIGH conversions"); doc
  codification + 4 conversions implemented and verified this pass. 6.4 sprint-status.yaml: **N/A** (no epic
  add/remove/renumber).

---

## Implementation status (2026-06-19)

**Codification — DONE (docs, uncommitted):**
- `_bmad-output/project-docs/architecture.md` — new **§4.3 "Layout-component policy (project-wide guideline)"**.
- `_bmad-output/project-context.md` — new **"Layout uses Fluent v5 layout components"** bullet (Blazor Shell rules).

**Conversions — DONE (src, uncommitted, verified):**
- `FcAccountMenu.razor` (+`.razor.css`), `FcSettingsDialog.razor` (+`.razor.css`), `FcCollapsedNavRail.razor`
  (+`.razor.css`), `FcProjectionLoadingSkeleton.razor` (+`.razor.css`) → `FluentStack`; redundant flex CSS removed.

**Verification — DONE:**
- Release build of Shell + Shell.Tests: **0 warnings / 0 errors** (TWAE).
- In-process xUnit v3: affected component + shell-integration + Governance classes = **77/77 green**.
- Full Shell default lane = **1890 total, 9 failed**; the **same 9** fail on a clean `git stash` baseline
  (`AuthBoundaryTests`×2, `FcHomeDirectoryTests.RendersMainRoleAndSortedByUrgencyAriaDescription`,
  `FrontComposerServerSecurityServiceExtensionsTests`, `CommandRendererFullPageTests`,
  `CounterStoryVerificationTests`×2, `CiGovernanceTests.PackageInventory…`,
  `NavigationEffectsLastActiveRouteTests`) — **pre-existing, unrelated to this change; zero new failures.**

### Residuals
1. **Backlog conversions** (MED candidates; `FcHomeDirectory` after the `--design-unit` token migration) — not run this pass.
2. **Submodule sweep** (Tenants.UI, Admin.UI) — separate per-repo correct-course (approval-gated).
3. **Live visual check** under Aspire.
4. **Project memory** entry for §4.3 (added this pass).
5. **Commit/PR** — not done (not requested). Conventional Commits: guideline docs = `docs`; conversions = `refactor` (NOT `feat`).
