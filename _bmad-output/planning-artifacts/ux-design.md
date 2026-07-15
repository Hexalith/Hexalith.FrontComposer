---
title: Hexalith.FrontComposer UX Design Planning Source
status: canonical-planning-source
created: 2026-07-05
updated: 2026-07-15
sourceOfRecord:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/project-docs/architecture.md
  - _bmad-output/project-docs/component-inventory.md
  - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
  - _bmad-output/planning-artifacts/ux-experience-2026-07-05.md
---

# Hexalith.FrontComposer UX Design Planning Source

This document makes the UX requirements discoverable to implementation-readiness workflows and is
the canonical UX authority. If UX artifacts conflict, this file wins. The detailed UX artifact is a
visual/style supplement; the experience artifact is a behavioral/journey supplement. Neither may
override canonical product, architecture, IA, route, accessibility, or timing contracts here.

## Canonical Information Architecture

- A bounded context is presented to operators as one **Module**.
- Each Module has one primary shell entry and one required default **Module Tab**.
- Primary module-tab routes use `/{module}/{tab}`.
- Projection flyouts are secondary navigation; they do not replace the module workspace or its
  default tab.
- Generated commands use `/commands/{BoundedContext}/{CommandTypeName}` from palette, CTA, and direct
  activation paths.

## UX Requirements

### UX-DR1 - Design Tokens

`Typography`, `FcTypoToken`, `TypographyStyle`, and their Fluent mappings are supplied by the
`Hexalith.FrontComposer.Contracts.UI` package/assembly while retaining the public
`Hexalith.FrontComposer.Contracts.Rendering` namespace. The nine roles and
`TypographyMappingVersion = "3.1.0"` remain unchanged. `DensityLevel` and `DensitySurface` remain
kernel-safe and apply density tokens through `<body data-fc-density>`.

### UX-DR2 - Semantic Status Slots

Projection badge enum members render accessible status affordances. Status members render as colored Fluent icons with hover and keyboard-focus tooltip labels plus always-present `aria-label`; numeric count slots remain FluentBadge pills.

### UX-DR3 - Responsive Layout

The shell supports breakpoint-aware navigation with a unified `FrontComposerNavigation` rail. The hamburger toggle is always visible; desktop toggles labeled and icon-only rail modes. The sidebar keeps exactly one active item using longest segment-prefix matching.

Compact projection grids use the exact `32px` row metric from `DataGridDensityMetrics`; grid headers
remain sticky while the body scrolls.

### UX-DR4 - Reusable Interaction Components

The shell provides reusable interaction components including `FcCommandPalette`, `FcSettingsDialog`, `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`, and `FcLifecycleWrapper`.

FC-CNC permits one in-flight local command. A second local submit is blocked, never queued or batched,
and receives localized, accessible feedback that it did not run while the existing in-flight command
remains visible.

### UX-DR5 - Status And Empty/Loading UX

Projection loading, empty, connection, and pending-command states render through reusable components such as `FcProjectionLoadingSkeleton`, `FcProjectionEmptyPlaceholder`, `FcProjectionConnectionStatus`, and `FcPendingCommandSummary`.

Lifecycle UX distinguishes HTTP acceptance from projection/status confirmation and names
`IdempotentConfirmed`, `NeedsReview`, `Warning`, and `Degraded`. Default evidence budgets are
confirming-to-Degraded at `10_000` ms, polling every `1_000` ms for up to `120_000` ms, and one
transient retry after `250` ms.

### UX-DR6 - Accessibility Patterns

Generated and hand-authored UI must conform to WCAG 2.2 AA and preserve skip links, visible focus
indicators, row-detail regions, live regions, keyboard reachability, reduced-motion behavior,
forced-colors behavior, accessible names, stable test selectors, and support-safe messaging.

### UX-DR7 - Page Layout Contract

The page-layout contract supports full-width and constrained content. Full-width is the default; constrained layout uses the approved maximum measure.

### UX-DR8 - Account Control And Server Security

The shell owns an always-rendered account menu backed by framework-owned server security wiring. Domain modules supply domain-specific security configuration rather than duplicating generic account-control plumbing.

## Governance Rules

- Use FrontComposer or Fluent UI Blazor v5 components for interactive UI.
- Do not introduce raw `<button>`, `<input>`, `<select>`, or `<textarea>` controls outside documented test/specimen carve-outs.
- Use Fluent 2 tokens and component parameters; do not recreate theme primitives in custom CSS.
- Group multiple sibling titled page sections with `FluentAccordion` where appropriate.
- Prefer Fluent layout primitives for design-system-owned layout.
- Use rendered-DOM, computed-style, bUnit, e2e, or governance-test evidence for visual and accessibility-sensitive changes.

## Story Design Notes

For visual or layout-sensitive stories, the story file must cite this canonical artifact and may cite
the detailed visual supplement, the experience/journey supplement, component inventory, an approved
sprint-change proposal, or a story-local design note for added depth. Supplementary artifacts may add
detail but cannot change the canonical IA, route, WCAG 2.2 AA, FC-CNC, or timing behavior above.

## Related Planning Artifacts

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md`
