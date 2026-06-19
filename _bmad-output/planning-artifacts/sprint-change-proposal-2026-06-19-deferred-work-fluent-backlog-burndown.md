# Sprint Change Proposal — Deferred-Work Implementation: Fluent backlog burn-down (§4.1 + §4.3)

- **Date:** 2026-06-19
- **Author:** Developer agent (correct-course)
- **Trigger:** `/bmad-correct-course implement deferred work`
- **Scope class:** Minor (Direct Adjustment — Option 1)
- **Mode:** Incremental
- **Status:** Implemented + verified (uncommitted)

---

## Section 1 — Issue Summary

"Implement deferred work." The project carries two standing, guideline/guard-tracked Fluent UI
backlogs introduced by the June correct-course passes:

- **§4.1 legacy-token migration backlog** — a hard, governance-guarded, shrink-only allowlist of **15**
  Shell `.css`/`.razor` files still referencing Fluent v4 / FAST tokens (`--neutral-*`, `--accent-*`,
  `--type-ramp-*`, `--design-unit`, `--corner-radius`, `--elevation-shadow-*`, `--focus-stroke-*`,
  `--palette-*`). Guard: `FluentConformanceTests.Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog`.
- **§4.3 layout-component conversion backlog** — guideline (no guard): `FcHomeDirectory` (blocked on the
  `--design-unit` migration) plus MED candidates (`FcCommandPalette`, `FcPaletteResultList`,
  `FcDensityPreviewPanel`, `FcPendingCommandSummary`).

**Evidence / key discovery.** `--design-unit` is **never defined** anywhere in the repo — it was a
Fluent v4 design-system-provider token dropped in v5. Every `calc(var(--design-unit) * Npx)` therefore
resolves to an invalid length and the gap/padding **silently collapses** (a latent v4→v5 regression).
The project's real density mechanism is `--fc-spacing-unit` (`wwwroot/css/fc-density.css`, ADR-041) via
the `calc(var(--fc-spacing-unit, 4px) * N)` idiom already used by `FcCommandPalette`/`FcPaletteResultList`/
`FcDensityPreviewPanel`. Migrating `--design-unit` → `--fc-spacing-unit` clears the guard, fixes the bug,
restores density scaling, and unblocks the `FcHomeDirectory` §4.3 conversion — all in one move.

## Section 2 — Impact Analysis

- **Epic impact:** none. Epics 1–7 are Done; no epic add/remove/resequence.
- **Story impact:** none (no formal story; standing course-correction directive).
- **Artifact conflicts:** `architecture.md` §4.1 backlog table + §4.3 burn-down record updated;
  `project-context.md` Blazor-Shell rules updated. No PRD. No public-API/CI/IaC/pact impact.
- **Technical impact:** Shell-only. 15 stylesheet/markup files migrated off legacy tokens; 4 layout
  conversions (+1 documented keep); 1 dynamic-var rename with its regression-lock baseline updated;
  guard allowlist emptied (15 → 0).

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment.** In-place migration; no rollback, no MVP review. Effort Low–Medium,
risk Low (every change is a token-name swap or a §4.3-prescribed structural move, each test-verified).

## Section 4 — Detailed Change Proposals

### §4.1 — Token migration (15 files → Fluent 2 / `--fc-spacing-unit`)

| Legacy v4/FAST token | Fluent 2 replacement |
|---|---|
| `--neutral-foreground-rest` | `--colorNeutralForeground1` |
| `--neutral-foreground-hint` | `--colorNeutralForeground3` |
| `--neutral-fill-rest` | `--colorNeutralBackground3` |
| `--neutral-fill-stealth-rest` / `-hover` | `--colorSubtleBackground` / `--colorSubtleBackgroundHover` |
| `--neutral-layer-1` / `-2` | `--colorNeutralBackground1` / `--colorNeutralBackground2` |
| `--neutral-stroke-rest` | `--colorNeutralStroke1` |
| `--neutral-stroke-divider-rest` | `--colorNeutralStroke2` |
| `--accent-fill-rest` (border/outline) | `--colorBrandStroke1` |
| `--accent-fill-rest` (bg tint in `color-mix`) | `--colorBrandBackground` |
| `--accent-base-color` (dynamic var) | **rename** → `--fc-accent-base-color` |
| `--focus-stroke-outer` | `--colorStrokeFocus2` |
| `--palette-red-70` | `--colorPaletteRedBorder2` |
| `--elevation-shadow-card-rest` / `-hover` | `--shadow4` / `--shadow8` |
| `calc(var(--corner-radius) * 1px)` | `var(--borderRadiusMedium)` |
| `--type-ramp-base-font-family` | `--fontFamilyBase` |
| `--type-ramp-base/plus-1/plus-2-font` | `--fontSizeBase300/400/500` + `--lineHeightBase300/400/500` |
| `calc(var(--design-unit) * Npx)` | `calc(var(--fc-spacing-unit, 4px) * N)` |
| `var(--design-unit, 0.5rem)` | `calc(var(--fc-spacing-unit, 4px) * 2)` (= 8px, density-scaled) |

All Fluent 2 targets confirmed emitted by the pinned `5.0.0-rc.3-26138.1` runtime. Existing hex/rgba
fallbacks preserved (pure token-name swaps). Non-targeted custom vars (`--error`,
`--control-corner-radius`, `--error-foreground-rest`) left untouched (not guard-flagged).

**Files migrated:** `FcNewItemIndicator.razor.css`, `FcDevModeAnnotation.razor.css`,
`FcDevModeOverlay.razor.css`, `FcDevModeToggleButton.razor.css`, `FcCustomizationDiagnosticPanel.razor.css`,
`FcPendingCommandSummary.razor.css`, `FcProjectionConnectionStatus.razor.css`, `FcHomeDirectory.razor.css`,
`FrontComposerShell.razor` (+`.razor.css`), `FcLifecycleWrapper.razor.css`, `FcFieldPlaceholder.razor.css`,
`FcProjectionLoadingSkeleton.razor.css`, `wwwroot/css/fc-empty-state.scoped.css`, `wwwroot/css/fc-projection.css`.

**Guard:** `FluentConformanceTests` `migrationBacklog` allowlist emptied (15 → 0). The guard now blocks
**any** legacy token anywhere in the Shell.

**Regression lock:** `--accent-base-color` → `--fc-accent-base-color` rename updated in
`SlotMappingRegressionTests.cs` (the `actual` table row + the `ShouldContain` assertion) and its
`SlotMappingRegressionTests.BindingTable.verified.txt` baseline.

### §4.3 — Layout-component conversions

| Component | Element | Action |
|---|---|---|
| `FcHomeDirectory` | `.fc-home-directory` root | `<div>` → `FluentStack` V; density gap as `VerticalGap` string `calc()`; padding kept in CSS; `aria-label`/`data-testid` splatted |
| `FcDensityPreviewPanel` | `.fc-density-preview` | plain flex column → `FluentStack` V; `data-fc-density` local override preserved |
| `FcPendingCommandSummary` | `.fc-pending-command-summary__details` | grid-as-stack `<div>` → `FluentStack` V; root `<section aria-live>` landmark kept |
| `FcCommandPalette` | `.fc-palette-root` | `role="dialog"` landmark kept; flex moved to a **nested** `FluentStack` |
| `FcPaletteResultList` | `.fc-palette-option` | **kept** — repeated `role="option"` rows with `flex:1 1 auto`/`margin-left:auto` item rules (§4.3 exclusion) |

## Section 5 — Implementation Handoff

**Routing — Developer agent (this pass, DONE).** Minor scope; no PO/PM/Architect escalation.

**Constraints honored (project-context + CLAUDE.md):** `.slnx` only; `TreatWarningsAsErrors`; tests run
with `DiffEngine_Disabled=true`; Fluent v5 / Fluent 2 tokens only; density via `--fc-spacing-unit`;
`.verified.txt` baseline updated intentionally; **no commit** (not requested). On commit: feature branch +
PR (no direct `main`); Conventional Commits — these changes are `refactor` (layout/token reshaping), **not**
`feat` (no false minor bump).

**Success criteria:**
- ✅ §4.1 backlog 15 → 0; guard now blocks all legacy tokens (allowlist empty).
- ✅ §4.3 `FcHomeDirectory` + 3 MED candidates converted; `FcPaletteResultList` option rows kept (documented).
- ✅ Latent `--design-unit` collapse bug fixed; density scaling restored via `--fc-spacing-unit`.
- ✅ Release build clean (0 warnings, TWAE); Governance + affected bUnit lanes green; **full Shell default
  lane 1905 passed / 0 failed**.
- ✅ `architecture.md` §4.1/§4.3 + `project-context.md` updated.
- ⏳ Live visual check under Aspire (optional follow-up).
- ⏳ Commit / PR (not done — not requested).

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ (1.1 no formal story — standing directive · 1.2 new-requirement /
  standardization + latent-bug fix, not a defect report · 1.3 guard allowlist + architecture §4.1/§4.3
  tables + the never-defined `--design-unit` as evidence)
- **§2 Epic Impact:** ✅ N/A (Epics 1–7 Done; no epic change)
- **§3 Artifact Conflicts:** 3.1 N/A (no PRD) · 3.2 ✅ architecture §4.1/§4.3 (done) · 3.3 ✅ (no UX spec;
  a11y landmarks/splatting preserved + test-verified) · 3.4 ✅ project-context (done); no public-API/CI/pact/IaC impact
- **§4 Path Forward:** ✅ Option 1 (Direct Adjustment)
- **§5 Proposal Components:** ✅ this document
- **§6 Final Review/Handoff:** ✅ approved (Administrator: "Approve + also do MED §4.3"); implemented +
  verified this pass. 6.4 sprint-status.yaml: **N/A** (no epic add/remove/renumber).

---

## Implementation status (2026-06-19)

**§4.1 — DONE (src, uncommitted, verified):** 15 files migrated off legacy tokens; guard allowlist emptied;
`SlotMappingRegressionTests` + baseline updated for the accent-var rename.

**§4.3 — DONE (src, uncommitted, verified):** `FcHomeDirectory`, `FcDensityPreviewPanel`,
`FcPendingCommandSummary` (`__details`), `FcCommandPalette` (nested) converted to `FluentStack`;
`FcPaletteResultList` option rows kept as a documented exclusion.

**Verification — DONE:**
- Release build of Shell + Shell.Tests: **0 warnings / 0 errors** (TWAE).
- Governance + all affected component bUnit classes: **239/239 green** (includes `FluentConformanceTests`
  with the empty allowlist).
- Full Shell default lane (`Category!=Performance&!=e2e-palette&!=NightlyProperty&!=Quarantined`):
  **1905 passed / 0 failed** (`DiffEngine_Disabled=true`).

### Residuals
1. **Submodule sweep** (Tenants.UI, Admin.UI) — any analogous legacy-token / div-flex layout there is a
   separate per-repo correct-course (submodule edits are approval-gated; never recurse into nested submodules).
2. **Live visual check** under Aspire — optional.
3. **Out-of-scope latent finding (not guard-flagged):** `FcDensityPreviewPanel.razor.css` uses
   `--typeRampBase200FontSize` (a non-existent token; should be `--fontSizeBase200`). Left untouched this
   pass — it is not a legacy v4/FAST kebab token, so the guard does not flag it; fix in a follow-up.
4. **Commit / PR** — not done (not requested). Conventional Commits: `refactor` (NOT `feat`).
