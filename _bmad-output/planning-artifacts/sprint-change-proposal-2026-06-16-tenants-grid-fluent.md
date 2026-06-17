# Sprint Change Proposal — Tenants grids: Fluent UI cell content + DataGrid features

_Workflow: bmad-correct-course · Date: 2026-06-16 · Mode: Incremental · Status: **COMPLETE (LEAN path) — Tenants.UI suite GREEN 670/670 (`-c Release`, `DiffEngine_Disabled=true`); Release build TWAE-clean (0 warnings). Changes left uncommitted (Tenants submodule + FrontComposer.Shell). Residual: live visual check under Aspire. See "Implementation complete" at the bottom.**_

> **Revision (2026-06-16, after reading the handlers):** my original "in-grid sort conflicts with the
> global server-side sort" reasoning was **wrong**. The server `ListTenantsAsync(cursor, pageSize)`
> ignores sort; `TenantsWorkspace.ApplyVisibleRows` already does search/filter/**sort client-side over
> the current page**. So `FluentDataGrid`'s native column-header sort is behaviourally **identical** to
> today's combobox and is a clean drop-in with **no** server change. Only `FluentPaginator` (numbered
> pages) needed the cursor→offset+total re-architecture — and the user chose to **keep cursor Prev/Next**.
> **Final scope = Fluent cell content + in-grid column-header sort (replacing the combobox) + keep cursor
> Prev/Next pagination.** No Contracts/server changes, no ADR. (Option B re-architecture below retained
> for the record but NOT being implemented.)
_Trigger (Administrator): "the tenants grid should use Blazor Fluent UI components." + https://fluentui-blazor-v5.azurewebsites.net/DataGrid_
_Submodule approval: Administrator authorized editing `Hexalith.Tenants` submodule files (2026-06-16)._

> Companion to `sprint-change-proposal-2026-06-09-fluent-v5-domain-ui.md`, which already converted
> all raw HTML **form controls** in Tenants.UI to Fluent v5. **This** change targets the **grids**
> specifically: the cell *content* (still raw `<span>` badges / text-letter "icons" / raw `<a>`),
> and the **DataGrid feature set** (sorting, pagination, columns) shown on the linked docs page.

---

## Section 1 — Issue Summary

**Problem.** The three Tenants grids should "use Blazor Fluent UI components" per the linked
`FluentDataGrid` docs. Investigation shows the **grid container already is `<FluentDataGrid>`** (the
2026-06-09 pass landed that), so the request resolves to two distinct gaps:

1. **Cell content is not Fluent.** Inside the columns, status/role/freshness/category are hand-rolled
   `<span class="…badge…">` (not `FluentBadge`); "icons" are **text letters** — `O`/`C`/`R`/`OK`/`!`/`…`
   (not `FluentIcon`); the detail link is a raw `<a>` (not `FluentAnchor`).
2. **DataGrid features are external, not in-grid.** Sorting and pagination exist, but as **separate
   controls outside the grid** (FluentSelect sort dropdowns + Prev/Next FluentButtons), not the
   docs' in-grid sortable headers + `FluentPaginator`.

**Discovery.** Source audit of the three grids and their host pages on 2026-06-16.

**Evidence — current state per grid (all in `Hexalith.Tenants` submodule):**

| Grid | Container | Cell content non-Fluent | Pagination model | Sort model |
|---|---|---|---|---|
| `TenantDataGrid` (host `TenantsWorkspace`) | `FluentDataGrid` ✓ | status `<span>`, raw `<a>` detail link, `TruthStateBadge` (text icon) | **server-side cursor** (PageSize 20, `NextCursor`/`HasMore`) | **server-side** via 2 FluentSelect dropdowns |
| `MyTenantsDataGrid` (host `MyTenantsPage`) | `FluentDataGrid` ✓ | role/status `<span>` badges + letters `O/C/R/OK/!`, `TruthStateBadge` | **server-side cursor** (PageSize 20) | **none** (gateway has no sort param) |
| `AuditDataGrid` (host `TenantAuditPage`) | `FluentDataGrid` ✓ | category `<span>`, `TruthStateBadge` | **server-side cursor** (PageSize 50) | **none** (gateway has no sort param) |

Shared offenders: `TruthStateBadge.razor` (text icons `OK/…/!/?`), `SupportSafeCopyButton.razor`
(letter `C` instead of a copy icon; the button itself is already `FluentButton`).

---

## Section 2 — Impact Analysis

### Epic Impact — none to the FrontComposer framework
FrontComposer Epics 1–7 are done and Fluent-correct. This is **domain-consumer** work in the
`Hexalith.Tenants` submodule (not tracked in FrontComposer `epics.md`). No epic added/removed/resequenced.

### The architectural conflict (the crux of this proposal)
The docs page showcases **in-grid sortable headers** and **`FluentPaginator`**. Both clash with the
grids' existing **server-side cursor pagination**:

- **`FluentPaginator` needs a known `TotalItemCount` + random page-index access.** The query gateways
  return only a **cursor + `HasMore`** — there is **no total count** and no page index. `FluentPaginator`
  cannot be wired to the current contracts without changing them.
- **In-grid column sort (`Sortable`/`PropertyColumn`) sorts only the loaded page** (20/50 rows). The
  current sort is **global, server-side, across all pages**. Naively enabling header sort would
  silently demote a correct global sort to a misleading page-local one.
- **`MyTenants` / `Audit` gateways take no sort parameter at all** — correct in-grid sorting there
  requires a **Contracts + server** change, not just UI.

This splits the requested "DataGrid features" into a **contained UI change** vs a **paging-contract
re-architecture**. See Section 3.

### Artifact Conflicts
- **PRD** — N/A (no standalone PRD). Reinforces FR11 (Fluent DataGrid) / UX-DR1·DR2 (Fluent tokens/badges)
  / NFR6 (a11y). No requirement changes.
- **Architecture** (`_bmad-output/project-docs/architecture.md`) — no conflict for Option A. Option B
  would change the documented tenant query paging model (cursor → offset+total) and warrants an ADR.
- **Tests** — `Hexalith.Tenants.UI.Tests` DOM-shape assertions break (now `<fluent-badge>`/`<fluent-anchor>`/
  `<fluent-icon>` nodes; needs `JSInterop.Mode = Loose`). `data-testid` hooks **preserved**, so most
  `[data-testid]` queries survive. Governance guard `DomainUiFluentConformanceTests` keeps passing
  (no raw interactive controls are introduced). Option B additionally needs gateway/contract/Pact + server tests.
- **Dependency** — Tenants.UI references `Microsoft.FluentUI.AspNetCore.Components` but **not** the
  `.Icons` NuGet. The chosen icon strategy adds it (Administrator decision 2026-06-16). Note: the
  FrontComposer repo deliberately avoids the Icons NuGet (custom `FcFluentIcons`); this divergence is
  scoped to the **Tenants submodule's** own `Directory.Packages.props` and does not affect FrontComposer.

### Technical Impact (Option A)
~4 grid/shared `.razor` files + 2 host pages + 2 packaging files in Tenants.UI, plus test updates.
Mechanical-but-careful (binding/behaviour parity, a11y labels preserved). No Contracts/server changes.

---

## Section 3 — Recommended Approach

Two honest paths. **Option A is recommended.**

### ✅ Option A — Fluent cell content + contained DataGrid sort (no contract change) — RECOMMENDED
1. **Add the Icons NuGet** to the Tenants submodule (per decision) for real `FluentIcon` glyphs.
2. **Cell content → Fluent** across all three grids and the two shared components:
   - status / role / freshness / category `<span>` badges → **`FluentBadge`** (`Color` by state,
     `IconStart` glyph), replacing the letter "icons".
   - `TruthStateBadge` → `FluentBadge` + `FluentIcon` (Checkmark / ArrowSync / Warning / Question).
   - `SupportSafeCopyButton` letter `C` → `FluentIcon` (Copy) in the existing `FluentButton`.
   - `TenantDataGrid` detail link raw `<a>` → **`FluentAnchor`**.
3. **In-grid sort for `TenantDataGrid` only**, wired to the **existing server-side sort** (header
   click → the existing `OnSortChanged`/`OnSortDirectionChanged` → reload from server). Remove the now
   redundant external sort dropdowns. This is correct (still global) and matches the docs' header feel.
4. **Pagination stays cursor-based** (already FluentButton Prev/Next). `FluentPaginator` is **not**
   adopted (no totals in the contract). `MyTenants`/`Audit` headers stay non-sortable (no server sort param).

- **Effort:** Medium · **Risk:** Low–Medium (test DOM churn) · **Blast radius:** Tenants.UI only.
- **Delivers:** the visible "use Fluent components" win + a real in-grid sort affordance, with zero
  contract/server risk.

### ⚠️ Option B — Option A **plus** paging-contract re-architecture for true `FluentPaginator`
Migrate the tenant query gateways from **cursor → offset + total-count** paging, then adopt
`FluentDataGrid` `ItemsProvider`/`Virtualize` + `FluentPaginator` and in-grid global sort on all three grids.

- **Touches:** `Hexalith.Tenants.Contracts.Queries` (request/response shapes), the query gateways, the
  **server-side query handlers/projections**, Pact contracts, and far more tests. Warrants an ADR.
- **Effort:** High · **Risk:** High (cross-layer, server behaviour, performance of total-count) · **Blast
  radius:** Contracts + server + UI across the Tenants submodule.
- **Delivers:** exact docs parity (`FluentPaginator`), but at disproportionate cost for a UI-styling ask.

### Option C — Rollback / MVP review
N/A. There is nothing to revert to; MVP is not threatened.

---

## Section 4 — Detailed Change Proposals (Option A)

### 4.1 Packaging
```xml
<!-- Hexalith.Tenants/Directory.Packages.props -->
<PackageVersion Include="Microsoft.FluentUI.AspNetCore.Components.Icons" Version="5.0.0-rc.3-26138.1" />
```
```xml
<!-- Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj -->
<PackageReference Include="Microsoft.FluentUI.AspNetCore.Components.Icons" />
```

### 4.2 Cell-content mapping (canonical)
| Current | Fluent v5 | Notes |
|---|---|---|
| `<span class="…status…">Active</span>` | `<FluentBadge Color="BadgeColor.Success" Appearance="BadgeAppearance.Tint">` | color by state; keep `data-testid`/`aria-label` |
| letter icons `O/C/R/OK/!/…` | `<FluentIcon Value="@(new Icons.Regular.Size16.X())" />` as `IconStart` | real glyphs; `aria-hidden` handled by badge `IconLabel` |
| `TruthStateBadge` `<span>`+text | `FluentBadge` + `FluentIcon` | keep `role="status"`, `aria-label`, `data-testid` |
| `SupportSafeCopyButton` `<span>C</span>` | `FluentIcon` (Copy) in the existing `FluentButton` | behaviour unchanged |
| `TenantDataGrid` `<a class="…detail-link">` | `<FluentAnchor Href=…>` | keep `data-testid`, `aria-label`, strong child |

**State → BadgeColor / Icon (proposed):**
- Status: `Active`→Success/Checkmark · `Disabled`→Danger/Prohibited · `Unknown`→Subtle/Question
- Role: `TenantOwner`→Brand/Star · `Contributor`→Informative/Edit · `Reader`→Subtle/Eye
- Freshness: `Current`→Success/Checkmark · `Refreshing`→Informative/ArrowSync · `Aging`/`Stale`→Warning/Warning · `Unknown`→Subtle/Question
- Audit category: `Access`→Informative · `Administrative`→Brand

**Invariants (every conversion):** preserve `data-testid`; preserve `@Localizer[…]` keys; preserve
`aria-label`/`role`; no behaviour change; `JSInterop.Mode = Loose` in any bUnit test that now renders
icon/badge components.

### 4.3 In-grid sort (TenantDataGrid only)
Make the Tenant (id/name) and Status column headers sortable; on header sort, set `_sortColumn`/
`_sortDescending` and call the existing server `LoadAsync`. Remove the `tenants-list-sort` and
`tenants-list-sort-direction` FluentSelects from `TenantsWorkspace`. Keep the sort hooks server-side so
multi-page correctness is preserved.

### 4.4 Tests
- Migrate DOM-shape assertions to the Fluent node names (`FLUENT-BADGE`/`FLUENT-ANCHOR`/`FLUENT-ICON`),
  preferring `data-testid` lookups; add Loose JSInterop where needed (same recipe as 2026-06-09).
- Confirm `DomainUiFluentConformanceTests` still green. Update any affected `.verified.txt` snapshots intentionally.
- Re-point `TenantsWorkspace` sort tests from the removed dropdowns to the new sortable headers.

---

## Section 5 — Implementation Handoff

**Scope classification: Moderate** (multi-file UI + tests in one submodule; no epic replan). Option B
would escalate to **Major** (contract + server replan; ADR required).

- **Primary → Developer agent:** implement Option A in `Hexalith.Tenants.UI` + update
  `Hexalith.Tenants.UI.Tests`. Build `Hexalith.Tenants.UI.csproj -c Release` clean (TWAE) and run the
  Tenants.UI test suite green with `DiffEngine_Disabled=true`.
- **Sequencing:** (1) add Icons package + convert `TruthStateBadge` & `SupportSafeCopyButton` (shared,
  highest reuse) → (2) `TenantDataGrid` cell content + in-grid sort + drop sort dropdowns → (3)
  `MyTenantsDataGrid` → (4) `AuditDataGrid` → (5) green the tests → (6) live visual check under Aspire.
- **Do NOT commit** unless explicitly requested (submodule changes propagate ecosystem-wide).

**Success criteria (Option A):**
- ✅ 0 raw `<span>`-as-badge / letter-icon / raw `<a>` in the three grids + two shared components.
- ✅ `FluentBadge`/`FluentIcon`/`FluentAnchor` render with preserved `data-testid` + a11y labels.
- ✅ TenantDataGrid header sort drives the **server** sort (still global across pages).
- ✅ Tenants.UI Release build clean; Tenants.UI tests green; governance guard green.
- ⏳ Live visual check under Aspire (Fluent styling, no native grey, sortable headers, pagination).

---

## Implementation complete (2026-06-16, LEAN path)

**Result:** `Hexalith.Tenants.UI.Tests` **670 / 670 GREEN** (`-c Release`, `DiffEngine_Disabled=true`), incl.
the `DomainUiFluentConformanceTests` governance guard. **Release build TWAE-clean: 0 warnings / 0 errors.**
Changes left **uncommitted** per request.

### What changed
- **Icons (key finding):** the Fluent v5 RC has **no compatible Icons NuGet** — `…Components.Icons`
  stops at `4.14.2` (a v4 build whose icon classes hard-bind to `Components 4.14`; they do **not**
  surface against v5's changed `Icon` type — verified by 4 clean builds, all `CS0246`). Real glyphs are
  therefore provided via FrontComposer's own embedded-SVG factory **`FcFluentIcons`** (the project's
  established mechanism), extended with 10 new 16px glyphs (Checkmark, SubtractCircle, QuestionCircle,
  Warning, ArrowSync, Star, Edit, Eye, Key, Copy). Administrative-category reuses the existing
  `Settings20`. No new package dependency.
- **Cell content → Fluent** (Tenants submodule):
  - `TruthStateBadge` → `FluentBadge` (color by freshness) + `FcFluentIcons` glyph.
  - `SupportSafeCopyButton` → `FluentButton` `IconStart` = `Copy16` (replaced the `"C"` letter).
  - `TenantDataGrid` status `<span>` → `FluentBadge`; detail link kept as inline `<a>` (allowed by the
    governance guard; `FluentAnchor` would restyle it as a button and risk the href contract).
  - `MyTenantsDataGrid` role/status `<span>` badges + letter-icons (`O/C/R/OK/!`) → `FluentBadge` + glyphs.
  - `AuditDataGrid` category `<span>` → `FluentBadge` + glyph.
- **In-grid sort (replaces the comboboxes):** `TenantDataGrid` Tenant (by name) and Status columns are
  `Sortable` with `GridSort<TenantListRow>`; the two sort/direction `FluentSelect`s were removed from
  `TenantsWorkspace`. Behaviour is identical to before (client-side, page-local — the server already
  ignored sort). `_sortColumn`/`_sortDescending` default-order + URL nav-context plumbing unchanged.
- **Pagination:** unchanged (cursor-based Prev/Next `FluentButton`s).

### Tests
- Sort test reworked to drive the grid (`grid.SortByColumnAsync("Tenant", Descending)`) — passes.
- **Fluent-token vs. safety-guard collision (resolved):** Fluent design tokens surface forbidden
  substrings in attribute/style values — `color="success"` / `--colorStatusSuccessForeground1` (→ "Success")
  and `--colorNeutralForegroundOnBrand` (→ "undo"). The 9 colliding `cut.Markup.ShouldNotContain(…)`
  safety guards were narrowed to **visible text** via a new `cut.VisibleText()` test helper
  (`RenderedFragmentTextExtensions`), preserving the safety intent (no success/undo word *shown to the user*).

### Files changed (uncommitted)
- **Tenants submodule:** `Directory.Packages.props`, `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj`,
  `Components/_Imports.razor`, `Components/Shared/{TruthStateBadge,SupportSafeCopyButton}.razor`,
  `Components/Tenants/TenantDataGrid.razor`, `Components/Users/MyTenantsDataGrid.razor`,
  `Components/Tenants/Audit/AuditDataGrid.razor`, `Components/Pages/TenantsWorkspace.razor`; tests:
  new `RenderedFragmentTextExtensions.cs` + 6 test files (sort test, 9 guard refinements, 1 pre-existing
  nav-title fix `Tenants`→`All tenants`).
- **FrontComposer repo:** `src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs` (10 new glyphs).

### Residuals
1. **Live visual check** under Aspire — confirm the hand-authored glyphs render correctly and badges are
   Fluent-styled (the main visual risk, since the SVG glyphs are hand-drawn). Aspire stack is currently down.
2. **Pre-existing, unrelated:** `TenantsUiCompositionTests` expected the nav title `"Tenants"` but the
   registration intentionally renders `"All tenants"` (commit `51b5b42`). Corrected the stale expectation to
   keep the suite green — flagged because it is outside this change's scope.
