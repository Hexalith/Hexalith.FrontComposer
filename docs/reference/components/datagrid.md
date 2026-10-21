---
title: "DataGrid Surface"
description: "The confirmed FC-TBL table surface: generated FluentDataGrid views, filtering, status badges, row detail, column prioritization, and live read-path notices."
genre: reference
audience: adopter
ownerStory: 2-8-confirm-the-fc-tbl-table-api-contract
status: published
reviewed: 2026-10-21
uid: frontcomposer.reference.components.datagrid
slug: reference/components/datagrid/
---

# DataGrid Surface

## Overview

The DataGrid surface is the confirmed **FC-TBL** table contract for read-only projection pages. The
adopter entry point is still the generated projection view: annotate a `partial` read model with
`[Projection]`, and FrontComposer emits a `FluentDataGrid<T>` page that composes the public DataGrid
components below. This public surface is pinned by
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
| `FcNewItemIndicator` | Accessible fresh-row indicator component. | `Text`, `AriaLabelOverride`, `Entry`, `SharedStatusOwner`. |

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

With `Entry`, `FcNewItemIndicator` displays an info icon and the localized “Updated row:
{EntityKey}.” cue. `Text` replaces only that visible copy; speech always uses the canonical localized
message, and `AriaLabelOverride` is ignored. Callers without `Entry` keep the original
`role="status"`, `aria-live="polite"`, live label, and copy. Generated grids now pass `Entry` with
`SharedStatusOwner=true`, so their cues no longer render `role="status"`, `aria-live`, `aria-label`,
or the previous “New item” copy. Host styles or tests that targeted those attributes should select the
`data-testid="fc-new-item-indicator"` hook or `fc-new-item-indicator` class instead. Entry-aware cues retain
the resolved scope in which their entry was first observed. Direct entries expire ten seconds after
`CreatedAt`; generated entries retain the state service's ten-second window from publication. Invalid
or unresolved provenance suppresses the cue before speech is claimed.

Register the Shell with `AddHexalithFrontComposer` before rendering a direct entry-aware cue; this
provides its clock, announcement coordinator, and scope ledger.

When an entry belongs to an `INewItemIndicatorStateService` snapshot at its first render, the cue uses
that service's lifetime and mutation subscription. Call `Add` before rendering the entry to select this
lifecycle; adding an equal entry afterward does not transfer a direct cue to state-service ownership.
Custom state services must implement `Subscribe` and notify
subscribers after removal, expiry, and scope clearing so mounted cues update automatically. The
interface's compatibility default sends no notifications.

Reserved filter keys remain framework-owned: `__status` for status filters, `__search` for in-grid
search, and `__hidden` for hidden-column persistence. Column filter keys beginning with `__` are
rejected.

Complete component parameter reference follows. Internal Fluent children are implementation details.

### `FcColumnFilterCell`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key used by the filter action. |
| `ColumnKey` | `string` | Declared property name of the filtered column. |
| `ColumnHeader` | `string?` | Humanised column header, used in placeholder / aria-label templates. |
| `InitialValue` | `string?` | Initial filter value supplied from the hydrated snapshot. |

### `FcColumnPrioritizer`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key. |
| `AllColumns` | `IReadOnlyList<ColumnDescriptor>` | Full list of columns in priority-then-declaration order (post-Transform sort); the source generator materialises this from the `Transforms.ColumnModel` IR. |
| `HiddenColumns` | `IReadOnlyList<string>` | Currently hidden column keys (property names). Typically bound to the CSV under GridViewSnapshot.Filters["__hidden"]. |
| `MaxVisibleColumns` | `int` | Maximum columns visible by default; generated grids supply 10. |
| `ChildContent` | `RenderFragment<ColumnVisibilityContext>` | Render fragment for the wrapped FluentDataGrid. Receives a `ColumnVisibilityContext` so the data grid can skip rendering hidden columns. |

### `FcExpandInRowDetail`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable key identifying the expanded-detail view, carried for consistency with sibling components. |
| `HasExpanded` | `bool` | True when the host view has resolved an expanded item for this view-key. Drives the panel render gate. |
| `ChildContent` | `RenderFragment` | The detail body emitted by the generator (factored EmitDetailRecordBody output). |
| `DetailPanelAriaLabel` | `string` | Localized aria-label for the outer region landmark (per-row item-name interpolated). |
| `SuppressedAnnouncement` | `string?` | Polite announcement supplied when an active expansion is hidden by a filter. Empty or null text suppresses the announcement. |
| `SuppressStandaloneAnnouncement` | `bool` | Uses the generated view's shared status instead of a standalone live node. |
| `PanelId` | `string?` | Gets or sets the deterministic element id consumed by the trigger button's aria-controls attribute. When omitted, the component falls back to an instance-local id for standalone use. |

### `FcExpandedRowHiddenBanner`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable persisted view key, matching the key used by `FiltersResetAction`. |
| `IsHiddenByFilter` | `bool` | True when the host view detects an expanded row that the current filter predicate has hidden from state.Items. Drives both the render gate and the suppression announcement; false renders nothing. |
| `IsMissing` | `bool` | Whether the expanded item is absent from the completed visible result, regardless of cause. |
| `ResultIdentity` | `string?` | Stable filter/result identity for one hidden-detail transition. |
| `ScopeGeneration` | `long` | Generation of the current tenant and user scope. |
| `ResultSettled` | `bool` | Whether the filter result that hides this detail has completed. |
| `SharedStatusOwner` | `bool` | Uses the generated projection's existing shared polite status node. |

### `FcFilterEmptyState`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key. |
| `TotalCount` | `int` | Total row count pre-filter. |
| `EntityPlural` | `string` | Humanised entity plural. |
| `ActiveFilterCount` | `int` | Number of currently active filters (for the reset button's aria-label). |

### `FcFilterResetButton`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key. |
| `ActiveFilterCount` | `int` | Number of currently active filters; interpolated into the aria-label. |
| `HasActiveFilters` | `bool` | Whether any filter is active (controls the disabled state). |

### `FcFilterSummary`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key (reserved for future use). |
| `Filters` | `IReadOnlyDictionary<string, string>` | Column-keyed filters including the reserved __status / __search keys. |
| `HumanisedColumnHeaders` | `IReadOnlyDictionary<string, string>?` | Column keys → humanised header text. |
| `SortColumn` | `string?` | Column key currently sorted by, or null. |
| `SortDescending` | `bool` | Whether the current sort is descending. |
| `FilteredCount` | `int` | Row count matching the current filters. |
| `TotalCount` | `int` | Total row count pre-filter. |
| `EntityPlural` | `string` | Humanised entity plural (e.g. "orders"). |

### `FcMaxItemsCapNotice`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key. It forms this notice's announcement surface. |
| `ItemsCount` | `int` | Current unfiltered row count in the view's state. |
| `AnyRealFilterActive` | `bool` | Whether any real column filter / search / status chip is active. The generator-emitted view computes this from column filters, search, and status chips while excluding display-only reserved keys such as hidden columns. |
| `ResultIdentity` | `string?` | Identity of the completed result that reached the configured limit. |
| `ScopeGeneration` | `long` | Generation of the current tenant and user scope. |
| `SharedStatusOwner` | `bool` | Uses the parent surface's persistent status node when true; direct callers own one by default. |

### `FcNewItemIndicator`

| Parameter | Type | Contract |
|---|---|---|
| `Text` | `string?` | Optional localized fresh-row indicator text. |
| `AriaLabelOverride` | `string?` | Optional complete accessible name for the fresh-row indicator. |
| `Entry` | `NewItemIndicatorEntry?` | Fresh-row provenance. When supplied, names the entity and announces through the shared channel. |
| `SharedStatusOwner` | `bool` | Uses the parent's persistent status node when true; entry-aware direct callers own one by default. |

### `FcProjectionGlobalSearch`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key. |
| `InitialValue` | `string?` | Initial query seeded from hydrated snapshot. |

### `FcSlowQueryNotice`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key ({boundedContext}:{projectionTypeFqn}). |
| `VisiblePageSkip` | `int?` | Displayed page offset from the grid's actual scroll position; null keeps standalone view-wide behavior. |
| `SharedStatusOwner` | `bool` | Uses the parent surface's persistent status node when true; direct callers own one by default. |

### `FcStatusFilterChips`

| Parameter | Type | Contract |
|---|---|---|
| `ViewKey` | `string` | Stable per-view key used by the filter action. |
| `AvailableSlots` | `IReadOnlyList<BadgeSlot>` | Slots to render (distinct set from state.Items / chip definition). |
| `ActiveSlots` | `IReadOnlyList<BadgeSlot>` | Slots currently marked active (filled appearance + aria-pressed="true"). |

### `ColumnVisibilityContext`

The prioritizer supplies this typed context to `ChildContent`. `IsHidden(string columnKey)` returns whether that case-sensitive key is hidden; use it to omit the matching column. The context exposes no public properties.

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
- `FcFilterSummary`, `FcFilterEmptyState`, and `FcExpandedRowHiddenBanner` render status regions
  for assistive technology. `FcNewItemIndicator` renders its own status region when `Entry` is omitted.
- An entry-aware `FcNewItemIndicator` cue is not itself a live region; its icon is hidden from
  assistive technology. The localized updated-row message is delivered through the single projection
  status node once per tenant, user, entity, and message across views and remounts during the active
  ten-second window. Expiry prevents the original direct entry from announcing afterward; the resolver
  separately makes at most one publication decision per accepted `MessageId`. Generated grids
  share their grid's status node; direct entry-aware callers own one by default. Set `SharedStatusOwner=true`
  only when the page already renders a status node for that same projection surface, as generated grids do.
  With the built-in announcement coordinator, removal, expiry, and dismissal do not announce the removed
  row. Already delivered status text restores silently; a previously unspoken pending projection outcome
  can still announce under its normal precedence rules. These cancellation and replay guarantees use
  delivery metadata supplied by the built-in coordinator. Wrappers preserve it when they forward its
  subscription callbacks synchronously. An independent `ISurfaceAnnouncementCoordinator` implementation
  needs matching status-rendering support; implementing the reserved `fresh-row:` family alone does not
  provide that metadata to `FcSurfaceStatus`.
- Inside `FcPageTabs`, a cue in an inactive panel waits until its panel and every containing panel
  are active before claiming its first announcement, and a reactivated panel shows its retained status
  without speaking it again. This deferral applies only to `FcPageTabs` panels; a cue hidden any other way
  claims speech as soon as it renders.
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
