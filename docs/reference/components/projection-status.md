---
title: "Projection status"
description: "Public projection status contracts, states, accessibility and customization ownership."
genre: reference
audience: adopter
ownerStory: 15-2-i-doc-a-reconcile-component-and-page-pattern-documentation
status: published
reviewed: 2026-10-21
uid: frontcomposer.reference.components.projection-status
slug: reference/components/projection-status/
---

# Projection status

## Overview

Generated projections compose loading, empty, failure, stale and scope-blocked surfaces. These read-path states are separate from aggregate-detail states and command lifecycle outcomes. `ProjectionViewContext.LifecycleState` is only a `Loading` / `Empty` / `Loaded` view summary; it is not a command outcome or connectivity proof.

## Usage

```razor
<FcProjectionLoadingSkeleton ColumnCount="3" EntityLabel="orders" />
```

This standalone skeleton keeps its own status semantics. Set `SharedStatusOwner="true"` only when a containing surface already mounts `FcSurfaceStatus` and its producer publishes the localized loading announcement to that same surface. A bare shared node without a producer does not announce loading. Direct callers rendering `FcProjectionFailure` must mount `FcSurfaceStatus` for the scoped surface, publish the localized failure announcement through `ISurfaceAnnouncementCoordinator.Announce` to that same surface with `terminal: true` and a new identity for each failure, and wire `OnRetry` to the actual scoped query. The coordinator treats an already-delivered identity as the same announcement and normally stays silent, so reusing one identity can leave a repeated failure unannounced. The failure component supplies neither its own live node nor an announcement producer. A generated operator-initiated load request is a request for adopter reload work; answer it with matching correlation and current scope provenance.

## Parameters / slots

The tables describe the public component parameters. Injected services and internal Fluent composition are implementation details.

### `FcProjectionLoadingSkeleton`

| Parameter | Type | Contract |
|---|---|---|
| `ColumnCount` | `int` | Number of preview columns; generated views cap this at 8 independently of the grid column-visibility limit. Direct callers supply the count. |
| `RowCount` | `int` | Gets or sets the number of skeleton rows. Defaults to 5. |
| `Layout` | `SkeletonLayout` | Gets or sets the layout variant. Default `SkeletonLayout.DataGrid`; DetailRecord renders `SkeletonLayout.Card`; Timeline renders `SkeletonLayout.Timeline`. |
| `AriaLabel` | `string?` | Gets or sets an accessible label for screen readers. When `null` the component resolves FcProjectionLoadingSkeletonAriaLabel with the projection display label. |
| `EntityLabel` | `string` | Gets or sets the display label inserted into the aria-label. The generator passes the humanized projection name; adopters composing the skeleton directly may override. |
| `SharedStatusOwner` | `bool` | Uses a containing projection's persistent status node for speech. |

### `FcProjectionEmptyPlaceholder`

| Parameter | Type | Contract |
|---|---|---|
| `ProjectionType` | `Type` | Gets or sets the projection type this placeholder represents. |
| `EntityPluralOverride` | `string?` | Optional localized plural label. When omitted, the projection type name is humanized with a basic English "s" suffix. |
| `Role` | `ProjectionRole?` | Projection role used to select the empty-state action copy. |
| `CtaCommandName` | `string?` | Optional action command name. When supplied, bypasses `[ProjectionEmptyStateCta]` discovery; otherwise the resolver uses that attribute or the bounded-context fallback. |
| `SecondaryText` | `string?` | Optional localized secondary guidance. When omitted, uses the `{ProjectionFqn}_EmptyStateSecondaryText` resource if present; otherwise renders no secondary line. |
| `SuppressStandaloneStatus` | `bool` | Uses the generated view's shared polite status instead of a second live node. |

### `FcProjectionFailure`

| Parameter | Type | Contract |
|---|---|---|
| `Message` | `string?` | Optional localized failure message; omitted values use shell query-failure copy. |
| `OnRetry` | `EventCallback` | Callback for retrying the affected read or boundary; the caller owns the work. |

### `FcProjectionStaleNotice`

| Parameter | Type | Contract |
|---|---|---|
| `Surface` | `string` | Scoped announcement surface identity; reuse the containing view's identity. |
| `ScopeGeneration` | `long` | Generation of the current scoped projection view. |
| `SharedStatusOwner` | `bool` | Uses the generated projection's existing shared polite status node. |
| `HasCachedRows` | `bool` | Whether confirmed rows are retained and can be shown with stale guidance. |
| `HasFailedRefresh` | `bool` | Whether the current refresh failed while cached rows remain. |
| `FreshnessIdentity` | `string?` | Identifies genuine completed reads of the retained data, independently of connectivity. |
| `QueryFailureGroup` | `string?` | Gets or sets the group of the current eligible query failure. |
| `QueryFailureIdentity` | `string?` | Gets or sets the event identity of the current eligible query failure. |

### `FcSurfaceStatus`

| Parameter | Type | Contract |
|---|---|---|
| `Surface` | `string` | Scoped announcement surface identity; reuse the containing view's identity. |
| `ExclusiveOwner` | `bool` | Shares a single node with other standalone owners of this scoped surface. |
| `BrowserManaged` | `bool` | Lets the scoped browser connectivity producer render this node without a server round trip. |
| `BrowserReady` | `bool` | Whether the native connectivity watcher now owns the status text. |
| `ServerFallback` | `bool` | Whether retained server copy still describes an active connection fallback. |

### `FcScopeBlocked`

No public component parameters. Framework services supply its state.

### `FcProjectionSubtitle`

| Parameter | Type | Contract |
|---|---|---|
| `ProjectionType` | `Type` | Gets or sets the projection runtime type this subtitle orients. |
| `Role` | `ProjectionRole?` | Gets or sets the projection role driving copy selection. `null` means the Default role copy (no [ProjectionRole] attribute on the projection). |
| `WhenState` | `string?` | Gets or sets the raw CSV WhenState carried through from `ProjectionRoleAttribute.WhenState` for tool support / telemetry. Not rendered directly in v1. |
| `FallbackCount` | `int` | Gets or sets the fallback count passed by the generated view (typically state.Items?.Count ?? 0). Used when `IBadgeCountService` is not registered OR throws during subscription. |
| `EntityLabel` | `string?` | Gets or sets the resolved singular entity label emitted by the generator. |
| `EntityPluralLabel` | `string?` | Gets or sets the resolved plural entity label emitted by the generator. |
| `DistinctStatusCount` | `int?` | Gets or sets the distinct status-bucket count for StatusOverview subtitles. Null or non-positive values fall back to the Default subtitle copy. |
| `IsLoading` | `bool` | Gets or sets the optional loading flag; true suppresses the subtitle render. |

### `FcFieldPlaceholder`

| Parameter | Type | Contract |
|---|---|---|
| `FieldName` | `string` | Gets or sets the name of the property this placeholder represents. |
| `TypeName` | `string` | Gets or sets the fully qualified name of the unsupported type. |
| `IsDevMode` | `bool` | Gets or sets a value indicating whether developer mode highlighting is active. |

### `FcStatusIcon`

| Parameter | Type | Contract |
|---|---|---|
| `Slot` | `BadgeSlot` | Gets or sets the semantic status slot. |
| `Label` | `string` | Gets or sets the status label shown in the tooltip and accessible name. |
| `ColumnHeader` | `string?` | Gets or sets the optional column header used to contextualize the accessible name. |

### `FcStatusBadge`

| Parameter | Type | Contract |
|---|---|---|
| `Slot` | `BadgeSlot` | Gets or sets the semantic slot. Resolves to a Fluent `BadgeColor` + `BadgeAppearance` pair via `SlotAppearanceTable`. Changing this slot at runtime re-resolves the pair on the next render. |
| `Label` | `string` | Visible status label, always rendered so status does not depend on color alone. Supply the localized or humanized value. |
| `ColumnHeader` | `string?` | Gets or sets the optional column-header text used to prefix the aria-label (for example "Status"). When `null` the aria-label falls back to just `Label`. |

### `FcDesaturatedBadge`

| Parameter | Type | Contract |
|---|---|---|
| `PriorSlot` | `BadgeSlot` | Prior confirmed slot selected for Rejected or NeedsReview; only one badge is rendered. |
| `PriorLabel` | `string` | Prior localized value label selected for Rejected or NeedsReview. |
| `OptimisticSlot` | `BadgeSlot` | Proposed optimistic status slot; does not prove confirmation. |
| `OptimisticLabel` | `string` | Localized proposed status label. |
| `State` | `OptimisticBadgeState` | Optimistic visual transition state; does not change authoritative command outcome. |
| `ColumnHeader` | `string?` | Optional localized column context for the accessible status label. |

### `FcProjectionConnectionStatus`

No public component parameters. Framework services supply its state.

## Layout (FC-LYT)

Loading previews the role layout with `aria-busy`; an empty confirmed read uses `FcProjectionEmptyPlaceholder`. A failed first read shows failure; retained cached rows may stay visible with stale guidance after a failed refresh or connectivity loss. `FcScopeBlocked` replaces sensitive content while no validated tenant/user scope exists. Never treat stale or blocked content as a fresh successful read.

### `OptimisticBadgeState`

`FcDesaturatedBadge` renders one `FcStatusBadge`. It selects the prior slot/value label for Rejected and NeedsReview, and the optimistic slot/value label for the other values. The localized state prefix is combined with the selected value label; it becomes the entire label when that value label is blank.

| State | Selection and presentation |
|---|---|
| `Confirming` | Default: optimistic slot/value, localized confirming prefix and desaturated confirming class. |
| `Confirmed` | Optimistic slot/value with localized confirmed prefix. |
| `Rejected` | Prior slot/value with localized rejected prefix. |
| `IdempotentConfirmed` | Optimistic slot/value with localized already-applied prefix. |
| `NeedsReview` | Prior slot/value with localized needs-review prefix. |

An unknown enum value uses the optimistic slot/value and confirming prefix, but receives no confirming-only desaturation class. The accessible label includes the localized column context, state prefix and selected value. This presentation never establishes an authoritative command outcome.

### `ProjectionConnectionStatus`

| State | Meaning |
|---|---|
| `Connected` | Projection hub transport connected; does not prove a particular query is fresh. |
| `Reconnecting` | Reconnection is active; cached data may be stale. |
| `Disconnected` | Hub transport disconnected; preserve appropriate stale/failure behavior. |

Status icons retain a textual label and accessible name as well as color. Badge and optimistic presentation helpers do not change the authoritative read state.

## Accessibility (FC-A11Y)

The shared polite status owner prevents duplicate speech from nested loading, empty, stale and connection notices. `SharedStatusOwner` / `SuppressStandaloneStatus` indicate existing speech ownership. `FcProjectionFailure` supplies visible retry content while speech stays on the shared node. Scope blocking focuses its localized heading once per generation; do not duplicate its focus or expose scope values. Browser measurements are required for computed names and focus. Keep caller content named and keyboard reachable; see [HFC1050](../../diagnostics/HFC1050.md) and [HFC1051](../../diagnostics/HFC1051.md).

## Localization (FC-L10N)

Built-in loading, empty, retry, stale and connection strings use shell resources. Domain labels, status `Label`, `Message`, empty-state secondary copy and overrides remain caller-owned. Empty-state fallback pluralization is basic English; supply a localized plural override for other languages.

## Related

- [Page patterns](page-patterns.md)
- [Command lifecycle](command-lifecycle.md)
- [Customization](customization.md)
- [Components](index.md)
