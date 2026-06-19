# Sprint Change Proposal — No Fluent v5 theme redefinition (trigger: `fc-page-header.css`)

_Workflow: bmad-correct-course · Date: 2026-06-19 · Mode: Incremental · Author: Administrator · Status: **APPROVED + IMPLEMENTED — Shell builds clean (TWAE, 0 warnings); FcPageHeader + FluentConformance lanes 7/7 GREEN; changes uncommitted. See "Implementation complete" at the bottom.**_

> Trigger (Administrator): _"why is this CSS needed? FrontComposer should not redefine Blazor Fluent UI v5
> design and theming unless required by a feature not implemented in Fluent UI."_
>
> Extends the Fluent-first governance family from **raw interactive controls** (2026-06-17
> `…-fluent-ui-project-policy.md`, architecture.md §4.1) to **CSS theming**: components must not
> re-implement what a Fluent v5 component already provides, nor use legacy Fluent v4/FAST design tokens.
>
> **Submodule approval:** Administrator authorized editing `Hexalith.AI.Tools/hexalith-ux-instructions.md`
> (Edit 5) on 2026-06-19. No other submodule is touched.

---

## Section 1 — Issue Summary

**Problem.** The newly added `FcPageHeader` component (commit 80afcd9) ships a hand-authored stylesheet,
`src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-page-header.css`, that **re-implements Fluent v5
typography and color**:

- `font-size` / `font-weight` / `line-height` on the route `<h1>` recreate a Fluent heading ramp that
  `FluentText`'s `Size`/`Weight` parameters already provide;
- `color:` declarations recreate foreground roles that `FluentText`'s `Color` parameter / the default
  foreground already provide;
- the tokens used (`--neutral-foreground-hint`, `--neutral-foreground-rest`, `--type-ramp-plus-3-font-size`,
  `--type-ramp-plus-3-line-height`) are **legacy Fluent v4 / FAST** names, inconsistent with the Fluent 2
  (`--colorNeutralForeground*`) set used elsewhere in the Shell. `fc-page-header.css` is the **only** file
  in the Shell using `--type-ramp-plus-3-*`.

This violates the ecosystem rule **"Reuse over hand-rolling / No theme redefinition"**
(`Hexalith.AI.Tools/CLAUDE.md` + `hexalith-ux-instructions.md`): *reach for a Fluent UI V5 / FrontComposer
component first; fall back to custom CSS only when no such component exists.* The project-level policy
(architecture.md §4.1) had codified this **only for raw interactive controls**, leaving CSS theming
ungoverned — the gap this trigger exposed.

**Issue type:** New requirement / governance standardization via stakeholder directive (not a defect). It
*reinforces* UX-DR1/DR2 (Fluent tokens) and NFR6 (a11y); it changes no FR/NFR.

**Discovery.** Administrator directive 2026-06-19, on review of the `FcPageHeader` styling added in 80afcd9 /
f4910d7.

**Evidence — line-by-line verdict on `fc-page-header.css`:**

| CSS rule | Purpose | Fluent v5 native equivalent | Verdict |
|---|---|---|---|
| `.fc-page-header { display:block }` | block layout | `<header>` is block by default | Redundant — delete |
| `__content,__title-row { width:100% }` | stretch the FluentStacks | `FluentStack Width="100%"` | Replaceable by parameter |
| `…{ margin:0 }` | reset UA margins | `FluentText Margin="0"`; `FluentStack VerticalGap` owns spacing | Replaceable by parameter |
| `__eyebrow { color:var(--neutral-foreground-hint) }` | de-emphasised color | `FluentText Color="Color.Lightweight"` | Replaceable (legacy v4 token) |
| `__heading { color/font-size/font-weight/line-height }` | H1 typography | `FluentText As=H1 Size=Size700 Weight=Semibold` | **Redefinition** — replaceable; lone v4 `--type-ramp-plus-3-*` user |
| `__description,__metadata { color:var(--neutral-foreground-rest) }` | default foreground | default color = no rule | Redundant — delete |

The only thing Fluent v5 genuinely cannot supply is **programmatic focus on the route heading after SPA
navigation** (`FcPageHeader.FocusHeadingAsync` → `_headingElement.FocusAsync()` with `tabindex`) — a
markup/C# concern that needs **zero CSS** (a real focusable `<h1>` wrapping a `FluentText` for typography).

---

## Section 2 — Impact Analysis

### Epic Impact — none
FrontComposer Epics 1–7 are Done and built on Fluent v5. No epic added/modified/removed/resequenced. This is
cross-cutting **conformance + governance** (NFR6, UX-DR1/DR2).

### Story Impact — none
No formal story; `FcPageHeader` was added ad-hoc. Recorded as a governance/conformance change (same handling
as the four prior correct-course passes).

### Artifact Conflicts

| Artifact | Impact | Action |
|---|---|---|
| **PRD** | N/A — no authored PRD; `epics.md` FR/NFR unchanged | none |
| **Component** (`FcPageHeader.razor` + `fc-page-header.css`) | CSS re-implements Fluent theming | ✏️ refactor + 🗑️ delete CSS (Edit 1) |
| **Architecture** (`architecture.md` §4.1) | governs only raw controls, silent on CSS theming | ✏️ add design-system-fidelity clause (Edit 2) |
| **AI rules** (`project-context.md`) | Blazor Shell rules lack the theming rule | ✏️ add rule (Edit 3) |
| **Tests** | need a guard; `FcPageHeaderTests` **survive unchanged** (verified) | ➕ add Governance fact (Edit 4) |
| **Ecosystem UX instructions** (`Hexalith.AI.Tools/hexalith-ux-instructions.md`) | `CLAUDE.md` carries the rule; topical module doesn't | ✏️ mirror rule (Edit 5, submodule — authorized) |
| **Memory** | recall the guard + debt registry at session start | ✏️ add (Edit 6) |
| **Public API / `docs/` / CI / IaC** | no public surface change; no new published component | none |

### Technical Impact
- **Blast radius: essentially zero.** `FcPageHeader` has **no consumers** — no `.razor`/page references it.
- **Broader discovery:** the legacy-v4-token footprint is **12 hand-authored Shell CSS files** (beyond the
  trigger), handled as a documented, allowlisted migration backlog — **not** migrated in this pass.
- Refactor + 2 doc edits + 1 governance test (FrontComposer repo) + 1 submodule doc edit (AI.Tools).

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (selected).** Refactor `FcPageHeader` to express all styling through Fluent
v5 component parameters (delete `fc-page-header.css`), codify the "no theme redefinition" principle
(architecture + project-context + ecosystem UX instructions), and enforce a mechanical slice of it via a
legacy-token Governance guard with a documented backlog allowlist.

- **Option 2 — Rollback:** N/A — `FcPageHeader` is new; the CSS *is* the original. Nothing to revert to.
- **Option 3 — MVP review:** N/A — MVP unaffected.

**Effort:** Low · **Risk:** Low (zero consumers; tests survive). · **Timeline:** no epic/milestone slip.

**Principle (decided this pass).** Components express typography/color/spacing via **Fluent v5 component
parameters or Fluent 2 tokens**; hand CSS must not recreate component-provided styling, nor use legacy v4/FAST
tokens. Custom CSS is reserved for **layout the design system doesn't own** (flex/grid, gaps, UA resets) or a
**feature Fluent UI lacks**. The broad principle is review-enforced (§4.1 prose, like the §4.2 accordion
guideline); the narrow legacy-token slice is guard-enforced (Edit 4) — the same guarded-vs-review split that
already exists between §4.1 and §4.2.

**Decisions captured (Administrator, 2026-06-19):** ① Mode = Incremental. ② Scope = component + principle +
guard. ③ 12-file legacy-token backlog = **documented debt**, migrated separately. ④ `hexalith-ux-instructions.md`
edit = **authorized**.

---

## Section 4 — Detailed Change Proposals (all approved)

### Edit 1 — Refactor `FcPageHeader.razor`; delete `fc-page-header.css`
Express styling via Fluent params; keep the focusable `<h1>` (Fluent-absent feature) with no CSS.

- `<HeadContent>` + `<link …/fc-page-header.css>` **removed**; `fc-page-header.css` **deleted**; all
  `fc-page-header__*` class hooks dropped (only the root `fc-page-header` class stays — tests + consumer
  `Class` passthrough).
- Eyebrow → `FluentText … Color="Color.Lightweight" Margin="0"`.
- Heading → `<h1 id tabindex style="margin:0" @ref>`<`FluentText As="TextTag.Span" Size="TextSize.Size700"
  Weight="TextWeight.Semibold">@Heading</FluentText>`</h1>` (Fluent type ramp + default color; focus preserved).
- Description → `FluentText … Margin="0"` (default color).
- FluentStacks → `Width="100%"` (replaces `width:100%`).
- `FcPageHeaderTests` need **no change** (verified: `<h1>` count/id/tabindex, recursive `TextContent`, root
  class, `data-fc-page-header-*` all preserved).

**Implementation residuals to verify against pinned RC `5.0.0-rc.3-26138.1`:** ① member names
`Color.Lightweight`, `TextSize.Size700/200/300`, `TextWeight.Semibold`, and that `FluentText` exposes
`Color`/`Margin` + splats `data-*`; ② visual-check the `Size700` heading scale (`--type-ramp-plus-3`≈28px ≈
`Size700`); ③ optional cleaner variant — if `FluentText As="TextTag.H1"` exposes a focusable `Element` ref in
the RC, collapse `<h1>`+span into one `FluentText As=H1 Margin="0"`. Use the safe wrap form unless ② verified.

### Edit 2 — `architecture.md` §4.1: design-system-fidelity clause
Append (after the carve-out table) a peer clause to the raw-control rule: components express
typography/color/spacing via Fluent v5 parameters or Fluent 2 tokens; no recreating component-provided
styling; no legacy v4/FAST tokens (`--neutral-foreground-*`, `--type-ramp-*`, `--accent-*`,
`--neutral-fill-*`, `--palette-*`); custom CSS only for layout the design system doesn't own or a
Fluent-absent feature; enforced by the legacy-token scan (Edit 4); the pre-v5 files are an allowlisted
migration backlog. Cites `hexalith-ux-instructions.md` §"Reuse over hand-rolling".

### Edit 3 — `project-context.md`: matching AI rule
New bullet after "Fluent-only UI (project-wide)" under *Blazor Shell & Fluxor Rules*, mirroring §4.1 in the
file's terse style; lists the 5-of-12 most-relevant backlog files and points to architecture.md §4.1.

### Edit 4 — Governance guard (`FluentConformanceTests.cs`)
New `[Fact] [Trait("Category","Governance")]` scanning `src/Hexalith.FrontComposer.Shell` `*.css` (obj/bin
excluded) for legacy tokens `--(neutral|accent|type-ramp|palette)-`, with a **12-file migration-backlog
allowlist** (`FcNewItemIndicator`, `FcDevModeAnnotation`, `FcDevModeOverlay`, `FcDevModeToggleButton`,
`FcPendingCommandSummary`, `FcHomeDirectory`, `FcLifecycleWrapper`, `FcFieldPlaceholder`,
`FcProjectionLoadingSkeleton`, `fc-empty-state.scoped.css`, `fc-projection.css`, `FrontComposerShell.razor.css`).
Green today; blocks new offenders. Limitations stated: file-level (not line-level); catches legacy *tokens*,
not v5-token/hardcoded-px theming (that stays review-enforced per §4.1).

### Edit 5 — `hexalith-ux-instructions.md` (submodule, authorized): mirror "No theme redefinition"
New "Theming and design tokens" section after "Reuse over hand-rolling", matching the rule already in
`Hexalith.AI.Tools/CLAUDE.md` so the topical UX module stays in sync.

### Edit 6 — Project memory
`memory/no-fluent-theme-redefinition.md` (type `feedback`) + `MEMORY.md` pointer, recording the principle,
the guard, and the 12-file shrinking backlog; linked to `[[fluent-v5-only-domain-ui]]`.

---

## Section 5 — Implementation Handoff

**Scope classification: Minor–Moderate** — one Shell component refactor + 1 deleted CSS file + 2 FrontComposer
doc edits + 1 Governance test, plus 1 authorized submodule doc edit and a memory. No epic/PRD/architecture-pattern
replan; MVP unaffected.

**Routing → Developer agent** (direct implementation).

**Sequencing:**
1. **FrontComposer repo:** refactor `FcPageHeader.razor` + delete `fc-page-header.css` (Edit 1); edit
   `architecture.md` §4.1 (Edit 2) + `project-context.md` (Edit 3); add the guard fact (Edit 4). Build
   Release clean (TWAE); run default + Governance lanes green
   (`dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`,
   `DiffEngine_Disabled=true`). Confirm `FcPageHeaderTests` still green and the new guard green.
2. **AI.Tools submodule:** add the "Theming and design tokens" section (Edit 5).
3. **Memory:** write Edit 6 files.
4. **Live visual check** under `Hexalith.FrontComposer.AppHost` (Aspire) once a page adopts `FcPageHeader`
   (none does yet) — confirm the heading renders at the intended Fluent ramp and the eyebrow is de-emphasised.

**Constraints (project-context + CLAUDE.md):** no direct commits to `main` (feature branch + PR); Conventional
Commits — the component change is `refactor`/`fix` (**not** `feat`: a styling cleanup must not trigger a minor
bump), docs/tests are `docs`/`test`; `.slnx` only; `TreatWarningsAsErrors`; **do NOT commit** unless explicitly
requested (the submodule edit propagates ecosystem-wide).

**Success criteria:**
- ✅ `fc-page-header.css` deleted; `FcPageHeader` styling expressed via Fluent v5 parameters; focus `<h1>` intact.
- ✅ architecture.md §4.1 + project-context.md carry the design-system-fidelity rule; `hexalith-ux-instructions.md`
  mirrors it.
- ✅ New Governance guard green with the 12-file backlog allowlist; `FcPageHeaderTests` green; default lane green;
  Release build clean (TWAE).
- ✅ Memory recorded.
- ⏳ Live-visual check deferred until a consumer adopts `FcPageHeader`.

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ (1.1 no formal story — stakeholder directive · 1.2 new-requirement/standardization,
  not a defect · 1.3 line-by-line CSS verdict + v4/v5 token grep + zero consumers)
- **§2 Epic Impact:** ✅ N/A (Epics 1–7 Done; no add/remove/resequence)
- **§3 Artifact Conflicts:** 3.1 N/A (no PRD) · 3.2 ✅ architecture §4.1 edit · 3.3 ✅ positive (no UX spec to
  conflict; reinforces NFR6) · 3.4 ✅ component + tests + project-context + ecosystem UX + memory; no
  public-API/CI/IaC impact
- **§4 Path Forward:** ✅ Option 1 (Direct Adjustment), Effort Low / Risk Low
- **§5 Proposal Components:** ✅ this document (6 approved edits)
- **§6 Final Review/Handoff:** ✅ approved (Administrator: "yes") + implemented this pass. 6.4 sprint-status.yaml: **N/A** (no epic change).

---

## Implementation complete (2026-06-19)

**Result:** `FcPageHeader` styling fully expressed via Fluent v5 component parameters; `fc-page-header.css`
deleted; Shell builds clean (TWAE, 0 warnings/0 errors); change-specific lanes **7/7 GREEN**. Changes left
**uncommitted** (no commit requested).

### Division of work
- **Authored in parallel by Administrator (left untouched, richer than the drafts):** architecture.md §4.1
  "No theme redefinition" clause (Edit 2); `FluentConformanceTests` legacy-token guard with a shrink-only,
  stale-entry-asserting backlog (Edit 4); `hexalith-ux-instructions.md` "No theme redefinition" section
  (Edit 5); ecosystem `Hexalith.AI.Tools/CLAUDE.md` rule.
- **Applied this pass (the remaining edits):**
  - **Edit 1** — `FcPageHeader.razor` refactored: `<HeadContent>`/`<link>` removed; eyebrow →
    `Color="Color.Lightweight"`; heading → `<h1 …>`<`FluentText As="TextTag.Span" Size="TextSize.Size700"
    Weight="TextWeight.Semibold">`</h1>` (focusable `<h1>` preserved); description default color; FluentStacks
    `Width="100%"`; `Style="margin: 0;"` UA-resets. **`fc-page-header.css` deleted.** All member names
    verified against the pinned RC assembly's XML docs before editing (docs ≠ RC: `FluentText` has **no**
    `Margin` property — used `Style` instead).
  - **Edit 3** — `project-context.md`: "No theme redefinition (project-wide)" rule added under Blazor Shell rules.
  - **Edit 6** — memory `no-fluent-theme-redefinition.md` + `MEMORY.md` pointer.
  - **Reconciliation for the deletion:** removed `wwwroot/css/fc-page-header.css` from the guard backlog
    (16→15) and updated the §4.1 note + backlog table (first backlog burn-down).

### Verification (2026-06-19, `DiffEngine_Disabled=true`)
- `dotnet build` Shell.Tests (Debug, TWAE): **0 warnings / 0 errors**.
- `FcPageHeaderTests` (4) + `FluentConformanceTests` (3, incl. the new legacy-token guard): **7/7 pass** —
  unchanged component tests survive; guard green after the deletion + allowlist trim.
- No code/build reference to the deleted CSS remains (only a historical note in the Tenants submodule's
  BMAD output + the past-tense note in architecture.md §4.1).

### Residuals
1. **Pre-existing, unrelated Governance failures (NOT this change):** `AuthBoundaryTests` ×2 (auth-token
   boundary) + `CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable` (inventory script exit 1).
   None reference any file touched here; orthogonal to the CSS/theming change.
2. **Full Release solution build + complete default test lane** — not run this pass (only the Shell.Tests
   project, Debug, change-specific + Governance lanes).
3. **Live-visual check** under Aspire — deferred until a page adopts `FcPageHeader` (no consumer yet);
   confirm the `Size700`/`Semibold` heading ramp and the `Color.Lightweight` eyebrow render as intended.
4. **Commit/PR** — not done (not requested). Per Conventional Commits: component change = `refactor`/`fix`
   (NOT `feat`); governance docs/tests = `docs`/`test`; the `Hexalith.AI.Tools` submodule edit propagates
   ecosystem-wide — branch + PR per repo, no direct commit to `main`.
