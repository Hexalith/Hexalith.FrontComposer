# Structure Review — OI-16 UX Authority Chain

This three-file chain exists to help human FrontComposer framework implementers, testers, and Product reviewers find and apply a testable UX rule or evidence status without reading it linearly, while the experience supplement carries the six journeys in deliberate sequence.

## Chosen structure model

**Reference/Database** is the primary model: stable IDs, consistent matrix schemas, and random access
are more important than narrative progression. `ux-experience-2026-07-05.md` uses the
**Tutorial/Guide (Linear)** model only for its ordered UJ-1–UJ-6 Key Flows. Content and requirements
were treated as sacrosanct; this pass reviews placement, hierarchy, scanability, and justified
repetition only.

## Exact word metrics

The required `word_metrics.py` run reports **13,277 words** across the chain.

| File | Exact words | Sections material to this review |
| --- | ---: | --- |
| `ux-design.md` | 7,334 | UX-AM-1 1,378; UX-SS-1 2,534; Requirement And Evidence Ledger 445; UX-OI16-1 204; Governance Rules 97; Story Design Notes 52 |
| `ux-design-detailed-2026-07-05.md` | 2,395 | preamble 349; UX-VC-1 380; UX-TS-1 140; UX-RF-1 346; Components 370; UX-RM-1 134 |
| `ux-experience-2026-07-05.md` | 3,548 | State Patterns 459; Announcement Behavior 154; Validation And Rejection Behavior 138; Focus Behavior 214; Accessibility Floor 129; Responsive & Platform 188; Key Flows plus UJ-1–UJ-6 737; Brownfield Reconciliation 101 |

## Findings

| Pass | Original Text | Revised Text | Changes |
| --- | --- | --- | --- |
| structure | `ux-design.md` — Requirement And Evidence Ledger (445 words) and UX-OI16-1 (204 words) appear after all six acceptance-matrix sections | **MOVE** both status/reference sections directly after UX-DR8 and before UX-AM-1 | Moves 649 words (8.8% of the canonical file), with 0-word reduction. Product reviewers see delivered/open ownership before entering 5,493 words of detailed matrices; stable IDs and content remain unchanged. |
| structure | `ux-design-detailed-2026-07-05.md` — frontmatter and introductory prose are followed directly by `## Brand & Style`, with no visible H1 | **QUESTION** → add `# Hexalith Common Application UX — Visual Supplement` before the introduction | Adds about 6 words to the 2,395-word file (0.25%). This supplies a human-visible document identity and restores an H1→H2 outline without changing frontmatter or requirements. |
| structure | `ux-design-detailed-2026-07-05.md` — UX-RM-1 (134 words) is an H3 beneath the 370-word Components section | **MOVE** / promote UX-RM-1 to an H2 peer, retaining its present position between Components and Do's and Don'ts | Reclassifies 134 words with 0-word reduction. Reduced motion becomes a first-class visual acceptance topic rather than appearing to be a child of the component registry. |
| structure | `ux-experience-2026-07-05.md` — Announcement Behavior (154 words) and Validation And Rejection Behavior (138 words) are H3 children of State Patterns | **MOVE** / promote both to H2 peers of State Patterns | Reclassifies 292 words (8.2% of the experience supplement), with 0-word reduction. The outline becomes MECE: states, announcements, and validation are independent random-access behavior families. |
| structure | `ux-experience-2026-07-05.md` — the 101-word Brownfield Reconciliation follows the 737-word linear Key Flows sequence | **MOVE** Brownfield Reconciliation immediately before Key Flows | Moves 101 words with 0-word reduction. Reference/history remains available, while the intentionally linear UJ-1–UJ-6 sequence becomes the document's closing progression. |
| structure | `ux-design.md` — UX-AM-1 (1,378 words) and UX-SS-1 (2,534 words) occupy 3,912 words | **PRESERVE** both complete matrices and their row-level schemas | Retains 3,912 words (53.3% of the canonical file). Their size is functional database structure: cutting or merging rows would impair stable-ID lookup and independent test extraction. |
| structure | `ux-design-detailed-2026-07-05.md` — UX-VC-1, UX-TS-1, UX-RF-1, and UX-RM-1 total 1,000 words | **PRESERVE** the four visual acceptance matrices as separate lookup tables | Retains exactly 1,000 words (41.8% of the visual supplement). Contrast, text spacing, reflow/geometry, and motion use different test schemas; merging them would reduce scanability rather than duplication. |
| structure | The visual Components section (370 words) and the experience Component/State/Announcement/Validation/Focus joins (1,389 words) repeat canonical identifiers and matrix references | **PRESERVE** the matched cross-file joins | Retains 1,759 words (13.2% of the whole chain). This is deliberate reinforcement: identical identifiers connect distinct visual, behavioral, state, and evidence views without competing authority. |
| structure | `ux-experience-2026-07-05.md` — Key Flows and UJ-1–UJ-6 total 737 words | **PRESERVE** the six named journeys as the one linear region | Retains 737 words (20.8% of the experience supplement). The numbered protagonist/climax/failure sequence provides human comprehension that a database-only index cannot replace. |

## Closing summary

- Recommendations: **9 total** — 4 MOVE, 1 QUESTION, 4 PRESERVE; no justified CUT, MERGE, or
  CONDENSE finding.
- Actionable structural changes: **5**. Explicit preserve decisions: **4**.
- Estimated reduction if all recommendations are accepted: **0 words (0.0%)**. The visible H1 adds
  about 6 words, for a net increase of about **6 words (0.05%)** across the 13,277-word chain.
- No length target was supplied. The documents are long because their stable acceptance matrices are
  the product; the useful improvement is hierarchy and entry-point placement, not compression.
- Tradeoff: moving status material forward slightly interrupts the canonical contract sequence, but
  improves Product-review entry; moving Brownfield Reconciliation forward gives up a historical coda
  so the six human journeys can close the experience supplement. The preserved matrices and
  cross-file joins avoid any loss of testability or comprehension.

This structure review makes no gate, approval, delivery, or requirement-validity claim.
