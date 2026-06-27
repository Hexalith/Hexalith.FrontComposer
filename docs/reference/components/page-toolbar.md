---
title: "Page Toolbar"
description: "Reusable FrontComposer page toolbar for search, filters, view menus, actions, and optional page tabs."
genre: reference
audience: adopter
ownerStory: 8-6-reusable-fcpagetoolbar
status: published
reviewed: 2026-06-25
uid: frontcomposer.reference.components.pagetoolbar
slug: reference/components/page-toolbar/
---

# Page Toolbar

## Overview

`FcPageToolbar` is the reusable page-level tool row for FrontComposer pages. It gives adopters one
consistent place for search, optional filters, optional view or overflow actions, right-aligned page
actions, and optional tabs. The component is intentionally generic: callers own labels, values,
callbacks, filter panel content, menu items, actions, and tab selection.

Use it through an existing page slot such as `FcAggregateListPage.Toolbar`, or place it near a
standalone `FcPageHeader` when a custom page needs the same toolbar pattern.

## Usage

Render the toolbar through the aggregate-list toolbar slot:

```razor
<FcAggregateListPage TItem="OrderRow"
                     Heading="Orders"
                     Toolbar="@OrderToolbar"
                     Body="@OrderGrid" />

@code {
    private string? search;
    private string? activeTab = "all";

    private RenderFragment OrderToolbar => @<FcPageToolbar SearchValue="@search"
                                                           SearchValueChanged="@OnSearchChanged"
                                                           SearchPlaceholder="Search orders"
                                                           SearchAriaLabel="Search orders"
                                                           FilterLabel="Filters"
                                                           FilterContent="@OrderFilters"
                                                           ViewMenuLabel="View"
                                                           ViewMenuContent="@ViewMenu"
                                                           Actions="@OrderActions"
                                                           Tabs="@Tabs"
                                                           ActiveTabId="@activeTab"
                                                           ActiveTabIdChanged="@OnTabChanged" />;

    private IReadOnlyList<FcPageToolbarTab> Tabs { get; } =
    [
        new("all", "All"),
        new("open", "Open"),
        new("archived", "Archived"),
    ];

    private Task OnSearchChanged(string? value)
    {
        search = value;
        return Task.CompletedTask;
    }

    private Task OnTabChanged(string? value)
    {
        activeTab = value;
        return Task.CompletedTask;
    }
}
```

## Parameters / slots

| Parameter | Type | Purpose |
|---|---|---|
| `TestId` | `string` | Stable selector for the toolbar row. Defaults to `fc-page-toolbar`. |
| `AriaLabel` | `string` | Accessible name for the row with `role="toolbar"`. |
| `Class` | `string?` | Optional root CSS class for page-specific layout hooks. |
| `SearchValue` / `SearchValueChanged` | `string?` / `EventCallback<string?>` | Caller-owned search state. |
| `SearchPlaceholder` | `string?` | Search input placeholder text. |
| `SearchAriaLabel` | `string` | Search input accessible name. |
| `FilterLabel` | `string` | Filter trigger label and filter panel title. |
| `FilterTriggerId` | `string` | Anchor id used by the filter popover. |
| `FilterTitleId` | `string` | Id used by the filter popover `aria-labelledby`. |
| `FilterContent` | `RenderFragment?` | Optional filter panel content. No filter trigger renders when omitted. |
| `ViewMenuLabel` | `string` | View or overflow menu trigger label. |
| `ViewMenuContent` | `RenderFragment?` | Optional `FluentMenuItem` content. No menu renders when omitted. |
| `Actions` | `RenderFragment?` | Caller-owned page actions rendered at the end of the toolbar row. |
| `Tabs` | `IReadOnlyList<FcPageToolbarTab>` | Optional tab descriptors. No tab strip renders when empty. |
| `ActiveTabId` / `ActiveTabIdChanged` | `string?` / `EventCallback<string?>` | Caller-owned active-tab state. |

`FcPageToolbarTab` contains `Id`, `Header`, `Disabled`, and optional `IconStart`. Use it for local
page tabs only; routing and persistence remain caller-owned.

## Layout (FC-LYT)

The toolbar renders a vertical `FluentStack` containing a horizontal `FluentStack` row with
`role="toolbar"`. The search input flexes first, filter and view controls follow, and the `Actions`
slot aligns to the far edge when space is available. The row wraps at narrow widths so controls do
not overlap. Optional tabs render below the row as `FluentTabs` with the subtle Fluent appearance.

The component does not change `FcPageHeader` landmarks. In aggregate-list pages it composes through
the existing `Toolbar` slot, which is forwarded to `FcPageHeader.Actions`.

## Accessibility (FC-A11Y)

The toolbar row has an accessible name through `AriaLabel`. The search input uses
`FluentTextInput` with `TextInputType.Search` and requires a caller-owned `SearchAriaLabel`. Filter
content renders only when supplied; its trigger exposes `aria-haspopup="dialog"` and
`aria-expanded`, and the `FluentPopover` is labelled by the filter title. View actions use
`FluentMenuButton`, `FluentMenu`, and caller-owned `FluentMenuItem` content, so keyboard behavior is
delegated to Fluent UI v5.

Because the toolbar exposes search, filter, menu, action, and tab controls through caller-owned
slots, custom toolbar content must preserve accessible names and keyboard reachability. The closest
published checks are [HFC1050](../../diagnostics/HFC1050.md) for missing accessible names and
[HFC1051](../../diagnostics/HFC1051.md) for blocked keyboard access.

## Localization (FC-L10N)

`FcPageToolbar` does not inject `IStringLocalizer`. Adopters pass localized strings for
`AriaLabel`, `SearchPlaceholder`, `SearchAriaLabel`, `FilterLabel`, `ViewMenuLabel`, action text,
menu items, and tab headers from their page or domain resource owner.

## Related

- [FrontComposerShell](front-composer-shell.md) — the root application shell around page content.
- [DataGrid Surface](datagrid.md) — generated projection grids and search/filter components.
- [Components](index.md) — component reference index.
