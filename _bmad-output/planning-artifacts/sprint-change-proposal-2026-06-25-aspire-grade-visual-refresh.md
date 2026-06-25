# Sprint Change Proposal — Aspire-grade visual refresh (Shell chrome parity)

- **Date:** 2026-06-25
- **Author:** Administrator (via Correct Course workflow)
- **Trigger type:** Quality-bar raise (direct UX comparison vs. the .NET Aspire Dashboard; running app `localhost:62445`, Tenants host)
- **Change scope:** **Major** — new **Epic 8** sequenced by PM/Architect. Lead story **8.1 ships immediately as Minor**.
- **Path forward:** Hybrid — new framework epic, independently-shippable stories, lowest-risk/highest-impact first, plus a downstream host-adoption track.
- **Review mode:** Incremental (each change refined and approved individually before compilation).

---

## Section 1 — Issue Summary

While running the app and comparing it to the **.NET Aspire Dashboard**, FrontComposer's shell reads
as **dated**. Root-cause analysis (Shell source trace + Aspire source study) shows FrontComposer
**inverts Aspire's single most important design principle**:

> **Aspire keeps all chrome neutral and uses the brand hue only as a _thread_** — active nav, focus
> rings, primary buttons, links, selected/badge states — **never as a surface fill.** FrontComposer
> instead paints the **entire header band with the saturated brand accent** (`#0097A7` teal).

### Evidence / root cause

- **The teal header is the accent itself.** No Shell `.razor.css` and **no Tenants host CSS** paints
  the header — the Tenants host `MainLayout.razor` is literally `<FrontComposerShell>@Body</FrontComposerShell>`.
  The teal comes from the Fluent theme painting the layout-header slot with `FcShellOptions.AccentColor`
  (`#0097A7`, applied via the custom `IThemeService.SetThemeAsync(new ThemeSettings(accent,0,0,mode,true))`).
  **→ The header is 100 % Shell-owned; the fix needs no submodule change.**
- **Secondary gaps vs. Aspire:** plain-text expandable nav tree (vs. an icon+label rail with
  outline→filled active swap), looser default density (vs. Aspire's compact ~46px rows + sticky
  header), no unified page toolbar/tab pattern (vs. Aspire's `FluentSearch` + filter-popover + view-menu
  + underline tabs), and heavyweight badge **pills** for status (vs. Aspire's lightweight colored icon).
- **Constraint that shapes every edit:** FrontComposer is **Fluent UI Blazor v5**; Aspire is **v4**.
  Every token Aspire uses (`--neutral-layer-*`, `--type-ramp-*`, `--design-unit`, `accentBaseColor`,
  `baseLayerLuminance`) is a **legacy v4/FAST token explicitly banned by architecture §4.1**. So nothing
  is copied — each Aspire pattern is **translated** to Fluent 2 tokens (`--colorNeutralBackground*`,
  `--colorNeutralStroke*`, `FluentText Size`, `--fc-spacing-unit`) and Fluent v5 components.

### Framework ↔ host boundary (decisive for scoping)

| Surface | Owner | This proposal |
|---|---|---|
| Header band, nav rail, footer, `FcPageHeader`, theme/accent, density, grid surface, `FcPageToolbar`, status rendering | **FrontComposer Shell** (+ SourceTools generator) | ✅ Epic 8 (this repo) |
| "Locataires" page title color, loose button/filter layout, page-toolbar adoption | **Tenants.UI host** (submodule) | ➡️ downstream **Host-A** track, separate Tenants correct-course |

---

## Section 2 — Impact Analysis

### Epic impact

- Framework **Epics 1–7 are DONE**; none is broken. This is **net-new post-MVP work** → a **new
  Epic 8 (Aspire-grade visual refresh)**, not a modification of a live epic. No future epic invalidated;
  no resequencing of 1–7.

### Artifact conflicts

| Artifact | Impact | Action |
|---|---|---|
| **PRD** | None — no PRD; `epics.md` FR/NFR scope unchanged. FR9 (shell frame), FR14 (nav), FR15 (theme/density) reinforced; NFR6 (a11y) preserved | **N/A** |
| **Architecture** (`architecture.md` §4, §4.1) | Additive **accent-as-thread** note (§4.1); chrome-surface guard documented; UX-DR1/DR3 refined; **UX-DR2 amended** (status icon model) | **Edited** |
| **Epics** (`epics.md`) | New **Epic 8** + stories 8.1–8.7; **UX-DR2** amended; UX-DR3 nav refinement noted | **Edited** |
| **UI/UX** | Header, nav (`FrontComposerNavigation` + `FcCollapsedNavRail`), footer, `FcPageHeader`, density defaults, new `FcPageToolbar`, status components | **Edited** |
| **SourceTools (generator)** | `[ProjectionBadge]` emit (status icon, VR-7); conditionally the grid sticky-header (VR-5) | **Edited** |
| **Tests** | bUnit (nav/shell/status), Verify snapshots (generated output + nav), a11y/visual Playwright baselines; `FluentConformanceTests` stays green + **new narrow accent-as-background guard** | **Edited** |
| **CI / IaC / deploy / docs site** | None beyond intentional snapshot/baseline updates + FC-DOC pages for new components | **N/A / additive** |

### Technical impact

Layout/CSS + component refactors (Fluent 2 tokens only, §4.1-clean), one new layout component, one
new status-icon model, two generator touch-points (status emit; conditional grid sticky-header), and
intentional snapshot/baseline refreshes. No state-machine, schema-fingerprint, MCP-security, or
EventStore-contract changes. No `.sln`, no `Version=` edits, ULIDs/`ConfigureAwait` discipline
unaffected.

---

## Section 3 — Recommended Approach

**Hybrid — new Epic 8, sequenced lowest-risk/highest-impact first.** Option 1 (direct adjustment)
alone is too small for full parity; Option 2 (rollback) N/A (nothing to revert); Option 3 (MVP review)
N/A (MVP shipped). Epic 8 is delivered as **independently shippable stories** (matching this project's
standalone-story philosophy), with **8.1 shippable immediately** as a Minor change and the heavier,
contract-touching stories (nav rail, status icons) sequenced after.

- **Effort:** Major overall; per-story Low→High (see Section 4).
- **Risk:** Low for chrome stories (8.1–8.4, 8.6); Med for the nav rail (8.5); High for the status-icon
  UX-DR2 amendment (8.7, generator + snapshots + a11y).
- **Host-A** (Tenants.UI page-body adoption) is documented here but **executed as a separate Tenants
  correct-course** (`Hexalith.Tenants:bmad-correct-course`) under explicit submodule approval.

---

## Section 4 — Detailed Change Proposals (Epic 8 stories)

> All edits use **Fluent v5 components + Fluent 2 tokens only** → pass the §4.1
> `Shell_styles_use_no_legacy_fluent_v4_tokens` guard. Components/layout follow §4.1/§4.2/§4.3.
> Generator output is changed **via the emitter**, never hand-edited.

### Story 8.1 — VR-1: Neutral header chrome + footer framing  *(Minor; ship first)*

Replace the accent-filled header with a neutral chrome surface + hairline divider; frame the footer
symmetrically. Accent (`#0097A7`) is retained — only its **use as a surface fill** is removed.

**Edit 1a — header band** · `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` (~line 49)

```razor
OLD:
    <FluentStack Horizontal="true" VerticalAlignment="VerticalAlignment.Center"
                 HorizontalAlignment="HorizontalAlignment.SpaceBetween" Style="height: 48px; padding: 0 12px;">
NEW:
    <FluentStack Horizontal="true" VerticalAlignment="VerticalAlignment.Center"
                 HorizontalAlignment="HorizontalAlignment.SpaceBetween"
                 Style="height: 48px; padding: 0 12px; background: var(--colorNeutralBackground2); border-block-end: 1px solid var(--colorNeutralStroke2);">
```

**Edit 1b — footer framing** · same file (~line 132)

```razor
NEW:
    <FluentLayoutItem Area="@LayoutArea.Footer">
        <FluentStack VerticalAlignment="VerticalAlignment.Center"
                     Style="padding: 8px 12px; min-height: 36px; background: var(--colorNeutralBackground2); border-block-start: 1px solid var(--colorNeutralStroke2);">
            @if (Footer is not null) { @Footer }
            else {
                <FluentText As="TextTag.Span" Size="TextSize.Size200" Color="Color.Lightweight">
                    @Localizer["FooterCopyright", DateTime.Now.Year].Value
                </FluentText>
            }
        </FluentStack>
    </FluentLayoutItem>
```

- **Why correct:** the header stack is `Width=100%` inside the 48px item with *inner* padding, so the
  neutral fill covers the slot edge-to-edge regardless of what paints it underneath. Chrome on
  `--colorNeutralBackground2` (subtly greyer) vs. canvas `--colorNeutralBackground1` (white) gives the
  Aspire chrome/canvas separation; dividers frame top and bottom.
- **Verify:** AppTitle text + subtle action icons auto-revert white→`--colorNeutralForeground1`; **check
  contrast in both light AND dark themes** in the running app. Release clean (TWAE); `FrontComposerShellTests`
  + Verify snapshots updated intentionally; a11y/visual baseline reshot.
- **Effort Low / Risk Low / Contract: none.**

### Story 8.2 — VR-2: Accent-as-thread doc + narrow regression guard

- **Architecture §4.1 note (additive):** the brand accent (`FcShellOptions.AccentColor`, default
  `#0097A7`) is a **thread** — used only for active nav, focus, primary buttons, links, and badge/selected
  states — and **MUST NOT** be used as a chrome surface fill; header/nav/footer backgrounds stay
  `--colorNeutralBackground*`. (Records the VR-1 principle.)
- **New guard** in `…FluentConformanceTests` (Governance lane): fail if `--fc-color-accent` /
  `--fc-accent-base-color` appears inside a `background`/`background-color` declaration in Shell chrome
  CSS. Mechanically checkable; locks VR-1 against regression. Add its (empty) allowlist following the
  §4.1 shrink-only pattern.
- **Effort Low / Risk Low / Contract: additive doc + guard.**

### Story 8.3 — VR-9: Brand/logo cell

- Add an **optional logo-mark slot** before `AppTitle` in header-start (adopter-supplied or a default
  `FcFluentIcons` mark) and tighten the lockup spacing, so the top-left reads as a proper brand cell
  (Aspire's logo cell). Renders inside the existing `HeaderStart`/title `FluentStack`.
- **Effort Low / Risk Low / Contract: additive (optional slot).**

### Story 8.4 — VR-5: Compact default density + grid polish

- **Default density → Compact** for unset sessions (Story 1.6 default value only; fully reversible in
  `FcSettingsDialog`). Files: `Shell/State/Density/*` or `FcShellOptions` default + `wwwroot/css/fc-density.css`.
- **Grid polish** in `wwwroot/css/fc-projection.css`: tighten row height toward the Aspire ~46px feel via
  the `--fc-spacing-unit` cascade, subtle row-hover (`--colorSubtleBackgroundHover`), aligned cell padding
  (tokens only).
- **Sticky header (verify-then-act):** confirm whether the **generated** `FluentDataGrid` sets
  `GenerateHeader="Sticky"`; if not, a small `ProjectionRoleBodyEmitter` (SourceTools) change + regenerated
  Verify snapshots.
- **Effort Low (Shell) / Med (if emitter tweak) / Risk Low / Contract: Story 1.6 default.**

### Story 8.5 — VR-3: Icon+label nav rail + projection flyout  *(incl. VR-4 active swap)*

Promote nav to an Aspire-style rail while preserving the registry-driven hierarchy + badges.

- **Unify** `FrontComposerNavigation` (tree) + `FcCollapsedNavRail` (48px) into **one rail** rendered at
  **72px labeled** or **48px icon-only**; the always-visible hamburger toggles the two widths via the
  existing `SidebarToggledAction`/`SidebarCollapsed` (UX-DR3 contract preserved). Mobile/Compact → drawer.
- **Context tile:** `FluentButton Appearance="Subtle"` → vertical `FluentStack`: `FluentIcon`
  (**rest = outline, active = filled**) + (labeled) short `FluentText Size200` name + aggregate-count
  `FluentBadge`. **Active context:** filled icon + **accent left-bar** (`inset 3px` via `--fc-color-accent`)
  + `aria-current`.
- **Projection flyout:** click/Enter opens a flyout (`FluentMenu`/`FluentPopover` anchored to the tile)
  listing that context's projections as nav links with count + "New" badges; the shipped single-active-item
  rule (longest segment-prefix) lights the current projection.
- **Files:** `FrontComposerNavigation.razor`(+`.css`), `FcCollapsedNavRail.razor`(+`.css`), new
  `FcNavContextFlyout.razor` (or inline `FluentMenu`), `FcFluentIcons` (rest/active icon **pairs**),
  `FrontComposerShell.razor` (swap unchanged), Navigation state (flyout open/close local; active logic
  untouched).
- **a11y (UX-DR6/FC-A11Y):** rail `role="navigation"`, labeled tiles, flyout `role="menu"` fully
  keyboard-navigable (Enter/Space open, arrows, Esc, focus-return), `aria-current` on active.
- **RC verify (§4.3 caveat):** confirm `FluentMenu`/`FluentPopover` anchoring + keyboard and
  `data-testid`/`role`/`aria-*` splatting on the pinned `5.0.0-rc.3-26138.1` — the one novel interaction.
- **Effort High / Risk Med / Contract: UX-DR3 refinement (additive).**

### Story 8.6 — VR-6: Reusable `FcPageToolbar`

- New **framework-owned** `Components/Layout/FcPageToolbar.razor` (+`.css`) giving pages Aspire's one
  toolbar pattern under `FcPageHeader`: `FluentToolbar` row with leading `FluentSearch` (filter) + filter
  `FluentButton`→`FluentPopover` + view/overflow `FluentMenuButton` + right-aligned actions slot; optional
  underline `FluentTabs` strip for multi-view pages. Additive; FC-DOC doc page (Story 1.5 contract).
- **Effort Med / Risk Low (additive).** Tenants adoption = Host-A.

### Story 8.7 — VR-7: Status as colored icon  *(UX-DR2 amendment)*

Replace status **badge pills** with **colored Fluent icons**, label-on-hover (and on focus).

- **State → (icon, color) mapping:**
  - **Success** → green **checkmark** (`CheckmarkCircle`, `Color.Success`)
  - **Error/Rejected** → red **cross** (`DismissCircle`, `Color.Error`)
  - **Unknown/Neutral** → grey **question** (`QuestionCircle`, `Color.Neutral`)
  - *(extensions)* **Warning** → amber warning glyph; **Info** → blue info glyph
- **Label reveal:** status label shows on **hover** via `FluentTooltip` — **and on keyboard focus**; the
  icon **always** carries an `aria-label` so the accessible name is never hover-only (preserves UX-DR2's
  mandatory accessible name + NFR6/WCAG). Touch users get the tooltip on tap.
- **Scope:** applies to **status enums** (`[ProjectionBadge]` status members). Numeric **count** badges
  (nav/grid) stay `FluentBadge` pills.
- **Files:** `SourceTools` `[ProjectionBadge]` emitter (FluentBadge → colored `FluentIcon` + `FluentTooltip`
  + `aria-label`) + regenerated Verify snapshots; `FcStatusBadge` → `FcStatusIcon` (`FcDesaturatedBadge`
  reassessed for counts); `FcFluentIcons` (+ check/cross/question/warning/info); **UX-DR2 text amended** in
  `epics.md` + `architecture.md` §4.1.
- **Effort High / Risk High / Contract: UX-DR2 amendment + generator.** Tests: status bUnit, generator
  snapshot tests, a11y e2e (tooltip on hover **and** focus, accessible name present).

### Downstream — Host-A: Tenants.UI page-body adoption *(separate Tenants change)*

Documented here, executed via `Hexalith.Tenants:bmad-correct-course` under **explicit submodule approval**:
neutralize the "Locataires" page title (confirm it's neutral — `FcPageHeader` is neutral by source; if a
host page hardcodes a non-neutral heading, fix it), adopt `FcPageToolbar` (replace the loose "Actualiser /
Réinitialiser les filtres" + search fields), and verify Compact density. **Not in this proposal's scope.**

---

## Section 5 — Implementation Handoff

- **Scope classification:** **Major** (new Epic 8). Lead story **8.1 = Minor** (direct dev implementation).
- **Routing:**
  - **PM / Architect** — add **Epic 8** + stories 8.1–8.7 to `epics.md`; apply the **architecture.md**
    edits (accent-as-thread §4.1, UX-DR2 amendment, UX-DR3 nav refinement); sequence the epic.
  - **Developer (`bmad-dev-story`)** — implement per story, **8.1 first** (immediately shippable), then
    8.2→8.4→8.5→8.6→8.7. One feature branch + PR per story; `/bmad-code-review` before each Done.
  - **Tenants team** — Host-A via the scoped Tenants correct-course (separate, approval-gated).
- **Suggested sequence:** 8.1 (header/footer) → 8.2 (accent doc+guard, locks 8.1) → 8.3 (logo) →
  8.4 (density/grid) → 8.5 (nav rail) → 8.6 (toolbar) → 8.7 (status icons).
- **Per-story Definition of Done:** `dotnet build -c Release` clean (TWAE); solution-level
  `dotnet test … -e DiffEngine_Disabled=true` (default lane + Governance) green; Verify/PublicAPI/pact
  baselines updated **intentionally**; a11y/visual reshot; both light **and** dark themes verified in the
  running app; `/bmad-code-review` passed.
- **Commits (Conventional):** `feat(shell): …` for new behavior/components (8.3/8.5/8.6/8.7),
  `fix(shell): neutralize header chrome and frame footer` for 8.1, `docs:`/`test:` for 8.2's doc+guard as
  appropriate (the guard is `test:`; the §4.1 note is `docs:`). No direct commits to `main`.

---

## Success Criteria

- Header + footer render as clean **neutral chrome** with hairline dividers; the brand accent appears
  **only** as a thread (active nav, focus, primary buttons, links, badges) — never as a surface fill —
  in both light and dark themes.
- Primary desktop nav is an **icon+label rail** (outline→filled active swap + accent bar) with a
  **projection flyout**, preserving registry hierarchy, count/"New" badges, single-active-item, and full
  keyboard a11y.
- Default density is **Compact**; projection grids feel Aspire-dense (sticky header, ~46px rows, subtle
  hover) and remain user-adjustable.
- A reusable **`FcPageToolbar`** exists (search + filter-popover + view-menu + optional underline tabs)
  for pages to adopt.
- Status renders as **colored icons** (green check / red cross / grey question) with **hover + focus**
  label reveal and an always-present accessible name (no WCAG/NFR6 regression).
- All configured tests pass; Release clean; `FluentConformanceTests` green incl. the new
  accent-as-background guard; **no legacy v4/FAST tokens introduced**.
- Host-A tracked as a separate Tenants.UI change.
