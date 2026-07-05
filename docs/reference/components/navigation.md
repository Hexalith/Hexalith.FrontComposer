---
title: "Navigation"
description: "The registry-driven FrontComposer navigation rail: one tile per registered domain, projection flyouts, count and New badges, and labelled or icon-only rail widths."
genre: reference
audience: adopter
ownerStory: 8-5-icon-label-navigation-rail-and-projection-flyout
status: published
reviewed: 2026-06-25
uid: frontcomposer.reference.components.navigation
slug: reference/components/navigation/
---

# Navigation

## Overview

`FrontComposerNavigation` is the framework-owned sidebar. It renders a unified Fluent UI v5 rail
driven entirely by `IFrontComposerRegistry`: one bounded-context tile per registered
`DomainManifest` whose projection list or explicit nav-entry list is non-empty. Activating a tile
opens a `FluentMenu` flyout with the context's projections and explicit entries. The rail surfaces
aggregate count badges, projection count badges, and a "New" badge for not-yet-visited capabilities.
At Desktop it renders as a 72 px labelled rail or a 48 px icon-only rail, controlled by the shell's
hamburger and navigation state; CompactDesktop also uses the 48 px rail. In the 72 px rail, each
context tile stacks the icon above the visible label, while count and "New" badges stay as separate
overlay indicators. Adopt it implicitly — the shell auto-renders it in the Navigation area when you
register at least one domain — or place it yourself via the shell's `Navigation` slot. You do not
configure it directly: it reflects what you register.

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
| Registered domains | `IFrontComposerRegistry` | One bounded-context tile per `DomainManifest` with at least one visible projection or explicit nav entry. |
| Projection counts | `IBadgeCountService` | Aggregate and per-projection count `FluentBadge` surfaces when counts are greater than zero. |
| Capability-seen set | Capability-discovery Fluxor state | A "New" `FluentBadge` when a context or projection capability is unvisited and has data; activation dispatches the visited action. |
| Viewport / collapse state | Navigation Fluxor state | Uses 72 px labelled rail at expanded Desktop and 48 px icon-only rail at collapsed Desktop or CompactDesktop. |

Because there are no parameters, there is no surface to lock or version — the rail's contract is the
registry and state it reads.

## Layout (FC-LYT)

The navigation rail occupies the shell's **Navigation** `FluentLayoutItem` (72 px expanded, 48 px
icon-only) and is independent of the FC-LYT page-measure contract, which governs the **Content**
region only. The rail is suppressed at Tablet and Phone tiers (navigation reaches those tiers through
the hamburger drawer instead), so a page's `FullWidth` vs `Constrained` measure never competes with
the rail for horizontal room.

## Accessibility (FC-A11Y)

- **Accessible name** — the rail root carries `role="navigation"` and an `aria-label` resolved from
  the shell-owned `NavMenuAriaLabel` string, so the navigation landmark is named for assistive
  technology. *(WCAG 4.1.2)*
- **Keyboard reachability** — context tiles are focusable Fluent UI controls; activating one opens a
  keyboard-navigable `FluentMenu` flyout, and the skip-to-navigation link (`href="#fc-nav"`) the
  shell renders lets keyboard users jump straight to the rail. *(WCAG 2.1.1 / 2.4.1)*

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
