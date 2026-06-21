# FC-LST / FC-DTL Aggregate List & Detail Page Contract

Date: 2026-06-21

Story: cc-2026-06-21-frontcomposer-aggregate-list-detail-extraction (Hexalith.Tenants Phase 2 Correct Course)

## Purpose

Extract the reusable operational list/detail page chrome out of the already-working Hexalith.Tenants
list and detail surfaces into FrontComposer as two domain-agnostic building blocks:

- `FcAggregateListPage<TItem>` (FC-LST) — list/browse page chrome.
- `FcAggregateDetailPage<TItem>` (FC-DTL) — aggregate detail page chrome.

Both compose existing FrontComposer/Fluent primitives only (`FcPageLayout`, `FcPageHeader`, `FluentStack`).
They own **no** domain copy, query gateway, `IQueryService`, freshness/ETag, search, or Fluxor projection
model. The consuming domain keeps all query/freshness/search/safety semantics and passes localized strings
and `RenderFragment` slots in.

## Public surface

| Type | Location | Disposition |
|---|---|---|
| `FcAggregateListPage<TItem>` | `src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor(.cs)` | public component |
| `FcAggregateDetailPage<TItem>` | `src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateDetailPage.razor(.cs)` | public component |
| `FcAggregateDetailState` | `src/Hexalith.FrontComposer.Contracts/Rendering/FcAggregateDetailState.cs` | public enum |

The components are intentionally **public package surface** of `Hexalith.FrontComposer.Shell` so domain
modules (Tenants today, others later) can consume them, exactly like the existing FC-TBL DataGrid
components and `FcPageHeader` / `FcPageLayout`. They live in the `Hexalith.FrontComposer.Shell.Components.Layout`
namespace, which is **outside** the focused FC-TBL baseline (`PublicAPI.FcTbl.Shipped.txt`,
`Components.DataGrid`), so this contract does not change the FC-TBL baseline. The public parameter surface is
pinned intentionally by `FcAggregateListPageTests` / `FcAggregateDetailPageTests` (including the
"does not inject domain resources" reflection guard). No type was internalized.

## FC-LST — `FcAggregateListPage<TItem>`

Composes, top to bottom: `FcPageLayout(Mode=LayoutMode)` → vertical `FluentStack` root (`RootTestId`,
`RootClass`, `RootGap`) → `FcPageHeader` (title/heading/eyebrow/description/headingId/tabindex), with:

- `Toolbar` rendered in the header **Actions** slot (the page toolbar: refresh/reset/navigation commands).
- `HeaderMetadata` rendered in the header **Metadata** slot (return context).

Below the header, in order: `Filters` → `Commands` → `States` → `Body` → `Pager` → `ChildContent`.

- `LayoutMode` defaults to `FcPageLayoutMode.FullWidth` (dense DataGrid-first list surface).
- `FocusHeadingAsync()` forwards to the composed `FcPageHeader` for return-context focus restoration.
- `OnItemSelected` (`EventCallback<TItem>`) is an optional typed row-navigation hook a domain row template
  can raise via `NotifyItemSelectedAsync(TItem)`. Domains that navigate with anchor links need not use it.

Non-goals: it does not own a grid, columns, search/filter values, query gateway, the generated projection
Fluxor search model, paging logic, or any domain copy. FC-TBL primitives (`FcProjectionGlobalSearch`,
`FcStatusFilterChips`, `FcFilterEmptyState`, …) remain available for domains that fit the generated
projection model; a server-side-BFF domain (Tenants) supplies its own search/filter controls into `Filters`.

## FC-DTL — `FcAggregateDetailPage<TItem>`

Composes `FcPageLayout(Mode=LayoutMode)` → vertical `FluentStack` root → optional back link
(`BackHref`/`BackLinkLabel`/`BackLinkTestId`/`BackLinkClass`/`ShowBackLink`), then **routes** the
`FcAggregateDetailState`:

| State | Renders |
|---|---|
| `Loading` | `LoadingContent` (only) |
| `Unauthorized` | `UnauthorizedContent` (only) |
| `NotFound` | `NotFoundContent` (only) |
| `Unavailable` | `UnavailableContent` (only; also the fail-closed default) |
| `Ready` | ready body |
| `Stale` | `StaleBanner` above the ready body |
| `Degraded` | `DegradedBanner` above the ready body |

The ready body is `ReadyTemplate(Item)` when both are supplied, otherwise `ReadyContent`. The ready body is
**never** rendered for a non-ready state, and a missing specific state slot fails closed to
`UnavailableContent` — so a non-ready surface can never be dressed as success (non-collapsing states).

- `LayoutMode` defaults to `FcPageLayoutMode.Constrained` (readable detail measure).
- The consuming domain supplies all `FcPageHeader` instances (per-state and the ready identity header, with
  the header's own Actions/Metadata slots), all copy, the facts/sections markup, the accordion grouping, and
  the command flows. The domain maps its own snapshot kind onto `FcAggregateDetailState` (for example Tenants
  maps `Unknown` → `Unavailable`).

Non-goals: it does not own headings, copy, sections, accordion content, command flows, evidence providers, or
return-URL validation. The back link href/label are caller-validated and caller-localized.

## Consumer boundary (Tenants)

- `TenantsWorkspace.razor` consumes `FcAggregateListPage<TenantListRow>`; `TenantDetailPage.razor` consumes
  `FcAggregateDetailPage<TenantDetail>`.
- Tenants keeps `ITenantQueryGateway` (server-side BFF), Memories-backed cross-set search (index-only),
  ETag/freshness, cursor paging, support-safe copy, audit entry points, and all `data-testid` selectors.
- The two Tenants governance guards `Domain_route_pages_declare_frontcomposer_page_headers` and
  `Domain_page_components_declare_frontcomposer_page_layout_modes` were broadened to recognize the wrappers as
  the FrontComposer-owned header/layout declaration (a page may declare them via `<FcPageHeader>`/`<FcPageLayout>`
  **or** via `<FcAggregateListPage>`/`<FcAggregateDetailPage>` with `LayoutMode="FcPageLayoutMode.*"`). No raw
  `<PageTitle>` / `<h1>` / `<main>` allowance was added.

## Test evidence

- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcAggregateListPageTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcAggregateDetailPageTests.cs`

Run with `DiffEngine_Disabled=true`.
