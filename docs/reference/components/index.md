---
title: "Components"
description: "Adopter reference pages for FrontComposer shell components, governed by the FC-DOC documentation contract."
genre: reference
audience: adopter
ownerStory: 1-5-produce-the-fc-doc-component-documentation-contract
status: published
reviewed: 2026-06-03
uid: frontcomposer.reference.components
slug: reference/components/
---

# Components

Adopter-facing reference pages for the FrontComposer shell component library. Each page documents a
component to the **FC-DOC contract** — a fixed section set (Overview, Usage, Parameters / slots,
Layout, Accessibility, Localization, Related) so you can adopt a component without reading its source.

## Component pages

- [FrontComposerShell](front-composer-shell.md) — the root application shell (Header / Navigation / Content / Footer).
- [Navigation](navigation.md) — the registry-driven navigation rail (`FrontComposerNavigation`).
- [DataGrid Surface](datagrid.md) — the confirmed FC-TBL table surface for generated projection grids.

## Coverage and tracked gaps

The read-only-MVP component set is **layout, navigation, DataGrid surface, and settings**. Layout,
navigation, and the FC-TBL DataGrid surface are documented above. One area remains a **tracked gap
with a named owner**, pending the story that finalizes its surface:

- **Settings** (`FcSettingsDialog`, `FcThemeToggle`, `FcDensityPreviewPanel`) — owed when Story 1.6
  finalizes the settings / theme / density UX.

## Related

- [Reference](../index.md) — the full reference index (API, diagnostics, CLI, IDE, generated-output, MCP, pact).
- [Diagnostics](../../diagnostics/HFC1050.md) — accessibility override diagnostics (`HFC1050`–`HFC1055`).
