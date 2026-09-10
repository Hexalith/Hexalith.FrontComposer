# Spine Pair Review — frontcomposer

## Overall verdict

The migrated pair is substantially stronger than the legacy three-file contract: every canonical UJ has a complete flow, component names join cleanly across both spines, token references resolve, and the required spine shapes are present. It is not yet handoff-ready because the command state machine still leaves consequential outcomes under-specified, and the declared source set contains an unresolved authority conflict plus an unapproved Fluent V5 inheritance boundary.

## 1. Flow coverage — strong

The six Key User Journeys named by the canonical PRD were checked against Key Flows and the IA surface-closure table. UJ-1 through UJ-6 retain their source names verbatim; each has a named protagonist, numbered steps, an explicit climax, and an applicable failure path (`EXPERIENCE.md:63-72,282-351`; `_bmad-output/planning-artifacts/prd.md:78-92`).

### Findings

No misses.

## 2. Token completeness — adequate

The frontmatter defines one hexadecimal color token, four inherited typography roles, two radius entries, four spacing tokens, and nine component token groups. Every design-token reference in both spines resolves; the route placeholders `{module}`, `{tab}`, and `{default}` are not design-token references. The configurable accent and inherited load-bearing contrast targets are stated for supported themes (`DESIGN.md:15-58,73-84`; `EXPERIENCE.md:24,101,266-268`).

### Findings

- **[medium]** `rounded.fluent-default` is an object containing `note`, but the DESIGN.md token schema requires each `rounded` scale value to be a CSS dimension; `{rounded.fluent-default}` therefore resolves to a non-renderable mapping rather than a scalar token (`DESIGN.md:26-29,108-112`; `.agents/skills/bmad-ux/references/design-md-spec.md`, “Frontmatter tokens”). *Fix:* omit this inherited radius token and state Fluent inheritance directly, or replace it with a schema-valid scalar only after the selected Fluent contract supplies one.

## 3. Component coverage — strong

The 21 named FrontComposer patterns in DESIGN.md Components have exact-name peers in EXPERIENCE.md Component Patterns, with substantive visual and behavioral rules. Public identifiers used by those rows are stable across the pair, while inherited Fluent internals are explicitly treated as implementation-owned (`DESIGN.md:114-140`; `EXPERIENCE.md:89-115`).

### Findings

No misses.

## 4. State coverage — thin

Every IA surface was walked against the shell/home table, supporting-surface table, projection matrix, command lifecycle table, fresh-row rules, MCP matrix, and developer/tooling states (`EXPERIENCE.md:30-72,117-221`). Most cold-load, empty, denied, tenant, error, focus/recovery, and degraded-data cases are explicit.

### Findings

- **[high]** The source-required command state matrix is not closed. A blocked second submit is specified only as a behavioral/interactions rule, not as a state row with entry evidence, visible meaning, recovery, announcement, and classification; `Warning` delegates terminality to unspecified lifecycle evidence; and `Degraded` says it remains non-terminal until the `120,000ms` ceiling without defining the state or permitted action once that ceiling is reached (`EXPERIENCE.md:113,144,175-187,233-234`; `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:76-100`). *Fix:* add explicit lifecycle/state rows for blocked-submit and post-polling-ceiling behavior, and commit deterministic terminal/non-terminal classification rules for Warning and Degraded.
- **[medium]** The projection matrix defines `Empty` only for zero rows without a filter mismatch; the distinct “filters produced no matches” state is described in the placeholder component and voice example but has no state row with entry evidence, reset/recovery, announcement, and classification (`EXPERIENCE.md:80-82,106,150-165`; `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md:82-90`). *Fix:* add a filtered-no-results projection row distinct from the unfiltered Empty state.

## 5. Visual reference coverage — strong

There are no files or directories under `imports/`, `mockups/`, or `wireframes/` in this workspace, so there are no orphaned or unspecific visual references. The pair states its spine-over-artifact conflict rule (`DESIGN.md:63`; `EXPERIENCE.md:18`).

### Findings

No misses.

## 6. Bloat & overspecification — strong

The detailed matrices are justified by the product's human UI, MCP, CLI, generated-output, and testing surfaces. The documents largely reference rather than restate upstream product scope, and the product-specific Security, Tenancy, and Support Safety section earns its place by governing cross-surface disclosure and fail-closed behavior (`EXPERIENCE.md:20-28,197-221,253-260`).

### Findings

No material overspecification found.

## 7. Inheritance discipline — broken

All eight frontmatter source paths resolve, the six UJ names match the PRD verbatim, the paired component identifiers are consistent, and every dotted token reference resolves. Two load-bearing inheritance boundaries remain open.

### Findings

- **[high]** The pair declares that it supersedes the legacy single-file UX precedence chain, while two of its own canonical sources still declare `ux-design.md` canonical and retain the legacy three-file authority chain (`DESIGN.md:63`; `EXPERIENCE.md:18`; `_bmad-output/planning-artifacts/prd.md:16-18,675-683`; `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:8-10,102-108`). The memlog records the user's migration decision, but a downstream consumer following `sources:` still encounters contradictory source-of-record instructions (`.memlog.md:7-11`). *Fix:* propagate the approved two-spine authority into PRD D-2/D-8 and the addendum's UX source-integrity language, or revise the pair to preserve the existing authority chain.
- **[high]** FrontComposer/Fluent inheritance is deliberately unresolved at the exact contract boundary: the selected Fluent V5 pin and supported accent token/API role are unapproved, and several load-bearing state visuals are delegated to the “nearest” semantic treatment (`DESIGN.md:75-84,154-158`; `EXPERIENCE.md:353-358`; `_bmad-output/planning-artifacts/prd.md:687`). That prevents deterministic source extraction for the accent, Warning, NeedsReview, Degraded, missing-tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row fallback presentations. *Fix:* record the Product/Architecture pin decision and exact supported Fluent roles/components, then replace nearest-role placeholders with those inherited names without redefining the theme.

## 8. Shape fit — strong

DESIGN.md includes all canonical body sections in the required order. EXPERIENCE.md includes Foundation, Information Architecture, Voice and Tone, Component Patterns, State Patterns, Interaction Primitives, Accessibility Floor, and Key Flows; Responsive & Platform and Inspiration & Anti-patterns are present because responsive web and reference-product inputs are in scope. The product-specific security section is justified by the human/MCP boundary (`DESIGN.md:65-156`; `EXPERIENCE.md:20-358`).

### Findings

No misses.

## Mechanical notes

- Source paths: 8/8 resolve.
- Key Flows: 6/6 preserve the canonical UJ name and include protagonist, numbered steps, climax, and failure handling.
- Component registry: 21/21 visual rows have exact behavioral peers.
- Dotted token references: all resolve; `rounded.fluent-default` resolves structurally but violates the required scalar radius type.
- Visual artifacts: none present; no orphan check failures.
- Mermaid: neither spine contains a Mermaid block.
- Severity totals: critical 0, high 3, medium 2, low 0.
