# Sprint Change Proposal — Page-section layout pattern: use FluentAccordion for titled page sections

_Workflow: bmad-correct-course · Date: 2026-06-17 · Mode: Incremental · Author: Administrator · Status: **APPROVED (doc codification) — the two in-repo guideline edits are APPLIED; the project-wide page conversions are routed as a per-repo handoff (not executed this pass). See "Implementation status" at the bottom.**_

> Trigger (Administrator): _"use FluentAccordion for page sections."_
>
> Establishes `FluentAccordion` / `FluentAccordionItem` as the **standard container for titled page
> sections** across every FrontComposer UI surface. Builds directly on the prior governance pass:
> - `sprint-change-proposal-2026-06-17-fluent-ui-project-policy.md` — project-wide "Fluent v5 only" policy
>   (architecture.md §4.1 + carve-out registry). This proposal adds the **layout** companion (§4.2) to that
>   **component** rule.
>
> **Scoping decisions (Administrator, 2026-06-17, via correct-course):**
> 1. Mode = **Incremental**.
> 2. Scope = **Project-wide** — Shell, Counter sample, `Hexalith.Tenants.UI`, `Hexalith.EventStore.Admin.UI`.
> 3. Coverage = **All titled page sections** (every page with 2+ sibling titled sections), per the precise
>    definition in §3 — *not* secondary-sections-only.
> 4. Enforcement = **Design guideline in `architecture.md`** — documented + AI rule, **no** automated
>    `…FluentConformanceTests` governance guard (unlike the §4.1 Fluent-only rule).

---

## Section 1 — Issue Summary

**Problem.** FrontComposer has a project-wide *component* policy (§4.1: every control is FrontComposer or
Fluent v5) but **no project-wide *layout* policy** for how a page composes its sections. As a result,
multi-section pages are structured inconsistently — some hand-authored pages stack bare `<h2>`/`<h3>` +
`<section>` blocks (e.g. `FcSettingsDialog` has three bare `<h3>` sections), while the framework's own
generated output already groups sections into `FluentAccordion` (`[ProjectionFieldGroup]` buckets →
`FluentAccordionItem`s in `ProjectionRoleBodyEmitter`; `FcHomeDirectory` collapses zero-urgency contexts
into a `FluentAccordion`). The directive standardizes the hand-authored side onto the pattern the
framework already emits.

**Issue type:** New requirement / UI-pattern standardization discovered via direct stakeholder directive
(not a defect). It **reinforces** existing requirements (NFR6 accessibility, UX-DR1/DR2 Fluent tokens,
ADR-003 build-on-Fluent-v5) rather than changing any of them. No FR/NFR is modified.

**Discovery.** Stakeholder directive on 2026-06-17, followed by a four-surface `.razor` inventory of
hand-authored multi-section pages.

**Evidence — `FluentAccordion` is already the de-facto section container in two framework spots:**

| Spot | Mechanism | Status |
|---|---|---|
| Generated projection detail bodies | `ProjectionRoleBodyEmitter` emits one `FluentAccordionItem` per `[ProjectionFieldGroup]` bucket (>6 supported props, or any field-group annotation) | ✅ already conforms — approval/snapshot-gated (Story 4-1, D10 regression gate) |
| `Shell/Components/Home/FcHomeDirectory.razor` | zero-urgency "other areas" collapse into a `FluentAccordion` | ✅ partial — pattern already in use |
| Hand-authored multi-section pages (e.g. `FcSettingsDialog`, `CounterPage`, `TenantDetailPage`) | bare `<h2>`/`<h3>` + `<section>` stacks | ⛔ inconsistent — the gap this proposal closes |

---

## Section 2 — Impact Analysis

### Epic Impact — none
FrontComposer Epics 1–7 are **Done** and already build on Fluent v5. No epic is modified, added, removed,
resequenced, or invalidated. This is cross-cutting **layout conformance**, anchored to NFR6 and UX-DR1/DR2.
Domain-consumer conformance (Tenants.UI, Admin.UI) is not tracked in FrontComposer's `epics.md`.

### Story Impact — none
No formal story. Recorded as a UI-pattern/conformance change (same handling as the five prior correct-course
passes).

### Artifact Conflicts

| Artifact | Impact | Action |
|---|---|---|
| **PRD** | N/A — no authored PRD exists; `epics.md` FR/NFR unchanged | none |
| **Architecture** (`_bmad-output/project-docs/architecture.md`) | §4.1 covers components but no page-section layout pattern | ✏️ add **§4.2** (done) |
| **AI rules** (`_bmad-output/project-context.md`) | Blazor Shell rules lack the accordion-section pattern | ✏️ edit (done) |
| **UI/UX** | No authored UX spec to conflict; the change *reinforces* the implemented Fluent token/badge/a11y contracts | positive |
| **Tests** | Enforcement = **design guideline (no guard)** per decision 4 → **no `…FluentConformanceTests` added**. Converted pages will need their existing bUnit DOM-shape assertions migrated (`<h3 id=...>` → accordion-item heading) | ✏️ update per converted page (handoff) |
| **Public API** | No public surface change | none |
| **`docs/` (Gate 2d)** | No new public component; governance/guideline doc only | none |
| **Memory** | Add a `project` memory recording the pattern + carve-outs | ✏️ after impl |
| **CI / IaC / deployment / observability** | none | none |

### Technical Impact
- **FrontComposer repo (this pass):** 1 architecture doc edit (§4.2), 1 project-context edit. **No test
  changes** (no guard).
- **Page conversions (handoff, per-repo):** ~9–13 hand-authored multi-section pages across 4 surfaces
  (2 of them in submodules). Each conversion preserves `data-testid`, localized heading text, and
  `aria-labelledby`/`id` relationships, and migrates the corresponding bUnit DOM-shape assertions. The RC
  `FluentAccordionItem` `Heading`/`Expanded` parameter surface must be confirmed against the pinned
  `5.0.0-rc.3-26138.1` during implementation (prior passes showed RC enums/params differ from docs).

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (selected).** Codify the layout pattern (architecture §4.2 +
project-context) in this repo this pass; route the page conversions to the Developer agent, per-repo
(submodules require their own approval). No epic restructuring; additive to the landed §4.1 component policy.

- **Option 2 — Rollback:** N/A. Nothing to revert — the pattern does not yet exist as a written standard.
- **Option 3 — MVP review:** N/A. MVP is not threatened; no scope reduction.

**Effort:** Medium (conversions span 2 submodules) · **Risk:** Low–Medium (accordion-appropriateness is
judgment-based — mitigated by the precise definition + carve-outs below) · **Timeline:** no epic/milestone
slip; contained.

### Precise definition (decided this pass — keeps "all titled page sections" sane)

> A **titled page section** is a content region introduced by its own section heading (`<h2>`/`<h3>`, a
> `<header>`/`<section>` carrying a heading, or `Heading=` on a Fluent container) that sits alongside one
> or more **sibling** titled regions. When a page or page-like surface (dialog body, detail panel) presents
> **two or more** sibling titled sections, those sections are grouped under a single `FluentAccordion`, one
> `FluentAccordionItem` per section. The first/primary item defaults `Expanded="true"` so primary content
> is never hidden behind a click (NFR6).

**Not a section (never converted):** the page-level `<h1>` title, breadcrumb, toolbar, and navigation
chrome; and any page whose primary content is a **single** region — one `FluentDataGrid`, one command form,
one detail view, or a single titled block. Grid-first / visualization-first pages keep their primary
grid/chart always-visible; only genuinely supplementary sibling sections may be accordion-grouped.

**Why a guideline, not a guard (decision 4).** Unlike "no raw `<button>`" (a mechanically-checkable §4.1
rule), accordion-appropriateness is contextual: single-section and grid-first pages are legitimate
exceptions that a regex guard would false-positive on. The rule is therefore enforced by **code review
against the §4.2 definition**, not by a `…FluentConformanceTests` governance guard.

---

## Section 4 — Detailed Change Proposals

### 4.A — Architecture layout guideline (`architecture.md` new §4.2) — **APPLIED**

Added a first-class §4.2 immediately after the §4.1 carve-out table: the rule, the precise
*titled-page-section* definition, the not-a-section exclusions, the note that generated output already
conforms, and the explicit "guideline, not a guard" framing. (Full text in the file.)

### 4.B — AI-agent rule (`_bmad-output/project-context.md`, "Blazor Shell & Fluxor Rules") — **APPLIED**

Added a "Page sections use FluentAccordion (project-wide guideline)" bullet after the existing
"Fluent-only UI (project-wide)" bullet: 2+ sibling titled sections → one `FluentAccordion`, one item per
section, first `Expanded="true"`; single-region / grid-first pages excluded; generated output conforms;
guideline-by-review (not a guard); cross-references `architecture.md` §4.2.

### 4.C — Canonical conversion (handoff reference) — `Shell/Components/Layout/FcSettingsDialog.razor`

```razor
OLD (3 bare <h3> sections in the dialog body):
<div class="fc-settings-body" data-testid="fc-settings-dialog">
    <h3 id="fc-density-section">@Localizer["DensitySectionLabel"].Value</h3>
    <FluentRadioGroup ...> ... </FluentRadioGroup>
    @if (IsForcedByViewport) { <FluentMessageBar ...>...</FluentMessageBar> }
    <h3 id="fc-theme-section">@Localizer["ThemeSectionLabel"].Value</h3>
    <FcThemeToggle />
    <h3 id="fc-preview-section">@Localizer["DensityPreviewHeading"].Value</h3>
    <FcDensityPreviewPanel ... />
</div>

NEW (one FluentAccordion, one item per section, primary expanded):
<div class="fc-settings-body" data-testid="fc-settings-dialog">
    <FluentAccordion>
        <FluentAccordionItem Heading="@Localizer["DensitySectionLabel"].Value" Expanded="true">
            <FluentRadioGroup aria-labelledby="..."> ... </FluentRadioGroup>
            @if (IsForcedByViewport) { <FluentMessageBar ...>...</FluentMessageBar> }
        </FluentAccordionItem>
        <FluentAccordionItem Heading="@Localizer["ThemeSectionLabel"].Value">
            <FcThemeToggle />
        </FluentAccordionItem>
        <FluentAccordionItem Heading="@Localizer["DensityPreviewHeading"].Value">
            <FcDensityPreviewPanel ... />
        </FluentAccordionItem>
    </FluentAccordion>
</div>
```

**Per-conversion invariants:** preserve `data-testid`, localized heading text, and `aria-labelledby`/`id`
relationships (the `id` moves onto the item heading or its content); migrate bUnit DOM-shape assertions
that query `<h3 id=...>` to the accordion-item heading; confirm the pinned `5.0.0-rc.3-26138.1`
`FluentAccordionItem` `Heading`/`Expanded` parameter surface during implementation.

### 4.D — Conversion inventory (per-repo handoff)

| Surface | Convert (2+ sibling titled sections) | Leave as-is (grid-/form-/single-section) |
|---|---|---|
| **Shell** (this repo) | `FcSettingsDialog` (Density/Theme/Preview), `FcHomeDirectory` (extend existing accordion to the urgent+other split) | most layout/panel components (single content block) |
| **Counter sample** (this repo) | `CounterPage` (4 `<h2>` specimen sections) | `Home` (single section) |
| **Tenants.UI** (submodule — needs approval) | `TenantDetailPage` (Identity/Metadata/Lifecycle/Members/Configuration…), `GlobalAdministratorsPage` (Scope/Status/Grant/Remove/List) | `TenantsWorkspace`, `MyTenantsPage`, `TenantAuditPage`, `UserMembershipLookupPage` (grid-first) |
| **Admin.UI** (submodule — needs approval) | `Health`, `Storage`, `StreamDetail`, `DaprComponents` (+1–2 Dapr variants) | `Streams`, `Commands`, `Events`, `Services`, `Projections`, … (grid-first/single) |

*Counts are an inventory estimate; the exact convert/leave decision for each borderline page is made at
implementation time against the §4.2 definition.*

---

## Section 5 — Implementation Handoff

**Scope classification: Moderate** — two doc edits in this repo (applied) + a project-wide page-conversion
sweep that spans 2 submodules with test-assertion updates; **no** epic/PRD/architecture-pattern replan, MVP
unaffected. (Matches the prior passes.)

**Routing → Developer agent** (`bmad-dev-story` / `bmad-quick-dev`), per repo, with PO awareness for the
submodule backlog entries.

**Suggested sequencing:**
1. **FrontComposer repo (codification — DONE this pass):** `architecture.md` §4.2 + `project-context.md`
   rule applied. No build/test impact (docs only).
2. **FrontComposer repo (in-repo conversions — no external approval needed):** convert `FcSettingsDialog`,
   `CounterPage`, and extend `FcHomeDirectory`; migrate affected bUnit assertions. Build Release clean
   (TWAE); run the default + Governance lanes green
   (`dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`, `DiffEngine_Disabled=true`).
3. **Tenants submodule (needs approval):** convert `TenantDetailPage`, `GlobalAdministratorsPage`; update
   Tenants.UI bUnit assertions; re-run the Tenants.UI suite green. (Use the `Hexalith.Tenants`-scoped
   `bmad-dev-story` skill.)
4. **EventStore Admin.UI submodule (needs approval):** convert `Health`, `Storage`, `StreamDetail`,
   `DaprComponents` (+ borderline Dapr variants per the §4.2 definition); update Admin.UI bUnit assertions
   (`JSInterop.Mode = Loose` where components import JS on first render). Note EventStore's **per-project**
   `dotnet test` rule — opposite of FrontComposer's solution-level rule.
5. **Live visual check** under `Hexalith.FrontComposer.AppHost` (Aspire): confirm converted pages render the
   accordion with the primary item expanded, headings/`data-testid`/a11y preserved, and the grid-first
   pages unchanged.
6. **Memory:** add a `project`-type memory recording the FluentAccordion page-section guideline + the
   convert/leave inventory (cross-link the §4.1 Fluent-only policy memory).

**Constraints (project-context + CLAUDE.md):** no direct commits to `main` (feature branch + PR);
Conventional Commits — the page conversions are `refactor` (layout reshaping), **not** `feat` (don't trigger
a false minor bump); the guideline docs are `docs`. **Submodule edits require explicit approval per repo**
(changes propagate ecosystem-wide) and **must not** touch nested submodules; `.slnx` only;
`TreatWarningsAsErrors`; **do NOT commit** unless explicitly requested.

**Success criteria:**
- ✅ `architecture.md` §4.2 carries the FluentAccordion page-section guideline + precise definition +
  exclusions; `project-context.md` carries the matching AI rule. *(Done this pass.)*
- ⏳ In-repo pages (`FcSettingsDialog`, `CounterPage`, `FcHomeDirectory`) converted, with bUnit assertions
  migrated and the default + Governance lanes green.
- ⏳ Tenants.UI + Admin.UI multi-section pages converted under per-repo approval; each repo's test lane green.
- ⏳ Grid-first / single-section pages confirmed **unchanged** (no over-application).
- ⏳ Live visual check under Aspire.

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ Done (1.1 no formal story — stakeholder UI-pattern directive · 1.2
  new-requirement/standardization, not a defect · 1.3 four-surface `.razor` inventory + the two existing
  framework accordion spots as evidence)
- **§2 Epic Impact:** ✅ N/A (Epics 1–7 Done; no epic add/remove/resequence)
- **§3 Artifact Conflicts:** 3.1 N/A (no PRD) · 3.2 ✅ architecture §4.2 edit (done) · 3.3 ✅ positive (no UX
  spec) · 3.4 ✅ project-context (done) + per-page bUnit updates (handoff) + memory; no public-API/CI/IaC
  impact; **no test guard** (guideline-by-review)
- **§4 Path Forward:** ✅ Option 1 (Direct Adjustment)
- **§5 Proposal Components:** ✅ this document
- **§6 Final Review/Handoff:** ✅ approved (Administrator: "a" = apply doc edits + write proposal) — doc
  codification implemented this pass; page conversions routed as per-repo handoff. 6.4 sprint-status.yaml:
  **N/A** (no epic add/remove/renumber).

---

## Implementation status (2026-06-17)

**Codification — DONE this pass (FrontComposer repo, docs only, uncommitted):**
- `_bmad-output/project-docs/architecture.md` — new **§4.2 "Page-section layout pattern (FluentAccordion)
  — project-wide guideline"** added after the §4.1 carve-out table.
- `_bmad-output/project-context.md` — new **"Page sections use FluentAccordion (project-wide guideline)"**
  bullet under Blazor Shell & Fluxor Rules.

**Page conversions — NOT executed this pass (routed as per-repo handoff).** Project-wide scope reaches two
submodules whose edits require their own approval; the in-repo Shell + Counter conversions are ready to run
on request (decision 4 made this a guideline, so there is no guard to add/keep green).

### Residuals
1. **In-repo conversions** (`FcSettingsDialog`, `CounterPage`, `FcHomeDirectory`) + bUnit assertion
   migration + default/Governance lanes — not run this pass (awaiting go-ahead).
2. **Submodule conversions** (Tenants.UI, Admin.UI) — awaiting per-repo approval.
3. **Live-visual check** under Aspire.
4. **Memory** entry for the guideline.
5. **Commit/PR** — not done (not requested). Conventional Commits: guideline docs = `docs`; page
   conversions = `refactor` (NOT `feat`). Submodule edits propagate ecosystem-wide — branch + PR per repo.
