# Spine Pair Review - frontcomposer

## Overall verdict
The spine pair is a useful draft for the common Hexalith module UI: it captures FrontComposer/Fluent governance, one-entry-per-module IA, support-safe microcopy, and projection-confirmed command truth. It is not yet a clean downstream contract because the sources include the full FrontComposer PRD while the flows cover only a narrow Parties operator scenario, several component names/specs drift across the two spines, and load-bearing route/flyout decisions remain open.

No critical defects were found. The high-severity issues should be resolved, or the declared source scope narrowed, before the pair is promoted beyond draft.

## 1. Flow coverage - thin
Checked PRD user journeys UJ-1 through UJ-6 from `_bmad-output/planning-artifacts/prd.md` against `EXPERIENCE.md` Key Flows.

### Findings
- **high** Source user journeys are not covered verbatim. The PRD names six journeys: Nina bootstraps a domain shell, Marc investigates a projection, Marc executes a command, Ravi exposes MCP, Camille preserves compatibility, and Sophie tests generated UX (`_bmad-output/planning-artifacts/prd.md:50`). `EXPERIENCE.md` has only two Parties flows for Alex and Maya (`_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:132`, `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md:147`). *Fix:* Either narrow `sources:` to the common application UX sources this spine actually covers, or add/rename key flows that trace UJ-1 through UJ-6 with the source journey names, protagonists, climax beats, and failure paths.

## 2. Token completeness - adequate
Checked DESIGN frontmatter tokens and prose references. Token references resolve internally, and Fluent UI inheritance makes semantic token references acceptable for this project.

### Findings
- **medium** Status and contrast tokens are not explicit enough for downstream mirroring. The design declares success, warning, and error tokens (`DESIGN.md:28`) and says status uses Fluent semantic colors (`DESIGN.md:88`), but the visual-refresh source requires success, error, unknown/neutral, warning, and info mappings plus contrast verification (`_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md:211`). *Fix:* Add the missing neutral/info status token semantics or explicitly state the Fluent semantic color source for those states, and record the contrast target/verification rule for the load-bearing status combinations.

## 3. Component coverage - thin
Checked component names in DESIGN frontmatter/body and EXPERIENCE Component Patterns.

### Findings
- **high** Component naming and coverage are not aligned across the spines. DESIGN defines kebab-case entries such as `shell-chrome`, `navigation-module-entry`, `module-tabs`, `page-toolbar`, `projection-grid`, `command-form`, and `status-icon` (`DESIGN.md:51`), while EXPERIENCE uses labels such as `Module workspace/dashboard`, `Add action`, `Modify action`, `Command lifecycle`, `Status affordance`, and `Multi-section page body` (`EXPERIENCE.md:62`). Several EXPERIENCE components have no matching visual row in DESIGN, and several DESIGN components have no identically named behavioral row. *Fix:* Create one canonical component table or glossary shared by both files, with stable component IDs and one visual row plus one behavioral row for each load-bearing component.

## 4. State coverage - adequate
Checked each IA surface against state patterns and command lifecycle requirements.

### Findings
- **medium** Command lifecycle states are named in Component Patterns but not fully covered in State Patterns. EXPERIENCE distinguishes `Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`, `IdempotentConfirmed`, `NeedsReview`, and `Degraded` (`EXPERIENCE.md:69`), but the state table only defines accepted, confirmed, rejected, destructive, stale, and reconnecting treatments (`EXPERIENCE.md:73`). *Fix:* Add rows for `Submitting`, `Syncing`, `IdempotentConfirmed`, `NeedsReview`, and `Degraded`, including retry/polling behavior, live-region behavior, and failure recovery.

## 5. Visual reference coverage - thin
Checked `mockups/`, `wireframes/`, and `imports/` under the UX workspace. No visual artifacts exist yet.

### Findings
- **low** The draft has no visual references for the shell, module workspace, toolbar, grid, command lifecycle, or navigation rail. This matches the memlog fast-path assumption, but `EXPERIENCE.md` still leaves the first reference implementation open (`EXPERIENCE.md:170`). *Fix:* Keep the draft status until at least the first reference implementation or key-screen mock is linked, or explicitly mark these surfaces as spine-only with the rationale.

## 6. Bloat & overspecification - strong
The spines are compact and mostly decision-oriented. They avoid restating the full PRD and keep brand/visual decisions in DESIGN while behavior remains in EXPERIENCE.

### Findings
No findings.

## 7. Inheritance discipline - thin
Checked source resolution, duplicated sources, component naming consistency, and DESIGN token references from EXPERIENCE.

### Findings
- **medium** Source inheritance is ambiguous. Both spines point to both the canonical PRD and the BMad run copy (`DESIGN.md:7`, `DESIGN.md:11`, `EXPERIENCE.md:6`, `EXPERIENCE.md:10`), while the PRD itself says those are the canonical and run copies of the same requirements (`_bmad-output/planning-artifacts/prd.md:12`). The DESIGN and EXPERIENCE source lists also differ, so consumers cannot tell whether a source is visual-only, behavior-only, or accidentally omitted. *Fix:* Use one PRD source of record, align the source lists where both spines inherit the same upstream decision, and add a short scope note for any visual-only or behavior-only source.

## 8. Shape fit - thin
DESIGN sections are in canonical order, and EXPERIENCE includes the required default sections plus Responsive/Platform and Inspiration/Anti-patterns. The shape is valid, but the contract is not closed.

### Findings
- **high** Load-bearing decisions remain open inside the spine. The open questions cover default tab naming, tab route encoding, whether projection flyouts remain, and which module becomes the first visual reference (`EXPERIENCE.md:168`). These affect routing, palette/CTA destinations, navigation IA, tests, and implementation handoff. *Fix:* Resolve the route/tab/flyout decisions or move them into a clearly marked non-blocking backlog with the current default decision in the spine.

## Mechanical notes
- All `sources:` paths found in DESIGN and EXPERIENCE resolve.
- No `mockups/`, `wireframes/`, or `imports/` directories/files are present in the UX workspace.
- DESIGN body section order is canonical.
- EXPERIENCE includes all required default sections.
- Reviewer execution was local rubric-only; no extra reviewer lenses were run.
