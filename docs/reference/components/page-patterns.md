---
title: "Page patterns"
description: "Public page patterns contracts, states, accessibility and customization ownership."
genre: reference
audience: adopter
ownerStory: 15-2-i-doc-a-reconcile-component-and-page-pattern-documentation
status: published
reviewed: 2026-10-21
uid: frontcomposer.reference.components.page-patterns
slug: reference/components/page-patterns/
---

# Page patterns

## Overview

Compose page bodies through `FcPageLayout`, `FcPageHeader`, `FcAggregateListPage<TItem>` and `FcAggregateDetailPage<TItem>`. Wrappers own layout and slot placement; domains own queries, data, route policy and copy. Home and Module landing surfaces are framework compositions.

## Usage

```razor
<FcAggregateListPage TItem="OrderRow" Heading="Orders" HeadingId="orders-heading"
                     HeadingTabIndex="-1" Toolbar="@Tools" Filters="@Filters"
                     Commands="@Commands" States="@States" Body="@Rows" Pager="@Pager" />
```

`Toolbar` maps to header `Actions`; `HeaderMetadata` maps to header `Metadata`. Bodies and tab panels belong below the header. `OnItemSelected` is a callback raised through `NotifyItemSelectedAsync`; it does not navigate automatically.

## Parameters / slots

The tables describe the public component parameters. Injected services and internal Fluent composition are implementation details.

### `FcPageLayout`

| Parameter | Type | Contract |
|---|---|---|
| `Mode` | `FcPageLayoutMode` | The layout mode this page declares. Defaults to `FcPageLayoutMode.FullWidth`, so a bare &lt;FcPageLayout&gt; is a no-op; pass `FcPageLayoutMode.Constrained` to opt into the readable max-measure. |
| `ChildContent` | `RenderFragment?` | The page content rendered within the declared measure. |

### `FcPageHeader`

| Parameter | Type | Contract |
|---|---|---|
| `PageTitle` | `string?` | The browser document title rendered through Blazor PageTitle. |
| `Heading` | `string` | The visible route-level heading text. |
| `Eyebrow` | `string?` | Optional short context text rendered above the heading. |
| `Description` | `string?` | Optional descriptive text rendered below the heading. |
| `HeadingId` | `string?` | Optional id assigned to the route-level heading. |
| `HeadingTabIndex` | `int?` | Optional tabindex assigned to the route-level heading when it is a focus target. |
| `TestId` | `string` | Optional stable test selector for the header root. |
| `IsCommandFormHeading` | `bool` | Marks this route heading as the owning full-page command form's fallback focus target. |
| `Class` | `string?` | Optional additional CSS classes for the header root. |
| `Actions` | `RenderFragment?` | Optional page actions rendered in the header title row. |
| `Metadata` | `RenderFragment?` | Optional page metadata or return-context content rendered below the description. |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | Additional attributes applied to the header root. |

### `FcAggregateListPage`

| Parameter | Type | Contract |
|---|---|---|
| `LayoutMode` | `FcPageLayoutMode` | The FC-LYT layout measure the page declares. Defaults to `FcPageLayoutMode.FullWidth` for dense list surfaces. |
| `PageTitle` | `string?` | The browser document title forwarded to the composed `FcPageHeader`. |
| `Heading` | `string` | The visible route-level heading; blank values suppress the heading. Provide a nonblank value when using heading focus. |
| `Eyebrow` | `string?` | Optional short context text rendered above the heading. |
| `Description` | `string?` | Optional descriptive text rendered below the heading. |
| `HeadingId` | `string?` | Optional id assigned to the route-level heading (focus target for return-context restoration). |
| `HeadingTabIndex` | `int?` | Optional tabindex assigned to the route-level heading when it is a focus target. |
| `HeaderTestId` | `string` | Stable test selector for the composed header root. |
| `HeaderClass` | `string?` | Optional additional CSS classes applied to the composed header root. |
| `RootTestId` | `string` | Stable test selector for the page root stack. |
| `RootClass` | `string?` | Optional CSS class applied to the page root stack. |
| `RootGap` | `string` | Vertical gap between root regions (CSS length). Defaults to 24px. |
| `Toolbar` | `RenderFragment?` | Toolbar content rendered in the header Actions slot (refresh/reset/navigation commands). |
| `HeaderMetadata` | `RenderFragment?` | Return-context or page metadata rendered in the header Metadata slot. |
| `Filters` | `RenderFragment?` | Search and filter controls rendered below the header. |
| `Commands` | `RenderFragment?` | Command flow surfaces (for example a create flow) rendered above the list body. |
| `States` | `RenderFragment?` | Surface-state content (loading/empty/filtered-empty/error/stale/degraded) rendered above the body. |
| `Body` | `RenderFragment?` | The data grid / list body, supplied by the domain (for example a FluentDataGrid). |
| `Pager` | `RenderFragment?` | Pagination controls rendered below the body. |
| `ChildContent` | `RenderFragment?` | Optional additional content rendered after the pager. |
| `OnItemSelected` | `EventCallback<TItem>` | Optional typed row-navigation callback a domain row template can raise via `NotifyItemSelectedAsync`. |

### `FcAggregateDetailPage`

| Parameter | Type | Contract |
|---|---|---|
| `LayoutMode` | `FcPageLayoutMode` | The FC-LYT layout measure the page declares. Defaults to `FcPageLayoutMode.Constrained` for readable detail surfaces. |
| `State` | `FcAggregateDetailState` | The current FC-DTL surface state. Drives which slot renders; never collapses a non-ready state into the ready body. |
| `RootTestId` | `string` | Stable test selector for the page root stack. |
| `RootClass` | `string?` | Optional CSS class applied to the page root stack. |
| `RootGap` | `string` | Vertical gap between root regions (CSS length). Defaults to 16px. |
| `ShowBackLink` | `bool` | Whether the back link is rendered. Defaults to `true`. |
| `BackHref` | `string` | The (caller-validated, safe) back navigation URL. |
| `BackLinkLabel` | `string?` | The visible back-link label. |
| `BackLinkTestId` | `string` | Stable test selector for the back link. |
| `BackLinkClass` | `string?` | Optional additional CSS classes applied to the back link. |
| `Item` | `TItem?` | Optional detail payload passed to `ReadyTemplate` when the surface is ready. |
| `ReadyTemplate` | `RenderFragment<TItem>?` | Optional typed ready body template; rendered with `Item` when both are supplied. |
| `ReadyContent` | `RenderFragment?` | Untemplated ready body (identity header, facts, sections). Used when `ReadyTemplate` is not supplied. |
| `LoadingContent` | `RenderFragment?` | Loading-state content rendered only for `FcAggregateDetailState.Loading`. |
| `UnauthorizedContent` | `RenderFragment?` | Unauthorized-state content rendered only for `FcAggregateDetailState.Unauthorized`. |
| `NotFoundContent` | `RenderFragment?` | Not-found-state content rendered only for `FcAggregateDetailState.NotFound`. |
| `UnavailableContent` | `RenderFragment?` | Unavailable-state content rendered for `FcAggregateDetailState.Unavailable` and the fail-closed default. |
| `StaleBanner` | `RenderFragment?` | Banner rendered above the ready body when the surface is `FcAggregateDetailState.Stale`. |
| `DegradedBanner` | `RenderFragment?` | Banner rendered above the ready body when the surface is `FcAggregateDetailState.Degraded`. |

### `FcHomeDirectory`

No public component parameters. Framework services supply its state.

### `FcHomeCard`

| Parameter | Type | Contract |
|---|---|---|
| `Card` | `HomeCardModel` | Gets or sets the per-card view-model rendered by this component. |
| `SeenCapabilities` | `ImmutableHashSet<string>` | Gets or sets the current per-user "seen capabilities" set used to gate the "New" badges. |
| `HydrationState` | `CapabilityDiscoveryHydrationState` | Gets or sets the hydration state; gates progressive disclosure of projection rows while the seed is still landing. |
| `OnActivated` | `EventCallback<HomeCardModel>` | Gets or sets the activation callback invoked when the card is clicked OR activated via Enter / Space. The parent is responsible for dispatching the `CapabilityVisitedAction` and navigating. |

### `FcHomeRouteView`

No public component parameters. Framework services supply its state.

### `FcModuleLandingPage`

| Parameter | Type | Contract |
|---|---|---|
| `Module` | `string?` | The registered bounded context selected by the route. |
| `Tab` | `string?` | The requested child tab. |

## Layout (FC-LYT)

Lists default to `FullWidth`, details to `Constrained`; `FcPageLayout` resets the shell to full width on disposal and is inert outside the shell coordinator. `RootGap`, `RootClass`, and the named slots control composition. Multiple sibling titled content sections use one Fluent accordion with the primary section expanded; a sole grid/form remains visible.

### `FcAggregateDetailState`

| State | Rendering |
|---|---|
| `Loading` | `LoadingContent`, falling back to `UnavailableContent`. |
| `Ready` | `ReadyTemplate(Item)` when both exist, otherwise `ReadyContent`. |
| `Stale` | `StaleBanner` followed by the ready body. |
| `Degraded` | `DegradedBanner` followed by the ready body. |
| `Unauthorized` | `UnauthorizedContent`, falling back to `UnavailableContent`. |
| `NotFound` | `NotFoundContent`, falling back to `UnavailableContent`. |
| `Unavailable` | `UnavailableContent`; unknown enum values fail closed here. |

Only Ready, Stale and Degraded expose projection-proven detail content. `BackHref` is validated by the caller; the wrapper owns no authorization or freshness policy.

## Accessibility (FC-A11Y)

`FcPageHeader` renders the route `h1` without adding a banner landmark. A blank `Heading` suppresses the heading. For `FocusHeadingAsync`, provide a nonblank heading and explicit `HeadingTabIndex` (usually `-1`); missing either throws. `FcAggregateListPage.FocusHeadingAsync` forwards this behavior. Give the heading a stable `HeadingId` and use the shell content-label contract. Route focus, tab focus and return-context restoration must have one owner per transition. Keep caller content named and keyboard reachable; see [HFC1050](../../diagnostics/HFC1050.md) and [HFC1051](../../diagnostics/HFC1051.md).

## Localization (FC-L10N)

Page wrappers do not translate domain headings, descriptions, back labels or slot content. Pass complete localized strings from the domain owner. Built-in home and Module landing copy uses shell resources; replace shell localization through the existing `IStringLocalizer<FcShellResources>` seam.

## Related

- [Projection status](projection-status.md)
- [Command lifecycle](command-lifecycle.md)
- [Customization](customization.md)
- [Components](index.md)
