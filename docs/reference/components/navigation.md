---
title: "Navigation"
description: "The registry-driven FrontComposer navigation rail: one category per registered domain, per-projection count and New badges, and a collapsed icon rail."
genre: reference
audience: adopter
ownerStory: 1-5-produce-the-fc-doc-component-documentation-contract
status: published
reviewed: 2026-06-03
uid: frontcomposer.reference.components.navigation
slug: reference/components/navigation/
---

# Navigation

## Overview

`FrontComposerNavigation` is the framework-owned sidebar. It composes Fluent UI v5's `FluentNav` into
a category-per-domain tree driven entirely by `IFrontComposerRegistry`: one `FluentNavCategory` per
registered `DomainManifest` whose projection list is non-empty, with a `FluentNavItem` per projection.
It surfaces per-projection count badges and a "New" badge for not-yet-visited capabilities, and swaps
to a compact icon rail (`FcCollapsedNavRail`) at CompactDesktop or when the sidebar is collapsed.
Adopt it implicitly — the shell auto-renders it in the Navigation area when you register at least one
domain — or place it yourself via the shell's `Navigation` slot. You do not configure it directly: it
reflects what you register.

## Usage

The common case is **zero markup** — register a domain and the shell renders the rail for you:

```csharp no-compile reason="illustrative adopter bootstrap"
// Program.cs — registering a domain makes its projections appear in the nav rail
builder.Services.AddHexalithDomain<MyDomainMarker>();
```

To render the rail in an explicit position (or to wrap it), supply it through the shell's `Navigation`
slot:

```razor
<FrontComposerShell Navigation="@(_ => { /* custom chrome */ })">
    @Body
</FrontComposerShell>

@* Or let the shell auto-populate it — no slot needed: *@
<FrontComposerShell>
    @Body
</FrontComposerShell>
```

Nav-item routes follow the convention `/{boundedContext-lowercase}/{projectionType-kebab-case}`.

## Parameters / slots

`FrontComposerNavigation` exposes **no public `[Parameter]` surface** — it is composed entirely from
injected state. Its content is a function of:

| Input | Source | Effect |
|---|---|---|
| Registered domains | `IFrontComposerRegistry` | One `FluentNavCategory` per `DomainManifest` with ≥ 1 projection (commands are excluded). |
| Projection counts | `IBadgeCountService` | A count `FluentBadge` beside an item when its count > 0. |
| Capability-seen set | Capability-discovery Fluxor state | A "New" `FluentBadge` when the capability is unvisited and the projection has data; clicking dispatches `CapabilityVisitedAction`. |
| Viewport / collapse state | Navigation Fluxor state | Swaps to `FcCollapsedNavRail` (48 px icon rail) at CompactDesktop or when collapsed. |

Because there are no parameters, there is no surface to lock or version — the rail's contract is the
registry and state it reads.

## Layout (FC-LYT)

The navigation rail occupies the shell's **Navigation** `FluentLayoutItem` (≈ 220 px expanded, 48 px
collapsed) and is independent of the FC-LYT page-measure contract, which governs the **Content**
region only. The rail is suppressed at Tablet and Phone tiers (navigation reaches those tiers through
the hamburger drawer instead), so a page's `FullWidth` vs `Constrained` measure never competes with
the rail for horizontal room.

## Accessibility (FC-A11Y)

- **Accessible name** — the `FluentNav` carries an `aria-label` resolved from the shell-owned
  `NavMenuAriaLabel` string, so the navigation landmark is named for assistive technology. *(WCAG
  4.1.2)*
- **Keyboard reachability** — nav items are standard focusable Fluent UI controls; the skip-to-
  navigation link (`href="#fc-nav"`) the shell renders lets keyboard users jump straight to the rail.
  *(WCAG 2.1.1 / 2.4.1)*

Adopter overrides of nav rendering are held to the same bar by the override-accessibility diagnostics
— most relevantly [HFC1050](../../diagnostics/HFC1050.md) (missing accessible name) and
[HFC1051](../../diagnostics/HFC1051.md) (keyboard reachability blocked).

## Localization (FC-L10N)

Every string the rail renders for itself — the navigation `aria-label` (`NavMenuAriaLabel`) and badge
labels — is **shell-owned** and resolves through `IStringLocalizer<FcShellResources>`. The **category
and item labels come from your registered domain** (the `DomainManifest` / projection metadata), so
domain display names are host-owned per the FC-L10N split — localize them via `[Display(Name=…)]` or
your own `IStringLocalizer<T>`. To whitelabel the rail's framework strings, `services.Replace(...)`
the `IStringLocalizer<FcShellResources>` registration.

## Related

- [FrontComposerShell](front-composer-shell.md) — the shell that auto-populates this rail.
- [Components](index.md) — the component reference index.
- [Reference](../index.md) — the full reference index.
