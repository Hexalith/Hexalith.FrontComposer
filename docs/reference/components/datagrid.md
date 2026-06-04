---
title: "DataGrid Surface"
description: "The confirmed FC-TBL table surface: generated FluentDataGrid views, filtering, status badges, row detail, column prioritization, and live read-path notices."
genre: reference
audience: adopter
ownerStory: 2-8-confirm-the-fc-tbl-table-api-contract
status: published
reviewed: 2026-06-04
uid: frontcomposer.reference.components.datagrid
slug: reference/components/datagrid/
---

# DataGrid Surface

## Overview

The DataGrid surface is the confirmed **FC-TBL** table contract for read-only projection pages. The
adopter entry point is still the generated projection view: annotate a `partial` read model with
`[Projection]`, and FrontComposer emits a `FluentDataGrid<T>` page that composes the public DataGrid
components below. Story 2.8 froze this public surface with
`src/Hexalith.FrontComposer.Shell/PublicAPI.FcTbl.Shipped.txt` and
`FcTblPackageBoundaryTests`, so component additions, removals, and parameter changes are intentional
package-boundary events.

Most adopters do not place these components by hand. Use the attributes and generated view first;
use the public components directly only when building custom projection chrome or Level-2/Level-3
customizations around the generated grid.

## Usage

Generated projection pages are the normal usage path:

```csharp no-compile reason="illustrative projection contract"
[Projection]
[ProjectionRole(ProjectionRenderStrategy.Default)]
public partial class OrderProjection
{
    [Display(Name = "Status")]
    public OrderStatus Status { get; init; }
}
```

For hand-authored customizations, keep the same view key used by the generated projection lane so
filter state, column visibility, row expansion, and notices resolve against the same Fluxor state:

```razor
<FcFilterSummary ViewKey="@viewKey"
                 EntityPlural="orders"
                 Filters="@filters"
                 HumanisedColumnHeaders="@headers"
                 FilteredCount="@filteredCount"
                 TotalCount="@totalCount" />

<FcColumnPrioritizer ViewKey="@viewKey"
                     AllColumns="@columns"
                     HiddenColumns="@hiddenColumns">
    @((visibility) => @<FluentDataGrid Items="@rows" />)
</FcColumnPrioritizer>
```

## Parameters / slots

The focused FC-TBL public baseline currently covers these public types under
`Hexalith.FrontComposer.Shell.Components.DataGrid`:

| Type | Purpose | Key surface |
|---|---|---|
| `FcColumnFilterCell` | Per-column filter input. | `ViewKey`, `ColumnKey`, `ColumnHeader`, `InitialValue`. |
| `FcFilterSummary` | Active filter/search/sort summary. | `Filters`, `HumanisedColumnHeaders`, `FilteredCount`, `TotalCount`, `SortColumn`, `SortDescending`. |
| `FcFilterResetButton` | Clear active filters. | `ViewKey`, `HasActiveFilters`, `ActiveFilterCount`. |
| `FcFilterEmptyState` | Distinct filtered-to-zero state. | `ViewKey`, `ActiveFilterCount`, `EntityPlural`, `TotalCount`. |
| `FcStatusFilterChips` | Status-slot toggles. | `ViewKey`, `AvailableSlots`, `ActiveSlots`. |
| `FcProjectionGlobalSearch` | In-grid row search. | `ViewKey`, `InitialValue`. |
| `FcColumnPrioritizer` | Wide-grid column visibility wrapper. | `ViewKey`, `AllColumns`, `HiddenColumns`, `MaxVisibleColumns`, `ChildContent`. |
| `ColumnDescriptor` | Public column descriptor for prioritization. | `Key`, `Header`, `Priority`. |
| `ColumnVisibilityContext` | Child-content visibility helper. | `IsHidden(string columnKey)`. |
| `FcExpandInRowDetail` | Always-present row-detail region. | `ViewKey`, `PanelId`, `HasExpanded`, `DetailPanelAriaLabel`, `SuppressedAnnouncement`, `ChildContent`. |
| `FcExpandedRowHiddenBanner` | Live notice when a filter hides an expanded row. | `ViewKey`, `IsHiddenByFilter`. |
| `FcSlowQueryNotice` | Slow-query grid notice. | `ViewKey`. |
| `FcMaxItemsCapNotice` | Max-items cap notice. | `ViewKey`, `ItemsCount`, `AnyRealFilterActive`, `Visible`. |
| `FcNewItemIndicator` | Accessible fresh-row indicator component. | `Text`, `AriaLabelOverride`. |

Reserved filter keys remain framework-owned: `__status` for status filters, `__search` for in-grid
search, and `__hidden` for hidden-column persistence. Column filter keys beginning with `__` are
rejected.

## Layout

Generated grid views render a `data-fc-datagrid` envelope around a table-mode `FluentDataGrid<T>`.
Wide projections activate `FcColumnPrioritizer` when the generated grid has more than 15 columns;
the prioritizer exposes a column-visibility popover while preserving the generated column order.
`[ColumnPriority(n)]` sorts columns by priority value first and declaration order second, with
unannotated columns trailing in declaration order.

Expand-in-row detail is rendered outside the virtualized grid body so the detail region remains
stable even as rows virtualize. Filter summaries, filter-empty state, slow-query notices, and
max-item notices are rendered as grid-adjacent status surfaces rather than cells.

## Accessibility

- `FcColumnFilterCell`, `FcFilterResetButton`, and `FcStatusFilterChips` expose labelled controls and
  dispatch Fluxor actions instead of mutating grid state directly.
- `FcFilterSummary`, `FcFilterEmptyState`, `FcExpandedRowHiddenBanner`, and `FcNewItemIndicator`
  render status regions for assistive technology.
- `FcExpandInRowDetail` always renders a `role="region"` panel. Expand triggers point
  `aria-controls` at that always-present panel, and filter-hidden expansions are announced through a
  polite live region.
- `FcStatusBadge` and `FcDesaturatedBadge` receive status column context so the generated badge
  cells have mandatory accessible labels.

Adopter overrides of table components are still subject to the override-accessibility diagnostics,
especially [HFC1050](../../diagnostics/HFC1050.md), [HFC1051](../../diagnostics/HFC1051.md), and
[HFC1053](../../diagnostics/HFC1053.md).

## Localization

Framework-owned table chrome resolves through `IStringLocalizer<FcShellResources>`: filter summary
copy, reset labels, status notices, row-detail labels, and live-region announcements are shell-owned.
Domain labels such as projection names and column titles remain host-owned through projection metadata
and `[Display(Name=...)]`.

## Related

- [Navigation](navigation.md) - registry-driven discovery of projection pages.
- [FrontComposerShell](front-composer-shell.md) - shell layout, shortcuts, and status chrome.
- [Components](index.md) - component reference index.
- [Generated output](../generated-output.md) - generator output path and inspection guidance.
