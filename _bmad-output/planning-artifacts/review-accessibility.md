# Accessibility Review — frontcomposer

## Overall verdict

**Thin.** The contract makes WCAG 2.2 AA, keyboard reachability, accessible names, focus visibility, live regions, reduced motion, forced colors, and support-safe output explicit. It still leaves several interaction-critical accessibility behaviors too vague for independent implementation and testing.

## Strengths

- WCAG 2.2 AA is a canonical requirement rather than optional guidance (`ux-design.md:74-78`).
- Skip links, route-heading focus, logical tab order, visible focus, accessible names, reduced motion, forced colors, and live-region restraint are named (`ux-experience-2026-07-05.md:107-118`).
- Status is not color-only: the contract requires an accessible label and a tooltip reachable by hover and keyboard focus (`ux-design.md:41-43`; `ux-experience-2026-07-05.md:77,115`).
- Failure copy is intentionally support-safe and preserves useful input when recovery is meaningful (`ux-experience-2026-07-05.md:49-60,93,167`).

## Findings

- **[high] Lifecycle announcements are not deterministic.** “Useful and non-noisy” does not define which transitions announce, `aria-live` politeness, deduplication, rapid-transition coalescing, terminal announcements, or how fresh-row expiration avoids noisy re-announcement (`ux-design.md:64-72`; `ux-experience-2026-07-05.md:91-93,116`). *Fix:* define an announcement matrix for lifecycle, connection, stale, loading, blocked-submit, and fresh-row transitions, including region role/politeness, exact semantic message, dedupe key, and silent transitions.
- **[high] Form validation and rejection lack an accessible error contract.** The create/edit and rejection rules do not specify field-error association, error summary, focus placement, invalid-control navigation, or how async rejection differs from client validation (`ux-experience-2026-07-05.md:73-76,93-94,155-167`). *Fix:* specify `aria-invalid`/described-by behavior through Fluent inputs, summary-to-field links, focus rules, preserved input, and announcement behavior for validation versus server rejection.
- **[high] Client-side navigation and tabs lack complete focus semantics.** Routes must be deep-linkable and tabs keyboard-operable, while the Accessibility Floor only generally names route-heading focus; there is no explicit rule for when focus moves to the route `h1`, when it stays on a selected tab, how the tabpanel is named, or where focus returns after palette/dialog navigation (`ux-experience-2026-07-05.md:45,70,98-105,113`). *Fix:* define focus and announcement outcomes for route changes, tab changes, command-palette activation, dialog close, and failed navigation.
- **[medium] Icon-only navigation relies on the global naming floor rather than a component-level rule.** The visual contract allows an icon-only rail, but the Navigation component behavior does not require an accessible name, current-page exposure, tooltip behavior, or badge announcement policy in that mode (`ux-design.md:45-47`; `ux-design-detailed-2026-07-05.md:56-59,134-136`; `ux-experience-2026-07-05.md:68,124-128`). *Fix:* put those requirements in the exact navigation component row.
- **[medium] Keyboard shortcuts need conflict and discoverability rules.** `/`, `Ctrl+K`, and `Ctrl+,` are listed without saying they are suppressed in editable controls/composition, how localized keyboard layouts are handled, or where users discover them (`ux-experience-2026-07-05.md:98-103`). *Fix:* define scope, editable-field exceptions, IME handling, discoverability, and browser/OS conflict fallback.
- **[medium] The responsive contract is not testable at accessibility zoom/reflow conditions.** “Narrow browser” behavior does not commit to 320 CSS-pixel reflow, 400% zoom, text-spacing resilience, target sizes, or prevention of focus obscuration (`ux-experience-2026-07-05.md:120-128`). *Fix:* add measurable responsive accessibility outcomes or cite an inherited FrontComposer conformance contract that supplies them.
- **[medium] Reduced-motion and forced-colors requirements are only global assertions.** The contract does not say which motion is removed or how accent/status/fresh-row meaning survives forced colors (`ux-design-detailed-2026-07-05.md:88-94`; `ux-experience-2026-07-05.md:115-117`). *Fix:* add component-level fallback behavior for navigation accent, focus, status icons, stale/reconnect state, lifecycle progress, and fresh-row indicators.

## Summary

- Critical: 0
- High: 3
- Medium: 4
- Low: 0
