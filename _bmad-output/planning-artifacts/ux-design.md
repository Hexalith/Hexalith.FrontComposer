---
title: Hexalith.FrontComposer UX Design Planning Source
status: canonical-planning-source
created: 2026-07-05
updated: 2026-07-11
sourceOfRecord:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/project-docs/architecture.md
  - _bmad-output/project-docs/component-inventory.md
---

# Hexalith.FrontComposer UX Design Planning Source

This document makes the UX requirements discoverable to implementation-readiness workflows. The UX requirements were originally embedded in `epics.md`, architecture section 4, component inventory, and approved sprint change proposals. This file is the planning artifact that readiness checks should load.

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

### UX-DR4 - Reusable Interaction Components

The shell provides reusable interaction components including `FcCommandPalette`, `FcSettingsDialog`, `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`, and `FcLifecycleWrapper`.

### UX-DR5 - Status And Empty/Loading UX

Projection loading, empty, connection, and pending-command states render through reusable components such as `FcProjectionLoadingSkeleton`, `FcProjectionEmptyPlaceholder`, `FcProjectionConnectionStatus`, and `FcPendingCommandSummary`.

### UX-DR6 - Accessibility Patterns

Generated and hand-authored UI must preserve skip links, visible focus indicators, row-detail regions, live regions, keyboard reachability, reduced-motion behavior, forced-colors behavior, accessible names, stable test selectors, and support-safe messaging.

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

For visual or layout-sensitive stories, the story file must cite the richer design source used:
`epics.md` UX-DRs, architecture section 4, component inventory, an approved sprint-change proposal, or
a story-local design note. This concise UX artifact is sufficient for readiness discovery, but not
automatically sufficient for pixel or layout decisions.

## Related Planning Artifacts

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md`
