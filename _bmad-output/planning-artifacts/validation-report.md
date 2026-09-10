# Validation Report — frontcomposer

- **Canonical UX authority:** `_bmad-output/planning-artifacts/ux-design.md`
- **Visual supplement:** `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`
- **Experience supplement:** `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`
- **Run at:** 2026-09-09T09:02:43+02:00

## Overall verdict

The three-file UX contract has a clear authority chain and strong FrontComposer/Fluent UI V5 posture, but it is **thin to broken as a source-extractable downstream contract**. The expected visual and behavioral shapes are present; journey coverage, component identity, lifecycle/state behavior, accessibility mechanics, and source resolution still require inference.

The accessibility and Fluent-specific reviews reinforce that conclusion. Accessibility intent is strong but several behaviors are not deterministic enough to implement or test independently, while two Fluent component names in the visual supplement do not exist in the repository's pinned package.

## Category verdicts

- Flow coverage — **broken**
- Token completeness — **adequate**
- Component coverage — **broken**
- State coverage — **thin**
- Visual reference coverage — **strong**
- Bloat & overspecification — **adequate**
- Inheritance discipline — **broken**
- Shape fit — **strong**
- Accessibility review — **thin**
- FrontComposer / Fluent UI Blazor V5 conformance — **adequate**

## Findings by severity

### Critical (0)

No critical findings.

### High (8)

**[Flow coverage] — Six source journeys are not traceably covered** (`prd.md:77-91`; `ux-experience-2026-07-05.md:138-167`)

The experience supplement has only two differently named operator flows. It omits shell bootstrap, MCP-agent use, compatibility maintenance, and generated-experience testing, and does not preserve the source names/protagonists for the overlapping journeys.

Fix: add verbatim-named Key Flows for each applicable source journey, or narrow and explain the supplement's source/applicability boundary.

**[Component coverage] — Component registries are incomplete and name-divergent** (`ux-design.md:52-62`; `ux-design-detailed-2026-07-05.md:51-71,132-140`; `ux-experience-2026-07-05.md:62-78`)

Canonical FrontComposer components lack paired visual/behavioral rows, and semantically equivalent rows use different identifiers.

Fix: establish one exact-name registry shared by DESIGN Components and EXPERIENCE Component Patterns; allow explicit “inherits Fluent defaults” visual rows.

**[State coverage] — Lifecycle and blocked-submit states are incomplete** (`ux-design.md:56-67`; `ux-experience-2026-07-05.md:75-76,80-95`)

State Patterns omit Submitting, Syncing, IdempotentConfirmed, NeedsReview, Warning, Degraded, and the blocked second submit.

Fix: define entry evidence, copy, actions, announcements, recovery, timeout, and terminality for every state.

**[Inheritance discipline] — A source path is broken in both supplements** (`ux-design-detailed-2026-07-05.md:11`; `ux-experience-2026-07-05.md:10`)

`_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md` does not exist.

Fix: point both supplements to the current canonical PRD or restore the intended immutable source.

**[Accessibility] — Lifecycle announcements are not deterministic** (`ux-design.md:64-72`; `ux-experience-2026-07-05.md:91-93,116`)

“Useful and non-noisy” leaves transitions, politeness, deduplication, coalescing, and fresh-row expiration unspecified.

Fix: add an announcement matrix for lifecycle, connection, loading, blocked-submit, stale, and fresh-row transitions.

**[Accessibility] — Forms lack an accessible validation/rejection contract** (`ux-experience-2026-07-05.md:73-76,93-94,155-167`)

Field-error associations, error summary, focus placement, invalid-control navigation, and client-versus-server error behavior are unspecified.

Fix: define Fluent-input error association, summary links, focus rules, preserved input, and announcement behavior.

**[Accessibility] — Client-side navigation and tabs lack complete focus semantics** (`ux-experience-2026-07-05.md:45,70,98-105,113`)

The contract does not determine when focus moves to the route heading, stays on the active tab, labels a tabpanel, or returns after palette/dialog actions.

Fix: define focus and announcement outcomes for every navigation primitive and failure path.

**[Fluent UI V5] — Two named Fluent components do not exist in the pinned API** (`ux-design-detailed-2026-07-05.md:63-64,137`)

`FluentToolbar` and `FluentSearch` do not resolve in `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1`. The current implementation uses `FluentStack role="toolbar"` and `FluentTextInput TextInputType.Search` (`FcPageToolbar.razor:4-27`).

Fix: use the exact composition names or make `FcPageToolbar` internals explicitly implementation-owned.

### Medium (11)

**[Token completeness] — Load-bearing contrast combinations are not committed** (`ux-design-detailed-2026-07-05.md:19-30,84-94`; `ux-experience-2026-07-05.md:107-117`)

Fix: add an inherited-role/contrast table for core surfaces, accent/focus, statuses, disabled text, themes, and forced colors.

**[State coverage] — Multiple IA surfaces lack implementable state coverage** (`ux-experience-2026-07-05.md:32-47,80-118`)

Fix: add a surface-by-state matrix covering tabs, detail/edit, add/create, offline, permission, validation, network failure, and focus.

**[Bloat & overspecification] — Settled IA decision history remains inline** (`ux-experience-2026-07-05.md:172-209`)

Fix: retain the decision and link; move chronology/closure evidence to reconciliation history.

**[Inheritance discipline] — Component identifiers do not join across supplements** (`ux-design-detailed-2026-07-05.md:51-71,132-140`; `ux-experience-2026-07-05.md:66-78`)

Fix: use canonical component identifiers verbatim everywhere.

**[Accessibility] — Icon-only navigation lacks component-level semantics** (`ux-design.md:45-47`; `ux-design-detailed-2026-07-05.md:56-59,134-136`; `ux-experience-2026-07-05.md:68,124-128`)

Fix: define accessible name, current-page state, tooltip behavior, and badge announcements for icon-only mode.

**[Accessibility] — Keyboard shortcuts lack conflict and discoverability rules** (`ux-experience-2026-07-05.md:98-103`)

Fix: define editable-field/IME exceptions, scope, localized-layout behavior, discoverability, and fallback.

**[Accessibility] — Responsive accessibility is not measurable** (`ux-experience-2026-07-05.md:120-128`)

Fix: commit to reflow/zoom, text-spacing, target-size, and focus-not-obscured outcomes or cite an inherited test contract.

**[Accessibility] — Reduced-motion and forced-colors behavior is not component-specific** (`ux-design-detailed-2026-07-05.md:88-94`; `ux-experience-2026-07-05.md:115-117`)

Fix: define fallbacks for navigation accent, focus, statuses, reconnect/stale states, lifecycle progress, and fresh rows.

**[Fluent UI V5] — Navigation mapping is too vague** (`ux-design.md:45-47`; `ux-design-detailed-2026-07-05.md:56-59,134-136`)

Fix: make `FrontComposerNavigation` the component contract and list exact Fluent primitives only where consumers need them.

**[Fluent UI V5] — Accent alias ownership is ambiguous** (`ux-design-detailed-2026-07-05.md:27,55,82,91`)

Fix: bind `--fc-color-accent` explicitly to the active Fluent V5 accent role and forbid an independent seed/palette.

**[Fluent UI V5] — The visual component table cannot enforce reuse-first behavior** (`ux-design.md:52-62`; `ux-design-detailed-2026-07-05.md:51-71`)

Fix: list every reusable FrontComposer component, even when its visual rule is simply inherited defaults.

### Low (1)

**[Inheritance discipline] — Visual reconciliation freshness is unclear** (`ux-design-detailed-2026-07-05.md:5`; `ux-design.md:5`; `ux-experience-2026-07-05.md:4`)

The visual supplement predates the other two artifacts by almost a month with no reconciliation marker.

Fix: add the source revision/reconciliation date on the next update.

## Mechanical notes

- Every `{path.to.token}` reference resolves; route placeholders are not token references.
- No `imports/`, `mockups/`, or `wireframes/` files are present, so there are no visual-reference orphans.
- The DESIGN section order and all required EXPERIENCE sections are present.
- No Mermaid blocks are present.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-fluent-ui-v5.md`
