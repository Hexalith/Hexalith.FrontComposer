---
name: Hexalith Common Application UX
description: Common visual identity for Hexalith web applications composed through FrontComposer and Fluent UI Blazor V5.
status: accepted-supplement
product_approval: pending-reapproval
updated: 2026-09-09
reconciliation_revision: oi-16-2026-09-09
sources:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/prd-addendum-2026-09-08.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-17-fluent-ui-project-policy.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-17-fluent-accordion-page-sections.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-fluent-layout-components.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-fluent-theme-redefinition.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-tenants-ui-menu-icon-label-stack.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md
colors:
  surface-canvas: 'inherit Fluent V5 --colorNeutralBackground1'
  surface-chrome: 'inherit Fluent V5 --colorNeutralBackground2'
  surface-raised: 'inherit Fluent V5 --colorNeutralBackground3'
  border-subtle: 'inherit Fluent V5 --colorNeutralStroke2'
  foreground-primary: 'inherit Fluent V5 --colorNeutralForeground1'
  foreground-secondary: 'inherit Fluent V5 --colorNeutralForeground2'
  foreground-subtle: 'inherit Fluent V5 --colorNeutralForeground3'
  accent-thread: 'alias of the active Fluent V5 accent role; no independent seed or palette'
typography:
  page-title:
    note: 'Fluent V5 Size700 Semibold, rendered as or inside the route h1'
  section-title:
    note: 'Fluent V5 Size500 or inherited accordion heading'
  body:
    note: 'Fluent V5 body defaults'
  caption:
    note: 'Fluent V5 Size200 with the inherited secondary foreground role'
rounded:
  fluent-default:
    note: 'Use the active Fluent V5 component radius'
  framed-surface-max: 8px
spacing:
  density-unit: 'var(--fc-spacing-unit, 4px)'
  stack-gap-compact: 'calc(var(--fc-spacing-unit, 4px) * 2)'
  stack-gap-default: 'calc(var(--fc-spacing-unit, 4px) * 4)'
  chrome-padding-x: 12px
  toolbar-gap: '{spacing.stack-gap-compact}'
components:
  shell-chrome:
    background: '{colors.surface-chrome}'
    border: '{colors.border-subtle}'
    accent-policy: '{colors.accent-thread} as thread only'
  navigation-module-entry:
    component: 'FrontComposerNavigation'
    active-accent: '{colors.accent-thread}'
    layout: 'icon above label in labelled rail; persistent accessible name in icon-only rail'
  module-tabs:
    component: 'FcPageTabs with FcPageTab children'
    placement: 'under the route page header or FcPageToolbar'
  page-toolbar:
    component: 'FcPageToolbar; internal Fluent composition is not public contract'
  projection-grid:
    component: 'inherited Fluent V5 data-grid surface through FrontComposer'
    density: '32px compact default; content-driven expansion under accessibility overrides'
  command-form:
    component: 'generated FrontComposer command form with inherited Fluent V5 inputs'
  status-affordance:
    component: 'semantic icon plus visible text; optional FluentTooltip and persistent accessible name'
---

# Hexalith Common Application UX — Visual Supplement

This visual/style supplement owns how the FrontComposer experience looks. The canonical authority is
`_bmad-output/planning-artifacts/ux-design.md`, which wins on IA, routes, behavior, accessibility
outcomes, timing, evidence status, and approval state. The behavior supplement is
`ux-experience-2026-07-05.md`. These three files win over mockups, wireframes, imports, and historical
screens. This reconciliation does not record Product approval or close G-4, OI-16, or OI-19.

## Brand & Style

Hexalith applications are professional operational products, not marketing surfaces. The UI is
precise, quiet, and trustworthy for administrators and support operators. It inherits from
**FrontComposer + Blazor Fluent UI V5**; this file records usage and accessibility deltas, not a new
theme.

The visual posture is neutral chrome, compact but readable data, clear command affordances, and
restrained status. Accent is a thread for current navigation, focus, primary actions, and links. It is
not a fill for headers, navigation, footers, dashboards, or tab panels.

## Colors

All colors inherit from the active Fluent V5 theme and Fluent 2 tokens. The semantic references in
frontmatter intentionally have no product-owned hex values: repository policy forbids redefining the
theme. `--fc-color-accent`, when an implementation exposes it, aliases the active Fluent V5 accent
role and cannot define an independent seed or palette. Status colors are not FrontComposer
pseudo-tokens: each owning Fluent component supplies its supported semantic appearance/parameters and
active-theme roles, while UX-VC-1 defines the required observable contrast and fallback.

### UX-VC-1 - Contrast And Forced-Colors Matrix

| ID | Visual pair/state | Light and dark requirement | Forced-colors fallback | Acceptance |
| --- | --- | --- | --- | --- |
| VC-01 | Primary text on canvas/raised/chrome | `{colors.foreground-primary}` meets 4.5:1 for normal text and 3:1 for large text on each owning surface | `CanvasText` on `Canvas`; inherited system selection remains intact | Computed-style contrast in both themes plus forced-colors snapshot |
| VC-02 | Secondary/caption text | Inherited role meets the applicable text ratio; never use low contrast for required instructions, status, or errors | `CanvasText`; disabled-only content may use `GrayText` | Computed-style assertion for every changed pair |
| VC-03 | Current navigation/tab | Accent/thread plus persistent current marker and accessible current/selected state; meaningful boundary meets 3:1 | System outline/border and visible current text; never background color alone | Current state visible with authored colors suppressed |
| VC-04 | Keyboard focus | Focus indicator meets 3:1 against adjacent colors and is distinct from selection/current state | `Highlight`/`CanvasText` or system focus outline; `forced-color-adjust: auto` | Keyboard screenshot + computed geometry/contrast |
| VC-05 | Status, stale, reconnect, lifecycle | Icon/shape and visible status text meet non-text/text ratios; semantic color is supplemental | `CanvasText` icon/text with system border; state name remains visible | One snapshot per state family in light/dark/forced colors |
| VC-06 | Fresh row | Visible “New/Updated” text and icon/shape accompany any accent/background treatment | System border plus visible state text/icon; no animation or fill dependency | Appearance/expiry snapshots and DOM-state assertion |
| VC-07 | Validation/error/rejection | Error summary, field relationship, icon, and text remain distinct; error color is supplemental | `CanvasText` plus system border/outline; invalid relationship remains in DOM | Computed contrast + semantic DOM assertion |
| VC-08 | Disabled/unavailable | Disabled state is distinguishable from enabled without color alone and retains readable explanation where needed | `GrayText` plus native disabled semantics; no opacity-only distinction | State comparison in all modes |
| VC-09 | No tenant/no access | Neutral panel, heading, explanation, and recovery action use normal text/action roles | `Canvas`/`CanvasText` with system button/link treatment | Theme/forced-colors snapshots plus accessible-name assertion |

Avoid legacy Fluent v4/FAST tokens, saturated chrome fills, gradients, decorative color bands, custom
module palettes, and `forced-color-adjust: none` without a component-specific reviewed necessity.

## Typography

Typography is Fluent-owned. Use the inherited text roles and component parameters instead of custom
font ramps. The ownership trace remains: `Typography`, `FcTypoToken`, and `TypographyStyle` ship from
the net10-only `Hexalith.FrontComposer.Contracts.UI` package/assembly under
`Hexalith.FrontComposer.Contracts.Rendering`; the nine mappings and
`ContractsMetadata.TypographyMappingVersion = "3.1.0"` are unchanged.

### UX-TS-1 - Text-Spacing Acceptance

| Override | Required value | Visual pass condition |
| --- | --- | --- |
| Line height | At least 1.5 times font size | No clipping, overlap, hidden text, or control loss |
| Paragraph spacing | At least 2 times font size | Sections and messages remain associated and readable |
| Letter spacing | At least 0.12 times font size | Labels, buttons, tabs, badges, and grid cells remain complete |
| Word spacing | At least 0.16 times font size | Wrapping does not conceal status or operation meaning |

The four values are applied together in the WCAG 1.4.12 test. Compact `32px` grid rows grow with
content. Ellipsis may shorten optional preview text only when the complete value remains available by
an accessible, keyboard-reachable method; instructions, errors, statuses, and controls never truncate.

## Layout & Spacing

Layout uses FrontComposer and inherited Fluent V5 primitives. Multiple sibling titled sections use
`FluentAccordion`; route titles, breadcrumbs, toolbars, navigation chrome, and a single primary content
region stay outside. `FcPageToolbar` owns the public search/filter/view/action surface; its internal
composition is not a separately promised API.

### UX-RF-1 - Reflow, Zoom, Target, And Focus Geometry

| Surface | 320 CSS pixels / 400% zoom | Target-size rule | Unobscured-focus rule |
| --- | --- | --- | --- |
| `FrontComposerShell` / `FrontComposerNavigation` | Drawer/rail and content form one reading-order stream; no page-level horizontal scroll | Skip, account, settings, menu, hamburger, and navigation actions are at least 24×24 CSS px | Skip target, navigation, and route heading clear sticky header/footer |
| Home directory | Module cards and their authorized CTAs stack without content or operation loss | Card links, CTAs, refresh, and other Home actions are at least 24×24 CSS px | Focused card/action remains entirely outside sticky chrome and transient messages |
| `FcPageTabs` / `FcPageTab` | Tab strip may own horizontal scrolling; selected tab and panel remain associated | Each tab meets 24×24 CSS px or records an applicable standard exception | Active tab and ring remain fully visible inside tab-strip scrollport |
| `FcPageToolbar` | Controls wrap or enter an accessible overflow without reordering focus | Search, filter, view, overflow, and actions meet 24×24 CSS px | Expanded popover/menu and invoking control remain visible |
| Projection grid/detail | Intrinsically two-dimensional grid may own horizontal scroll; omitted columns remain accessible | Row actions and disclosure targets meet 24×24 CSS px; inline text exception is never used for icon buttons | Focused cell/action/detail stays clear of sticky header and scroll edges |
| Command form/lifecycle | Fields, summary, actions, and state stack to one column without loss | Inputs, lifecycle actions, and recovery actions meet 24×24 CSS px; inline text-link exceptions are documented | Summary, invalid field, lifecycle, and recovery action are entirely visible |
| Palette/dialog/abandonment guard | Overlay or in-flow warning fits the viewport/reading order and owns bounded scrolling where needed; no control falls outside it | Results, close, dialog, Stay, and Leave actions meet 24×24 CSS px | Modal focus stays inside the active overlay; the in-flow guard and every returned origin/fallback remain unobscured |

Target-size exceptions are limited to the five WCAG 2.2 SC 2.5.8 cases and are recorded separately:
**Inline** (target in a sentence or constrained by line height), **Spacing** (a 24 CSS-pixel diameter
circle centered on each undersized target intersects neither another target nor such a circle around
one), **Equivalent** (a separate conforming control provides the same function), **User Agent
Control**, or **Essential**. Evidence records the target, selected exception, spacing measurement or
equivalent control, rationale, and keyboard path. An exception never removes keyboard operation.

## Elevation & Depth

Depth is tonal, not decorative. Use inherited Fluent surface roles, dividers, and component defaults.
Avoid shadowed page sections and nested cards. Cards are limited to repeated items, framed tools, and
modal/dialog content where the component already owns the surface. In forced colors, elevation is not
a meaning carrier; borders, text, and semantics survive without shadows.

## Shapes

Shapes inherit Fluent V5 defaults. If FrontComposer must create a custom framed surface, its radius is
no larger than `{rounded.framed-surface-max}` unless an inherited component requires otherwise. Shape
may reinforce a status but never replaces text, icon, accessible state, or border.

## Components

The identifiers below are exact and match the behavior supplement. “Inherited” means the active Fluent
V5 component owns its visual tokens; it does not waive UX-VC-1 or the canonical evidence matrices.

| Component | Visual contract |
| --- | --- |
| `FrontComposerShell` | Neutral canvas/chrome with visible skip target, account access, route heading, content, and footer separation. |
| `FrontComposerNavigation` | One current Module; labelled rail stacks icon above label, icon-only mode retains accessible name and optional tooltip; badges do not replace labels. |
| `FcPageTabs` | Page-body tab list inherits Fluent V5 visuals and exposes one visible selected state distinct from focus. |
| `FcPageTab` | Tab/header and owned panel remain visually associated; disabled and selected states satisfy UX-VC-1. |
| `FcPageToolbar` | Search/filter/view/overflow/action composition follows inherited control visuals and responsive wrapping; internal Fluent composition is not public contract. |
| `FcCommandPalette` | Bounded overlay with visible query focus, active result, no-results state, and close affordance. |
| `FcSettingsDialog` | Fluent-owned dialog surface with labelled heading, bounded scroll, and visible safe actions. |
| `FcDestructiveConfirmationDialog` | Destructive consequence and both actions are explicit; Confirm may use primary appearance while the non-destructive Cancel receives initial focus. Visual appearance never changes the safe focus contract. |
| `FcFormAbandonmentGuard` | In-flow warning appears only after the 30-second edit threshold; “Stay on form” receives initial focus and “Leave anyway” is the explicit destructive choice. It is not a modal dialog. |
| `FcLifecycleWrapper` | Visible state name, icon, explanatory text, and permitted actions for every UX-SS-1 lifecycle state; pulse is non-essential. |
| `FcProjectionLoadingSkeleton` | Geometry approximates the resolving surface; animation stops under reduced motion. |
| `FcProjectionEmptyPlaceholder` | Heading, explanation, and at most one authorized CTA; no decorative false urgency. |
| `FcProjectionConnectionStatus` | Persistent text/icon distinction for stale, reconnecting, fallback, and recovery; no color-only dot. |
| `FcPendingCommandSummary` | Bounded pending/rejected summary; rows keep visible state text and recovery/action hierarchy. |
| `FcNewItemIndicator` | Static icon/shape plus visible text; initial accent/animation is optional, never semantic; expiry leaves normal row appearance. |
| `FluentAccordion` (inherited) | Used for multiple sibling titled sections; the primary item is expanded by default. |
| `FluentBadge` (inherited) | Numeric counts only; not used as the sole carrier for semantic status. |
| `FluentTooltip` (inherited) | Supplemental concise label on hover and keyboard focus; never the sole accessible name or state text. |

## UX-RM-1 - Reduced-Motion Matrix

| Motion source | Default | `prefers-reduced-motion: reduce` | Meaning retained by |
| --- | --- | --- | --- |
| Navigation/drawer transition | Inherited short transition | Disable non-essential slide/fade; state changes immediately | Current text/marker and focus |
| Skeleton/loading shimmer | Inherited when present | Static skeleton | Loading text/status semantics |
| Lifecycle Syncing/pending pulse | Optional restrained pulse | No pulse/loop | Visible state text/icon and AM-12 |
| Reconnect/recovery transition | Optional short emphasis | Immediate state swap | Connection text/icon and AM-05–AM-07 |
| Fresh-row arrival/expiry | Optional short emphasis | Static `New/Updated` cue; silent removal | Visible text/icon/shape and AM-21 |
| Focus/scroll | Instant or minimal browser behavior | No smooth animated scrolling; place focus immediately | Focus indicator and UX-FM-1 destination |

## Do's and Don'ts

| Do | Don't |
| --- | --- |
| Use FrontComposer and Fluent UI Blazor V5 components first | Hand-roll raw interactive controls when an equivalent exists |
| Use exact public identifiers from the shared component registry | Promise implementation-owned internals or nonexistent Fluent APIs |
| Keep one shell entry per Module and use `FcPageTabs`/`FcPageTab` inside | Put every projection or command in primary navigation |
| Use inherited Fluent V5 roles and verify contrast in every mode | Define a custom module palette or legacy token |
| Pair color with visible text/icon/shape and accessible state | Depend on color, animation, tooltip, or hover alone |
| Let content grow under zoom and text-spacing overrides | Enforce `32px` rows when doing so clips content |
| Keep every focused element entirely clear of app-owned overlays | Allow sticky chrome, messages, or dialogs to cover focus |

## Source Journey Coverage

| Source journey | Visual applicability | Behavior owner |
| --- | --- | --- |
| UJ-1. Nina boots a domain shell from annotated types. | Shell loading, empty, startup-failure, navigation, and generated-surface presentation | Experience Flow UJ-1 |
| UJ-2. Marc investigates a live projection. | Full shell/navigation/grid/status/fresh-row visual contract | Experience Flow UJ-2 |
| UJ-3. Marc executes a command safely. | Form, validation, confirmation, lifecycle, rejection, and degraded visuals | Experience Flow UJ-3 |
| UJ-4. Ravi exposes the domain surface to an AI agent. | No additional human-facing visual surface; shared semantics must not contradict the MCP contract | Experience Flow UJ-4 |
| UJ-5. Camille preserves generator/runtime compatibility. | No product-shell visual delta; diagnostics/docs examples must use exact component names | Experience Flow UJ-5 |
| UJ-6. Sophie tests a generated consumer experience. | Evidence fixtures render every visual matrix state without creating a second visual system | Experience Flow UJ-6 |
