---
title: "Settings"
description: "The FrontComposer settings dialog for live theme and density preferences, viewport-aware density preview, and persisted shell preferences."
genre: reference
audience: adopter
ownerStory: 1-6-theme-density-and-settings-persistence
status: published
reviewed: 2026-07-01
uid: frontcomposer.reference.components.settings
slug: reference/components/settings/
---

# Settings

## Overview

FrontComposer owns the shell settings surface. The header settings button and the `Ctrl+,` /
`meta+,` shortcut both open `FcSettingsDialog`, which hosts density selection, theme selection, a
live density preview, Restore defaults, and Done. Changes are live: there is no Apply or Cancel
step. Persistence and restoration are handled by the shell state effects through `IStorageService`.

The settings area is composed from:

- `FcSettingsButton` — header trigger rendered by `FrontComposerShell` when `HeaderEnd` is not supplied.
- `FcSettingsDialog` — modal content with density, theme, preview, reset, and close actions.
- `FcThemeToggle` — Light / Dark / System theme menu.
- `FcDensityPreviewPanel` — local preview specimen for the selected density.
- `FcDensityAnnouncer` and `FcDensityApplier` — headless accessibility and DOM-application helpers mounted by the shell.

## Usage

Most adopters do not render the settings components directly. Use `FrontComposerShell` and leave
`HeaderEnd` unset; the shell auto-populates the palette trigger and settings button:

```razor
<FrontComposerShell>
    @Body
</FrontComposerShell>
```

To replace the right header cluster, supply `HeaderEnd`. In that case the shell does not auto-render
`FcSettingsButton`, so your fragment owns any settings entry point you still want to expose.

Configure deployment defaults through `FcShellOptions` at startup:

```csharp no-compile reason="illustrative shell options configuration"
builder.Services.Configure<FcShellOptions>(options =>
{
    options.DefaultDensity = DensityLevel.Compact;
    options.DefaultTheme = ThemeValue.System;
});
```

User changes persist per tenant and user. A missing tenant or user scope applies the visual change
for the current session but skips storage, so preferences do not leak across scopes.

## Parameters / slots

| Component | Public input | Purpose |
|---|---|---|
| `FcSettingsButton` | none | Uses the shell-owned dialog service and localization to open `FcSettingsDialog`. |
| `FcSettingsDialog` | cascaded `IDialogInstance?` | Lets Fluent UI close the hosted dialog. When rendered standalone in tests, Done is a no-op close. |
| `FcThemeToggle` | none | Reads theme state and dispatches `ThemeChangedAction` for Light, Dark, or System. |
| `FcDensityPreviewPanel` | `Density` | Required density to render in the preview specimen. |
| `FcDensityPreviewPanel` | `ShowForcedViewportBadge` | Shows the preview-only badge when a narrow viewport forces Comfortable density while another density is selected. |

`FcSettingsDialog` does not expose adopter slots. It is framework chrome, not a page-level extension
surface. Use shell options and localization for supported customization.

## Layout (FC-LYT)

The dialog body groups its three titled areas in one `FluentAccordion`:

- Density is the primary item and opens by default.
- Theme contains `FcThemeToggle`.
- Preview contains `FcDensityPreviewPanel`.

The preview panel renders a local `data-fc-density` wrapper so the density token cascade applies to
the specimen without changing the current page. This lets an operator inspect Compact, Comfortable,
or Roomy even when the active viewport forces a different effective density.

## Accessibility (FC-A11Y)

The settings trigger is a `FluentButton` with a localized accessible name. The dialog uses Fluent UI
dialog primitives, Fluent accordion sections, and a named density radio group. Density changes are
announced through `FcDensityAnnouncer` with `role="status"` and `aria-live="polite"` after the first
render, so loading a page does not announce stale preference text. The accessible-name expectation
matches the published [HFC1050](../../diagnostics/HFC1050.md) diagnostic guidance for custom
interactive override elements.

The viewport-forced density note renders as an informational `FluentMessageBar`. The preview-only
badge uses `role="note"` so the effective-density distinction is perceivable without relying on
color alone.

## Localization (FC-L10N)

All settings chrome strings are shell-owned and resolve through `FcShellResources`: dialog title,
density labels, theme labels, button labels, helper text, forced-viewport copy, preview-only badge,
and the density announcement template.

The preview's sample data is illustrative shell chrome for the settings specimen. Domain labels in
real generated pages remain host-owned through `[Display(Name=...)]` or the host's
`IStringLocalizer<T>`.

## Persistence

Theme and density persist under scoped keys built from tenant id and user id:

- `{tenantId}:{userId}:theme`
- `{tenantId}:{userId}:density`

Theme persistence and the user-driven theme apply path are owned by `ThemeEffects`. Density
persistence is owned by `DensityEffects`; the DOM `data-fc-density` write is owned by
`FcDensityApplier` so viewport recompute can update the effective density without creating a storage
writer. The standing NFR17 tripwire pins storage write call sites, and
`SliceSingleWriterGovernanceTests` pins the settings single-writer reading.

## Related

- [FrontComposerShell](front-composer-shell.md) — shell slot behavior and default header actions.
- [Page Toolbar](page-toolbar.md) — page-level search/filter/action toolbar.
- [Components](index.md) — component reference index.
