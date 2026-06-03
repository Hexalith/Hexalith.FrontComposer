---
baseline_commit: 251a0b5
---

# Story 1.5: Produce the FC-DOC component documentation contract

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield + docs-discipline reality — read this first.** This is the **fourth
> confirm-and-document ready-gate** in Epic 1 — the FC-LYT (1.2) / FC-A11Y (1.3) / FC-L10N (1.4)
> shape again — **but with one decisive difference that flips a prior rule.** Stories 1.2–1.4 all
> wrote their contract to `_bmad-output/contracts/` and **deferred the published cross-link to
> "Story 1.5 (FC-DOC), which owns the CI-gated `docs/` DocFX site."** **You are Story 1.5.** FC-DOC
> is precisely the contract about **published component documentation under `docs/`**, so this story
> is the **one place in Epic 1 where writing to the CI-gated `docs/` DocFX site is the deliverable,
> not a violation.** The general project-context rule ("`docs/` is the published DocFX site — do NOT
> use it as scratch space") still holds — but authoring **conforming, validated** component
> reference pages *is the FC-DOC contract being satisfied*, not scratch.
>
> Concretely, at baseline `251a0b5`:
> - **The docs site already exists and is Gate-2d-validated.** `docs/` is a DocFX (Diataxis) site
>   gated by `eng/validate-docs.ps1` (CI **Gate 2d**, `.github/workflows/ci.yml:191-195`). `docfx`
>   `2.78.5` is pinned (`.config/dotnet-tools.json`). Today it has reference pages for
>   API / diagnostics / CLI / IDE / generated-output / MCP / pact — **but NO per-component reference
>   pages.** Component docs are genuinely new surface.
> - **Three confirmed contracts are waiting to be cross-linked** from published component docs:
>   `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md`,
>   `…/fc-a11y-accessibility-primitives-2026-06-03.md`,
>   `…/fc-l10n-shell-string-ownership-2026-06-03.md`. Each ends with an "FC-DOC linkage (deferred to
>   Story 1.5)" section naming itself as your input.
> - **The read-only-MVP component set already ships and is documented in the inventory** — see
>   `_bmad-output/project-docs/component-inventory.md` §A (Layout & frame, Navigation, DataGrid
>   surface, Forms/dialogs incl. Settings). You are documenting *what exists*, not building components.
>
> So Story 1.5 is **(1) author the FC-DOC documentation contract** (the *required-sections-per-component*
> template + front-matter rules + placement + TOC rule + cross-link convention + the per-component
> status/owner map) in `_bmad-output/contracts/`; **(2) prove the contract is real by authoring at
> least the anchor conforming published component page** (`FrontComposerShell`) under
> `docs/reference/components/` that **passes `eng/validate-docs.ps1` Gate 2d**, wiring it into
> `docs/toc.yml` *without breaking the exactly-4-Diataxis-entries rule*; **(3) cover the rest of the
> read-only-MVP set (navigation, DataGrid surface, settings) with a conforming page each, OR record
> each as a tracked gap with a named owner** (AC2 explicitly permits the gap path); **(4) confirm or
> escalate with a named owner** per the 1.2–1.4 precedent. Do **NOT** build or modify shell
> components, and do **NOT** touch the producer-fingerprint / api-summary baselines.

## Story

As an adopter developer,
I want each shell-facing component documented to a confirmed FC-DOC contract,
so that I can adopt components without reading their source.

## Acceptance Criteria

**AC1 — A published component doc page satisfies the FC-DOC contract and is validated by `eng/validate-docs.ps1` (Gate 2d) under `docs/`. *(AR4)***
**Given** the FC-DOC documentation contract (the required sections per component, the required front-matter, placement, TOC, and snippet rules),
**When** a shell component is published,
**Then** its doc page satisfies the contract **and** `eng/validate-docs.ps1` (Gate 2d) passes with the new page(s) present — i.e. the page carries the 7 required front-matter fields (`title`, `description`, `genre`, `audience`, `ownerStory`, `status`, `reviewed`) **plus** a stable `uid`/`slug`, uses `genre: reference` / `audience: adopter`, every ` ```csharp ` fence is marked `compile` or `no-compile reason="…"`, contains no unsafe text (no absolute `/home/...` paths, tenant-id/secret patterns, or terminal control sequences), and `docs/toc.yml` still has **exactly four** top-level Diataxis entries (`Tutorials`, `How-to`, `Reference`, `Concepts`).

**AC2 — The read-only-MVP component set each has a conforming doc page, or a tracked gap with a named owner. *(AR4)***
**Given** the read-only-MVP component set (**layout, navigation, DataGrid surface, settings**),
**When** Epic 1 closes,
**Then** each has a conforming doc page **or** a tracked gap with a **named owner** recorded in the FC-DOC contract's component status map. (Epic 1 is not yet closed — Story 1.6 remains — so a documented gap-with-owner is acceptable for this story; the anchor page must nonetheless be authored and validated to prove the contract.)

**AC3 — The FC-DOC contract is confirmed, or escalated with a named owner (mirrors the 1.2–1.4 ready-gate precedent).**
**Given** the FC-DOC documentation contract,
**When** the named owner reviews it,
**Then** it is marked **confirmed** **or** the open question is **escalated with a named owner**, and the published component page(s) cross-reference the confirmed FC-LYT / FC-A11Y / FC-L10N behaviors so the reusable ready-gates are discoverable from the component docs.

## Tasks / Subtasks

- [x] **Task 1 — Author the FC-DOC documentation contract (AC: #1, #2, #3) — the DECISION/DOC deliverable**
  - [x] Create `_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md`. **Mirror the structure/tone of the FC-LYT / FC-A11Y / FC-L10N contracts** (`_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md`, `…/fc-a11y-accessibility-primitives-2026-06-03.md`, `…/fc-l10n-shell-string-ownership-2026-06-03.md`): front-matter (`title`, `date: '2026-06-03'`, `story: '1.5'`, `status`, `owner`, `supersedes`), a "Decision deliverable" intro, the contract body, a **Confirmation** section, a **component status map**, and a **References** section. This contract lives under `_bmad-output/contracts/` (BMAD artifact); the *published* pages it governs live under `docs/`.
  - [x] **Define the required-sections-per-component template (the heart of AC1).** Specify the canonical section set every published component reference page MUST contain. Recommended (carry unless review overrides):
    | Required section | Content |
    |---|---|
    | **Overview** | One-paragraph "what it is / when to adopt it" — the source-free adoption summary. |
    | **Usage** | Minimal adopter markup (` ```razor ` fence — see snippet rule below) showing the component in place. |
    | **Parameters / slots** | The public `[Parameter]` surface and named render-fragment slots (e.g. `FrontComposerShell`'s `HeaderStart/Center/End`, `Navigation`, `Footer`). |
    | **Layout (FC-LYT)** | How the component behaves under the full-width vs constrained `<PageLayout>` contract; cross-reference the FC-LYT behavior. |
    | **Accessibility (FC-A11Y)** | Which FC-A11Y primitives the component honors (skip-link targets, accessible names, `aria-live`, keyboard reachability); link the published HFC1050–HFC1055 diagnostic pages (`docs/diagnostics/HFC105*.md`). |
    | **Localization (FC-L10N)** | Which strings are shell-owned (`IStringLocalizer<FcShellResources>`) vs host/domain-owned; the `services.Replace` swap seam. |
    | **Related** | Cross-links to sibling component pages and relevant reference/concept pages. |
  - [x] **Pin the front-matter + placement + TOC + snippet rules (the Gate-2d alignment — verbatim from `eng/validate-docs.ps1`):**
    - **Placement:** published component pages live under `docs/reference/components/` (a new sub-area; `genre: reference`, `audience: adopter`). The validator auto-discovers `reference/**/*.md`.
    - **Required front-matter (all 7, non-empty):** `title`, `description`, `genre` (must be `reference`), `audience` (must be `adopter`), `ownerStory` (`1-5-produce-the-fc-doc-component-documentation-contract`), `status` (`published`), `reviewed` (`2026-06-03`) — **plus** a stable `uid` **or** `slug` (use both, like the existing reference pages: `uid: frontcomposer.reference.components.<name>`, `slug: reference/components/<name>/`). uid/slug are canonicalized then collision-checked, so keep them unique.
    - **TOC rule (do NOT break):** `docs/toc.yml` MUST keep **exactly four** top-level entries in order — `Tutorials`, `How-to`, `Reference`, `Concepts`. Add the new component pages as **nested `items` under `Reference`** (e.g. a `Components` group), **never** a 5th top-level entry. (The validator throws if the top-level count ≠ 4 or order differs.)
    - **Snippet rule:** every ` ```csharp ` fence is compiled by the validator against Contracts+Shell+Testing unless marked `no-compile reason="…"`; an unmarked or reasonless fence **fails the gate**. For component *usage* examples use a ` ```razor ` fence (not validated as C#) or ` ```csharp no-compile reason="illustrative adopter markup" `. Only use a bare `compile` C# fence if you genuinely want it built (and have verified it compiles).
    - **Unsafe-text rule:** no absolute private paths (`/home/...`, `C:\Users\...`), no `tenant-id`/`api-key`/`secret`/`password`-like literals, no terminal control sequences — the validator rejects the page body and any snippet on match.
  - [x] **Define the component status map (AC2 mechanism).** A table covering the read-only-MVP set with each component's doc status + owner — this is both the AC2 coverage record and the "tracked gap with a named owner" mechanism:
    | Component area | Anchor component(s) | Doc page | Status | Owner |
    |---|---|---|---|---|
    | Layout & frame | `FrontComposerShell` (+ `FcPageLayout`, nav-rail/hamburger) | `docs/reference/components/front-composer-shell.md` | **authored (this story)** | FrontComposer |
    | Navigation | `FrontComposerNavigation` | `docs/reference/components/navigation.md` *(or tracked gap)* | authored **or** gap-with-owner | FrontComposer + Tenants author |
    | DataGrid surface | `FcColumnFilterCell`, `FcExpandInRowDetail`, `FcColumnPrioritizer`, … | `docs/reference/components/datagrid.md` *(or tracked gap)* | authored **or** gap-with-owner | FrontComposer (confirmed-stable in Epic 2 / FC-TBL, Story 2.8) |
    | Settings | `FcSettingsDialog` (+ `FcThemeToggle`, `FcDensityPreviewPanel`) | `docs/reference/components/settings.md` *(or tracked gap)* | authored **or** gap-with-owner | FrontComposer (Story 1.6 finalizes settings UX) |
  - [x] **Confirmation section (AC3):** mark `confirmed` OR escalate with a **named owner**. Owner per the readiness request (`frontcomposer-readiness-request-2026-06-03.md:23`, 🔴 FC-DOC row): **FrontComposer + Tenants author**. **YOLO mode:** if no live confirmation is available, write it as **escalated with owner = "FrontComposer + Tenants author (pending)"** and proceed — AC3 permits escalate-with-owner (precedent: FC-LYT/FC-A11Y/FC-L10N all shipped `status: escalated`). List the genuinely open items (e.g. the cross-link mechanism nuance below; whether DataGrid/settings pages are authored now or deferred to Epic 2 / Story 1.6 with an owner).
  - [x] **Record the cross-link mechanism nuance (a genuine open item to surface, not silently resolve):** `_bmad-output/contracts/*` are **NOT part of the published DocFX site**, so a published page **cannot** DocFX-xref them. The FC-DOC contract must state how component pages surface the FC-LYT/FC-A11Y/FC-L10N behaviors: **recommended** — the published page **summarizes the relevant contract behavior inline** and links to **already-published** related pages (e.g. the HFC1050–HFC1055 diagnostic pages under `docs/diagnostics/` for FC-A11Y enforcement); the **mapping** from each published page → its governing `_bmad-output/contracts/*` source is recorded in *this* FC-DOC contract (the traceability ledger), not as a broken site link. Flag this as the one cross-link-convention item for the owner to confirm.

- [x] **Task 2 — Author the anchor published component page + wire the TOC (AC: #1, #3) — the CI-touching deliverable that PROVES the contract**
  - [x] Create `docs/reference/components/index.md` (a `Components` landing/list page) **and** `docs/reference/components/front-composer-shell.md` (the anchor `FrontComposerShell` page). Both carry the full required front-matter (see Task 1 rules); `genre: reference`, `audience: adopter`, `ownerStory: 1-5-produce-the-fc-doc-component-documentation-contract`, `status: published`, `reviewed: 2026-06-03`, unique `uid` + `slug`. **Mirror the front-matter shape of an existing reference page** (`docs/reference/cli.md`, `docs/reference/index.md`) exactly.
  - [x] The `front-composer-shell.md` page MUST contain every required section from the FC-DOC template (Overview, Usage, Parameters/slots, Layout, Accessibility, Localization, Related). Source the content from the real component — `FrontComposerShell.razor`/`.razor.cs` (7-parameter locked surface; slots `HeaderStart/Center/End`, `Navigation`, `Footer`; skip links → `#fc-main-content`/`#fc-nav`; `Ctrl+,`/`Ctrl+K`) and the three contracts. **Usage example as ` ```razor `** (`<FrontComposerShell>@Body</FrontComposerShell>` + the 3-call DI bootstrap) so the snippet compiler is not invoked; if you write any ` ```csharp `, mark it `no-compile reason="…"` unless you've verified it compiles against Contracts+Shell+Testing.
  - [x] **Wire `docs/toc.yml`:** add a `Components` group as a **nested `items` entry under the existing `Reference` node** (alongside `Diagnostics`), pointing at `reference/components/index.md`. **Do NOT add a 5th top-level entry** and **do NOT reorder** the four top-level Diataxis entries — the validator enforces exactly `Tutorials, How-to, Reference, Concepts` in that order. Also add the new pages to `docs/reference/index.md`'s link list (mirror its existing bullet style).
  - [x] **Cross-reference the confirmed contracts (AC3):** the Accessibility section links the published `docs/diagnostics/HFC1050.md … HFC1055.md` pages; the Layout/Localization sections summarize the FC-LYT / FC-L10N behaviors inline (do not link `_bmad-output/`). This closes the "FC-DOC linkage deferred to Story 1.5" forward-references the three prior contracts left open.

- [x] **Task 3 — Cover the rest of the read-only-MVP set: author conforming pages OR record tracked gaps with owners (AC: #2)**
  - [x] For **navigation** (`FrontComposerNavigation`), **DataGrid surface** (the `FcColumn*`/`FcExpand*`/filter family), and **settings** (`FcSettingsDialog` + `FcThemeToggle` + `FcDensityPreviewPanel`): **either** author a conforming `docs/reference/components/<area>.md` page (same template + front-matter + TOC wiring as Task 2) **or** record it as a **tracked gap with a named owner** in the FC-DOC contract's component status map. **Recommended for YOLO:** author **navigation** as a second conforming page (it is read-only-MVP-central and stable), and record **DataGrid surface** (confirm-stable under FC-TBL / Story 2.8) and **settings** (finalized by Story 1.6) as **tracked gaps with named owners** — this keeps the CI surface bounded while satisfying AC2's explicit "or a tracked gap with an owner" and the "when Epic 1 closes" timing. If authoring all four is low-friction and Gate 2d stays green, prefer full coverage.
  - [x] Whatever set you author, **every** authored page must pass the same Gate-2d checks (front-matter, snippet marking, unsafe-text, unique uid/slug) and be wired into the `Reference > Components` TOC group + the `reference/components/index.md` list.

### Review Follow-ups (AI)

- [ ] [AI-Review][Low] **Stale XML-doc on `HeaderEnd`** — the C# summary says *"Defaults to empty"*, but the razor `else`-branch renders `FcPaletteTriggerButton` + `FcSettingsButton`; the published doc page (`front-composer-shell.md` Parameters/slots table) is the correct version. Pre-existing `src/` defect surfaced by FC-DOC authoring; **out of scope for this docs-only story** (must-not-break: no `src/` changes / api-summary baseline) — file against the shell-component owner to correct the XML comment. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:117-119`]

- [x] **Task 4 — Run the docs validator (the enforcement gate) + build clean (DoD)**
  - [x] **Run the FC-DOC ready-gate enforcement — `eng/validate-docs.ps1` — and record the result.** Restore tools first: `dotnet tool restore` (pins `docfx 2.78.5`). Then run `pwsh ./eng/validate-docs.ps1` from the repo root. This runs front-matter/TOC/snippet/unsafe-text checks **plus** `docfx metadata` + `docfx build` + compile-snippet builds + producer-fingerprint + api-summary-baseline checks. **The new pages must NOT introduce any blocking failure.** If `pwsh` or `docfx` is unavailable in the environment, run the **structural subset** with `pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` (still enforces front-matter, uid/slug uniqueness, TOC, unsafe-text, marker rules), **and** record in the Dev Agent Record exactly which checks ran vs were skipped and why. The evidence manifest lands at `artifacts/docs/validation-manifest.json` — note the `blockingFailures: []` result.
  - [x] **Do NOT regenerate or perturb the gated baselines:** `docs/validation/producer-fingerprints.json` (5 producer files) and `docs/validation/api-summary-baseline.txt`. This story adds *new* reference pages only; it must not change any producer artifact or the public-API surface, so both baselines stay byte-identical. If the validator complains about either, you've touched something you shouldn't have — stop and re-check.
  - [x] **Build clean:** `dotnet build -c Release Hexalith.FrontComposer.slnx` — **0 warnings** (TWAE). A docs-only story adds **no** `src/` code, so there is no new public API / XML-doc / `PublicAPI.Shipped.txt` obligation; still confirm the build is clean.
  - [x] **Confirm the test baseline is unchanged (not a regression hunt):** this story changes **no** code, so the documented Story 1.1–1.4 full-lane failure baseline (**13** failures: 8 Shell + 3 SourceTools + 2 Cli at `251a0b5`) must be **identical**. A quick `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` confirms it; if a failure looks new, it cannot be from this docs-only change — stash and compare to `251a0b5` (the documented 1.1–1.4 method).

## Dev Notes

### What already exists vs. what's new

| Concern | State today (baseline `251a0b5`) | This story |
|---|---|---|
| DocFX (Diataxis) site + Gate 2d (`eng/validate-docs.ps1`) | **Exists** (`docs/`, `.github/workflows/ci.yml:191-195`, `docfx 2.78.5` pinned) | **Author into it** (conforming new reference pages) — do NOT restructure |
| Reference pages (API, diagnostics, CLI, IDE, generated-output, MCP, pact) | **Exist** (`docs/reference/*`) | **Add a new `components/` sub-area** beside them |
| Per-component reference pages | **Do NOT exist** | **Author** the anchor (`FrontComposerShell`) + ≥1 more or tracked gaps |
| FC-LYT / FC-A11Y / FC-L10N contracts (with "FC-DOC linkage deferred to 1.5") | **Exist** (`_bmad-output/contracts/`) | **Cross-reference + record the mapping** in the FC-DOC contract |
| Shell read-only-MVP components (layout, nav, DataGrid, settings) | **Exist + tested** (`component-inventory.md` §A) | **Document** them — do NOT build/modify |
| FC-DOC documentation contract | **Does NOT exist** (only FC-LYT/FC-A11Y/FC-L10N in `_bmad-output/contracts/`) | **Author it** |
| HFC1050–HFC1055 diagnostic pages | **Exist + published** (`docs/diagnostics/HFC105*.md`) | **Link** from the Accessibility section — do NOT modify |

### Exact anchors (read these before authoring)

- **The validator (the gate you must satisfy) — read it fully** — `eng/validate-docs.ps1`. Critical clauses:
  - `requiredFrontMatter = title, description, genre, audience, ownerStory, status, reviewed` + a `uid`/`slug` (`:430-453`).
  - `allowedGenres = tutorial|how-to|reference|concept`; `allowedAudiences = adopter|framework-contributor|agent|operator` (`:431-432, 455-461`).
  - **TOC: exactly 4 top-level entries** `Tutorials, How-to, Reference, Concepts` in order (`:499-522`) — add components as nested `items` under `Reference`.
  - **Snippet harness:** ` ```csharp ` fences are built unless `no-compile reason="…"`; unmarked → failure (`:360-424`). ` ```razor ` and other languages are not C#-compiled.
  - **Unsafe text:** `/home/…`, tenant-id/secret patterns, terminal control sequences rejected (`:99-105, 484-487`).
  - **Content discovery:** `index.md, tutorials/**, how-to/**, reference/**, concepts/**, diagnostics/HFC*, migrations/**` (`:58-84`) — `reference/components/*.md` is auto-picked-up.
  - **Baselines you must NOT perturb:** producer-fingerprints for 5 files (`:594-645`) + api-summary-baseline (`:206-231`).
- **Front-matter shape to mirror** — `docs/reference/cli.md:1-11`, `docs/reference/index.md:1-11` (both `genre: reference, audience: adopter, status: published, reviewed: 2026-05-10, uid: frontcomposer.reference.*, slug: reference/*/`).
- **TOC to edit** — `docs/toc.yml` (4 top-level entries; `Reference` already has a nested `Diagnostics` item — add `Components` the same way).
- **Reference index to extend** — `docs/reference/index.md:13-21` (bullet list of reference pages).
- **The three contracts to cross-reference + their "deferred to 1.5" sections** — `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md`, `…/fc-a11y-accessibility-primitives-2026-06-03.md` (see its "FC-DOC linkage (deferred to Story 1.5)" at the tail), `…/fc-l10n-shell-string-ownership-2026-06-03.md`.
- **Component facts (source-of-truth for the page content)** — `_bmad-output/project-docs/component-inventory.md` §A (Layout & frame / Navigation / DataGrid surface / Forms-dialogs incl. Settings); `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`(+`.razor.cs`) for the locked 7-parameter surface, slots, skip links, shortcuts; `FrontComposerNavigation.razor`(+`.cs`); `FcSettingsDialog.razor`(+`.cs`); the `FcColumn*`/`FcExpand*`/filter family for the DataGrid surface.
- **Published diagnostic pages to link (FC-A11Y enforcement)** — `docs/diagnostics/HFC1050.md … HFC1055.md` (reference only — do not modify).
- **Prior-story precedent (shape + tone to mirror)** — `_bmad-output/implementation-artifacts/1-4-establish-fc-l10n-shell-string-ownership.md` (most recent confirm-and-document story: contract structure, escalate-with-owner under YOLO, the Dev Agent Record / Change Log house style, the pre-existing-failure baseline discipline).

### The FC-DOC decision (encode in the contract doc)

The story's first job is a **decision/declaration**, not new components. Recommended, carry unless reviewers override:

- **The contract is "required sections + Gate-2d-conformant front-matter + nested-under-Reference placement," and naming that *is* the contract.** Every published shell-component reference page MUST: live under `docs/reference/components/`; carry the 7 required front-matter fields + a unique uid/slug; use `genre: reference, audience: adopter`; contain the required section set (Overview, Usage, Parameters/slots, Layout, Accessibility, Localization, Related); mark every `csharp` fence `compile`/`no-compile reason`; contain no unsafe text; and be wired into the `Reference > Components` TOC group. **Passing `eng/validate-docs.ps1` (Gate 2d) is the enforcement** — there is no bUnit "pin test" for this ready-gate the way 1.2–1.4 had; the docs validator *is* the gate.
- **The cross-link convention is "summarize inline + link published siblings; record the `_bmad-output` mapping in the FC-DOC contract."** Published pages can't xref `_bmad-output/contracts/*` (not in the site). So the FC-LYT/FC-A11Y/FC-L10N behaviors are summarized inline and linked to **published** related pages (HFC105* diagnostics for a11y); the FC-DOC contract holds the page→contract traceability ledger.
- **Confirm-or-escalate, don't redesign.** The components, the site, the gate, and the three contracts all exist. This story *documents and wires*; the only genuinely open items are the cross-link convention and the author-now-vs-defer scope for DataGrid/settings — escalate those with the named owner.

### Must-not-break (regression surface)

A ready-gate story must leave the system working end-to-end. Preserve:

- **Gate 2d stays green.** The new pages must satisfy `eng/validate-docs.ps1` in full; a malformed front-matter field, an unmarked `csharp` fence, a duplicate uid/slug, an unsafe path, or a broken TOC **fails CI Gate 2d** and blocks the merge.
- **`docs/toc.yml`'s exactly-4-top-level-Diataxis-entries invariant** (`Tutorials, How-to, Reference, Concepts`, in order). Add `Components` as a nested item under `Reference` only.
- **The gated baselines** — `docs/validation/producer-fingerprints.json` (5 producer artifacts) and `docs/validation/api-summary-baseline.txt` stay byte-identical (this story adds no producer artifact and no public API).
- **No `src/` changes** — components, services, options, analyzers, and the locked 7-parameter `FrontComposerShell` surface are read-only here; documenting them must not edit them. (If documentation work surfaces a genuine component defect, file it — do **not** fix it in this docs story.)
- **The three prior contracts** (`_bmad-output/contracts/fc-{lyt,a11y,l10n}-*.md`) are referenced, not modified.
- **`docs/diagnostics/HFC105*.md`** are linked, not edited.

### Previous story intelligence (Stories 1.2 + 1.3 + 1.4 — all `done`)

- **1.2/1.3/1.4 are the template for 1.5's *contract* half:** author a contract in `_bmad-output/contracts/` mirroring the FC-LYT/FC-A11Y/FC-L10N shape, escalate-with-owner under YOLO, surface (don't silently resolve) the genuinely open items. **The new half unique to 1.5:** it also authors the *published* `docs/` pages those stories deferred — and the enforcement is **Gate 2d (`validate-docs.ps1`)**, not a bUnit test.
- **Each prior contract explicitly named Story 1.5 as the owner of the published cross-link** ("FC-DOC linkage (deferred to Story 1.5)"). This story closes those three forward-references.
- **Docs discipline, now inverted for this one story:** 1.1–1.4 all kept *out* of `docs/` (scratch → `_bmad-output/`). FC-DOC is the deliberate exception: authoring **conforming, validated** component pages under `docs/` *is* the FC-DOC contract. Scratch/working notes still go to `_bmad-output/`; only the finished, validated pages go to `docs/`.
- **Pre-existing-failure baseline:** 1.1–1.4 all recorded **13** full-lane failures (8 Shell: `PendingStatusReopenGovernanceTests`×4, `NavigationEffectsLastActiveRouteTests`×1, Generated snapshot×3; 3 SourceTools; 2 Cli) reproduced identically across `f40dece`/`68034f1`/`df37313`/`251a0b5` — **NOT regressions.** A docs-only story cannot change this; don't chase it.
- **YOLO escalate-with-owner is acceptable** — FC-LYT/FC-A11Y/FC-L10N all shipped `status: escalated` with a pending owner and passed review. FC-DOC can do the same with the readiness-request owner (**FrontComposer + Tenants author**).

### Git intelligence

- HEAD `251a0b5` = Story 1.4 (`feat(story-1.4): Establish FC-L10N shell-string ownership`). The recent commits (`0db0fb0` spike, `f40dece` bootstrap, `68034f1` FC-LYT, `df37313` FC-A11Y, `251a0b5` FC-L10N) are all "confirm-and-document + minimal additive" Epic-1 ready-gate stories — the shape 1.5 continues. None added a per-component `docs/` page, so the component-docs surface is genuinely new.
- Working tree has one unrelated modified file (`_bmad-output/story-automator/orchestration-*.md`); leave it alone.
- Branch `feat/<desc>` (continue on `feat/story-1-2-fc-lyt-page-layout` or branch `feat/story-1-5-fc-doc` — **never** commit to `main`). **Conventional Commit:** this story adds documentation + a BMAD contract, **no shipped code behaviour** → **`docs(story-1.5): …`** (project-context: "`docs`/… → no release"; **never `feat` for a docs-only story** — it would trigger a false minor NuGet publish). *Observed nuance:* the repo's prior Epic-1 commits used `feat(story-1.x): …` even for doc/test-leaning stories; the project-context rule still says docs→`docs`. Recommend `docs(story-1.5):`. Run `/bmad-code-review` before flipping to done.

### Latest tech / DocFX + validator notes

- **DocFX `2.78.5`** (pinned, `.config/dotnet-tools.json`) via `dotnet tool restore` → `dotnet docfx docs/docfx.json`. The validator calls `docfx metadata` then `docfx build`; a new page must be valid Markdown reachable from the TOC, with a unique uid/slug. DocFX xref resolves links within the site only — that's *why* `_bmad-output/contracts/*` can't be linked from a published page.
- **PowerShell:** the gate is `pwsh ./eng/validate-docs.ps1` (PowerShell Core, cross-platform). On this WSL/Linux box, `pwsh` may or may not be installed; if absent, install or run the structural subset (`-SkipDocFx -SkipSnippetBuild`) and record what was skipped. The full gate is what CI runs — the goal is *that* passes.
- **Snippet harness reality:** `compile` C# fences are built as a throwaway net10.0 project referencing Contracts+Shell+Testing with `TreatWarningsAsErrors=true` — so a `compile` example must be warning-clean against the real assemblies. For adopter *usage* prefer ` ```razor ` (not C#-compiled) to avoid that burden; reserve `compile` for genuinely verified C#.
- **FluentUI v5 RC** (`5.0.0-rc.3-26138.1`, ADR-003): documentation references FluentUI component names (`FluentLayout`, `FluentNav*`, `FluentDialogBody`, `FluentDataGrid`) as prose — **no new FluentUI API surface**, so the RC pin is untouched.

### Project-context rules that bite here

- **`docs/` is the published, CI-gated DocFX site — and FC-DOC is the *one* story whose deliverable is authoring into it.** Conforming, validated pages only; working notes still go to `_bmad-output/`.
- **Generated/BMAD contracts → `_bmad-output/contracts/`** — the FC-DOC *contract* doc lives there (like FC-LYT/FC-A11Y/FC-L10N); only the *published component pages* go to `docs/`.
- **No copyright/license headers** (0 of 483 files) — N/A to markdown pages, but don't introduce any.
- **`.slnx` only**; **centralized package versions** (no `Version=` edits); **no new analyzer/tool/framework** — this story adds none.
- **Conventional Commits / semantic-release** — `docs(story-1.5):` (no release); **never `feat`** for a docs-only change; **never commit to `main`**.
- **Skill-corpus docs** (`docs/skills/frontcomposer/**`) are a *separate* MCP-embedded contract (front-matter + `agent-reference` section) — **out of scope** here; FC-DOC component pages are `genre: reference` adopter docs, not skill-corpus entries. Don't conflate them.
- **ULIDs / Fluxor / MCP / source-generator rules** — not in play for this docs+contract story; no command/projection/MCP/generator surface is touched.

### Project Structure Notes

- New contract doc: `_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md` (beside FC-LYT/FC-A11Y/FC-L10N).
- New published pages: `docs/reference/components/index.md` + `docs/reference/components/front-composer-shell.md` (anchor) + optionally `…/navigation.md`, `…/datagrid.md`, `…/settings.md` (or tracked gaps).
- Edited: `docs/toc.yml` (add `Components` nested under `Reference`), `docs/reference/index.md` (add the component-pages bullet(s)).
- **No `src/` changes expected** — components, services, options, and the locked 7-parameter `FrontComposerShell` surface all pre-exist; this story documents them. No structural variance from the dependency-down-to-`Contracts` rule (no new code types). The contract-in-`_bmad-output` + published-pages-in-`docs/reference/components` split matches the FC-LYT/FC-A11Y/FC-L10N precedent (contract in `_bmad-output`) extended with the published-pages half that FC-DOC uniquely owns.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.5: Produce the FC-DOC component documentation contract] (story + 2 ACs)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Shell Foundation & Bootstrap] (AR4 FC-DOC; ready-gate framing; FR9/FR10/FR15)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:23] (🔴 FC-DOC ask + owner: FrontComposer + Tenants author — "component docs … every story's ready-gate")
- [Source: eng/validate-docs.ps1:58-84,99-105,360-432,455-522,594-645] (Gate-2d content discovery, unsafe-text, snippet harness, required front-matter, allowed genres/audiences, exactly-4-TOC, producer/api baselines — the rules every new page must satisfy)
- [Source: .github/workflows/ci.yml:191-195] (Gate 2d: Docs Validation — `./eng/validate-docs.ps1`)
- [Source: .config/dotnet-tools.json] (docfx 2.78.5 pinned — `dotnet tool restore`)
- [Source: docs/reference/cli.md:1-11 + docs/reference/index.md:1-11] (reference-page front-matter shape to mirror)
- [Source: docs/toc.yml] (4 top-level Diataxis entries; `Reference` already nests `Diagnostics` — add `Components` the same way)
- [Source: _bmad-output/project-docs/component-inventory.md:7-67] (read-only-MVP component set: Layout & frame, Navigation, DataGrid surface, Forms/dialogs incl. Settings — source-of-truth for page content)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor (+.razor.cs)] (locked 7-parameter surface; slots HeaderStart/Center/End, Navigation, Footer; skip links → #fc-main-content/#fc-nav; Ctrl+,/Ctrl+K)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] (FC-LYT contract — structure/tone to mirror; layout behavior to summarize; "FC-DOC linkage deferred to 1.5")
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md:134-146] (FC-A11Y — "FC-DOC linkage (deferred to Story 1.5)"; HFC1050–HFC1055 published diagnostic pages to link)
- [Source: _bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md] (FC-L10N — string-ownership boundary to summarize; "FC-DOC linkage deferred to 1.5")
- [Source: docs/diagnostics/HFC1050.md … HFC1055.md] (published a11y diagnostic pages — link from the Accessibility section, do not modify)
- [Source: _bmad-output/implementation-artifacts/1-4-establish-fc-l10n-shell-string-ownership.md] (previous story; confirm-and-document shape, escalate-with-owner under YOLO, Dev Agent Record/Change Log house style, the 13-failure pre-existing baseline)
- [Source: _bmad-output/project-context.md#Development Workflow Rules + #Code Quality & Style Rules] ("docs/ is a PUBLISHED DocFX site, CI-gated (Gate 2d)"; Conventional Commits — docs→no release, never feat for docs; .slnx; centralized versions)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (Claude Opus 4.8, 1M context) — BMAD dev-story workflow.

### Debug Log References

**Gate-2d validator (`eng/validate-docs.ps1`) — what ran vs. skipped, and why.**

- `dotnet tool restore` succeeded (docfx `2.78.5` pinned per `.config/dotnet-tools.json`). `pwsh`
  7.6.2 present.
- **Full gate (`pwsh ./eng/validate-docs.ps1`)** — failed at the `dotnet docfx metadata docs/docfx.json`
  step with **pre-existing, environmental** C# build errors in `src/` files I did not touch
  (`FcFieldSlotHost.cs`, `FcProjectionTemplateHost.cs`, `FcProjectionViewOverrideHost.cs` → CS0234;
  `FcHomeDirectory.razor.cs`, `FcProjectionSubtitle.razor.cs` → CS0115), triggered by a Razor
  compiler analyzer version mismatch inside docfx's bundled Roslyn
  (`FailedToLoadAnalyzer: Microsoft.CodeAnalysis.Razor.Compiler … ReferencesNewerCompiler,
  ReferencedCompilerVersion: 5.5.0.0`). This is docfx's API-metadata build of the C# projects — it is
  **independent of the new Markdown reference pages** (a docs-only change adds zero C#/`src/` files).
- **Structural subset (`pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`)** — ran and
  enforced: required 7-field front-matter, `uid`/`slug` presence + cross-site canonicalized
  uniqueness, allowed genre/audience, **exactly-4-top-level-TOC** order, unsafe-text on bodies,
  marker rules, producer-fingerprint baselines, and api-summary baseline (skipped under `-SkipDocFx`).
- **Result: my three new pages + the `toc.yml` / `reference/index.md` edits produced ZERO failures.**
  The evidence manifest (`artifacts/docs/validation-manifest.json`) records exactly **3** blocking
  failures, all of which are **pre-existing producer-fingerprint staleness on Epic-9 artifacts I never
  touched** and which are **byte-identical to baseline `251a0b5`** (verified via `git diff 251a0b5`):
  `docs/diagnostics/samples/registry-drift-report.json` (9-1), `docs/migrations/9.1-to-9.2.md` (9-2),
  `docs/ide-parity-matrix.md` (9-3). Per Task 4, the gated baselines were **NOT** perturbed
  (`producer-fingerprints.json` and `api-summary-baseline.txt` are byte-identical to baseline). These
  3 are the docs-gate analogue of the documented 13-failure test baseline — not regressions from this
  story, and not mine to fix.
- **Release build** (`dotnet build -c Release Hexalith.FrontComposer.slnx`) — **succeeded, 0 errors.**
  The single warning is a transient `MSB3026` parallel-build file-lock on an XML-doc copy ("used by
  another process"), an I/O race, not a CS-prefixed/TWAE compiler warning.
- **Test baseline** (`DiffEngine_Disabled=true dotnet test … --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`)
  — **exactly 13 failures, identical to the documented `251a0b5` baseline**: 8 Shell
  (`PendingStatusReopenGovernanceTests`×4, `NavigationEffectsLastActiveRouteTests`×1, Generated
  snapshot ×3), 3 SourceTools, 2 Cli. Zero new failures — the docs-only change is regression-free.

### Completion Notes List

- **AC1 — satisfied.** Authored the FC-DOC contract (`_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md`)
  pinning the required-sections-per-component template + Gate-2d front-matter/placement/TOC/snippet
  rules, and **proved it** with the anchor published page `docs/reference/components/front-composer-shell.md`
  (full 7-field front-matter + unique `uid`/`slug`, `genre: reference`/`audience: adopter`, all
  required sections, `razor` usage fence + a `csharp no-compile reason="…"` bootstrap fence, no unsafe
  text). `docs/toc.yml` keeps **exactly four** top-level Diataxis entries — `Components` was added as a
  nested `items` group under `Reference` (beside `Diagnostics`). The structural Gate-2d subset reports
  **zero** failures for any new page.
- **AC2 — satisfied.** Read-only-MVP coverage recorded in the contract's component status map: **layout**
  (`FrontComposerShell`) and **navigation** (`FrontComposerNavigation`) authored as conforming pages;
  **DataGrid surface** and **settings** recorded as **tracked gaps with named owners** (FC-TBL / Story
  2.8 and Story 1.6 respectively) — permitted because Epic 1 is not yet closed.
- **AC3 — escalated with a named owner.** Contract `status: escalated`, owner **"FrontComposer + Tenants
  author (pending)"** (mirrors FC-LYT/FC-A11Y/FC-L10N precedent). Open items surfaced, not silently
  resolved: (1) the cross-link convention (published pages can't xref `_bmad-output/contracts/*`, so
  summarize-inline + link published siblings + record the mapping in the FC-DOC traceability ledger),
  (2) author-now-vs-defer scope for DataGrid/settings. The published pages cross-reference the FC-LYT /
  FC-A11Y / FC-L10N behaviors inline and link the published `HFC1050`–`HFC1055` diagnostic pages,
  closing the "FC-DOC linkage deferred to Story 1.5" forward-references the three prior contracts left.
- **Scope discipline honored.** No `src/` change (zero components/services modified); the three prior
  contracts, the `HFC105*` diagnostic pages, and both gated baselines are referenced, not modified.

### File List

**New:**
- `_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md` (the FC-DOC contract)
- `docs/reference/components/index.md` (Components landing/list page)
- `docs/reference/components/front-composer-shell.md` (anchor `FrontComposerShell` page)
- `docs/reference/components/navigation.md` (`FrontComposerNavigation` page)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcDocComponentDocumentationContractTests.cs` (FC-DOC governance pin test — 15 cases — added by the `bmad-qa-generate-e2e-tests` step; **`tests/`, not `src/`** — no shipped-code/public-API change)
- `_bmad-output/implementation-artifacts/tests/1-5-fc-doc-test-summary.md` (QA-automation test summary)

**Modified:**
- `docs/toc.yml` (added `Components` nested under `Reference`; four top-level entries preserved)
- `docs/reference/index.md` (added the `Components` bullet)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (1-5 → in-progress → review)
- `_bmad-output/implementation-artifacts/1-5-produce-the-fc-doc-component-documentation-contract.md` (this story: checkboxes, Dev Agent Record, Change Log, Status)

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-03 · **Workflow:** `bmad-story-automator-review` (adversarial, auto-fix) · **Outcome: APPROVE — Status → done.**

**Scope reviewed:** the FC-DOC contract, the three published pages (`index.md`, `front-composer-shell.md`, `navigation.md`), the `docs/toc.yml` + `docs/reference/index.md` edits, and the new governance pin test. `_bmad-output/` artifacts excluded from code review per skill policy (read for cross-reference only).

**Claim-vs-reality validation (every documented behavior checked against source):**
- **AC1 — PASS.** Anchor page carries all 7 required front-matter fields + unique `uid`/`slug`, `genre: reference` / `audience: adopter`, the full required section set, a ` ```razor ` usage fence + a `csharp no-compile reason="…"` bootstrap fence, no unsafe text. `docs/toc.yml` keeps **exactly four** top-level Diataxis entries (verified additive diff); `Components` nested under `Reference`. `docs/validation/` baselines **byte-untouched** (`git status` clean).
- **AC2 — PASS.** Layout + Navigation authored; DataGrid surface + Settings recorded as tracked gaps with named owners (FC-TBL/Story 2.8; Story 1.6) — permitted (Epic 1 open).
- **AC3 — PASS.** Contract `status: escalated`, owner "FrontComposer + Tenants author (pending)"; open items surfaced; pages cross-link the six published `HFC1050`–`HFC1055` diagnostics inline.
- **Source fidelity (spot-audited):** 7-parameter surface (`HeaderStart/HeaderCenter/HeaderEnd/Navigation/Footer/ChildContent/AppTitle`) ✅ exact; skip-link targets `#fc-main-content` / `#fc-nav` ✅; shortcuts `Ctrl+K` / `Ctrl+,` / `g h` ✅; `FrontComposerNavigation` has no `[Parameter]` ✅; `AddHexalithFrontComposer` / `AddHexalithShellLocalization` / `AddHexalithFrontComposerQuickstart` (chains `AddLocalization()`) ✅. **The `HeaderEnd` null-default → palette+settings claim is CORRECT** (razor `else`-branch), even though the component's own C# XML-doc comment is stale (see LOW-1).
- **Tests:** new `FcDocComponentDocumentationContractTests` re-run by reviewer → **15 passed, 0 failed** (claim confirmed, not taken on trust).

**Findings:**
- 🟡 **MED-1 (fixed):** git changed two files absent from the story File List — the new governance test `tests/…/FcDocComponentDocumentationContractTests.cs` and `…/tests/1-5-fc-doc-test-summary.md`. **Auto-fixed:** both added to the File List with a `tests/`-not-`src/` note.
- 🟢 **LOW-1 (tracked, not fixed — out of scope):** `FrontComposerShell.razor.cs` XML-doc on `HeaderEnd` says "Defaults to empty" but the razor renders palette+settings. Published page is correct; the `src/` XML comment is stale. Recorded under **Review Follow-ups (AI)** — editing `src/` is forbidden by this story's must-not-break / api-summary-baseline constraints.
- 🟢 **LOW-2 (reconciled here):** the contract/story state "there is no bUnit pin test … the docs validator *is* the gate," yet a governance pin test now exists. Reconciliation: the pin test **complements** Gate 2d (keeps the FC-DOC-specific clauses enforceable in the `dotnet test` lane without `pwsh`); it does not replace the validator. Narrative noted, not blocking.

**Risk posture:** docs-only + `tests/`-only change; no `src/`, no public API, no producer/api baselines perturbed; full `docfx` gate blocked only by a documented pre-existing environmental Razor-analyzer/docfx-metadata mismatch, structural Gate-2d subset green. **0 CRITICAL → Status `done`** per workflow v3.0 (HIGH/MED/LOW tracked, non-blocking).

## Change Log

| Date | Change |
|---|---|
| 2026-06-03 | **Senior Developer Review (AI)** — adversarial auto-fix review. APPROVE; 0 CRITICAL. Validated all 3 ACs against real component source (7-param surface, skip-link IDs, shortcuts, DI methods, no-param nav, `HeaderEnd` default all confirmed); re-ran the FC-DOC pin tests (15/15 pass); confirmed additive TOC/index edits and byte-untouched gated baselines. **MED-1 auto-fixed:** added the omitted governance test + test-summary to the File List. LOW-1 (stale `HeaderEnd` XML-doc in `src/`) tracked as a Review Follow-up (out of docs-story scope). LOW-2 (pin-test-vs-"validator-is-the-gate" narrative) reconciled. Status review → done. |
| 2026-06-03 | Authored the FC-DOC component-documentation contract (`_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md`, `status: escalated`, owner "FrontComposer + Tenants author (pending)"). Authored Gate-2d-conforming published reference pages `docs/reference/components/index.md`, `front-composer-shell.md`, `navigation.md`; wired `Components` into `docs/toc.yml` (under `Reference`, four top-level entries preserved) + `docs/reference/index.md`. DataGrid surface + settings recorded as tracked gaps with named owners. New pages pass the structural Gate-2d checks with zero failures (full docfx gate blocked by a pre-existing environmental Razor-compiler/docfx-metadata issue; 3 producer-fingerprint failures are pre-existing Epic-9 staleness, byte-identical to baseline). Release build clean (0 errors); test baseline unchanged (13 pre-existing failures). No `src/` change. Story → review. |
