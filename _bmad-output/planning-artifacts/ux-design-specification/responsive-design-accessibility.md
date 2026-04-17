# Responsive Design & Accessibility

This section consolidates all responsive and accessibility decisions from previous steps into a single reference. The design decisions themselves are established in Steps 3, 8, and 9; this section provides the consolidated implementation reference that developers need when building and testing.

### Responsive Strategy

**Desktop-first, responsive down.** FrontComposer is an operational business application, not a consumer product. The primary users (developers configuring, business users operating) work at desks with keyboards and mice. The responsive strategy degrades gracefully to smaller viewports rather than building up from mobile.

**Three viewport tiers:**

| Tier | Width | Name | Design commitment |
|---|---|---|---|
| **Desktop** | ≥1366px | Full experience | All features, sidebar expanded, compact DataGrid density |
| **Compact desktop** | 1024px-1365px | Adapted experience | Sidebar auto-collapsed to icon-only, all features functional |
| **Tablet** | 768px-1023px | Touch-adapted experience | Drawer navigation, comfortable density (44px touch targets), all features functional |
| **Phone** | <768px | Functional fallback | Single-column layout, drawer nav, tap-to-expand DataGrid rows, usable but not optimized. Not a design target for v1. |

**Design commitment distinction:** Desktop and compact desktop receive full design attention and visual regression testing. Tablet receives touch-adaptation testing. Phone is functional (no broken layouts, no missing features) but is not optimized for daily use.

**Blazor Server mobile latency note:** On phone-tier connections (3G, spotty wifi), Blazor Server's rendering model produces perceptible UI delays (200-500ms) for overlay interactions (drawer nav, command palette). This is inherent to Blazor Server's round-trip rendering, not a FrontComposer bug. Blazor Auto mode (production deployment) mitigates this after WASM download completes, as overlay interactions become client-side. Adopters testing on phones during development (Blazor Server mode) should expect this latency and not treat it as a framework issue.

### Breakpoint Behavior Matrix

**Consolidated breakpoint table (single source of truth):**

| Behavior | Desktop (≥1366px) | Compact desktop (1024-1365px) | Tablet (768-1023px) | Phone (<768px) |
|---|---|---|---|---|
| **Sidebar** | Expanded (~220px) with labels | Auto-collapsed to icon-only (~48px); hamburger expands as overlay | Drawer (off-screen); hamburger toggles | Drawer (off-screen); hamburger toggles |
| **DataGrid density** | User preference (default: compact) | User preference (default: compact) | Forced comfortable (44px rows) regardless of user preference | Forced comfortable |
| **DataGrid inline actions** | Visible on all rows | Visible on all rows | Visible on all rows (larger touch targets) | **Hidden.** Tap row to expand; actions inside expansion (see below) |
| **Expand-in-row** | Full 3-column detail grid | Full 3-column detail grid | 2-column detail grid | Single-column stacked fields |
| **Full-page form** | Centered, max 720px | Centered, max 720px | Full-width with 16px margins | Full-width with 8px margins |
| **Command palette** | Centered card, max 600px | Centered card, max 600px | Full-width card | Full-width card, bottom-anchored |
| **Home directory** | Cards in 2-3 column grid | Cards in 2-column grid | Cards in single column | Cards in single column |
| **Header** | All elements visible: title, breadcrumbs, command palette, theme, settings | Breadcrumbs may truncate with ellipsis | Command palette icon-only (no shortcut hint text); settings in overflow menu | Minimal: hamburger + title + overflow menu |
| **Dev-mode overlay** | Full annotations + 360px drawer | Full annotations + 360px drawer | Annotations only; drawer opens full-width | Not supported (dev work happens on desktop) |

**Phone DataGrid pattern: tap-to-expand, no inline buttons.** On phone viewports (<768px), inline action buttons are hidden from DataGrid rows entirely. Instead, tapping a row expands it (the same expand-in-row pattern used on all viewports), and action buttons appear inside the expanded detail view. This prevents the phone experience from becoming actively hostile: without this rule, stacked buttons below each row would double or triple the visual row height, reducing a scannable list of 10 rows to a cluttered list of 2-3 visible rows with interleaved buttons. The tap-to-expand pattern keeps the DataGrid scannable (one comfortable-density row per item) and makes actions available one tap away. This is not "optimizing for phone" -- it is preventing the phone tier from being unusable. Minimal implementation effort (CSS media query hides inline buttons and enables row tap), massive usability improvement.

### Touch Target Guarantees

**Minimum touch target sizes by breakpoint:**

| Breakpoint | Minimum touch target | Source |
|---|---|---|
| Desktop (≥1024px) | No minimum enforced (mouse precision assumed) | -- |
| Tablet (768-1023px) | 44×44px (WCAG 2.5.8 Target Size) | Forced comfortable density achieves this |
| Phone (<768px) | 44×44px | Same as tablet |

**Component-level touch targets at tablet breakpoint:**

| Component | Desktop size | Tablet size (≥44px enforced) | How enforced |
|---|---|---|---|
| DataGrid row | Compact: ~32px height | Comfortable: ~44px height | Density auto-switch at <1024px |
| Inline action button | ~28px height (compact) | ~44px height (comfortable) | Density auto-switch |
| Sidebar nav item | ~36px height (comfortable) | ~48px height (drawer item) | Drawer layout uses larger items |
| Command palette result | ~36px height | ~48px height | Responsive padding increase |
| Status filter badge | ~28px | ~44px | Responsive padding increase |
| Column filter input | ~32px height | ~44px height | Density auto-switch |
| Settings panel controls | ~36px | ~44px | FluentDialog responsive padding |

**Touch target enforcement:** The density auto-switch at <1024px is the primary mechanism. By forcing comfortable density (which Fluent UI sizes for touch), most targets exceed 44px automatically. Components that don't inherit density (command palette results, filter badges) apply responsive CSS padding to meet the minimum.

### Responsive Component Behavior

**How each custom Fc component adapts per breakpoint:**

| Component | Desktop (≥1366px) | Compact (1024-1365px) | Tablet (768-1023px) | Phone (<768px) |
|---|---|---|---|---|
| `FcCommandPalette` | Centered card, 600px max, Ctrl+K shortcut hint visible | Same | Full-width card, no shortcut hint text | Bottom-anchored full-width card |
| `FcLifecycleWrapper` | All states identical across breakpoints -- lifecycle behavior is viewport-independent | Same | Same | Same |
| `FcDesaturatedBadge` | Viewport-independent (badge size inherits from density) | Same | Larger badge at comfortable density | Same as tablet |
| `FcSyncIndicator` | Header text: "Reconnecting..." | Same | Header icon-only (tooltip for text) | Same as tablet |
| `FcHomeDirectory` | 2-3 column card grid | 2-column card grid | Single-column card list | Single-column card list |
| `FcColumnPrioritizer` | "More columns" toggle in column header | Same | "More columns" button moves above DataGrid (column header too cramped) | Same as tablet |
| `FcNewItemIndicator` | Full indicator text below row | Same | Indicator text truncated to "New" | Same as tablet |
| `FcEmptyState` | Icon + message + CTA button (horizontal layout) | Same | Stacked vertical layout | Same as tablet |
| `FcFieldPlaceholder` | Full message + docs link | Same | Truncated message; link preserved | Same as tablet |
| `FcDevModeOverlay` | Full annotations + 360px drawer | Same | Annotations only; drawer full-width | Not supported |

**Viewport-independent components:** `FcLifecycleWrapper`, `FcDesaturatedBadge`, and `FcSyncIndicator` (connection state logic) behave identically at all breakpoints. Their visual output adapts through density inheritance (badge size, row height) but their state machine and lifecycle logic are viewport-independent. This is an explicit architectural decision: eventual consistency UX must be consistent regardless of device.

### Accessibility Testing Matrix

**What's automated vs. manual, and when:**

| Test type | Tool | Scope | Frequency | Blocks merge? | Owner |
|---|---|---|---|---|---|
| **Structural a11y** | axe-core via Playwright | Type specimen + data formatting specimen | Every PR | Yes (serious/critical violations) | CI pipeline |
| **Contrast verification** | axe-core | Specimen views, both themes | Every PR | Yes | CI pipeline |
| **Custom accent contrast** | Build-time Roslyn analyzer | Verify custom accent meets WCAG AA (4.5:1 text, 3:1 UI) against Light and Dark backgrounds | Every build | Yes (warning, `TreatWarningsAsErrors=true`) | Build pipeline |
| **Keyboard navigation** | Playwright scripted tab-order tests | Specimen view: shell → nav → DataGrid → form → palette | Every PR | Yes | CI pipeline |
| **Focus visibility** | Playwright screenshot diff | Focus ring visibility at each interactive element in specimen | Every PR | Yes | CI pipeline |
| **Density parity** | Playwright screenshot diff | Specimen at compact/comfortable/roomy | Every PR | Yes (per tier) | CI pipeline |
| **Forced-colors mode** | Playwright in `forced-colors` emulation | Specimen view | Every PR | Yes | CI pipeline |
| **Reduced motion** | Playwright with `prefers-reduced-motion: reduce` | Specimen view (verify no animations) | Every PR | Yes | CI pipeline |
| **Zoom/reflow** | Playwright at 100%, 200%, 400% zoom | Specimen view | Every PR | Yes | CI pipeline |
| **Screen reader (NVDA)** | Manual verification | 5 custom Fc components + specimen | Before each release branch | No (blocks release, not merge) | Jerome |
| **Screen reader (JAWS)** | Manual verification | 5 custom Fc components + specimen | Before each release branch | No (blocks release) | Jerome |
| **Screen reader (VoiceOver)** | Manual verification | 5 custom Fc components + specimen | Before each release branch | No (blocks release) | Jerome |
| **Real device (tablet)** | Manual testing | Core flows: queue processing, command submission, navigation | Before each release branch | No (blocks release) | Jerome |

**Screen reader + browser pairings (mandatory for verification logs):**

| Screen reader | Primary browser | Secondary browser | Notes |
|---|---|---|---|
| **NVDA** | Firefox | Chrome | NVDA's reference browser is Firefox; Chrome is well-supported. Avoid Edge (known inconsistencies). |
| **JAWS** | Chrome | Edge | JAWS is optimized for Chrome and Edge. Firefox support is inconsistent with JAWS. |
| **VoiceOver** | Safari | -- | VoiceOver is Safari-only on macOS. Chrome support is poor and not a valid test target. |

**Why pinned pairings matter:** Screen reader behavior varies significantly by browser. An NVDA bug in Edge may be a browser-AT compatibility issue, not a framework bug. Pinning pairings ensures verification logs are reproducible and diagnosable. Each manual verification log must document: screen reader version, browser version, OS version, pass/fail per component, and any issues found with their resolution status.

**Custom accent build-time contrast check:** When an adopter overrides the default teal accent (`#0097A7`), the framework runs a contrast verification at build time (not just a startup log). A Roslyn analyzer computes the contrast ratio of the custom accent against both Light and Dark theme neutral backgrounds. If either ratio fails WCAG AA (4.5:1 for normal text, 3:1 for large text and UI components), a build warning is emitted. With the shipped build template's `TreatWarningsAsErrors=true` default for accessibility warnings, this effectively blocks builds with inaccessible accent colors. Adopters who deliberately choose a lower-contrast accent must acknowledge it by suppressing the specific warning -- a conscious decision, not an oversight.

**Verification logs:** Manual screen reader and device verification results are committed to `docs/accessibility-verification/` and dated per release. Each log documents: screen reader version, browser version (per pinned pairing), OS version, pass/fail per component, and any issues found with their resolution status.

**Automated test coverage by custom component:**

| Component | axe-core | Keyboard test | Screenshot diff | Forced-colors | Reduced motion |
|---|---|---|---|---|---|
| `FcCommandPalette` | ✓ | ✓ (open/navigate/select/close) | ✓ | ✓ | ✓ (no animation) |
| `FcLifecycleWrapper` | ✓ | ✓ (submit/dismiss) | ✓ (all states) | ✓ | ✓ (instant state change) |
| `FcDesaturatedBadge` | ✓ | -- (not interactive) | ✓ (syncing/confirmed) | ✓ | ✓ (instant saturation) |
| `FcSyncIndicator` | ✓ | -- (not interactive) | ✓ (connected/disconnected/reconciled) | ✓ | ✓ (instant sweep) |
| `FcEmptyState` | ✓ | ✓ (CTA button focus) | ✓ | ✓ | -- (no animation) |
| `FcFieldPlaceholder` | ✓ | ✓ (focus + link activation) | ✓ | ✓ | -- (no animation) |
| `FcColumnPrioritizer` | ✓ | ✓ (toggle/checkbox navigation) | ✓ | ✓ | -- (no animation) |
| `FcNewItemIndicator` | ✓ | -- (auto-dismiss) | ✓ (active/dismissing) | ✓ | ✓ (instant removal) |
| `FcHomeDirectory` | ✓ | ✓ (card navigation) | ✓ | ✓ | -- (no animation) |
| `FcDevModeOverlay` | ✓ | ✓ (annotation tab/drawer) | -- (Tier 3) | -- (Tier 3) | -- (Tier 3) |

### Adopter Accessibility Responsibilities

**Clear line between framework guarantees and adopter obligations:**

**What FrontComposer guarantees (framework responsibility):**

| Guarantee | Scope | Enforcement |
|---|---|---|
| All auto-generated views pass WCAG 2.1 AA | Every view produced by the auto-generation engine | CI-enforced via axe-core + specimen views |
| All Fc custom components are keyboard-accessible | 11 custom components | CI-enforced keyboard navigation tests |
| Focus visibility preserved on all framework elements | Shell, navigation, DataGrid, forms, overlays | CI-enforced focus ring screenshot tests |
| Screen reader announcements for lifecycle states | Two-category policy: polite (confirmed/syncing) and assertive (rejected/timeout) | Manual verification before each release |
| Color is never the sole signal | All badges, lifecycle indicators, and errors combine color with text/icon | CI-enforced: specimen asserts every badge has a text label |
| Reduced motion respected | All CSS animations replaced with instant state changes when `prefers-reduced-motion: reduce` | CI-enforced: specimen rendered in both motion modes |
| Forced-colors (High Contrast) support | All framework elements use system color keywords | CI-enforced: specimen rendered in forced-colors mode |
| Build-time accessibility warnings | Missing accessible names, labels, landmark roles on auto-generated output | Roslyn analyzers; shipped build templates default to `TreatWarningsAsErrors=true` |
| Custom accent contrast verification | Build-time check against Light and Dark backgrounds | Roslyn analyzer; build warning if WCAG AA fails |

**What adopters own (adopter responsibility):**

| Responsibility | Context | Framework support |
|---|---|---|
| Custom component accessibility | Level 2-4 customization gradient overrides | Custom component accessibility contract (6 requirements documented in Step 8); dev-mode overlay flags contract violations |
| Content accessibility | Projection descriptions, field labels in resource files, domain-specific messages | Build-time warnings for missing accessible names; label resolution chain provides fallback |
| Localized screen reader quality | Languages beyond EN/FR | Framework provides localization mechanism (IStringLocalizer); adopter provides translations |
| Real device testing for their deployment | Their specific browser/device/AT matrix | Framework provides specimen views as testing targets |
| Color contrast of custom accent | If adopter overrides the default teal accent | Build-time Roslyn analyzer emits warning if contrast fails; adopter must acknowledge or fix |
| Third-party component accessibility | If adopter adds non-Fluent-UI components outside the customization gradient | Outside framework scope entirely |

**The accessibility contract is the line.** FrontComposer guarantees accessibility for everything it auto-generates and every Fc custom component. The moment an adopter provides a custom component through the customization gradient, the **custom component accessibility contract** (documented in Step 8) defines the minimum requirements. The dev-mode overlay flags contract violations in development. Beyond the contract, adopters own their own accessibility.
