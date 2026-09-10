# Spine Pair Review — frontcomposer

## Overall verdict

The three-file UX contract has a clear authority chain and strong design-system posture, but it is **thin to broken as a source-extractable downstream contract**. The visual and behavioral supplements have the expected section shapes; however, journey coverage, component naming, lifecycle states, and source resolution contain load-bearing gaps that architecture and story-development consumers would have to infer.

## 1. Flow coverage — broken

The six named PRD journeys and the UX requirements reachable from the experience supplement's `sources` were compared with its two Key Flows.

### Findings

- **[high]** The source defines UJ-1 through UJ-6, but the supplement has only two differently named operator flows. Those flows cover parts of projection investigation and command execution, while shell bootstrap, MCP-agent use, compatibility maintenance, and generated-experience testing have no flow; even the overlapping source names and protagonists are not preserved (`prd.md:77-91`; `ux-experience-2026-07-05.md:138-167`). *Fix:* add verbatim-named Key Flows for every applicable source journey, including numbered steps, climax, and failure path, or explicitly narrow the experience contract's source/applicability boundary and record why excluded non-UI journeys do not require surfaces.

## 2. Token completeness — adequate

All `{path.to.token}` references in the visual supplement resolve. Non-hex color values are defensible because the Foundation explicitly inherits FrontComposer and Fluent UI Blazor V5, so active-theme roles own light/dark values.

### Findings

- **[medium]** The contract says visual contrast lives in the detailed supplement, but it does not name the load-bearing foreground/background and status combinations or state that each inherited/custom combination must retain WCAG 2.2 AA in every active theme and forced-colors mode (`ux-design-detailed-2026-07-05.md:19-30,84-94`; `ux-experience-2026-07-05.md:107-117`). *Fix:* add an inheritance/contrast table for canvas, chrome, raised surfaces, accent actions/focus, status icons, disabled text, and forced-colors fallback, referring to exact Fluent roles instead of duplicating theme values.

## 3. Component coverage — broken

Component names in the canonical requirements, DESIGN frontmatter/body, and EXPERIENCE Component Patterns were compared as contract identifiers.

### Findings

- **[high]** The component registries are neither complete nor name-stable. Examples include `navigation-module-entry` versus “Navigation module entry,” `projection-grid` versus “Search/list grid,” and `status-icon` versus “Status affordance”; the behavioral rows “Module workspace/dashboard,” “Command lifecycle,” “Second local submit,” and “Multi-section page body” have no exact visual peers. Canonical components such as `FcCommandPalette`, `FcSettingsDialog`, `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`, `FcLifecycleWrapper`, `FcProjectionLoadingSkeleton`, `FcProjectionEmptyPlaceholder`, `FcProjectionConnectionStatus`, and `FcPendingCommandSummary` also lack paired visual and behavioral rows (`ux-design.md:52-62`; `ux-design-detailed-2026-07-05.md:51-71,132-140`; `ux-experience-2026-07-05.md:62-78`). *Fix:* create one exact-name component registry used by both supplements; every component needs a visual row and a behavioral row, with “inherits Fluent defaults; no visual delta” allowed where accurate.

## 4. State coverage — thin

Every Information Architecture surface was walked against the State Patterns table and canonical lifecycle requirements.

### Findings

- **[high]** The canonical lifecycle names `IdempotentConfirmed`, `NeedsReview`, `Warning`, and `Degraded`, and Component Patterns also name Submitting, Acknowledged, and Syncing, but State Patterns specify only accepted, confirmed, and rejected outcomes. The second-submit-blocked state also has no state treatment (`ux-design.md:56-67`; `ux-experience-2026-07-05.md:75-76,80-95`). *Fix:* add a complete lifecycle state machine/table with entry evidence, visible copy, available actions, announcement behavior, retry/recovery, timeout, and terminality for every named state plus the blocked second submit.
- **[medium]** Several IA surfaces lack implementable state coverage: Module tabs have no invalid/deep-link/selection/focus state; Detail/edit and Add/create omit load, not-found, validation, denied, and save/network failure behavior; offline behavior is not distinguished from reconnecting; focus behavior is only a global floor (`ux-experience-2026-07-05.md:32-47,80-118`). *Fix:* add a surface-by-state matrix and mark genuinely inapplicable states explicitly.

## 5. Visual reference coverage — strong

No `imports/`, `mockups/`, or `wireframes/` directories/files exist beside these artifacts, so there are no orphaned visual references. The authority/conflict rule is stated in the canonical and experience documents (`ux-design.md:16-19`; `ux-experience-2026-07-05.md:19-21`).

### Findings

No misses.

## 6. Bloat & overspecification — adequate

Most visual prose earns its place and the canonical file is compact. One historical section dilutes the consumer-facing behavior contract.

### Findings

- **[medium]** The signed-off FC-IA-1 decision-gate history repeats settled route and navigation choices, dates, ownership, and old blocking language after the current contract already states the result (`ux-experience-2026-07-05.md:172-209`). *Fix:* retain only the resulting IA decisions and a link to the decision record; move chronology and closure evidence to reconciliation/history.

## 7. Inheritance discipline — broken

Authority statements are consistent, most source paths resolve, and canonical UX-DR identifiers remain stable. Source and component-name integrity still fail mechanical extraction.

### Findings

- **[high]** Both supplements reference `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`, which does not exist (`ux-design-detailed-2026-07-05.md:11`; `ux-experience-2026-07-05.md:10`). *Fix:* replace it with the current canonical PRD path or restore the intended immutable source at the cited path.
- **[medium]** Semantically equivalent component concepts use different contract names across the two supplements, preventing deterministic joins by downstream extractors (`ux-design-detailed-2026-07-05.md:51-71,132-140`; `ux-experience-2026-07-05.md:66-78`). *Fix:* choose canonical component identifiers and use them verbatim in frontmatter, Components, Component Patterns, IA, and flows.
- **[low]** The visual supplement was last updated on 2026-07-15 while the canonical and behavioral artifacts were updated on 2026-08-12, with no reconciliation marker showing that later UX-DR/timing changes were checked against visual tokens (`ux-design-detailed-2026-07-05.md:5`; `ux-design.md:5`; `ux-experience-2026-07-05.md:4`). *Fix:* add a reconciliation timestamp/source revision when the visual supplement is next validated.

## 8. Shape fit — strong

The detailed visual supplement uses the canonical DESIGN section order. The experience supplement includes every default section plus applicable Responsive & Platform and Inspiration & Anti-patterns sections. The three-file canonical-plus-supplements form is nonstandard but explicitly governed and supported by Validate's “any format” rule.

### Findings

No misses.

## Mechanical notes

- Every DESIGN token reference resolves; route placeholders such as `{module}` and `{BoundedContext}` are not design-token references.
- One source path is broken in both supplements.
- No visual-reference directories or files are present.
- No Mermaid blocks are present.
- Severity totals: critical 0, high 4, medium 4, low 1.
