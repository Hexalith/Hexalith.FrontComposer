---
name: Hexalith Common Application UX
description: Common visual identity for Hexalith web applications that compose Hexalith modules through FrontComposer and Fluent UI Blazor V5.
status: accepted-supplement
updated: 2026-07-15
sources:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-17-fluent-ui-project-policy.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-17-fluent-accordion-page-sections.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-fluent-layout-components.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-fluent-theme-redefinition.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-tenants-ui-menu-icon-label-stack.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md
colors:
  surface-canvas: 'Fluent 2 --colorNeutralBackground1'
  surface-chrome: 'Fluent 2 --colorNeutralBackground2'
  surface-raised: 'Fluent 2 --colorNeutralBackground3'
  border-subtle: 'Fluent 2 --colorNeutralStroke2'
  foreground-primary: 'Fluent 2 --colorNeutralForeground1'
  foreground-secondary: 'Fluent 2 --colorNeutralForeground2'
  foreground-subtle: 'Fluent 2 --colorNeutralForeground3'
  accent-thread: 'Fluent theme accent / --fc-color-accent'
  status-success: 'Fluent Color.Success'
  status-warning: 'Fluent Color.Warning'
  status-error: 'Fluent Color.Error'
typography:
  page-title:
    note: 'FluentText Size700, Semibold, rendered as or inside the route h1'
  section-title:
    note: 'FluentText Size500 or FluentAccordionItem Heading'
  body:
    note: 'FluentText body defaults'
  caption:
    note: 'FluentText Size200, Color.Lightweight'
rounded:
  fluent-default:
    note: 'Use the active Fluent component radius'
  framed-surface-max:
    note: '8px maximum when a FrontComposer-owned framed surface is unavoidable'
spacing:
  density-unit: '--fc-spacing-unit'
  stack-gap-compact: 'calc(var(--fc-spacing-unit, 4px) * 2)'
  stack-gap-default: 'calc(var(--fc-spacing-unit, 4px) * 4)'
  chrome-padding-x: 'FluentStack / component parameters, normally 12px equivalent'
  toolbar-gap: 'FluentToolbar and FluentStack gap parameters'
components:
  shell-chrome:
    background: '{colors.surface-chrome}'
    border: '{colors.border-subtle}'
    accent-policy: '{colors.accent-thread} as thread only'
  navigation-module-entry:
    background: 'FluentButton / FluentNav defaults'
    active-accent: '{colors.accent-thread}'
    layout: 'icon above label in labeled rail'
  module-tabs:
    component: 'FluentTabs'
    placement: 'under FcPageHeader or FcPageToolbar'
  page-toolbar:
    component: 'FcPageToolbar using FluentToolbar, FluentSearch, FluentPopover, FluentMenuButton'
  projection-grid:
    component: 'FluentDataGrid'
    density: 'Compact by default, user adjustable'
  command-form:
    component: 'Generated FrontComposer command form with Fluent inputs'
  status-icon:
    component: 'FluentIcon with FluentTooltip and aria-label'
---

This accepted visual/style supplement adds detailed design tokens and component presentation. The
canonical authority is `_bmad-output/planning-artifacts/ux-design.md`, which wins any conflict in IA,
routes, accessibility, interaction behavior, or timing.

## Brand & Style

Hexalith applications are professional operational products, not marketing surfaces. The common UI should feel precise, quiet, and trustworthy for both administrators and consumer-facing module users. The design system inherits from **FrontComposer + Blazor Fluent UI V5**; this file records the Hexalith usage discipline, not a custom theme.

The visual posture is neutral chrome, dense but readable data, clear command affordances, and restrained status. The brand accent is a thread for active navigation, focus, primary actions, links, and status emphasis. It is not a surface fill for headers, navigation, footers, dashboards, or tab panels.

## Colors

All colors inherit from the active Fluent theme and Fluent 2 tokens. Do not hard-code custom colors for module pages, tabs, toolbars, grids, command forms, or state panels unless a Fluent component or token cannot express the required state.

- **Canvas** uses `{colors.surface-canvas}`.
- **Header, footer, and other chrome** use `{colors.surface-chrome}` with `{colors.border-subtle}` dividers.
- **Raised or grouped regions** use Fluent surface layering, not custom tinted cards.
- **Accent** uses `{colors.accent-thread}` only as a thread.
- **Status** uses Fluent semantic colors through icons and accessible labels; numeric counts may remain FluentBadge pills.

Avoid legacy Fluent v4 / FAST tokens, saturated chrome fills, gradients, decorative color bands, and one-off module palettes.

## Typography

Typography is Fluent-owned. Use `FluentText` roles and parameters (`Size`, `Weight`, `Color`) instead of CSS font ramps.

Ownership trace: `Typography`, `FcTypoToken`, and `TypographyStyle` are shipped by the net10-only
`Hexalith.FrontComposer.Contracts.UI` package/assembly under their existing
`Hexalith.FrontComposer.Contracts.Rendering` namespace. The nine role mappings and
`ContractsMetadata.TypographyMappingVersion = "3.1.0"` are unchanged by the assembly move.

- Page title: `{typography.page-title}`.
- Section title: `{typography.section-title}`.
- Body and helper text: Fluent defaults, with lightweight color for secondary copy.
- Do not create custom heading sizes, letter spacing, line heights, or foreground colors in module CSS.

## Layout & Spacing

Layout uses Fluent and FrontComposer primitives: `FluentLayout`, `FluentStack`, `FluentGrid` when the layout maps to the component, `FluentAccordion` for sibling titled sections, and `FcPageToolbar` for page search/filter/view/action patterns.

The common desktop structure is:

1. Neutral shell chrome.
2. One primary menu entry per Hexalith module.
3. Module workspace/dashboard page.
4. `FluentTabs` for that module's UX pages.
5. Page toolbar, grids, details, and command surfaces inside the active tab.

Use density-aware spacing through `{spacing.density-unit}` and Fluent component gap parameters. Keep raw CSS only for layout that Fluent does not own, such as positioning, overlays, sticky regions, or documented browser focus work.

## Elevation & Depth

Depth is tonal, not decorative. Use Fluent surface tokens, dividers, and component defaults. Avoid shadowed page sections and nested cards. Cards are reserved for repeated items, framed tools, and modal/dialog content where the component already owns the surface.

## Shapes

Shapes inherit Fluent defaults. If FrontComposer must create a custom framed surface, the radius must remain restrained and no larger than `{rounded.framed-surface-max}` unless the active Fluent component requires it.

## Components

- **Shell chrome**: Neutral header and footer with subtle dividers. Header actions align flush-right. User display name lives inside the account menu, not as duplicated header text.
- **Navigation module entry**: One entry per module. The labeled rail stacks the module icon above the label; count and "New" indicators sit outside the icon/label stack. Active state uses the accent thread, not a filled chrome surface.
- **Module workspace tabs**: `FluentTabs` under the page header or toolbar. Tabs are the visual control for module UX pages.
- **Page toolbar**: `FcPageToolbar` provides search, filter popover, view/overflow menu, optional tabs, and right-aligned actions.
- **Projection grid**: `FluentDataGrid` with compact density, sticky header where supported, accessible row detail regions, and explicit loading/empty/stale states.
- **Command form**: Generated FrontComposer forms use Fluent inputs and hide server-controlled or derived fields. Destructive commands use the shared confirmation dialog.
- **Status icon**: Status renders as Fluent icons with always-present `aria-label` and tooltip on hover and keyboard focus. Counts remain badges.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Use FrontComposer and Fluent UI Blazor V5 components first | Hand-roll raw buttons, inputs, selects, textareas, tabs, toolbars, or grids |
| Keep one application menu entry per module | Put every module page or projection directly into the app menu |
| Use Fluent theme colors and Fluent 2 tokens | Define custom module colors or legacy Fluent v4 / FAST tokens |
| Use the accent as an active/focus/action thread | Paint header, nav, footer, or panels with the accent |
| Use `FluentTabs` for module UX pages | Recreate tabs with links, buttons, or custom CSS |
| Express spacing through Fluent layout parameters | Rebuild design-system spacing in CSS |
