---
title: "FrontComposerShell"
description: "The framework-owned application shell: Header / Navigation / Content / Footer regions, skip links, theme toggle, settings, and command palette."
genre: reference
audience: adopter
ownerStory: 8-3-brand-logo-cell-in-header-start
status: published
reviewed: 2026-06-25
uid: frontcomposer.reference.components.frontcomposershell
slug: reference/components/front-composer-shell/
---

# FrontComposerShell

## Overview

`FrontComposerShell` is the framework-owned application shell. It composes Fluent UI v5's
`FluentLayout` into the four spec-pinned regions — **Header**, **Navigation**, **Content**, and
**Footer** — and mounts the cross-cutting infrastructure every FrontComposer app needs: the Fluxor
store initializer, `FluentProviders`, accessibility skip links, the theme toggle, the settings
launcher, the command palette trigger, and the global keyboard shortcuts. Adopt it when you want the
standard FrontComposer chrome: your `MainLayout.razor` collapses to a single element wrapping
`@Body`, and the shell supplies everything else. You do not need to read its source to use it — the
slots and behaviors below are the whole contract.

## Usage

Wrap your routed page body in your layout. The shell fills the header chrome, navigation rail, and
footer automatically:

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase

<FrontComposerShell>
    @Body
</FrontComposerShell>
```

Register the shell services once at startup. The granular three-call path keeps you in control of
`LocalizationOptions`:

```csharp no-compile reason="illustrative adopter bootstrap"
// Program.cs
builder.Services
    .AddLocalization()
    .AddHexalithShellLocalization()
    .AddHexalithFrontComposer();
```

First-time adopters can use the one-line `AddHexalithFrontComposerQuickstart()` sugar, which chains
`AddLocalization()` + `AddHexalithShellLocalization()` for you.

## Parameters / slots

The shell exposes a **locked 12-parameter surface**. The original header/navigation/content/footer
slots keep their established order, and newer accessibility/brand parameters are appended to preserve
metadata-order compatibility. All slots are optional render fragments except `AppTitle` (a string);
leaving a slot `null` triggers the documented default.

| Parameter | Type | Default when `null` |
|---|---|---|
| `HeaderStart` | `RenderFragment?` | Auto-populates `FcHamburgerToggle` (left of the app title). |
| `HeaderCenter` | `RenderFragment?` | Slot omitted (breadcrumb slot; filled by later stories). |
| `HeaderEnd` | `RenderFragment?` | Auto-populates the command-palette trigger + settings button. |
| `Navigation` | `RenderFragment?` | Auto-renders `FrontComposerNavigation` when the registry has ≥ 1 renderable manifest; the Navigation area is omitted entirely when the registry is empty. |
| `Footer` | `RenderFragment?` | Renders the default localized copyright (`{resolved app title} © {Year}`). |
| `ChildContent` | `RenderFragment?` | The page body (your `@Body`). |
| `AppTitle` | `string?` | Resolves `FcShellOptions.AppTitle`, then the framework-owned `FcShellResources.AppTitle` product-name string. |
| `ContentLabel` | `string?` | Optional accessible name for the `#fc-main-content` landmark when no visible heading labels it. |
| `ContentLabelledBy` | `string?` | Optional id reference that names the `#fc-main-content` landmark; takes precedence over `ContentLabel`. |
| `HeaderLogo` | `RenderFragment?` | Optional adopter-supplied logo rendered between `HeaderStart` or the default hamburger and `AppTitle`. |
| `ShowDefaultHeaderLogo` | `bool` | Opts into the framework default decorative logo when `HeaderLogo` is not supplied; default is `false`. |
| `ShowAccountMenu` | `bool` | Renders the framework account menu; default is `true`. Set to `false` when the host has no working login/logout endpoints. |

The header also always renders the theme toggle, renders the account menu when `ShowAccountMenu` is
`true`, and renders the dev-mode toggle in DEBUG + `IsDevelopment()` only. An adopter-supplied
fragment always wins over the framework default; to render **no**
sidebar even with registered domains, pass an empty fragment to `Navigation`.

> **Surface stability:** this 12-parameter list is locked by `FrontComposerShellParameterSurfaceTests`.
> Additions must be append-only; no parameter may be removed, renamed, or retyped without a major
> version bump.

## Layout (FC-LYT)

The shell's content region (`#fc-main-content`) honors the **FC-LYT page-layout contract**. Pages
render at one of two measures, selected per page:

- **`FullWidth`** *(default)* — content spans the content area edge-to-edge, exactly the shell's
  out-of-the-box behavior. Right for DataGrid-dense, read-only projection pages.
- **`Constrained`** *(opt-in)* — content is capped at a readable max measure and centred
  (`max-inline-size: var(--fc-page-max-inline-size, 75rem); margin-inline: auto;`). Right for prose,
  forms, and detail pages.

A page opts in to constrained measure by dropping `<FcPageLayout Mode="FcPageLayoutMode.Constrained">`
into its content; it lives *inside* `ChildContent` and signals the shell through a cascaded
coordinator, so the shell's parameter surface stays untouched. The max measure is the themeable
`--fc-page-max-inline-size` custom property (default `75rem`), expressed with logical properties for
RTL-awareness.

## Accessibility (FC-A11Y)

The shell ships the FC-A11Y primitive set as part of its own frame:

- **Skip links** — visually-hidden-until-focused `.fc-skip-link` anchors render first in the shell
  root: `href="#fc-main-content"` (always) and `href="#fc-nav"` (when navigation is present). Each
  target carries `tabindex="-1"` so the link resolves to a real, focusable region. *(WCAG 2.4.1)*
- **Focus visibility** — focus rings inherit Fluent UI's `:focus-visible` default; the shell CSS
  suppresses no focus. *(WCAG 2.4.7)*
- **`aria-live` status** — status surfaces (projection connection, pending commands, density
  announcer) use the polite→assertive politeness ladder with skip-first-render. *(WCAG 4.1.3)*
- **Accessible names + keyboard reachability** — the theme toggle, palette trigger, settings button,
  and nav rail all carry accessible names; global `Ctrl+K` (command palette), `Ctrl+,` (settings),
  and `g h` (home) never swallow `Tab`, so no focus trap is introduced. *(WCAG 4.1.2 / 2.1.1)*

Adopter customizations are held to the same bar at build time by the **override-accessibility
diagnostics**, which are `Warning` severity promoted to build-breakers under
`TreatWarningsAsErrors=true`:

- [HFC1050](../../diagnostics/HFC1050.md) — interactive element missing an accessible name.
- [HFC1051](../../diagnostics/HFC1051.md) — keyboard reachability blocked.
- [HFC1052](../../diagnostics/HFC1052.md) — suppressed focus without a `:focus-visible` restore.
- [HFC1053](../../diagnostics/HFC1053.md) — status override missing `aria-live` parity.
- [HFC1054](../../diagnostics/HFC1054.md) — motion without a reduced-motion fallback.
- [HFC1055](../../diagnostics/HFC1055.md) — custom colors without forced-colors evidence.

## Localization (FC-L10N)

The shell follows the **FC-L10N two-localizer ownership split**:

- **Shell chrome** — nav, settings, status, palette, footer, skip-links, the default `AppTitle`, and
  every other framework string — is **shell-owned** and resolves through
  `IStringLocalizer<FcShellResources>` (the embedded `FcShellResources.resx` / `.fr.resx`, EN + FR at
  parity). You do not author these strings.
- **Domain / host text** — projection column titles and command-form field labels — is **host-owned**
  via `[Display(Name=…)]` or your own `IStringLocalizer<T>`.

To whitelabel or DB-back the shell's strings, swap the localizer without touching the resx convention:
`services.Replace(...)` the `IStringLocalizer<FcShellResources>` registration. Set
`Hexalith:Shell:AppTitle` when you only need the product name changed, for example
`"Hexalith Tenants"` in a tenants UI host. Override `AppTitle` directly via the parameter for
per-layout exceptions. Add a culture by extending
`FcShellOptions.SupportedCultures` and shipping the matching `FcShellResources.<culture>.resx`
satellite — no code change required.

## Related

- [Navigation](navigation.md) — the registry-driven navigation rail the shell auto-populates.
- [Components](index.md) — the component reference index.
- [Reference](../index.md) — the full reference index.
