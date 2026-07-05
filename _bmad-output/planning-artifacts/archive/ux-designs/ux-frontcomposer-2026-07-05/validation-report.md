# Validation Report - frontcomposer

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md`
- **Run at:** 2026-07-05T09:22:07+02:00

## Overall verdict
The spine pair is a useful draft for the common Hexalith module UI: it captures FrontComposer/Fluent governance, one-entry-per-module IA, support-safe microcopy, and projection-confirmed command truth. It is not yet a clean downstream contract because the sources include the full FrontComposer PRD while the flows cover only a narrow Parties operator scenario, several component names/specs drift across the two spines, and load-bearing route/flyout decisions remain open.

No critical defects were found. The high-severity issues should be resolved, or the declared source scope narrowed, before the pair is promoted beyond draft.

## Category verdicts
- Flow coverage - thin
- Token completeness - adequate
- Component coverage - thin
- State coverage - adequate
- Visual reference coverage - thin
- Bloat & overspecification - strong
- Inheritance discipline - thin
- Shape fit - thin

## Findings by severity

### Critical (0)
None.

### High (3)
**Flow coverage** - Source user journeys are not covered verbatim (`_bmad-output/planning-artifacts/prd.md:50`; `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:132`)

The PRD names six journeys, but EXPERIENCE has only two Parties flows for Alex and Maya. Several product-level journeys, including bootstrap, MCP, compatibility, and testing, are absent or not named from source.

Fix: Narrow `sources:` to the common application UX sources this spine actually covers, or add/rename key flows that trace UJ-1 through UJ-6 with source journey names, protagonists, climax beats, and failure paths.

**Component coverage** - Component names and rows are not aligned across spines (`_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/DESIGN.md:51`; `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:62`)

DESIGN defines kebab-case component tokens while EXPERIENCE uses different labels. Several EXPERIENCE components have no matching visual row in DESIGN, and several DESIGN components have no identically named behavioral row.

Fix: Create one canonical component table or glossary shared by both files, with stable component IDs and one visual row plus one behavioral row for each load-bearing component.

**Shape fit** - Load-bearing decisions remain open inside the spine (`_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:168`)

Open questions cover default tab naming, tab route encoding, whether projection flyouts remain, and the first visual reference implementation. These affect routing, palette/CTA destinations, navigation IA, tests, and implementation handoff.

Fix: Resolve the route/tab/flyout decisions or move them into a clearly marked non-blocking backlog with the current default decision in the spine.

### Medium (3)
**Token completeness** - Status and contrast tokens are not explicit enough for downstream mirroring (`_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/DESIGN.md:28`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md:211`)

The design declares success, warning, and error tokens and says status uses Fluent semantic colors, but the visual-refresh source requires success, error, unknown/neutral, warning, and info mappings plus contrast verification.

Fix: Add missing neutral/info status token semantics or explicitly state the Fluent semantic color source for those states, and record the contrast target/verification rule for load-bearing status combinations.

**State coverage** - Command lifecycle states are not fully covered in State Patterns (`_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:69`; `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:73`)

EXPERIENCE names `Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`, `IdempotentConfirmed`, `NeedsReview`, and `Degraded`, but the state table omits rows for several of them.

Fix: Add rows for `Submitting`, `Syncing`, `IdempotentConfirmed`, `NeedsReview`, and `Degraded`, including retry/polling behavior, live-region behavior, and failure recovery.

**Inheritance discipline** - Source inheritance is ambiguous (`_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/DESIGN.md:7`; `_bmad-output/planning-artifacts/prd.md:12`)

Both spines point to both the canonical PRD and the BMad run copy, while the PRD says those are the canonical and run copies of the same requirements. DESIGN and EXPERIENCE source lists also differ.

Fix: Use one PRD source of record, align the source lists where both spines inherit the same upstream decision, and add a short scope note for visual-only or behavior-only sources.

### Low (1)
**Visual reference coverage** - No visual artifacts are linked or present (`_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:170`)

There are no `mockups/`, `wireframes/`, or `imports/` artifacts for the shell, module workspace, toolbar, grid, command lifecycle, or navigation rail. This matches the fast-path draft status but leaves downstream visual interpretation to prose.

Fix: Keep the draft status until at least the first reference implementation or key-screen mock is linked, or explicitly mark these surfaces as spine-only with rationale.

## Reviewer files
- `review-rubric.md`
