# Sprint Change Proposal — Deferred-Work follow-ups: dead-token fix + §4.2 accordion conversions

- **Date:** 2026-06-19
- **Author:** Developer agent (correct-course)
- **Trigger:** `/bmad-correct-course implement deferred work` (second pass of the day)
- **Scope class:** Minor (Direct Adjustment — Option 1)
- **Mode:** Incremental
- **Status:** Implemented + verified (uncommitted)
- **Builds on:** `sprint-change-proposal-2026-06-19-deferred-work-fluent-backlog-burndown.md`
  (§4.1/§4.3 burn-down, already committed) and
  `sprint-change-proposal-2026-06-17-fluent-accordion-page-sections.md` (§4.2 guideline + handoff).

---

## Section 1 — Issue Summary

"Implement deferred work." The morning's burn-down pass (§4.1 tokens + §4.3 layout) is **done and
committed** (commits `a58a995`, `2396ef9`; tree clean). The `deferred-work.md` ledger (666 rows) is the
old Epic 8–12 decomposition and is **fully reconciled (0 unresolved-owned)**. What remained actionable
**in this repo** were two documented residuals:

1. **A latent non-existent-token bug** flagged as residual #3 of the morning burn-down — and found to be
   **broader** than recorded. Four CSS references to `--typeRamp*` tokens that **do not exist** in Fluent
   v5 (neither legacy-v4 kebab nor correct Fluent 2 camelCase), so `font-size`/`font-weight` silently
   collapse to inherited. The §4.1 guard misses them because they are not kebab-case v4/FAST tokens.
2. **The §4.2 in-repo accordion conversions** (residual #1 of the 2026-06-17 accordion proposal) —
   `FcSettingsDialog`, `CounterPage`, `FcHomeDirectory` — routed but "awaiting go-ahead."

**Evidence.** The Shell already uses the correct Fluent 2 tokens (`--fontSizeBase300/400/500`,
`--fontWeightSemibold`, `--fontFamilyBase`); `--fontSizeBase200` is the missing pair-mate. The
`--typeRampBase*` names are a v4/FAST↔Fluent-2 hybrid that resolves to nothing.

## Section 2 — Impact Analysis

- **Epic impact:** none. Epics 1–7 Done; no add/remove/resequence.
- **Story impact:** none (standing course-correction directive; no formal story).
- **Artifact conflicts:** `architecture.md` §4.2 (new in-repo-conversion record) + §4.3 (FcSettingsDialog
  body-wrapper revert note) updated. `project-context.md` **unchanged** — the §4.2/§4.3 AI rules already
  describe these patterns; the conversions follow them (no rule changed). No PRD; no public-API/CI/pact/IaC impact.
- **Technical impact:** Shell + Counter-sample only. 2 stylesheets (4 token refs) + 1 sample inline-style
  (2 token refs) corrected; 2 components converted to `FluentAccordion`; 1 dead CSS rule removed; 1 e2e
  test-anchor (`#fc-theme-section`) and the `.command-section`/`.inline-section`/`.fullpage-section`
  e2e selectors preserved. No `.verified.txt`/PublicAPI/pact change.

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment.** In-place token correction + the two prescribed §4.2 conversions.
Effort Low, risk Low: every change is a token-name swap or a guideline-prescribed structural move, each
build- and bUnit-verified. (Rollback / MVP-review N/A — nothing to revert, MVP unaffected.)

## Section 4 — Detailed Change Proposals

### 4.A — Dead-token fix (`--typeRamp*` → Fluent 2)

| File | Before | After |
|---|---|---|
| `FcDensityPreviewPanel.razor.css:18` | `var(--typeRampBase200FontSize)` | `var(--fontSizeBase200)` |
| `FcSettingsDialog.razor.css:16` | `var(--typeRampBase500FontSize)` | `var(--fontSizeBase500)` |
| `FcSettingsDialog.razor.css:17` | `var(--typeRampBase500FontWeight)` | `var(--fontWeightSemibold)` |
| `FcSettingsDialog.razor.css:41` | `var(--typeRampBase200FontSize)` | `var(--fontSizeBase200)` |
| `CounterPage.razor:23` (inline) | `var(--neutral-fill-subtle-rest)` / `var(--accent-fill-rest)` | `var(--colorNeutralBackground2)` / `var(--colorBrandStroke1)` |

The `FcSettingsDialog.razor.css` `.fc-settings-body h3` rule became dead after 4.B (no `<h3>` in the
body) and was removed; the token fixes on lines 16–17 were folded into that removal.

### 4.B — `FcSettingsDialog` → `FluentAccordion` (§4.2 canonical "4.C", **full conversion** per user choice)

Three bare `<h3>` sections (Density / Theme / Preview) → one `FluentAccordion`, one `FluentAccordionItem`
each, **Density `Expanded="true"`** (Theme/Preview collapsed). Body wrapper reverted from the §4.3
single-child `FluentStack` to a plain `<div>` (accordion now owns sectioning). Preserved:
`data-testid="fc-settings-dialog"`, the `#fc-theme-section` e2e anchor (now `Id=` on the Theme item),
the radio group's accessible name (`aria-labelledby="fc-density-section"` → `aria-label`), and the
`fc-settings-forced-note` / footer `data-testid`s. **Pinned-RC note:** `FluentAccordionItem` uses
`Heading=`/`Expanded=` (mirrors the working `FcHomeDirectory`); the `fluent-ui-blazor` MCP documents a
different build's `Header=` — the in-repo usage + the Release build (TWAE) are authoritative.

### 4.C — `CounterPage` (Counter sample) → `FluentAccordion` (**convert, per user choice**)

Three `<h2>` command-density specimen sections → one `FluentAccordion ExpandMode="AccordionExpandMode.Multi"`
with **every item `Expanded="true"`** and `HeadingLevel="2"`. Rationale: `FluentAccordion` defaults to
Multi mode and the specimen's purpose (and the a11y/visual + command-form e2e gate) is the three
densities **visible side-by-side** — all-expanded Multi groups the sections under §4.2 and adds the
collapse affordance **without hiding any specimen and without breaking the e2e assumptions** that the
sections are simultaneously visible/interactive. The `.command-section` / `.inline-section` /
`.fullpage-section` classes ride on the items so the e2e descendant selectors (`.inline-section .fc-popover`,
`.command-section .fc-expand-in-row`) and visibility survive — **no `.ts` e2e edits were required**. The
data grid stays an always-visible primary region below the accordion (grid-first, not a section). The
**live Playwright run remains the CI gate** (the e2e lane is socket-blocked locally, per every prior pass).

### 4.D — `FcHomeDirectory` — assessed, **left unchanged**

Already conforms: urgent cards are the always-visible primary region; only the zero-urgency "other
areas" collapse into a `FluentAccordion`. The 2026-06-17 inventory's "extend the accordion to the
urgent+other split" suggestion was **declined with rationale** — wrapping the urgent cards would
*violate* §4.2 by hiding primary content.

## Section 5 — Implementation Handoff

**Routing — Developer agent (this pass, DONE).** Minor scope; no PO/PM/Architect escalation.

**Constraints honored (project-context + CLAUDE.md):** `.slnx` only; `TreatWarningsAsErrors`; tests run
with `DiffEngine_Disabled=true`; Fluent v5 / Fluent 2 tokens only; `.verified.txt`/PublicAPI/pact
unchanged; submodules untouched (Shell + in-repo sample only). On commit: feature branch + PR (no direct
`main`); Conventional Commits — token fix = `fix` (latent bug), accordion conversions = `refactor`
(layout reshaping), **not** `feat`. **No commit** (not requested).

**Success criteria:**
- ✅ Dead `--typeRamp*` (×4) + sample `--neutral-fill`/`--accent-fill` (×2) tokens corrected to Fluent 2.
- ✅ `FcSettingsDialog` converted (full §4.2 4.C); `CounterPage` converted (Multi/all-expanded);
  `FcHomeDirectory` confirmed already-conforming.
- ✅ Full solution Release build clean (0 warnings, TWAE).
- ✅ `FluentConformanceTests` green; full Shell default lane **1905 passed / 0 failed**
  (`DiffEngine_Disabled=true`) — incl. `FcSettingsDialogTests` (6/6) and `CounterStoryVerificationTests`,
  which render the converted components.
- ✅ `architecture.md` §4.2/§4.3 updated; `project-context.md` reviewed (no rule change needed).
- ⏳ Live Playwright e2e (`npm run test:a11y`) — **CI gate** (socket-blocked locally). Selectors +
  visibility preserved by design; no `.ts` edits.
- ⏳ Live visual check under Aspire (optional).
- ⏳ Commit / PR (not done — not requested).

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ (1.1 no formal story — standing directive · 1.2 latent-bug fix +
  guideline-prescribed standardization, not a defect report · 1.3 the never-defined `--typeRamp*` tokens
  + the two 2026-06-17/06-19 residual lists as evidence)
- **§2 Epic Impact:** ✅ N/A (Epics 1–7 Done)
- **§3 Artifact Conflicts:** 3.1 N/A (no PRD) · 3.2 ✅ architecture §4.2/§4.3 · 3.3 ✅ (no UX spec; a11y
  anchors/heading-order/landmarks preserved + bUnit-verified) · 3.4 ✅ project-context reviewed (unchanged);
  no public-API/CI/pact/IaC impact
- **§4 Path Forward:** ✅ Option 1 (Direct Adjustment)
- **§5 Proposal Components:** ✅ this document
- **§6 Final Review/Handoff:** ✅ approved interactively (Administrator chose: token fix + accordion
  review; FcSettingsDialog = full 4.C; CounterPage = convert + e2e-migrate). 6.4 sprint-status.yaml: **N/A**
  (no epic add/remove/renumber).

---

## Implementation status (2026-06-19, deferred-work follow-up pass)

**4.A token fix — DONE (uncommitted, verified):** 6 token references corrected across 3 files; dead
`.fc-settings-body h3` rule removed.

**4.B `FcSettingsDialog` — DONE (uncommitted, verified):** full §4.2 accordion (Density expanded);
a11y/e2e anchors preserved; `FcSettingsDialogTests` 6/6 green.

**4.C `CounterPage` — DONE (uncommitted, bUnit-verified):** Multi/all-expanded accordion; e2e selectors
+ visibility preserved (no `.ts` edits); `CounterStoryVerificationTests` green; live Playwright = CI gate.

**4.D `FcHomeDirectory` — no change (already conforms).**

**Verification — DONE:**
- Full solution Release build: **0 warnings / 0 errors** (TWAE).
- Full Shell default lane (`Category!=Performance&!=e2e-palette&!=NightlyProperty&!=Quarantined`):
  **1905 passed / 0 failed** (`DiffEngine_Disabled=true`) — after both conversions.

### Residuals
1. **Live Playwright e2e** (`command-form-generation.spec.ts`, `smoke.spec.ts`, `settings-persistence`,
   `density-transition`) — CI gate; selectors/visibility preserved by design, no spec edits.
2. **Live visual check** under Aspire — optional.
3. **Submodule sweep** (Tenants.UI, Admin.UI) §4.2 conversions — approval-gated, separate per-repo passes.
4. **Commit / PR** — not done (not requested). Conventional Commits: `fix` (token) + `refactor` (accordion).
