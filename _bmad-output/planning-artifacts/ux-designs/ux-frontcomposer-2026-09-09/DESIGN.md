---
name: Hexalith Common Application UX
description: Visual identity contract for operational web applications composed with FrontComposer and Blazor Fluent UI V5.
status: draft
updated: 2026-09-09
sources:
  - _bmad-output/planning-artifacts/ux-design.md
  - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
  - _bmad-output/planning-artifacts/ux-experience-2026-07-05.md
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/prd-addendum-2026-09-08.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/epics.md
colors:
  accent-thread: '#0097A7'
typography:
  page-title:
    note: 'Inherited FluentText Size700 and Semibold; rendered as, or inside, the route-level h1.'
  section-title:
    note: 'Inherited FluentText Size500 or the active Fluent accordion heading role.'
  body:
    note: 'Inherited Fluent body role.'
  caption:
    note: 'Inherited FluentText Size200 with the active lightweight foreground role.'
rounded:
  fluent-default:
    note: 'Inherit the active Fluent component radius.'
  framed-surface-max: 8px
spacing:
  navigation-rail-labeled: 72px
  navigation-rail-icon-only: 48px
  projection-row-compact: 32px
  constrained-content-max: 75rem
components:
  shell-frame:
    background: 'Inherited Fluent neutral background roles'
    accent: '{colors.accent-thread}'
  navigation-rail:
    labeled-width: '{spacing.navigation-rail-labeled}'
    icon-only-width: '{spacing.navigation-rail-icon-only}'
    active-accent: '{colors.accent-thread}'
  module-tabs:
    component: 'FrontComposer route-backed tabs over inherited Fluent tab visuals'
  page-toolbar:
    component: 'FrontComposer toolbar over inherited Fluent controls'
  projection-grid:
    component: 'Inherited Fluent data-grid visuals'
    compact-row-height: '{spacing.projection-row-compact}'
  command-form:
    component: 'Generated form using inherited Fluent input visuals'
  lifecycle-feedback:
    component: 'Inherited Fluent semantic feedback visuals'
  status-affordance:
    component: 'Inherited Fluent semantic icon, tooltip, and badge visuals'
  fresh-row-indicator:
    accent: '{colors.accent-thread}'
    meaning: 'Shape and text must remain perceivable without color or motion'
---

# Hexalith Common Application Design

This file owns **how FrontComposer surfaces look**. `EXPERIENCE.md` is its peer and owns information architecture, behavior, states, interactions, accessibility behavior, and journeys. Together they supersede the legacy single-file UX precedence chain. Within their respective domains, both spines win over mockups, wireframes, imports, historical supplements, and implementation examples.

## Brand & Style

Hexalith FrontComposer is a professional operations framework, not a marketing surface. Its visual posture is precise, quiet, trustworthy, and dense enough for administrators, support operators, adopter developers, AI-agent integrators, framework maintainers, and release owners.

The design system inherits from **FrontComposer + Blazor Fluent UI V5**. Fluent owns the theme, component anatomy, type ramp, spacing defaults, semantic colors, focus treatment, elevation, and interactive state visuals. This spine records only the Hexalith composition discipline and product-specific deltas. It does not redefine Fluent.

Neutral chrome frames the product. Data and task state carry hierarchy. The configured accent is a thread through active navigation, focus, primary actions, links, and selected emphasis; it is never a decorative brand wash or a chrome fill. An optional adopter brand fragment may appear once in the header start, with an accessible name; no placeholder is injected when it is absent.

## Colors

`{colors.accent-thread}` is the default value of configurable `FcShellOptions.AccentColor`. It is a default, not a fixed product palette: adopters may configure it, while the shell preserves the accent-as-thread rule.

All other roles—including canvas, chrome, raised surfaces, borders, foregrounds, focus, success, warning, error, information, disabled, hover, and selected states—inherit the active Fluent theme and Fluent 2 tokens. Light, dark, reduced-motion, and forced-colors presentations remain theme-owned.

- Use the accent for active navigation, focus emphasis, primary actions, links, and a fresh-row thread.
- Keep header, navigation, footer, page bodies, tab panels, and grouped surfaces on Fluent neutral roles.
- Render semantic state with icon or shape plus text; color is supplemental.
- Validate the configured accent and every load-bearing foreground/background combination to WCAG 2.2 AA in each supported theme. If the accent does not meet the needed contrast in a placement, use the inherited Fluent semantic role and retain another non-color cue.

[NOTE FOR UX] The exact inherited Fluent token/API role that replaces the legacy `--fc-color-accent` alias has not been approved against the selected Fluent V5 pin. Do not encode an additional alias or hard-coded substitute in this spine.

## Typography

Typography is Fluent-owned. Use `{typography.page-title}`, `{typography.section-title}`, `{typography.body}`, and `{typography.caption}` through Fluent component parameters and the nine existing `FcTypoToken` mappings. `TypographyMappingVersion = "3.1.0"` remains unchanged; the contract lives in `Hexalith.FrontComposer.Contracts.UI` while retaining the public `Hexalith.FrontComposer.Contracts.Rendering` namespace.

Route titles are real, focusable `h1` headings. Helper copy, counts, timestamps, and secondary status use inherited caption or lightweight roles. Do not recreate font size, weight, line height, letter spacing, or foreground roles in module CSS.

## Layout & Spacing

The default page layout is full width. Opt-in constrained pages cap their measure at `{spacing.constrained-content-max}`. The application frame is desktop-first responsive web, with semantic Desktop, Compact, and Narrow-browser behavior defined in the peer spine.

The primary navigation rail is `{spacing.navigation-rail-labeled}` when labels are shown and `{spacing.navigation-rail-icon-only}` in icon-only mode. The hamburger remains visible in both modes. Compact projection rows are exactly `{spacing.projection-row-compact}`, and projection headers remain sticky while the grid body scrolls.

Use FrontComposer and Fluent layout primitives for design-system-owned layout. Raw CSS is limited to layout or browser behavior Fluent does not own: sticky regions, overlays, positioning, user-agent resets, and the focusable route heading. Page titles, breadcrumbs, toolbars, navigation chrome, and a single primary content region stay outside section accordions. A page, dialog, or detail panel with two or more sibling titled content sections uses one inherited Fluent accordion; if a primary section belongs there, it is expanded by default.

[NOTE FOR UX] Numeric responsive breakpoints are not specified by the confirmed sources. Preserve Desktop, Compact, and Narrow-browser behavior without fabricating widths.

## Elevation & Depth

Depth is tonal and component-owned. Use inherited Fluent surface layering, dividers, popovers, dialogs, and shadows. Page hierarchy comes from layout, type, state, and neutral surface roles—not nested cards or custom shadows.

Do not add shadowed page sections, decorative gradients, tinted dashboard bands, or elevation solely to make a surface feel “branded.” Repeated items, modal content, popovers, and framed tools may use the elevation already owned by their Fluent component.

## Shapes

Shapes inherit Fluent defaults via `{rounded.fluent-default}`. When FrontComposer must own a framed surface for which Fluent has no component, keep the radius no larger than `{rounded.framed-surface-max}`. Imagery and nested content follow the containing component radius.

Avoid custom pill surfaces. Pills remain appropriate only where the inherited Fluent badge/count pattern owns them; semantic statuses use the icon-plus-label pattern instead.

## Components

Every entry below has behavioral peer coverage in `EXPERIENCE.md`. Fluent implementation details inherit the selected V5 package; names below identify the public or generated FrontComposer surface, not permission to restyle Fluent internals.

| Component / pattern | Visual contract |
|---|---|
| **shell-frame — `FrontComposerShell`** | Neutral header/footer chrome with subtle inherited dividers around a neutral content canvas. Header actions align to the end. The accent is only a thread; the optional brand fragment appears once before the app title. |
| **navigation-rail — `FrontComposerNavigation` + `FcHamburgerToggle`** | One rail at `{spacing.navigation-rail-labeled}` or `{spacing.navigation-rail-icon-only}`. Labeled items stack icon above short label; icon-only items stay centered. Active state combines filled/selected icon treatment, accent edge, and inherited focus/current-state styling. Count and New indicators sit outside the icon/label stack. |
| **account-control — `FcAccountMenu`** | Always-present account affordance in header chrome, using inherited avatar/menu styling. The user display name belongs inside the menu, not as duplicated header text. |
| **home-directory — `FcHomeDirectory` + `FcHomeCard`** | Neutral directory surface with skeletons that preserve card geometry. Ready/actionable modules receive hierarchy through ordering, text, counts, and inherited surface emphasis—not saturated fills. “Other areas” uses the inherited accordion treatment. |
| **command-palette — `FcCommandPalette`** | Inherited dialog/combobox/search/result visuals. Keyboard highlight and focus use the active Fluent selected/focus roles; no custom palette color ramp. |
| **settings — `FcSettingsDialog`** | Inherited dialog, radio, toggle, preview, and action visuals. Density preview demonstrates the selected density without inventing a second token system. |
| **page-frame — `FcPageHeader` + `FcPageLayout`** | A clear route title using `{typography.page-title}` above full-width or constrained content. No accent title band. |
| **module-tabs — `FcPageTabs` / inherited Fluent tabs** | Route-backed module views use the inherited selected-tab indicator below the page header or toolbar. Tabs never mimic primary shell navigation. |
| **page-toolbar — `FcPageToolbar`** | One coherent inherited toolbar strip: leading search/filter/view affordances and end-aligned actions. Its internal Fluent composition is implementation-owned and must not be promoted into a new visual API accidentally. |
| **page-sections — inherited Fluent accordion** | Two or more sibling titled sections share one accordion. The primary section is visually discoverable and expanded by default when placed inside; the only primary region is never collapsed. |
| **projection-grid — generated inherited Fluent data grid** | Compact default rows are exactly `{spacing.projection-row-compact}`; header is sticky and body scrolls. Hover, selection, focus, separators, column resizing, and virtualization styling inherit Fluent. |
| **projection-placeholders — `FcProjectionLoadingSkeleton` + `FcProjectionEmptyPlaceholder`** | Skeleton geometry mirrors the expected Card, Timeline, or Grid layout. Empty treatment is neutral, uses inherited illustration/icon treatment if supplied, and presents no false error coloration. |
| **projection-health — `FcProjectionConnectionStatus` + `FcSlowQueryNotice` + `FcMaxItemsCapNotice`** | Inherited semantic message/notice treatment above or adjacent to the affected grid without shifting shell chrome. Reconnecting may use motion only when allowed; every state retains text and shape in reduced motion and forced colors. |
| **row-detail — `FcExpandInRowDetail` + `FcExpandedRowHiddenBanner`** | Expanded content reads as a nested region within the row, separated by inherited surface/stroke roles. The filter-hidden notice is visually adjacent to the grid and distinguishable without color alone. |
| **status-affordance — `FcStatusIcon` + inherited icon/tooltip; `FcDesaturatedBadge` + inherited badge** | Semantic status is a lightweight Fluent icon with an always-available accessible label and a label tooltip on hover/focus. Counts remain pill badges; non-urgent counts use desaturated treatment. Color never carries status alone. |
| **command-form — generated form + `FcFieldPlaceholder`** | Fluent inputs and validation styling only. Server-controlled/derived fields do not leave visual gaps. Unsupported types show a bounded neutral placeholder rather than a broken control. Density is Inline, CompactInline, or FullPage as defined behaviorally. |
| **command-authorization — `FcAuthorizedCommandRegion`** | Pending, Authorized, and NotAuthorized use inherited progress/content/denied treatments with stable geometry; no protected form flashes before authorization resolves. |
| **command-safety — `FcDestructiveConfirmationDialog` + `FcFormAbandonmentGuard`** | Inherited destructive dialog and warning/message treatment. Destructive emphasis uses Fluent semantic roles rather than the brand accent. |
| **lifecycle-feedback — `FcLifecycleWrapper` + `FcPendingCommandSummary`** | Inherited semantic badges/message bars distinguish progress, confirmation, rejection, review, warning, and degradation. Accepted transport never receives the confirmed-success treatment. |
| **fresh-row-indicator — `FcNewItemIndicator`** | A restrained row-level marker using `{colors.accent-thread}` only as supplemental emphasis. Shape/text persist in forced colors; reduced motion removes animation without removing meaning. |
| **customization-diagnostic — `FcCustomizationDiagnosticPanel`** | Development-only inherited diagnostic-panel treatment, visually distinct from operator data while avoiding raw payload, tenant, token, or stack-trace presentation. |

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit FrontComposer and Fluent UI V5 for all design-system-owned visuals | Redefine the Fluent theme, its typography, spacing, semantic palette, focus, or component anatomy |
| Keep `FcShellOptions.AccentColor` configurable with default `{colors.accent-thread}` | Hard-code other Fluent theme roles or use the accent as header, navigation, footer, dashboard, or panel fill |
| Use Fluent 2 roles and component parameters | Use legacy Fluent V4/FAST tokens or one-off module palettes |
| Use semantic icon/shape plus text and accessible naming | Communicate status through color, motion, tooltip, or hover alone |
| Preserve `{spacing.navigation-rail-labeled}` / `{spacing.navigation-rail-icon-only}` rails, `{spacing.projection-row-compact}` rows, sticky headers, and `{spacing.constrained-content-max}` constrained measure | Invent new dimensions or numeric responsive breakpoints |
| Use FrontComposer/Fluent controls and page-section accordion rules | Hand-roll interactive controls, tabs, toolbars, grids, or hide the only primary region |
| Keep visual state changes stable under zoom, text spacing, reduced motion, and forced colors | Add layout shift, decorative animation, nested card stacks, gradients, or bespoke shadows |

### Open visual questions

- [NOTE FOR UX] Warning, NeedsReview, Degraded, missing-tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal/forced-colors treatments have behavioral contracts but no approved exact presentation or microcopy. Until decided, inherit the nearest Fluent semantic treatment, preserve the stated meaning, and do not create a new visual language.
- [NOTE FOR UX] Numeric responsive breakpoints remain unspecified.
- [NOTE FOR UX] The selected Fluent V5 catalog pin and the approved exact token/API role for the configurable accent must be reconciled before the visual contract can be marked final.
