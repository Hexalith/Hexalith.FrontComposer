---
title: "DataGrid Surface"
description: "The confirmed FC-TBL table surface: generated FluentDataGrid views, filtering, status badges, row detail, column prioritization, and live read-path notices."
genre: reference
audience: adopter
ownerStory: 2-8-confirm-the-fc-tbl-table-api-contract
status: published
reviewed: 2026-08-12
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

`FcNewItemIndicator` producer wiring uses an explicit command-to-projection `[CommandTarget]`
declaration. The FC-NIP base record was created on 2026-07-04; its decision was approved and the
record updated on 2026-07-05. Those dates are distinct chronology, not references to two contracts.
`SameAsSource` is valid only for `Update` and copies the generated row snapshot once
immediately before dispatch. `Provider` resolves create, cross-row update, status-move, and delete
identity through exactly one `ICommandTargetIdentityProvider<TCommand>`. A fixed declaration
`ViewKey`, and every provider-returned view key, must equal the declared projection's canonical
generated view key; mismatches fail closed with no fallback to a route or visible lane.

Only a confirmed or idempotent-confirmed `Material` terminal observation can publish an indicator.
Delete, rejection, `NoOp`, `Unknown`, invalid time/scope, or unresolved/conflicting identity suppresses
publication without changing command dispatch or lifecycle. Every terminal adapter routes through the
single pending-outcome resolver boundary; projection nudges and ambient row placement are never target
or materiality evidence. The resolver makes the indicator publication or non-publication decision at
most once per accepted `MessageId`.

Target resolution emits one redacted completion event per non-cancelled attempt in the generated
`<Command>Form` logger category: Warning 5912 (`CommandFormTargetResolutionFailed`) carries only a
closed framework-owned failure category, while Information 5913
(`CommandFormTargetResolutionSucceeded`) is payload-free. Neither event includes command values,
target/view/entity/status/scope values, exception text, or other adopter data, and logging-provider
failures do not alter command dispatch or lifecycle.

For one generated form category and observation window, operators calculate the suppression rate as
`count(5912) / (count(5912) + count(5913))`. Both Information 5913 and Warning 5912 must be retained
for that category and window; if either level is filtered, or if there are zero completions, no rate
is available.

If lifecycle delivery fails after the first terminal outcome is committed, FrontComposer preserves
that immutable terminal truth and queues bounded circuit-local convergence work. A duplicate terminal
observation or the polling coordinator can retry the lifecycle state from the stored terminal entry;
the retry runs before status transport polling and never re-queries command status, changes the
outcome, or retries the indicator decision. Convergence completes only when both lifecycle state and
`MessageId` match the stored terminal entry, and it remains bounded by the configured pending-entry
capacity, polling duration, and per-tick polling budget.

Reserved filter keys remain framework-owned: `__status` for status filters, `__search` for in-grid
search, and `__hidden` for hidden-column persistence. Column filter keys beginning with `__` are
rejected.

## Layout (FC-LYT)

Generated grid views render a `data-fc-datagrid` envelope around a table-mode `FluentDataGrid<T>`.
Wide projections activate `FcColumnPrioritizer` when the generated grid has more than 15 columns;
the prioritizer exposes a column-visibility popover while preserving the generated column order.
`[ColumnPriority(n)]` sorts columns by priority value first and declaration order second, with
unannotated columns trailing in declaration order.

Expand-in-row detail is rendered outside the virtualized grid body so the detail region remains
stable even as rows virtualize. Filter summaries, filter-empty state, slow-query notices, and
max-item notices are rendered as grid-adjacent status surfaces rather than cells.

## Accessibility (FC-A11Y)

- `FcColumnFilterCell`, `FcFilterResetButton`, and `FcStatusFilterChips` expose labelled controls and
  dispatch Fluxor actions instead of mutating grid state directly.
- `FcFilterSummary`, `FcFilterEmptyState`, `FcExpandedRowHiddenBanner`, and `FcNewItemIndicator`
  render status regions for assistive technology.
- `FcExpandInRowDetail` always renders a `role="region"` panel. Expand triggers point
  `aria-controls` at that always-present panel, and filter-hidden expansions are announced through a
  polite live region.
- Generated `[ProjectionBadge]` status cells render `FcStatusIcon` with a contextual accessible
  label and hover/focus tooltip. Numeric counts, filter chips, and optimistic command summaries
  remain `FluentBadge` pill surfaces.

Adopter overrides of table components are still subject to the override-accessibility diagnostics,
especially [HFC1050](../../diagnostics/HFC1050.md), [HFC1051](../../diagnostics/HFC1051.md), and
[HFC1053](../../diagnostics/HFC1053.md).

## Localization (FC-L10N)

Framework-owned table chrome resolves through `IStringLocalizer<FcShellResources>`: filter summary
copy, reset labels, status notices, row-detail labels, and live-region announcements are shell-owned.
Domain labels such as projection names and column titles remain host-owned through projection metadata
and `[Display(Name=...)]`.

## Related

- [Navigation](navigation.md) - registry-driven discovery of projection pages.
- [FrontComposerShell](front-composer-shell.md) - shell layout, shortcuts, and status chrome.
- [Components](index.md) - component reference index.
- [Generated output](../generated-output.md) - generator output path and inspection guidance.
