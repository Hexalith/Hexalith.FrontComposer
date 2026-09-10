# UX Input Reconciliation — Canonical PRD (2026-09-09)

## Source

Exact source reconciled: `_bmad-output/planning-artifacts/prd.md` (`updated: 2026-09-09`). Stable FR/NFR/UJ/D/G/OI/SM identifiers are preserved.

## Adopted Into The UX Authority Chain

The three-file chain adopts the PRD's operator-facing contract without duplicating product scope:

- `_bmad-output/planning-artifacts/ux-design.md` remains canonical; `ux-design-detailed-2026-07-05.md` remains the visual supplement; `ux-experience-2026-07-05.md` remains the behavior/journey supplement (D-8). FC-IA-1 remains supporting decision history.
- FR-8 and FR-10 define shell, navigation, route, tab, palette, dialog, reflow, zoom, text-spacing, target-size, and unobscured-focus outcomes.
- FR-11 through FR-13 define projection states, bounded recovery, non-noisy announcements, semantic status, and fresh-row behavior.
- FR-14 through FR-16 define client validation, asynchronous rejection, lifecycle states, deterministic budgets, one-at-a-time blocking, and keyboard recovery.
- FR-22 defines the adopter-test assertions; FR-23 defines exact public component/API and documentation integrity; NFR-3 and SM-6 define the accessibility floor and evidence outcome.
- The behavior supplement covers every applicable UJ-1 through UJ-6, or records an explicit out-of-scope disposition.

## Delivery And Evidence Status

- Preserve the delivered runtime baseline for FR-8 and FR-10 through FR-16. FR-13 remains complete through Stories 9.3–9.8 and its 2026-08-27 live proof; DW-679 remains the explicit server-allocated-key non-goal.
- Preserve the delivered FR-22 failure-state harness and FR-23 documentation baseline.
- Treat the 2026-09-09 announcement, validation/rejection, focus, state-by-surface, responsive-accessibility, source/component, testing, and evidence additions as new work. SM-5's UX assertion expansion and SM-6 remain unmet through OI-16.
- The UX sources specify deterministic acceptance matrices and evidence hooks; they do not claim that implementation or e2e/bUnit evidence already exists.

## Conflicts And Dispositions

- Any old PRD link under `prds/prd-frontcomposer-2026-07-05/` is stale and is replaced by the canonical `_bmad-output/planning-artifacts/prd.md` path.
- Any unresolved or invented public component/API name is dropped or replaced with a repository-resolvable FrontComposer identifier or selected-pin Fluent API. `FcPageToolbar` remains the public product contract; its internal Fluent composition is not promoted to public API.
- Any active behavior prose that treats the settled FC-IA-1 chronology as current authority is moved to reconciliation history; D-8 and the canonical PRD own current behavior.
- Any implication that hover, color, motion, transport acceptance, retry ticks, or a projection nudge alone communicates confirmed state is rejected by FR-11 through FR-15 and NFR-3.
- No qualitative source idea was silently dropped: adopted behavior is represented in the authority chain; stale/conflicting material is listed above with its disposition; explicit non-goals and residuals retain their stable IDs.

## Open Gate

OI-16 and SM-6 evidence work remain open. This reconciliation does not close G-4, amend D-9, record Product approval, close OI-19, or claim v1 readiness. Product re-approval remains pending the exact PRD/addendum digest-bound postfix gate and every prerequisite named by G-4.
