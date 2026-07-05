---
title: 'FC-DOC — Component-documentation contract'
date: '2026-06-03'
story: '1.5'
status: 'confirmed'   # required-section template, Gate-2d rules, component status map, and inline-summarize cross-link convention are confirmed
owner: 'FrontComposer + Technical Writer'
supersedes: ''
---

# FC-DOC — Component-documentation contract

> **Decision deliverable for Story 1.5 — and the one Epic-1 story whose deliverable is authoring
> *into* the CI-gated `docs/` DocFX site.** Unlike a build-from-scratch story, **the site, the gate,
> the components, and the three upstream contracts all already ship** at baseline `251a0b5`: `docs/`
> is a DocFX (Diataxis) site validated by `eng/validate-docs.ps1` (CI **Gate 2d**,
> `.github/workflows/ci.yml:191-195`) with `docfx 2.78.5` pinned (`.config/dotnet-tools.json`); the
> read-only-MVP shell components (layout, navigation, DataGrid surface, settings) are implemented and
> tested (`component-inventory.md` §A); and FC-LYT (1.2) / FC-A11Y (1.3) / FC-L10N (1.4) each shipped
> a contract under `_bmad-output/contracts/` that *deferred its published cross-link to Story 1.5*.
> What did **not** exist before this story: **per-component reference pages** under
> `docs/reference/components/`. This note therefore **(1) names the FC-DOC documentation contract**
> — the required-sections-per-component template + Gate-2d-conformant front-matter + nested-under-
> `Reference` placement + the cross-link convention — as the single ready-gate every later component
> doc points at, **(2) proves the contract is real** by authoring the validated anchor page
> (`FrontComposerShell`) plus a second conforming page (`Navigation`) and wiring them into
> `docs/toc.yml` without breaking the exactly-four-Diataxis-entries rule, **(3) records the component
> status map** (the AC2 coverage record / tracked-gap-with-owner mechanism), and **(4) confirms the
> prior cross-link convention and settings-page scope decisions**. Adopting FC-DOC introduces **zero behaviour
> change and no `src/` change**: it documents what the shell already does and pins how every future
> component page must be authored. **Enforcement is Gate 2d (`validate-docs.ps1`) itself — there is no
> bUnit "pin test" for this ready-gate the way 1.2–1.4 had; the docs validator *is* the gate.**

## The contract

The FC-DOC contract is **"required sections + Gate-2d-conformant front-matter + nested-under-Reference
placement + the inline-summarize cross-link convention," and naming that *is* the contract.** Every
published shell-component reference page MUST satisfy all of the following, and **passing
`eng/validate-docs.ps1` (Gate 2d) is the enforcement**:

- live under `docs/reference/components/`;
- carry the 7 required front-matter fields (all non-empty) **plus** a unique `uid` and `slug`;
- use `genre: reference` and `audience: adopter`;
- contain the required section set (Overview, Usage, Parameters / slots, Layout (FC-LYT),
  Accessibility (FC-A11Y), Localization (FC-L10N), Related);
- mark every ` ```csharp ` fence `compile` or `no-compile reason="…"` (adopter usage uses ` ```razor `);
- contain no unsafe text (no absolute private paths, tenant-id/secret literals, terminal control
  sequences);
- be wired into the `Reference > Components` TOC group and listed on `reference/components/index.md`.

### Required-sections-per-component template (the heart of AC1)

Every published component reference page MUST contain the following canonical section set:

| Required section | Content |
|---|---|
| **Overview** | One-paragraph "what it is / when to adopt it" — the source-free adoption summary. |
| **Usage** | Minimal adopter markup (a ` ```razor ` fence — see snippet rule) showing the component in place. |
| **Parameters / slots** | The public `[Parameter]` surface and named render-fragment slots (e.g. `FrontComposerShell`'s `HeaderStart/Center/End`, `Navigation`, `Footer`). |
| **Layout (FC-LYT)** | How the component behaves under the full-width vs constrained `<FcPageLayout>` contract; summarize the FC-LYT behavior inline. |
| **Accessibility (FC-A11Y)** | Which FC-A11Y primitives the component honors (skip-link targets, accessible names, `aria-live`, keyboard reachability); link the published `HFC1050`–`HFC1055` diagnostic pages under `docs/diagnostics/`. |
| **Localization (FC-L10N)** | Which strings are shell-owned (`IStringLocalizer<FcShellResources>`) vs host/domain-owned; the `services.Replace` swap seam. |
| **Related** | Cross-links to sibling component pages and relevant reference / concept pages. |

### Front-matter + placement + TOC + snippet rules (Gate-2d alignment, verbatim from `eng/validate-docs.ps1`)

- **Placement.** Published component pages live under `docs/reference/components/` (a new sub-area).
  The validator auto-discovers `reference/**/*.md` (`validate-docs.ps1:58-84`), so a new page is
  picked up and validated as soon as it lands.
- **Required front-matter (all 7, non-empty — `validate-docs.ps1:430-453`):** `title`,
  `description`, `genre` (must be `reference`), `audience` (must be `adopter`), `ownerStory`
  (`1-5-produce-the-fc-doc-component-documentation-contract`), `status` (`published`), `reviewed`
  (`2026-06-03`) — **plus** a stable `uid` **and** `slug` (mirror the existing reference pages:
  `uid: frontcomposer.reference.components.<name>`, `slug: reference/components/<name>/`). `uid`/`slug`
  are canonicalized (lower-cased, separators stripped) then collision-checked across the whole site
  (`:471-482`), so keep them unique.
- **Allowed genres / audiences (`:431-432, 455-461`):** `genre ∈ tutorial|how-to|reference|concept`;
  `audience ∈ adopter|framework-contributor|agent|operator`. Component pages are `reference` / `adopter`.
- **TOC rule — do NOT break (`:499-522`).** `docs/toc.yml` MUST keep **exactly four** top-level
  entries, in order: `Tutorials`, `How-to`, `Reference`, `Concepts`. Add component pages as **nested
  `items` under `Reference`** (a `Components` group, alongside the existing `Diagnostics` item) —
  **never** a 5th top-level entry, and **never** reorder the four. The validator throws if the
  top-level count ≠ 4 or the order differs.
- **Snippet rule (`:360-424`).** Every ` ```csharp ` fence is compiled by the validator against
  Contracts + Shell + Testing (a throwaway `net10.0` project, `TreatWarningsAsErrors=true`) unless
  marked `no-compile reason="…"`; an **unmarked** or **reasonless** fence **fails the gate**. For
  component *usage* examples use a ` ```razor ` fence (not validated as C#) or
  ` ```csharp no-compile reason="illustrative adopter markup" `. Only use a bare `compile` C# fence
  if you genuinely want it built and have verified it compiles.
- **Unsafe-text rule (`:99-105, 484-487`).** No absolute private paths (`/home/...`, `C:\Users\...`),
  no `tenant-id` / `api-key` / `secret` / `password`-like literals, no terminal control sequences —
  the validator rejects the page body and any snippet on match.

### Component status map (the AC2 mechanism)

This table is both the AC2 coverage record and the "tracked gap with a named owner" mechanism for the
read-only-MVP component set. Epic 1 is **not yet closed** (Story 1.6 remains), so a documented
gap-with-owner is acceptable for this story; the anchor page is authored and validated to prove the
contract, and a second page (`Navigation`) is authored to demonstrate repeatability.

| Component area | Anchor component(s) | Doc page | Status | Owner |
|---|---|---|---|---|
| Layout & frame | `FrontComposerShell` (+ `FcPageLayout`, `FcHamburgerToggle`, `FcCollapsedNavRail`) | `docs/reference/components/front-composer-shell.md` | **authored (this story)** — Gate-2d-validated | FrontComposer |
| Navigation | `FrontComposerNavigation` | `docs/reference/components/navigation.md` | **authored (this story)** — Gate-2d-validated | FrontComposer + Tenants author |
| DataGrid surface | `FcColumnFilterCell`, `FcExpandInRowDetail`, `FcColumnPrioritizer`, filter family | `docs/reference/components/datagrid.md` | **authored (Story 2.8 follow-through)** — FC-TBL confirmed-stable and public baseline frozen | FrontComposer (FC-TBL, Story 2.8) |
| Settings | `FcSettingsDialog` (+ `FcThemeToggle`, `FcDensityPreviewPanel`) | `docs/reference/components/settings.md` | **authored (correct-course follow-through, 2026-07-01)** — Story 1.6 settings UX is complete and FC-SETTINGS is behavior-confirmed | FrontComposer (Story 1.6) |

**Rationale for remaining gap.** Authoring reference pages for components whose surface is about to be
finalized would publish documentation that contradicts the governing story. The DataGrid surface is
now confirmed-stable under **FC-TBL (Story 2.8)** and `docs/reference/components/datagrid.md` has
been authored as retrospective follow-through. The Settings surface landed in Story 1.6 and
`docs/reference/components/settings.md` was authored on 2026-07-01, closing the tracked settings
documentation gap.

### Cross-link convention (confirmed 2026-07-05)

`_bmad-output/contracts/*` are **NOT part of the published DocFX site**, so a published page **cannot**
DocFX-xref them (DocFX resolves links within the site only). The FC-DOC contract therefore prescribes:

- a published component page **summarizes the relevant FC-LYT / FC-A11Y / FC-L10N behavior inline**
  (no broken link to `_bmad-output/`);
- it **links only to already-published siblings** — e.g. the `HFC1050`–`HFC1055` diagnostic pages
  under `docs/diagnostics/` for FC-A11Y enforcement, and sibling component pages;
- the **mapping from each published page → its governing `_bmad-output/contracts/*` source** is
  recorded in *this* FC-DOC contract (the traceability ledger below), **not** as a site link.

This convention is confirmed. Published component pages summarize the contract behavior inline and
link only to published siblings; this ledger records the `_bmad-output/contracts/*` mapping that
DocFX cannot publish as an xref.

### Page → governing-contract traceability ledger

This ledger is the canonical mapping the published pages cannot themselves express as DocFX xrefs. It
closes the "FC-DOC linkage (deferred to Story 1.5)" forward-reference that FC-LYT, FC-A11Y, and
FC-L10N each left open.

| Published page | Summarizes / cross-references | Governing `_bmad-output/contracts/*` source |
|---|---|---|
| `docs/reference/components/front-composer-shell.md` | FC-LYT full-width vs constrained measure; FC-A11Y skip links / focus / `aria-live` / keyboard (links `HFC1050`–`HFC1055`); FC-L10N shell-vs-host string ownership + `services.Replace` swap seam | `fc-lyt-page-layout-2026-06-03.md`, `fc-a11y-accessibility-primitives-2026-06-03.md`, `fc-l10n-shell-string-ownership-2026-06-03.md` |
| `docs/reference/components/navigation.md` | FC-A11Y nav-rail accessible name + keyboard reachability (links `HFC1050`/`HFC1051`); FC-L10N nav strings shell-owned | `fc-a11y-accessibility-primitives-2026-06-03.md`, `fc-l10n-shell-string-ownership-2026-06-03.md` |
| `docs/reference/components/datagrid.md` | FC-TBL generated grid surface, public DataGrid components, reserved filter keys, expand-in-row a11y, wide-column prioritizer, and the remaining new-item producer open item | `fc-tbl-table-api-contract-2026-06-04.md` |
| `docs/reference/components/settings.md` | FC-SETTINGS live-no-Apply settings UX, theme/density persistence, density preview, aria-live density announcements, and the single-writer reading | `fc-settings-persistence-2026-06-03.md` |

## Confirmation

**Status: CONFIRMED (2026-07-05).** The required-section template, Gate-2d front-matter / placement /
TOC / snippet rules, component status map, and inline-summarize cross-link convention are confirmed.

The previous settings and DataGrid documentation questions are closed: Story 1.6 completed the
settings UX and `docs/reference/components/settings.md` exists; FC-TBL was confirmed in Story 2.8
and `docs/reference/components/datagrid.md` exists.

The sprint action **"Drive residual FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS wording decisions to
confirmed or dated owned follow-up"** is closed for FC-DOC by this disposition. No dated follow-up is
required.

## Surface confirmed / pinned by Story 1.5

- **No new `src/` surface.** The components, the DocFX site, Gate 2d, and the three upstream contracts
  all pre-exist and are unchanged. This story adds **only** new `docs/` reference pages + this contract.
- **New published pages (Gate-2d-validated):** `docs/reference/components/index.md`,
  `docs/reference/components/front-composer-shell.md`, `docs/reference/components/navigation.md`,
  plus follow-through pages `docs/reference/components/datagrid.md` and
  `docs/reference/components/settings.md`.
- **Edited (additive, non-breaking):** `docs/toc.yml` (a `Components` group nested under `Reference`),
  `docs/reference/index.md` (a Components bullet).
- **Referenced, not modified:** the locked 7-parameter `FrontComposerShell` surface; the three
  `_bmad-output/contracts/fc-{lyt,a11y,l10n}-*.md` contracts; the `docs/diagnostics/HFC105*.md`
  pages; the gated baselines `docs/validation/producer-fingerprints.json` and
  `docs/validation/api-summary-baseline.txt` (byte-identical — this story adds no producer artifact
  and no public API).

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.5: Produce the FC-DOC component documentation contract] (story + ACs; AR4 FC-DOC; FR9/FR10/FR15)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:23] (🔴 FC-DOC ask + owner: FrontComposer + Tenants author)
- [Source: eng/validate-docs.ps1:58-84,99-105,360-432,455-522,594-645] (Gate-2d content discovery, unsafe-text, snippet harness, required front-matter, allowed genres/audiences, exactly-4-TOC, producer/api baselines)
- [Source: .github/workflows/ci.yml:191-195] (Gate 2d: Docs Validation — `./eng/validate-docs.ps1`)
- [Source: .config/dotnet-tools.json] (docfx 2.78.5 pinned — `dotnet tool restore`)
- [Source: docs/reference/cli.md:1-11 + docs/reference/index.md:1-11] (reference-page front-matter shape mirrored)
- [Source: docs/toc.yml] (4 top-level Diataxis entries; `Reference` nests `Diagnostics` — `Components` added the same way)
- [Source: _bmad-output/project-docs/component-inventory.md:7-67] (read-only-MVP component set — source-of-truth for page content)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor (+.razor.cs)] (locked 7-parameter surface; slots HeaderStart/Center/End, Navigation, Footer; skip links → #fc-main-content/#fc-nav; Ctrl+,/Ctrl+K)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] (FC-LYT — structure/tone mirrored; layout behavior summarized; "FC-DOC linkage deferred to 1.5" closed)
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md:134-146] (FC-A11Y — "FC-DOC linkage (deferred to Story 1.5)"; HFC1050–HFC1055 published diagnostic pages linked)
- [Source: _bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md:112-126] (FC-L10N — string-ownership boundary summarized; "FC-DOC linkage deferred to 1.5" closed)
- [Source: docs/diagnostics/HFC1050.md … HFC1055.md] (published a11y diagnostic pages — linked from the Accessibility section, not modified)
- [Source: _bmad-output/implementation-artifacts/1-4-establish-fc-l10n-shell-string-ownership.md] (previous confirm-and-document story; contract shape, escalate-with-owner under YOLO, Dev Agent Record / Change Log house style, 13-failure baseline)
- [Source: _bmad-output/project-context.md#Development Workflow Rules + #Code Quality & Style Rules] ("docs/ is a PUBLISHED DocFX site, CI-gated (Gate 2d)"; Conventional Commits — docs→no release, never feat for docs; .slnx; centralized versions)
